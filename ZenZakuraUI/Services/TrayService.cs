using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;

namespace ZenZakuraUI.Services;

public class TrayService : IDisposable
{
    private TaskbarIcon? _trayIcon;
    private readonly Window _window;

    public TrayService(Window window)
    {
        _window = window;
    }

    public void Show()
    {
        if (_trayIcon != null) return;

        using var stream = Application.GetResourceStream(new Uri("pack://application:,,,/app.ico"))?.Stream;
        if (stream == null) return;

        _trayIcon = new TaskbarIcon
        {
            Icon = new System.Drawing.Icon(stream),
            ToolTipText = "Zen Zakura Macro",
            Visibility = Visibility.Visible
        };

        _trayIcon.TrayMouseDoubleClick += (_, _) => RestoreWindow();

        var contextMenu = new ContextMenu();
        var showItem = new MenuItem { Header = "Show" };
        showItem.Click += (_, _) => RestoreWindow();
        contextMenu.Items.Add(showItem);

        var exitItem = new MenuItem { Header = "Exit" };
        exitItem.Click += (_, _) =>
        {
            _trayIcon.Dispose();
            Application.Current.Shutdown();
        };
        contextMenu.Items.Add(exitItem);

        _trayIcon.ContextMenu = contextMenu;
    }

    public void Hide()
    {
        _trayIcon?.Dispose();
        _trayIcon = null;
    }

    private void RestoreWindow()
    {
        _window.Show();
        _window.WindowState = WindowState.Normal;
        _window.Activate();
    }

    public void Dispose()
    {
        Hide();
    }
}
