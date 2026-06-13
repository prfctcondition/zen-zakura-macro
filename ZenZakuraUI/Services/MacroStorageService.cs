using System.IO;
using System.Text.Json;
using ZenZakuraUI.Models;

namespace ZenZakuraUI.Services;

public class MacroStorageService
{
    private readonly string _basePath;

    public MacroStorageService()
    {
        _basePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ZenZakura", "Macros");
        try
        {
            Directory.CreateDirectory(_basePath);
        }
        catch { }
    }

    public string StoragePath => _basePath;

    public string[] GetAllFiles()
    {
        try
        {
            if (Directory.Exists(_basePath))
                return Directory.GetFiles(_basePath, "*.zmacro");
        }
        catch { }
        return [];
    }

    public List<Macro> LoadAll()
    {
        var macros = new List<Macro>();
        foreach (var file in GetAllFiles())
        {
            try
            {
                var json = File.ReadAllText(file);
                if (string.IsNullOrWhiteSpace(json)) continue;
                var m = Macro.FromJson(json);
                if (m != null)
                {
                    m.FilePath = file;
                    macros.Add(m);
                }
            }
            catch { }
        }
        return macros;
    }

    public void Save(Macro macro)
    {
        if (string.IsNullOrEmpty(macro.FilePath) || !File.Exists(macro.FilePath))
        {
            macro.FilePath = Path.Combine(_basePath, SanitizeName(macro.Name) + ".zmacro");
        }
        try
        {
            var json = macro.ToJson();
            if (!string.IsNullOrWhiteSpace(json))
                File.WriteAllText(macro.FilePath, json);
        }
        catch { }
    }

    public void SaveAs(Macro macro, string filePath)
    {
        macro.FilePath = filePath;
        try
        {
            var json = macro.ToJson();
            if (!string.IsNullOrWhiteSpace(json))
                File.WriteAllText(filePath, json);
        }
        catch { }
    }

    public Macro? Open(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        try
        {
            var json = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(json)) return null;
            var m = Macro.FromJson(json);
            if (m != null)
            {
                m.FilePath = filePath;
            }
            return m;
        }
        catch { return null; }
    }

    public void Delete(Macro macro)
    {
        if (!string.IsNullOrEmpty(macro.FilePath) && File.Exists(macro.FilePath))
        {
            try { File.Delete(macro.FilePath); } catch { }
        }
        macro.FilePath = "";
    }

    public void Rename(Macro macro, string newName)
    {
        var oldPath = macro.FilePath;
        macro.Name = newName;
        if (!string.IsNullOrEmpty(oldPath) && File.Exists(oldPath))
        {
            try
            {
                var dir = Path.GetDirectoryName(oldPath)!;
                var newPath = Path.Combine(dir, SanitizeName(newName) + ".zmacro");
                if (!oldPath.Equals(newPath, StringComparison.OrdinalIgnoreCase))
                {
                    File.Move(oldPath, newPath, overwrite: true);
                }
                else
                {
                    File.WriteAllText(oldPath, macro.ToJson());
                }
                macro.FilePath = newPath;
            }
            catch { }
        }
    }

    private static string SanitizeName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return string.IsNullOrWhiteSpace(name) ? "Unnamed" : name;
    }
}
