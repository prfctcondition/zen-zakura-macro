#include "HookEngine.h"
#include "Timing.h"
#include "BindingEngine.h"
#include "PlaybackEngine.h"

extern BindingEngine* g_bindingEngine;
extern PlaybackEngine* g_playbackEngine;
extern volatile bool g_hookPaused;
extern volatile bool g_autoPaused;
extern volatile uint32_t g_pauseToggleVK;
extern char g_processFilter[MAX_PATH];
extern volatile bool g_processFilterEnabled;
extern void CheckAutoPause();

// Don't execute bindings when Shift, Ctrl, or Alt is held
static bool IsModifierHeld() {
    return (GetAsyncKeyState(VK_SHIFT) & 0x8000) ||
           (GetAsyncKeyState(VK_CONTROL) & 0x8000) ||
           (GetAsyncKeyState(VK_MENU) & 0x8000);
}

HookEngine* HookEngine::s_instance = nullptr;

HookEngine::HookEngine()
    : m_hHook(nullptr)
    , m_hThread(nullptr)
    , m_threadId(0)
    , m_running(false)
    , m_mode(HookMode::Idle)
    , m_captureEvent(nullptr)
    , m_capturedVK(0)
    , m_keyEventCb(nullptr)
{
    InitializeCriticalSection(&m_cs);
    m_captureEvent = CreateEventW(nullptr, TRUE, FALSE, nullptr);
}

HookEngine::~HookEngine() {
    Shutdown();
    DeleteCriticalSection(&m_cs);
    if (m_captureEvent) CloseHandle(m_captureEvent);
}

void HookEngine::Initialize() {
    if (m_running) return;
    s_instance = this;
    m_running = true;
    m_hThread = CreateThread(nullptr, 0, HookThread, this, 0, &m_threadId);
    SetThreadPriority(m_hThread, THREAD_PRIORITY_TIME_CRITICAL);
}

void HookEngine::Shutdown() {
    m_running = false;
    m_mode = HookMode::Idle;
    if (m_hThread) {
        PostThreadMessage(m_threadId, WM_QUIT, 0, 0);
        WaitForSingleObject(m_hThread, 2000);
        CloseHandle(m_hThread);
        m_hThread = nullptr;
    }
    if (m_hHook) {
        UnhookWindowsHookEx(m_hHook);
        m_hHook = nullptr;
    }
    s_instance = nullptr;
}

DWORD WINAPI HookEngine::HookThread(LPVOID param) {
    HookEngine* self = (HookEngine*)param;
    self->m_hHook = SetWindowsHookExW(WH_KEYBOARD_LL, HookProc, GetModuleHandleW(nullptr), 0);
    MSG msg;
    while (GetMessageW(&msg, nullptr, 0, 0)) {
        TranslateMessage(&msg);
        DispatchMessageW(&msg);
    }
    return 0;
}

LRESULT CALLBACK HookEngine::HookProc(int code, WPARAM wParam, LPARAM lParam) {
    if (code >= 0 && s_instance) {
        KBDLLHOOKSTRUCT* kb = (KBDLLHOOKSTRUCT*)lParam;
        bool down = (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN);
        bool injected = (kb->flags & 0x10) != 0;

        // Skip our own injected events (e.g. Caps Lock undo toggle)
        if (!injected) {
            s_instance->HandleKeyEvent(kb->vkCode, down, wParam);

            bool isPauseKey = kb->vkCode < 256 && g_pauseToggleVK != 0 && kb->vkCode == g_pauseToggleVK;
            bool effectivelyPaused = g_hookPaused || g_autoPaused;

            // Block bound keys (only if no modifier held) and pause toggle key
            if (kb->vkCode < 256 && s_instance->m_mode == HookMode::Idle && !effectivelyPaused) {
                bool bound = g_bindingEngine && g_bindingEngine->IsBound(kb->vkCode);
                if (bound && !IsModifierHeld()) {
                    // Undo Caps Lock toggle — driver toggles it before the hook runs
                    if (kb->vkCode == VK_CAPITAL && down) {
                        INPUT toggle[2] = {};
                        toggle[0].type = INPUT_KEYBOARD;
                        toggle[0].ki.wVk = VK_CAPITAL;
                        toggle[1].type = INPUT_KEYBOARD;
                        toggle[1].ki.wVk = VK_CAPITAL;
                        toggle[1].ki.dwFlags = KEYEVENTF_KEYUP;
                        SendInput(2, toggle, sizeof(INPUT));
                    }
                    return 1;
                }
                if (isPauseKey)
                    return 1;
            }

            // When paused, still block the pause toggle key itself
            if (effectivelyPaused && isPauseKey)
                return 1;
        }
    }
    return CallNextHookEx(nullptr, code, wParam, lParam);
}

void HookEngine::HandleKeyEvent(uint32_t vk, bool down, WPARAM wParam) {
    if (m_mode == HookMode::Recording) {
        LARGE_INTEGER now = m_timer.Now();
        double delayMs = m_timer.DeltaToMs(m_recordingStart, now);
        m_recordingStart = now;

        ZenKeyEvent evt;
        evt.vk = vk;
        evt.down = down ? TRUE : FALSE;
        evt.delayMs = delayMs;

        EnterCriticalSection(&m_cs);
        m_events.push_back(evt);
        LeaveCriticalSection(&m_cs);

        ZenKeyEventCallback cb = m_keyEventCb;
        if (cb) cb(evt);
    }
    else if (m_mode == HookMode::Capture && down) {
        m_capturedVK = vk;
        SetEvent(m_captureEvent);
    }
    else if (m_mode == HookMode::Idle) {
        if (vk >= 256) return;

        if (down) {
            // Ignore auto-repeat — only process first WM_KEYDOWN
            if (m_keyWasDown[vk]) return;
            m_keyWasDown[vk] = true;

            // Check process filter and auto-pause/resume
            CheckAutoPause();

            // Global pause toggle — always works even when paused
            if (g_pauseToggleVK != 0 && vk == g_pauseToggleVK) {
                g_hookPaused = !g_hookPaused;
                if ((g_hookPaused || g_autoPaused) && g_playbackEngine) g_playbackEngine->Stop();
                return;
            }

            // When paused (manually or auto), skip all macro processing
            if (g_hookPaused || g_autoPaused) return;

            if (g_bindingEngine) {
                Binding* b = g_bindingEngine->Find(vk);
                if (b && g_playbackEngine) {
                    if (IsModifierHeld()) return;
                    if (b->mode == ZEN_PLAY_TOGGLE_REPEAT) {
                        if (g_playbackEngine->IsPlaying())
                            g_playbackEngine->Stop();
                        else
                            g_playbackEngine->Play(b->events.data(), (int)b->events.size(),
                                ZEN_PLAY_TOGGLE_REPEAT, b->repeatDelayMs, vk);
                    }
                    else {
                        g_playbackEngine->Play(b->events.data(), (int)b->events.size(),
                            b->mode, b->repeatDelayMs, vk);
                    }
                }
            }
        }
        else {
            m_keyWasDown[vk] = false;
            // Stop hold-to-repeat playback when key is released
            if (g_playbackEngine && g_bindingEngine) {
                Binding* b = g_bindingEngine->Find(vk);
                if (b && b->mode == ZEN_PLAY_REPEAT_WHILE_HELD)
                    g_playbackEngine->Stop();
            }
        }
    }
}

void HookEngine::SetMode(HookMode mode) {
    m_mode = mode;
}

void HookEngine::StartRecording() {
    ClearEvents();
    m_recordingStart = m_timer.Now();
    m_mode = HookMode::Recording;
}

void HookEngine::StopRecording() {
    m_mode = HookMode::Idle;
}

void HookEngine::ClearEvents() {
    EnterCriticalSection(&m_cs);
    m_events.clear();
    LeaveCriticalSection(&m_cs);
}

uint32_t HookEngine::CaptureSingleKey() {
    m_capturedVK = 0;
    ResetEvent(m_captureEvent);
    m_mode = HookMode::Capture;
    DWORD wait = WaitForSingleObject(m_captureEvent, 10000);
    m_mode = HookMode::Idle;
    if (wait == WAIT_OBJECT_0) {
        return m_capturedVK;
    }
    return 0;
}
