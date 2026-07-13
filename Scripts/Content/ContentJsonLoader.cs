using System;
using System.Collections.Generic;
using System.Text.Json;

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
        where TFile : class, IContentFile
    {
        foreach (LoadedContentItem<TItem> item in LoadItemsWithSources(fileName, selectItems))
        {
            yield return item.Value;
        }
    }

    public static IEnumerable<LoadedContentItem<TItem>> LoadItemsWithSources<TFile, TItem>(
        string fileName,
        Func<TFile, IEnumerable<TItem>?> selectItems,
        int supportedSchemaVersion = 1)
        where TFile : class, IContentFile
    {
        foreach (string path in GetLoadPaths(fileName))
        {
            TFile? contentFile = LoadFile<TFile>(path);

            if (contentFile == null)
            {
                continue;
            }

            if (contentFile.SchemaVersion <= 0)
            {
                ContentDiagnostics.Warn($"Content file {path} is missing schemaVersion and was skipped.");
                continue;
            }

            if (contentFile.SchemaVersion != supportedSchemaVersion)
            {
                ContentDiagnostics.Warn(
                    $"Content file {path} uses schemaVersion {contentFile.SchemaVersion}; expected {supportedSchemaVersion}.");
                continue;
            }

            IEnumerable<TItem>? items = selectItems(contentFile);

            if (items == null)
            {
                continue;
            }

            foreach (TItem item in items)
            {
                yield return new LoadedContentItem<TItem>(item, path);
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
        foreach (string path in ContentFileSystem.GetModFilePaths(modsRoot, fileName))
        {
            yield return path;
        }
    }

    private static TFile? LoadFile<TFile>(string path)
        where TFile : class
    {
        if (!ContentFileSystem.TryReadAllText(path, out string json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<TFile>(json, JsonOptions);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Could not load content file {path}: {exception.Message}");
            return null;
        }
    }

    internal static string ResolvePath(string path)
    {
        return ContentFileSystem.ResolveHostPath(path);
    }
}
