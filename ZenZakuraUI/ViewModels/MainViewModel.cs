using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using ZenZakuraUI.Models;
using ZenZakuraUI.Services;

namespace ZenZakuraUI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly MacroStorageService _storage;
    private readonly TrayService _trayService;
    private readonly Window _window;

    [ObservableProperty]
    private ObservableCollection<Macro> _macros = [];

    [ObservableProperty]
    private Macro? _selectedMacro;

    [ObservableProperty]
    private bool _isRecording;

    [ObservableProperty]
    private string _bindKeyDisplay = "";

    [ObservableProperty]
    private uint _bindKey;

    [ObservableProperty]
    private string _batchDelayText = "100";

    [ObservableProperty]
    private string _renameText = "";

    [ObservableProperty]
    private bool _isPaused;

    private CoreInterop.KeyEventCallbackNative? _keyCallback;
    private GCHandle _keyCallbackHandle;

    public Action? SelectedMacroChanged { get; set; }
    public Action? EventAdded { get; set; }

    public MainViewModel(MacroStorageService storage, TrayService trayService, Window window)
    {
        _storage = storage;
        _trayService = trayService;
        _window = window;
        KeyEvent.AnyDelayChanged += ReapplyBindingIfActive;

        // Poll pause state every 500ms for UI
        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        timer.Tick += (_, _) => IsPaused = CoreInterop.Zen_GetPaused();
        timer.Start();
    }

    public void HookKeyEvents()
    {
        _keyCallback = OnNativeKeyEvent;
        _keyCallbackHandle = GCHandle.Alloc(_keyCallback);
        CoreInterop.Zen_SetKeyEventCallback(_keyCallback);
    }

    public void UnhookKeyEvents()
    {
        CoreInterop.Zen_SetKeyEventCallback(null);
        if (_keyCallbackHandle.IsAllocated) _keyCallbackHandle.Free();
    }

    public void LoadSavedMacros()
    {
        var loaded = _storage.LoadAll();
        Macros.Clear();
        foreach (var m in loaded)
        {
            Macros.Add(m);
            if (m.IsHotkeyEnabled && m.BindKey.HasValue && m.Events.Count > 0)
                RegisterNativeBinding(m);
        }

        if (Macros.Count == 0)
        {
            var m = new Macro { Name = GetDefaultMacroName() };
            Macros.Add(m);
            SelectedMacro = m;
        }
        else
        {
            SelectedMacro = Macros[0];
        }
    }

    private static string GetDefaultMacroName() => "Macro 1";

    private void RegisterNativeBinding(Macro macro)
    {
        if (!macro.BindKey.HasValue || macro.Events.Count == 0) return;
        var native = macro.Events.Select(e => new NativeKeyEvent
        {
            vk = e.Vk,
            down = e.Down,
            delayMs = e.DelayMs
        }).ToArray();
        CoreInterop.Zen_RegisterBinding(macro.BindKey.Value, native, native.Length, (int)macro.PlayMode);
    }

    private void UnregisterNativeBinding(Macro macro)
    {
        if (macro.BindKey.HasValue)
            CoreInterop.Zen_UnregisterBinding(macro.BindKey.Value);
    }

    private void OnNativeKeyEvent(NativeKeyEvent evt)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (SelectedMacro == null) return;
            var ke = new KeyEvent { Vk = evt.vk, Down = evt.down, DelayMs = evt.delayMs };
            SelectedMacro.Events.Add(ke);
            EventAdded?.Invoke();
        });
    }

    public void ReapplyBindingIfActive()
    {
        if (SelectedMacro == null) return;
        if (SelectedMacro.IsHotkeyEnabled && SelectedMacro.BindKey.HasValue)
        {
            if (SelectedMacro.Events.Count > 0)
                RegisterNativeBinding(SelectedMacro);
            else
                UnregisterNativeBinding(SelectedMacro);
        }
    }

    partial void OnSelectedMacroChanged(Macro? value)
    {
        UpdateBindKeyDisplay(value?.BindKey);
        Application.Current.Dispatcher.Invoke(() => SelectedMacroChanged?.Invoke());
    }

    private void UpdateBindKeyDisplay(uint? vk)
    {
        BindKeyDisplay = vk.HasValue ? $"{VkToString(vk.Value)} (0x{vk.Value:X2})" : "";
        BindKey = vk ?? 0;
    }

    [RelayCommand]
    private void Record()
    {
        if (SelectedMacro == null) return;
        if (IsRecording)
        {
            CoreInterop.Zen_StopRecording();
            IsRecording = false;
            return;
        }
        SelectedMacro.Events.Clear();
        CoreInterop.Zen_ClearRecording();
        CoreInterop.Zen_StartRecording();
        IsRecording = true;
    }

    [RelayCommand]
    private void StopRecording()
    {
        if (!IsRecording) return;
        CoreInterop.Zen_StopRecording();
        IsRecording = false;
        ReapplyBindingIfActive();
    }

    [RelayCommand]
    private void AddKey()
    {
        if (SelectedMacro == null) return;
        var vk = CoreInterop.Zen_CaptureSingleKey();
        if (vk == 0) return;
        var evtDown = new KeyEvent { Vk = vk, Down = true, DelayMs = 0 };
        SelectedMacro.Events.Add(evtDown);
        var evtUp = new KeyEvent { Vk = vk, Down = false, DelayMs = 50 };
        SelectedMacro.Events.Add(evtUp);
        ReapplyBindingIfActive();
    }

    [RelayCommand]
    private void ApplyBatchDelay()
    {
        if (SelectedMacro == null || !double.TryParse(BatchDelayText, out var ms)) return;
        foreach (var e in SelectedMacro.Events)
            e.DelayMs = ms;
        SelectedMacro.RaiseEventsChanged();
        ReapplyBindingIfActive();
    }

    [RelayCommand]
    private void BindMacro()
    {
        if (SelectedMacro == null || SelectedMacro.Events.Count == 0) return;
        var vk = CoreInterop.Zen_CaptureSingleKey();
        if (vk == 0) return;

        SelectedMacro.BindKey = vk;
        SelectedMacro.IsHotkeyEnabled = true;
        UpdateBindKeyDisplay(vk);
        RegisterNativeBinding(SelectedMacro);
    }

    [RelayCommand]
    private void UnbindMacro()
    {
        if (SelectedMacro == null || !SelectedMacro.BindKey.HasValue) return;
        UnregisterNativeBinding(SelectedMacro);
        SelectedMacro.BindKey = null;
        UpdateBindKeyDisplay(null);
    }

    public void ToggleMacroHotkey(Macro macro, bool enable)
    {
        if (enable && macro.BindKey.HasValue && macro.Events.Count > 0)
            RegisterNativeBinding(macro);
        else
            UnregisterNativeBinding(macro);
    }

    [RelayCommand]
    private void NewMacro()
    {
        var macro = new Macro { Name = GetDefaultMacroName() };
        Macros.Add(macro);
        SelectedMacro = macro;
    }

    [RelayCommand]
    private void OpenMacro()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Zen Zakura Macro (*.zmacro)|*.zmacro|All files (*.*)|*.*",
            Title = "Open Macro",
            InitialDirectory = _storage.StoragePath
        };
        if (dialog.ShowDialog() == true)
        {
            var m = _storage.Open(dialog.FileName);
            if (m != null)
            {
                Macros.Add(m);
                SelectedMacro = m;
            }
            else
            {
                MessageBox.Show("Could not open macro file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    private void SaveMacro()
    {
        if (SelectedMacro == null) return;
        _storage.Save(SelectedMacro);
    }

    [RelayCommand]
    private void SaveMacroAs()
    {
        if (SelectedMacro == null) return;
        var dialog = new SaveFileDialog
        {
            Filter = "Zen Zakura Macro (*.zmacro)|*.zmacro",
            FileName = SelectedMacro.Name + ".zmacro",
            InitialDirectory = _storage.StoragePath
        };
        if (dialog.ShowDialog() == true)
            _storage.SaveAs(SelectedMacro, dialog.FileName);
    }

    [RelayCommand]
    private void DeleteMacro()
    {
        if (SelectedMacro == null) return;
        var name = SelectedMacro.Name;
        var result = MessageBox.Show($"Delete macro \"{name}\"?", "Confirm",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;
        UnregisterNativeBinding(SelectedMacro);
        _storage.Delete(SelectedMacro);
        Macros.Remove(SelectedMacro);
        SelectedMacro = Macros.FirstOrDefault();
    }

    [RelayCommand]
    private void RenameMacro()
    {
        if (SelectedMacro == null || string.IsNullOrWhiteSpace(RenameText)) return;
        _storage.Rename(SelectedMacro, RenameText);
        _storage.Save(SelectedMacro);
        RenameText = "";
    }

    public void Cleanup()
    {
        KeyEvent.AnyDelayChanged -= ReapplyBindingIfActive;
        UnhookKeyEvents();
        CoreInterop.Zen_ClearBindings();
    }

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
}
