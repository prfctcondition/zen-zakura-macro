using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ZenZakuraUI.Services;

public class ProcessWatcherService : IDisposable
{
    private readonly Timer _timer;
    private string _targetProcess = "";
    private bool _enabled;

    public event Action<bool>? FocusChanged;

    public bool IsFocused { get; private set; } = true;

    public ProcessWatcherService()
    {
        _timer = new Timer(CheckFocus, null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Start(string processName, bool enabled)
    {
        _targetProcess = processName;
        _enabled = enabled;
        IsFocused = !enabled;
        if (enabled)
            _timer.Change(0, 100);
        else
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    public void Stop()
    {
        _timer.Change(Timeout.Infinite, Timeout.Infinite);
        IsFocused = true;
    }

    private void CheckFocus(object? state)
    {
        if (!_enabled || string.IsNullOrEmpty(_targetProcess)) return;

        var hwnd = GetForegroundWindow();
        GetWindowThreadProcessId(hwnd, out uint pid);
        bool focused = false;
        try
        {
            var proc = Process.GetProcessById((int)pid);
            focused = proc.ProcessName.Equals(_targetProcess, StringComparison.OrdinalIgnoreCase)
                      || proc.MainModule?.ModuleName?.Equals(_targetProcess, StringComparison.OrdinalIgnoreCase) == true;
        }
        catch { }

        if (focused != IsFocused)
        {
            IsFocused = focused;
            FocusChanged?.Invoke(focused);
        }
    }

    public void Dispose()
    {
        _timer.Dispose();
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
}
