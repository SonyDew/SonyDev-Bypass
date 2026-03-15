namespace SonyDevBypass.App.Models;

public sealed class RemoteFile
{
    public required string RelativePath { get; init; }

    public required Uri Uri { get; init; }

    public long Size { get; init; }
}
