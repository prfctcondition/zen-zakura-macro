#pragma once
#include <atomic>
#include <windows.h>

class HighResTimer {
public:
    HighResTimer();
    LARGE_INTEGER Now() const;
    double ElapsedMs(LARGE_INTEGER start) const;
    double DeltaToMs(LARGE_INTEGER start, LARGE_INTEGER end) const;
    LONGLONG MsToTicks(double ms) const;
    void SpinWait(double ms, const std::atomic<bool>* abortFlag = nullptr) const;
    void PreciseWait(double ms, const std::atomic<bool>* abortFlag = nullptr) const;
    LARGE_INTEGER Frequency() const { return m_freq; }
    static void SetSystemTimerResolution();
    static void ResetSystemTimerResolution();
private:
    LARGE_INTEGER m_freq;
};
