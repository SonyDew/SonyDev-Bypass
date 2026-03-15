namespace SonyDevBypass.App.Models;

public sealed class AppSettings
{
    public string Language { get; set; } = "en";

    public string DownloadDirectory { get; set; } = string.Empty;

    public string? LastSelectedGame { get; set; }

    public bool AutoCheckUpdates { get; set; } = true;
}
