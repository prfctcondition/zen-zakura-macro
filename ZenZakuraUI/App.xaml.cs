using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using ZenZakuraUI.Services;

namespace ZenZakuraUI;

public partial class App : Application
{
    private static string SettingsPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ZenZakura", "settings.json");

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        CoreInterop.Zen_Initialize();
        LoadSettings();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SaveSettings();
        CoreInterop.Zen_Shutdown();
        base.OnExit(e);
    }

    private void LoadSettings()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;
            var json = File.ReadAllText(SettingsPath);
            var data = JsonSerializer.Deserialize<SettingsData>(json);
            if (data == null) return;

            if (data.PauseToggleKey != 0)
            {
                CoreInterop.Zen_SetPauseToggleKey(data.PauseToggleKey);
                if (data.Paused) CoreInterop.Zen_SetPaused(true);
            }
            CoreInterop.Zen_SetProcessFilter(data.ProcessFilter ?? "", data.ProcessFilterEnabled);
        }
        catch { }
    }

    private void SaveSettings()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath);
            if (dir != null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var sb = new StringBuilder(260);
            CoreInterop.Zen_GetProcessFilter(sb, sb.Capacity);

            var data = new SettingsData
            {
                PauseToggleKey = CoreInterop.Zen_GetPauseToggleKey(),
                Paused = CoreInterop.Zen_GetPaused(),
                ProcessFilterEnabled = CoreInterop.Zen_GetProcessFilterEnabled(),
                ProcessFilter = sb.ToString()
            };

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch { }
    }

    private class SettingsData
    {
        public uint PauseToggleKey { get; set; }
        public bool Paused { get; set; }
        public bool ProcessFilterEnabled { get; set; }
        public string ProcessFilter { get; set; } = "";
    }
}
