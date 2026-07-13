using System;
using System.Collections.Generic;
using System.Text.Json;

namespace TheThingImDoing.Content;

public static class GameStrings
{
    private const string BaseStringsPath = "res://Content/Base/strings.json";
    private const string ProjectModsPath = "res://Mods";
    private const string UserModsPath = "user://mods";
    private const string ModStringsFileName = "strings.json";

    private static readonly object LoadGate = new();
    private static readonly Dictionary<string, string> Strings = new(StringComparer.Ordinal);
    private static bool _loaded;

    public static string Get(string key)
    {
        lock (LoadGate)
        {
            EnsureLoaded();
            return Strings.TryGetValue(key, out string? value) ? value : $"#{key}";
        }
    }

    public static bool Has(string key)
    {
        lock (LoadGate)
        {
            EnsureLoaded();
            return Strings.ContainsKey(key);
        }
    }

    public static void Reload()
    {
        lock (LoadGate)
        {
            _loaded = false;
            Strings.Clear();
            EnsureLoaded();
        }
    }

    private static void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        LoadStringFile(BaseStringsPath);
        LoadMods(ProjectModsPath);
        LoadMods(UserModsPath);
        _loaded = true;
    }

    private static void LoadMods(string modsRoot)
    {
        foreach (string path in ContentFileSystem.GetModFilePaths(modsRoot, ModStringsFileName))
        {
            LoadStringFile(path);
        }
    }

    private static void LoadStringFile(string path)
    {
        if (!ContentFileSystem.TryReadAllText(path, out string json))
        {
            return;
        }

        Dictionary<string, string>? loadedStrings;

        try
        {
            loadedStrings = JsonSerializer.Deserialize<Dictionary<string, string>>(
                json);
        }
        catch (Exception exception)
        {
            ContentDiagnostics.Warn($"Could not load strings file {path}: {exception.Message}");
            return;
        }

        if (loadedStrings == null)
        {
            return;
        }

        foreach ((string key, string value) in loadedStrings)
        {
            Strings[key] = value;
        }
    }
}
