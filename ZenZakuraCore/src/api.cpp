#include "../include/ZenZakuraCore.h"
#include "HookEngine.h"
#include "PlaybackEngine.h"
#include "BindingEngine.h"
#include "Timing.h"

// Compare process names ignoring trailing .exe
static bool NameMatches(const char* a, const char* b) {
    if (!a || !b) return false;
    // Copy and strip .exe from both sides
    char bufA[MAX_PATH], bufB[MAX_PATH];
    strcpy_s(bufA, a);
    strcpy_s(bufB, b);
    auto trimExe = [](char* s) {
        size_t len = strlen(s);
        if (len > 4 && _stricmp(s + len - 4, ".exe") == 0)
            s[len - 4] = '\0';
    };
    trimExe(bufA);
    trimExe(bufB);
    return _stricmp(bufA, bufB) == 0;
}

HookEngine* g_hookEngine = nullptr;
PlaybackEngine* g_playbackEngine = nullptr;
BindingEngine* g_bindingEngine = nullptr;

volatile bool g_hookPaused = false;
volatile bool g_autoPaused = false;
volatile uint32_t g_pauseToggleVK = 0;
char g_processFilter[MAX_PATH] = "";
volatile bool g_processFilterEnabled = false;
static DWORD s_lastFilterPid = 0;

bool IsForegroundProcess(const char* targetName) {
    if (!targetName || !*targetName) return true;
    HWND hwnd = GetForegroundWindow();
    if (!hwnd) return false;
    DWORD pid = 0;
    GetWindowThreadProcessId(hwnd, &pid);
    if (pid == s_lastFilterPid)
        return !g_autoPaused;
    s_lastFilterPid = pid;
    HANDLE hProc = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, FALSE, pid);
    if (!hProc) return false;
    char exePath[MAX_PATH] = {};
    DWORD size = sizeof(exePath);
    bool match = false;
    if (QueryFullProcessImageNameA(hProc, 0, exePath, &size)) {
        // Extract base name from path
        const char* base = strrchr(exePath, '\\');
        base = base ? base + 1 : exePath;
        match = NameMatches(base, targetName);
    }
    CloseHandle(hProc);
    return match;
}

void CheckAutoPause() {
    if (g_processFilterEnabled && strlen(g_processFilter) > 0) {
        if (IsForegroundProcess(g_processFilter)) {
            g_autoPaused = false;
        } else {
            if (!g_autoPaused) {
                g_autoPaused = true;
                if (g_playbackEngine) g_playbackEngine->Stop();
            }
        }
    } else {
        g_autoPaused = false;
    }
}

void Zen_Initialize() {
    HighResTimer::SetSystemTimerResolution();
    g_hookEngine = new HookEngine();
    g_playbackEngine = new PlaybackEngine();
    g_bindingEngine = new BindingEngine();
    g_hookEngine->Initialize();
}

void Zen_Shutdown() {
    if (g_playbackEngine) g_playbackEngine->Stop();
    if (g_hookEngine) g_hookEngine->Shutdown();
    delete g_hookEngine; g_hookEngine = nullptr;
    delete g_playbackEngine; g_playbackEngine = nullptr;
    delete g_bindingEngine; g_bindingEngine = nullptr;
    HighResTimer::ResetSystemTimerResolution();
}

void Zen_StartRecording() {
    if (g_hookEngine) g_hookEngine->StartRecording();
}

void Zen_StopRecording() {
    if (g_hookEngine) g_hookEngine->StopRecording();
}

void Zen_ClearRecording() {
    if (g_hookEngine) g_hookEngine->ClearEvents();
}

uint32_t Zen_CaptureSingleKey() {
    return g_hookEngine ? g_hookEngine->CaptureSingleKey() : 0;
}

void Zen_StopPlayback() {
    if (g_playbackEngine) g_playbackEngine->Stop();
}

BOOL Zen_RegisterBinding(uint32_t vk, ZenKeyEvent* events, int count, ZenPlayMode mode) {
    if (g_bindingEngine) {
        g_bindingEngine->Register(vk, events, count, mode);
        return TRUE;
    }
    return FALSE;
}

void Zen_UnregisterBinding(uint32_t vk) {
    if (g_bindingEngine) g_bindingEngine->Unregister(vk);
}

void Zen_ClearBindings() {
    if (g_bindingEngine) g_bindingEngine->Clear();
}

void Zen_SetKeyEventCallback(ZenKeyEventCallback cb) {
    if (g_hookEngine) g_hookEngine->SetKeyEventCallback(cb);
}

void Zen_SetPlaybackStatusCallback(ZenPlaybackStatusCallback cb) {
    if (g_playbackEngine) g_playbackEngine->SetPlaybackStatusCallback(cb);
}

void Zen_SetPauseToggleKey(uint32_t vk) {
    g_pauseToggleVK = vk;
}

uint32_t Zen_GetPauseToggleKey() {
    return g_pauseToggleVK;
}

BOOL Zen_GetPaused() {
    return (g_hookPaused || g_autoPaused) ? TRUE : FALSE;
}

BOOL Zen_GetAutoPaused() {
    return g_autoPaused ? TRUE : FALSE;
}

void Zen_SetPaused(BOOL paused) {
    g_hookPaused = paused != FALSE;
    if (g_hookPaused && g_playbackEngine) g_playbackEngine->Stop();
}

void Zen_SetProcessFilter(const char* name, BOOL enabled) {
    if (name) {
        strcpy_s(g_processFilter, sizeof(g_processFilter), name);
    } else {
        g_processFilter[0] = '\0';
    }
    g_processFilterEnabled = enabled != FALSE;
    s_lastFilterPid = 0;
    if (!g_processFilterEnabled) g_autoPaused = false;
}

BOOL Zen_GetProcessFilterEnabled() {
    return g_processFilterEnabled ? TRUE : FALSE;
}

void Zen_GetProcessFilter(char* buffer, int size) {
    strcpy_s(buffer, size, g_processFilter);
}
