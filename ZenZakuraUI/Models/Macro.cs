using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ZenZakuraUI.Models;

public partial class Macro : ObservableObject
{
    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BindKeyDisplay))]
    private uint? _bindKey;

    [ObservableProperty]
    private bool _isHotkeyEnabled;

    [ObservableProperty]
    private PlaybackMode _playMode = PlaybackMode.Once;

    [ObservableProperty]
    private ObservableCollection<KeyEvent> _events = [];

    [JsonIgnore]
    public string FilePath { get; set; } = "";

    [JsonIgnore]
    public string BindKeyDisplay
    {
        get
        {
            if (!BindKey.HasValue) return "";
            return $"{VkToString(BindKey.Value)} (0x{BindKey.Value:X2})";
        }
    }

    public void RaiseEventsChanged() => OnPropertyChanged(nameof(Events));

    private static string VkToString(uint vk)
    {
        if (vk >= 0x41 && vk <= 0x5A) return ((char)vk).ToString();
        if (vk >= 0x30 && vk <= 0x39) return ((char)vk).ToString();
        return vk switch
        {
            0x1B => "Esc", 0x09 => "Tab", 0x14 => "Caps",
            0x10 => "Shift", 0x11 => "Ctrl", 0x12 => "Alt",
            0x20 => "Space", 0x0D => "Enter", 0x08 => "Back",
            0x25 => "Left", 0x26 => "Up", 0x27 => "Right", 0x28 => "Down",
            >= 0x70 and <= 0x7B => $"F{vk - 0x6F}",
            _ => $"0x{vk:X2}"
        };
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
    }

    public static Macro? FromJson(string json)
    {
        return JsonSerializer.Deserialize<Macro>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
}
