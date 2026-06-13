#include "PlaybackEngine.h"
#include <intrin.h>
#pragma comment(lib, "user32.lib")

PlaybackEngine* PlaybackEngine::s_instance = nullptr;

PlaybackEngine::PlaybackEngine()
    : m_hThread(nullptr)
    , m_abort(false)
    , m_playing(false)
    , m_playbackVK(0)
    , m_statusCb(nullptr)
{
    s_instance = this;
}

PlaybackEngine::~PlaybackEngine() {
    Stop();
    s_instance = nullptr;
}

void PlaybackEngine::Play(const ZenKeyEvent* events, int count, ZenPlayMode mode, int repeatDelayMs, uint32_t bindVK) {
    Stop();
    m_abort = false;
    m_playing = true;
    m_playbackVK = bindVK;

    SetPriorityClass(GetCurrentProcess(), HIGH_PRIORITY_CLASS);

    PlaybackData* data = new PlaybackData();
    data->events.assign(events, events + count);
    data->mode = mode;
    data->repeatDelayMs = repeatDelayMs;
    data->bindVK = bindVK;

    m_hThread = CreateThread(nullptr, 0, PlaybackThread, data, 0, nullptr);
    if (m_hThread) {
        SetThreadPriority(m_hThread, THREAD_PRIORITY_TIME_CRITICAL);
    }
}

void PlaybackEngine::Stop() {
    m_abort = true;
    if (m_hThread) {
        WaitForSingleObject(m_hThread, 2000);
        CloseHandle(m_hThread);
        m_hThread = nullptr;
    }
    m_playing = false;
}

bool PlaybackEngine::IsPlaying() const {
    return m_playing;
}

void PlaybackEngine::SendKey(uint32_t vk, BOOL down) {
    INPUT inp = {};
    inp.type = INPUT_KEYBOARD;

    WORD scan = (WORD)MapVirtualKeyW((UINT)vk, MAPVK_VK_TO_VSC);
    inp.ki.wScan = scan;
    inp.ki.wVk = 0;

    DWORD flags = KEYEVENTF_SCANCODE;
    if (!down) flags |= KEYEVENTF_KEYUP;

    // Extended keys need KEYEVENTF_EXTENDEDKEY
    switch (vk) {
        case VK_LEFT: case VK_UP: case VK_RIGHT: case VK_DOWN:
        case VK_HOME: case VK_END: case VK_PRIOR: case VK_NEXT:
        case VK_INSERT: case VK_DELETE:
        case VK_LWIN: case VK_RWIN:
        case VK_APPS:
        case VK_DIVIDE: case VK_NUMLOCK:
        case VK_RETURN:
        case VK_SNAPSHOT:
        case VK_CANCEL:
            flags |= KEYEVENTF_EXTENDEDKEY;
            break;
    }

    inp.ki.dwFlags = flags;
    inp.ki.dwExtraInfo = 0x5A656E5A;

    SendInput(1, &inp, sizeof(INPUT));
}

DWORD WINAPI PlaybackEngine::PlaybackThread(LPVOID param) {
    PlaybackData* data = (PlaybackData*)param;

    if (data->mode == ZEN_PLAY_ONCE) {
        s_instance->ExecuteOnce(data->events);
    }
    else if (data->mode == ZEN_PLAY_REPEAT_WHILE_HELD) {
        s_instance->ExecuteRepeatWhileHeld(data->events, data->repeatDelayMs, data->bindVK);
    }
    else if (data->mode == ZEN_PLAY_TOGGLE_REPEAT) {
        s_instance->ExecuteToggleRepeat(data->events, data->repeatDelayMs);
    }

    s_instance->m_playing = false;
    ZenPlaybackStatusCallback cb = s_instance->m_statusCb;
    if (cb) cb(!s_instance->m_abort, 0);

    delete data;
    return 0;
}

void PlaybackEngine::ExecuteOnce(const std::vector<ZenKeyEvent>& events) {
    for (size_t i = 0; i < events.size(); i++) {
        if (m_abort) return;
        m_timer.PreciseWait(events[i].delayMs, &m_abort);
        if (m_abort) return;
        SendKey(events[i].vk, events[i].down);
    }
}

void PlaybackEngine::ExecuteRepeatWhileHeld(const std::vector<ZenKeyEvent>& events, int repeatDelayMs, uint32_t bindVK) {
    (void)bindVK;
    while (!m_abort) {
        for (size_t i = 0; i < events.size(); i++) {
            if (m_abort) return;
            m_timer.PreciseWait(events[i].delayMs, &m_abort);
            if (m_abort) return;
            SendKey(events[i].vk, events[i].down);
        }
        if (repeatDelayMs > 0) {
            m_timer.PreciseWait((double)repeatDelayMs, &m_abort);
        }
    }
}

void PlaybackEngine::ExecuteToggleRepeat(const std::vector<ZenKeyEvent>& events, int repeatDelayMs) {
    for (int cycle = 0; !m_abort; cycle++) {
        for (size_t i = 0; i < events.size(); i++) {
            if (m_abort) return;
            m_timer.PreciseWait(events[i].delayMs, &m_abort);
            if (m_abort) return;
            SendKey(events[i].vk, events[i].down);
        }
        if (repeatDelayMs > 0) {
            m_timer.PreciseWait((double)repeatDelayMs, &m_abort);
        }
    }
}
