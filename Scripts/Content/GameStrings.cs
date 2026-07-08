using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace TheThingImDoing.Content;

public static class GameStrings
{
    private const string BaseStringsPath = "res://Content/Base/strings.json";
    private const string ProjectModsPath = "res://Mods";
    private const string UserModsPath = "user://mods";
    private const string ModStringsFileName = "strings.json";

    private static readonly Dictionary<string, string> Strings = new(StringComparer.Ordinal);
    private static bool _loaded;

    public static string Get(string key)
    {
        EnsureLoaded();
        return Strings.TryGetValue(key, out string? value) ? value : $"#{key}";
    }

    public static void Reload()
    {
        _loaded = false;
        Strings.Clear();
        EnsureLoaded();
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
        DirAccess? dir = DirAccess.Open(modsRoot);

        if (dir == null)
        {
            return;
        }

        var modDirectories = new List<string>();
        dir.ListDirBegin();

        while (true)
        {
            string entry = dir.GetNext();

            if (string.IsNullOrEmpty(entry))
            {
                break;
            }

            if (entry.StartsWith('.') || !dir.CurrentIsDir())
            {
                continue;
            }

            modDirectories.Add(entry);
        }

        dir.ListDirEnd();
        modDirectories.Sort(StringComparer.Ordinal);

        foreach (string modDirectory in modDirectories)
        {
            LoadStringFile($"{modsRoot}/{modDirectory}/{ModStringsFileName}");
        }
    }

    private static void LoadStringFile(string path)
    {
        if (!FileAccess.FileExists(path))
        {
            return;
        }

        using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        string json = file.GetAsText();

        Dictionary<string, string>? loadedStrings = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

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

