namespace SonyDevBypass.App.Models;

public sealed class RemoteDirectoryEntry
{
    public required string Name { get; init; }

    public required string Href { get; init; }

    public required Uri Uri { get; init; }

    public required bool IsDirectory { get; init; }

    public long? Size { get; init; }
}
