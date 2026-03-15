using System.Text.Json.Serialization;

namespace SonyDevBypass.App.Models;

public sealed class UpdateInfo
{
    [JsonPropertyName("version")]
    public string Version { get; init; } = string.Empty;

    [JsonPropertyName("release_date")]
    public string ReleaseDate { get; init; } = string.Empty;

    [JsonPropertyName("changelog")]
    public string Changelog { get; init; } = string.Empty;
}
