using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZenZakuraUI.Services;

namespace ZenZakuraUI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _pauseKeyDisplay = "";

    [ObservableProperty]
    private uint _pauseKey;

    [ObservableProperty]
    private bool _isPaused;

    [ObservableProperty]
    private bool _processFilterEnabled;

    [ObservableProperty]
    private string _processName = "";

    [ObservableProperty]
    private ObservableCollection<string> _runningProcesses = [];

    public event Action? Applied;

    public SettingsViewModel()
    {
        RefreshProcesses();
    }

    public void Load()
    {
        PauseKey = CoreInterop.Zen_GetPauseToggleKey();
        UpdateDisplay(PauseKey);
        IsPaused = CoreInterop.Zen_GetPaused();

        ProcessFilterEnabled = CoreInterop.Zen_GetProcessFilterEnabled();
        var sb = new System.Text.StringBuilder(260);
        CoreInterop.Zen_GetProcessFilter(sb, sb.Capacity);
        ProcessName = sb.ToString();
    }

    [RelayCommand]
    private void BindPauseKey()
    {
        var vk = CoreInterop.Zen_CaptureSingleKey();
        if (vk == 0) return;
        PauseKey = vk;
        UpdateDisplay(vk);
    }

    [RelayCommand]
    private void ClearPauseKey()
    {
        PauseKey = 0;
        UpdateDisplay(0);
    }

    [RelayCommand]
    private void Apply()
    {
        CoreInterop.Zen_SetPauseToggleKey(PauseKey);
        CoreInterop.Zen_SetProcessFilter(ProcessName, ProcessFilterEnabled);
        Applied?.Invoke();
    }

    [RelayCommand]
    private void RefreshProcesses()
    {
        var current = ProcessName;
        RunningProcesses.Clear();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var p in Process.GetProcesses())
            {
                try
                {
                    if (p.SessionId != 0 && names.Add(p.ProcessName))
                        RunningProcesses.Add(p.ProcessName);
                }
                catch { }
            }
        }
        catch { }

        var sorted = RunningProcesses.OrderBy(n => n).ToList();
        RunningProcesses.Clear();
        foreach (var n in sorted)
            RunningProcesses.Add(n);

        if (!string.IsNullOrEmpty(current) && RunningProcesses.Contains(current))
            ProcessName = current;
    }

    private void UpdateDisplay(uint vk)
    {
        PauseKeyDisplay = vk != 0 ? VkToString(vk) : "none";
    }

    private static string VkToString(uint vk)
    {
        if (vk >= 0x41 && vk <= 0x5A) return ((char)vk).ToString();
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
}
