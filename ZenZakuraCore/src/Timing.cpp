#include "Timing.h"
#include <mmdeviceapi.h>
#include <avrt.h>
#pragma comment(lib, "winmm.lib")

HighResTimer::HighResTimer() {
    QueryPerformanceFrequency(&m_freq);
}

LARGE_INTEGER HighResTimer::Now() const {
    LARGE_INTEGER now;
    QueryPerformanceCounter(&now);
    return now;
}

double HighResTimer::ElapsedMs(LARGE_INTEGER start) const {
    LARGE_INTEGER now = Now();
    return DeltaToMs(start, now);
}

double HighResTimer::DeltaToMs(LARGE_INTEGER start, LARGE_INTEGER end) const {
    return (double)(end.QuadPart - start.QuadPart) * 1000.0 / (double)m_freq.QuadPart;
}

LONGLONG HighResTimer::MsToTicks(double ms) const {
    return (LONGLONG)((ms / 1000.0) * m_freq.QuadPart);
}

void HighResTimer::SpinWait(double ms, volatile const bool* abortFlag) const {
    if (ms <= 0.0) return;
    LARGE_INTEGER target;
    target.QuadPart = Now().QuadPart + MsToTicks(ms);
    LARGE_INTEGER now;
    do {
        if (abortFlag && *abortFlag) return;
        _mm_pause();
        QueryPerformanceCounter(&now);
    } while (now.QuadPart < target.QuadPart);
}

void HighResTimer::PreciseWait(double ms, volatile const bool* abortFlag) const {
    if (ms <= 0.0) return;
    if (ms < 2.0) {
        SpinWait(ms, abortFlag);
        return;
    }
    LARGE_INTEGER start = Now();
    double elapsed = 0.0;
    while (elapsed < ms - 1.0) {
        if (abortFlag && *abortFlag) return;
        LARGE_INTEGER now;
        QueryPerformanceCounter(&now);
        elapsed = DeltaToMs(start, now);
        double remaining = ms - elapsed;
        if (remaining > 2.0) {
            Sleep(1);
        }
    }
    SpinWait(ms - elapsed, abortFlag);
}

void HighResTimer::SetSystemTimerResolution() {
    HMODULE ntdll = GetModuleHandleW(L"ntdll.dll");
    if (ntdll) {
        typedef NTSTATUS(NTAPI* NtSetTimerResolution_t)(ULONG, BOOLEAN, PULONG);
        auto NtSetTimerResolution = (NtSetTimerResolution_t)
            GetProcAddress(ntdll, "NtSetTimerResolution");
        if (NtSetTimerResolution) {
            ULONG current;
            NtSetTimerResolution(5000, TRUE, &current);
        }
    }
    timeBeginPeriod(1);
}

void HighResTimer::ResetSystemTimerResolution() {
    timeEndPeriod(1);
}
