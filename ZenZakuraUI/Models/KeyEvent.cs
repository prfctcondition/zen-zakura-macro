using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ZenZakuraUI.Models;

public partial class KeyEvent : ObservableObject
{
    public static event Action? AnyDelayChanged;

    [ObservableProperty]
    private uint _vk;

    [ObservableProperty]
    private bool _down;

    [ObservableProperty]
    private double _delayMs;

    partial void OnDelayMsChanged(double value) => AnyDelayChanged?.Invoke();

    public string KeyDisplay
    {
        get
        {
            if (Vk >= 0x41 && Vk <= 0x5A)
                return ((char)Vk).ToString();
            var name = (Keys)Vk;
            return name.ToString();
        }
    }

}

public enum Keys
{
    Escape = 0x1B,
    Tab = 0x09,
    CapsLock = 0x14,
    Shift = 0x10,
    Control = 0x11,
    Alt = 0x12,
    Space = 0x20,
    Return = 0x0D,
    Back = 0x08,
    Left = 0x25,
    Up = 0x26,
    Right = 0x27,
    Down = 0x28,
    F1 = 0x70, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
}
