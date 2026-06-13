using System.Runtime.InteropServices;
using ZenZakuraUI.Models;

namespace ZenZakuraUI.Services;

[StructLayout(LayoutKind.Sequential)]
internal struct NativeKeyEvent
{
    public uint vk;
    [MarshalAs(UnmanagedType.Bool)]
    public bool down;
    public double delayMs;
}

internal static class CoreInterop
{
    private const string DllName = "ZenZakuraCore.dll";

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void Zen_Initialize();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void Zen_Shutdown();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void Zen_StartRecording();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void Zen_StopRecording();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void Zen_ClearRecording();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint Zen_CaptureSingleKey();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool Zen_RegisterBinding(uint vk, [In] NativeKeyEvent[] events, int count, int mode);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void Zen_UnregisterBinding(uint vk);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void Zen_ClearBindings();

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void KeyEventCallbackNative(NativeKeyEvent evt);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void Zen_SetKeyEventCallback(KeyEventCallbackNative? cb);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void Zen_SetPauseToggleKey(uint vk);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint Zen_GetPauseToggleKey();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool Zen_GetPaused();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void Zen_SetPaused([MarshalAs(UnmanagedType.Bool)] bool paused);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void Zen_SetProcessFilter(string name, [MarshalAs(UnmanagedType.Bool)] bool enabled);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool Zen_GetProcessFilterEnabled();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void Zen_GetProcessFilter(System.Text.StringBuilder buffer, int size);
}
