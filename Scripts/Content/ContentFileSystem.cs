using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace TheThingImDoing.Content;

/// <summary>
/// Reads content through Godot's virtual filesystem when the engine is available,
/// while preserving a host-filesystem fallback for pure .NET tools and tests.
/// </summary>
internal static class ContentFileSystem
{
    private const string ResourcePrefix = "res://";
    private const string UserPrefix = "user://";

    private static readonly Lazy<bool> GodotFileSystemAvailable = new(DetectGodotFileSystem);
    private static readonly Lazy<string> ProjectRoot = new(FindProjectRoot);

    internal static bool TryReadAllText(string path, out string contents)
    {
        if (IsVirtualPath(path) && GodotFileSystemAvailable.Value)
        {
            try
            {
                if (Godot.FileAccess.FileExists(path))
                {
                    contents = Godot.FileAccess.GetFileAsString(path);
                    return true;
                }
            }
            catch (Exception exception)
            {
                ContentDiagnostics.Warn(
                    $"Could not read content file {path} through Godot: {exception.Message}");
            }
        }

        string hostPath = ResolveHostPath(path);

        try
        {
            if (File.Exists(hostPath))
            {
                contents = File.ReadAllText(hostPath);
                return true;
            }
        }
        catch (Exception exception)
        {
            ContentDiagnostics.Warn($"Could not read content file {path}: {exception.Message}");
        }

        contents = string.Empty;
        return false;
    }

    internal static IReadOnlyList<string> GetModFilePaths(string modsRoot, string fileName)
    {
        if (IsVirtualPath(modsRoot) && GodotFileSystemAvailable.Value)
        {
            try
            {
                using Godot.DirAccess? directory = Godot.DirAccess.Open(modsRoot);

                if (directory == null)
                {
                    return Array.Empty<string>();
                }

                return directory
                    .GetDirectories()
                    .Where(IsVisibleDirectoryName)
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .Select(name => CombineVirtualPath(modsRoot, name, fileName))
                    .Where(Godot.FileAccess.FileExists)
                    .ToArray();
            }
            catch (Exception exception)
            {
                ContentDiagnostics.Warn(
                    $"Could not enumerate mod directory {modsRoot} through Godot: {exception.Message}");
            }
        }

        string hostModsRoot = ResolveHostPath(modsRoot);

        try
        {
            if (!Directory.Exists(hostModsRoot))
            {
                return Array.Empty<string>();
            }

            return Directory
                .GetDirectories(hostModsRoot)
                .Select(Path.GetFileName)
                .OfType<string>()
                .Where(IsVisibleDirectoryName)
                .OrderBy(name => name, StringComparer.Ordinal)
                .Select(name => Path.Combine(hostModsRoot, name, fileName))
                .Where(File.Exists)
                .ToArray();
        }
        catch (Exception exception)
        {
            ContentDiagnostics.Warn($"Could not enumerate mod directory {modsRoot}: {exception.Message}");
            return Array.Empty<string>();
        }
    }

    internal static string ResolveHostPath(string path)
    {
        if (IsVirtualPath(path) && GodotFileSystemAvailable.Value)
        {
            try
            {
                return Godot.ProjectSettings.GlobalizePath(path);
            }
            catch (Exception exception)
            {
                ContentDiagnostics.Warn(
                    $"Could not globalize content path {path} through Godot: {exception.Message}");
            }
        }

        if (path.StartsWith(ResourcePrefix, StringComparison.Ordinal))
        {
            return Path.Combine(
                ProjectRoot.Value,
                path[ResourcePrefix.Length..].Replace('/', Path.DirectorySeparatorChar));
        }

        if (path.StartsWith(UserPrefix, StringComparison.Ordinal))
        {
            string userRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "the-thing-im-doing");
            return Path.Combine(
                userRoot,
                path[UserPrefix.Length..].Replace('/', Path.DirectorySeparatorChar));
        }

        return path;
    }

    private static bool DetectGodotFileSystem()
    {
        // Godot loads the managed project from its native host, so it has no
        // managed entry assembly. Editor/headless executables also identify
        // themselves by name, while exported games rely on the native-host
        // signal. dotnet test/tools have a managed entry assembly and neither
        // signal. This guard must stay entirely managed: invoking a Godot API in
        // a plain .NET process can terminate that process natively.
        string executableName = Path.GetFileNameWithoutExtension(Environment.ProcessPath ?? string.Empty);
        bool isNamedGodotExecutable = executableName.Contains("godot", StringComparison.OrdinalIgnoreCase);
        bool isNativeManagedHost = Assembly.GetEntryAssembly() == null;
        bool available = isNamedGodotExecutable || isNativeManagedHost;

        if (string.Equals(
                Environment.GetEnvironmentVariable("THE_THING_CONTENT_FS_TRACE"),
                "1",
                StringComparison.Ordinal))
        {
            Console.Error.WriteLine(
                available
                    ? $"Content filesystem: Godot virtual paths ({executableName})."
                    : $"Content filesystem: host fallback ({executableName}).");
        }

        return available;
    }

    private static bool IsVirtualPath(string path)
    {
        return path.StartsWith(ResourcePrefix, StringComparison.Ordinal) ||
               path.StartsWith(UserPrefix, StringComparison.Ordinal);
    }

    private static bool IsVisibleDirectoryName(string? name)
    {
        return !string.IsNullOrWhiteSpace(name) &&
               !name.StartsWith(".", StringComparison.Ordinal);
    }

    private static string CombineVirtualPath(string root, params string[] segments)
    {
        return $"{root.TrimEnd('/')}/{string.Join('/', segments)}";
    }

    private static string FindProjectRoot()
    {
        DirectoryInfo? directory = new(Directory.GetCurrentDirectory());

        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "project.godot")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}
