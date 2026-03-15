namespace SonyDevBypass.App.Models;

public sealed class DownloadProgress
{
    public required string CurrentFileName { get; init; }

    public required long CompletedBytes { get; init; }

    public required long TotalBytes { get; init; }

    public required int CompletedFiles { get; init; }

    public required int TotalFiles { get; init; }

    public required double Fraction { get; init; }
}
