#pragma once
#include <windows.h>
#include <vector>
#include "../include/ZenZakuraCore.h"
#include "Timing.h"

class PlaybackEngine {
public:
    PlaybackEngine();
    ~PlaybackEngine();

    void Play(const ZenKeyEvent* events, int count, ZenPlayMode mode, int repeatDelayMs, uint32_t bindVK);
    void Stop();
    bool IsPlaying() const;

    void SetPlaybackStatusCallback(ZenPlaybackStatusCallback cb) { InterlockedExchangePointer((void**)&m_statusCb, cb); }

private:
    static DWORD WINAPI PlaybackThread(LPVOID param);

    struct PlaybackData {
        std::vector<ZenKeyEvent> events;
        ZenPlayMode mode;
        int repeatDelayMs;
        uint32_t bindVK;
    };

    void ExecuteOnce(const std::vector<ZenKeyEvent>& events);
    void ExecuteRepeatWhileHeld(const std::vector<ZenKeyEvent>& events, int repeatDelayMs, uint32_t bindVK);
    void ExecuteToggleRepeat(const std::vector<ZenKeyEvent>& events, int repeatDelayMs);
    void SendKey(uint32_t vk, BOOL down);

    HANDLE m_hThread;
    volatile bool m_abort;
    volatile bool m_playing;
    uint32_t m_playbackVK;

    HighResTimer m_timer;
    ZenPlaybackStatusCallback m_statusCb;

    static PlaybackEngine* s_instance;
};
