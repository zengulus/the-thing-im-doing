using System;

namespace TheThingImDoing.Content;

public interface IContentFile
{
    int SchemaVersion { get; }
}

public interface IContentDefinition
{
    string Id { get; }
    string Operation { get; }
}

public sealed record LoadedContentItem<T>(T Value, string SourcePath);

public static class ContentDiagnostics
{
    public static void Warn(string message)
    {
        Console.Error.WriteLine(message);
    }
}
