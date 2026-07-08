using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

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
        string hostModsRoot = ContentJsonLoader.ResolvePath(modsRoot);

        if (!Directory.Exists(hostModsRoot))
        {
            return;
        }

        foreach (string modDirectory in Directory
                     .GetDirectories(hostModsRoot)
                     .Select(Path.GetFileName)
                     .OfType<string>()
                     .Where(name => !string.IsNullOrWhiteSpace(name) && !name.StartsWith('.'))
                     .OrderBy(name => name, StringComparer.Ordinal))
        {
            LoadStringFile(Path.Combine(hostModsRoot, modDirectory, ModStringsFileName));
        }
    }

    private static void LoadStringFile(string path)
    {
        string hostPath = ContentJsonLoader.ResolvePath(path);

        if (!File.Exists(hostPath))
        {
            return;
        }

        Dictionary<string, string>? loadedStrings = JsonSerializer.Deserialize<Dictionary<string, string>>(
            File.ReadAllText(hostPath));

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
