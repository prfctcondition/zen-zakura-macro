#pragma once
#include <windows.h>
#include <stdint.h>

#define ZENZAKURA_API __declspec(dllexport)

#ifdef __cplusplus
extern "C" {
#endif

typedef struct {
    uint32_t vk;
    BOOL     down;
    double   delayMs;
} ZenKeyEvent;

typedef enum {
    ZEN_PLAY_ONCE = 0,
    ZEN_PLAY_REPEAT_WHILE_HELD = 1,
    ZEN_PLAY_TOGGLE_REPEAT = 2
} ZenPlayMode;

typedef void (__stdcall *ZenKeyEventCallback)(ZenKeyEvent evt);
typedef void (__stdcall *ZenPlaybackStatusCallback)(BOOL completed, DWORD errorCode);

ZENZAKURA_API void Zen_Initialize();
ZENZAKURA_API void Zen_Shutdown();

ZENZAKURA_API void Zen_StartRecording();
ZENZAKURA_API void Zen_StopRecording();
ZENZAKURA_API void Zen_ClearRecording();

ZENZAKURA_API uint32_t Zen_CaptureSingleKey();

ZENZAKURA_API void Zen_StopPlayback();

ZENZAKURA_API BOOL Zen_RegisterBinding(uint32_t vk, ZenKeyEvent* events, int count, ZenPlayMode mode);
ZENZAKURA_API void Zen_UnregisterBinding(uint32_t vk);
ZENZAKURA_API void Zen_ClearBindings();

ZENZAKURA_API void Zen_SetKeyEventCallback(ZenKeyEventCallback cb);
ZENZAKURA_API void Zen_SetPlaybackStatusCallback(ZenPlaybackStatusCallback cb);

ZENZAKURA_API void Zen_SetPauseToggleKey(uint32_t vk);
ZENZAKURA_API uint32_t Zen_GetPauseToggleKey();
ZENZAKURA_API BOOL Zen_GetPaused();
ZENZAKURA_API BOOL Zen_GetAutoPaused();
ZENZAKURA_API void Zen_SetPaused(BOOL paused);

ZENZAKURA_API void Zen_SetProcessFilter(const char* name, BOOL enabled);
ZENZAKURA_API BOOL Zen_GetProcessFilterEnabled();
ZENZAKURA_API void Zen_GetProcessFilter(char* buffer, int size);

#ifdef __cplusplus
}
#endif
