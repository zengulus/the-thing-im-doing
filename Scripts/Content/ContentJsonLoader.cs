using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace TheThingImDoing.Content;

public static class ContentJsonLoader
{
    private const string BaseContentPath = "res://Content/Base";
    private const string ProjectModsPath = "res://Mods";
    private const string UserModsPath = "user://mods";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static IEnumerable<TItem> LoadItems<TFile, TItem>(
        string fileName,
        Func<TFile, IEnumerable<TItem>?> selectItems)
        where TFile : class
    {
        foreach (string path in GetLoadPaths(fileName))
        {
            TFile? contentFile = LoadFile<TFile>(path);

            if (contentFile == null)
            {
                continue;
            }

            IEnumerable<TItem>? items = selectItems(contentFile);

            if (items == null)
            {
                continue;
            }

            foreach (TItem item in items)
            {
                yield return item;
            }
        }
    }

    private static IEnumerable<string> GetLoadPaths(string fileName)
    {
        yield return $"{BaseContentPath}/{fileName}";

        foreach (string modPath in GetModPaths(ProjectModsPath, fileName))
        {
            yield return modPath;
        }

        foreach (string modPath in GetModPaths(UserModsPath, fileName))
        {
            yield return modPath;
        }
    }

    private static IEnumerable<string> GetModPaths(string modsRoot, string fileName)
    {
        DirAccess? dir = DirAccess.Open(modsRoot);

        if (dir == null)
        {
            yield break;
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

            if (!entry.StartsWith('.') && dir.CurrentIsDir())
            {
                modDirectories.Add(entry);
            }
        }

        dir.ListDirEnd();
        modDirectories.Sort(StringComparer.Ordinal);

        foreach (string modDirectory in modDirectories)
        {
            string path = $"{modsRoot}/{modDirectory}/{fileName}";

            if (FileAccess.FileExists(path))
            {
                yield return path;
            }
        }
    }

    private static TFile? LoadFile<TFile>(string path)
        where TFile : class
    {
        if (!FileAccess.FileExists(path))
        {
            return null;
        }

        try
        {
            using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            return JsonSerializer.Deserialize<TFile>(file.GetAsText(), JsonOptions);
        }
        catch (Exception exception)
        {
            GD.PushWarning($"Could not load content file {path}: {exception.Message}");
            return null;
        }
    }
}

