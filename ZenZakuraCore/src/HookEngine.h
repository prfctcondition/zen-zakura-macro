#pragma once
#include <windows.h>
#include <vector>
#include "../include/ZenZakuraCore.h"
#include "Timing.h"

enum class HookMode {
    Idle,
    Recording,
    Capture
};

class HookEngine {
public:
    HookEngine();
    ~HookEngine();

    void Initialize();
    void Shutdown();

    bool IsRunning() const { return m_running; }

    void StartRecording();
    void StopRecording();
    void ClearEvents();

    uint32_t CaptureSingleKey();

    void SetKeyEventCallback(ZenKeyEventCallback cb) { InterlockedExchangePointer((void**)&m_keyEventCb, cb); }

private:
    static LRESULT CALLBACK HookProc(int code, WPARAM wParam, LPARAM lParam);
    static DWORD WINAPI HookThread(LPVOID param);

    void SetMode(HookMode mode);
    void HandleKeyEvent(uint32_t vk, bool down, WPARAM wParam);

    HHOOK m_hHook;
    HANDLE m_hThread;
    DWORD m_threadId;
    volatile bool m_running;
    volatile HookMode m_mode;

    std::vector<ZenKeyEvent> m_events;
    CRITICAL_SECTION m_cs;

    HighResTimer m_timer;
    LARGE_INTEGER m_recordingStart;

    HANDLE m_captureEvent;
    volatile uint32_t m_capturedVK;

    ZenKeyEventCallback m_keyEventCb;

    // Track per-key down state to suppress auto-repeat in Idle mode
    bool m_keyWasDown[256]{};

    static HookEngine* s_instance;
};
