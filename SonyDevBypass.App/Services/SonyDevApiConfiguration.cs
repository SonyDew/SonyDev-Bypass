using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SonyDevBypass.App.Services;

public sealed class SonyDevApiConfiguration
{
    public const string RuntimeConfigFileName = "sonydev.runtime.json";
    public const string ExampleConfigFileName = "sonydev.runtime.example.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private SonyDevApiConfiguration(SonyDevRuntimeOptions options)
    {
        SecretKey = options.SecretKey?.Trim() ?? string.Empty;
        PackageId = string.IsNullOrWhiteSpace(options.PackageId) ? "SonyDevBypass" : options.PackageId.Trim();
        PackageTitle = string.IsNullOrWhiteSpace(options.PackageTitle) ? "SonyDev Bypass" : options.PackageTitle.Trim();
        UpdateChannel = string.IsNullOrWhiteSpace(options.UpdateChannel) ? "win-x64-beta" : options.UpdateChannel.Trim();
        PreferredInstallDirectory = string.IsNullOrWhiteSpace(options.PreferredInstallDirectory)
            ? @"C:\SonyDev\Bypass"
            : options.PreferredInstallDirectory.Trim();
        OfficialWebsite = string.IsNullOrWhiteSpace(options.OfficialWebsite)
            ? "https://sonydev.de/"
            : options.OfficialWebsite.Trim();

        GamesBaseUri = CreateOptionalUri(options.GamesBaseUrl);
        UpdatesBaseUri = CreateOptionalUri(options.UpdatesBaseUrl);
        LatestUpdateUri = UpdatesBaseUri is null ? null : new Uri(UpdatesBaseUri, "latest.json");
        SetupInstallerUri = UpdatesBaseUri is null ? null : new Uri(UpdatesBaseUri, $"{PackageId}-{UpdateChannel}-Setup.exe");
    }

    public string SecretKey { get; }

    public string PackageId { get; }

    public string PackageTitle { get; }

    public string UpdateChannel { get; }

    public string PreferredInstallDirectory { get; }

    public string OfficialWebsite { get; }

    public Uri? GamesBaseUri { get; }

    public Uri? UpdatesBaseUri { get; }

    public Uri? LatestUpdateUri { get; }

    public Uri? SetupInstallerUri { get; }

    public bool HasSecretKey => !string.IsNullOrWhiteSpace(SecretKey);

    public bool HasCatalogConfiguration => GamesBaseUri is not null && !IsPlaceholderUri(GamesBaseUri);

    public bool HasUpdateConfiguration => UpdatesBaseUri is not null && !IsPlaceholderUri(UpdatesBaseUri);

    public static SonyDevApiConfiguration Load()
    {
        var options = new SonyDevRuntimeOptions();

        var configPath = ResolveConfigPath();
        if (configPath is not null)
        {
            using var stream = File.OpenRead(configPath);
            var loaded = JsonSerializer.Deserialize<SonyDevRuntimeOptions>(stream, JsonOptions);
            if (loaded is not null)
            {
                options = loaded;
            }
        }

        ApplyEnvironmentOverrides(options);
        return new SonyDevApiConfiguration(options);
    }

    public string GetCatalogConfigurationMessage()
    {
        return $"Remote catalog is not configured. Create {RuntimeConfigFileName} from {ExampleConfigFileName} and set games_base_url.";
    }

    public string GetUpdateConfigurationMessage()
    {
        return $"Update endpoint is not configured. Create {RuntimeConfigFileName} from {ExampleConfigFileName} and set updates_base_url.";
    }

    public void EnsureCatalogConfigured()
    {
        if (!HasCatalogConfiguration || GamesBaseUri is null)
        {
            throw new InvalidOperationException(GetCatalogConfigurationMessage());
        }
    }

    public void EnsureUpdatesConfigured()
    {
        if (!HasUpdateConfiguration || UpdatesBaseUri is null || LatestUpdateUri is null || SetupInstallerUri is null)
        {
            throw new InvalidOperationException(GetUpdateConfigurationMessage());
        }
    }

    private static string? ResolveConfigPath()
    {
        var configuredPath = Environment.GetEnvironmentVariable("SONYDEV_RUNTIME_CONFIG");
        if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
        {
            return configuredPath;
        }

        var candidates = new List<string>
        {
            Path.Combine(AppContext.BaseDirectory, RuntimeConfigFileName),
            Path.Combine(Environment.CurrentDirectory, RuntimeConfigFileName)
        };

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        for (var index = 0; index < 5 && directory is not null; index++)
        {
            candidates.Add(Path.Combine(directory.FullName, RuntimeConfigFileName));
            directory = directory.Parent;
        }

        return candidates
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(File.Exists);
    }

    private static void ApplyEnvironmentOverrides(SonyDevRuntimeOptions options)
    {
        options.GamesBaseUrl = GetEnvironmentOverride("SONYDEV_GAMES_BASE_URL") ?? options.GamesBaseUrl;
        options.UpdatesBaseUrl = GetEnvironmentOverride("SONYDEV_UPDATES_BASE_URL") ?? options.UpdatesBaseUrl;
        options.SecretKey = GetEnvironmentOverride("SONYDEV_SECRET_KEY") ?? options.SecretKey;
        options.PackageId = GetEnvironmentOverride("SONYDEV_PACKAGE_ID") ?? options.PackageId;
        options.PackageTitle = GetEnvironmentOverride("SONYDEV_PACKAGE_TITLE") ?? options.PackageTitle;
        options.UpdateChannel = GetEnvironmentOverride("SONYDEV_UPDATE_CHANNEL") ?? options.UpdateChannel;
        options.PreferredInstallDirectory = GetEnvironmentOverride("SONYDEV_INSTALL_DIR") ?? options.PreferredInstallDirectory;
        options.OfficialWebsite = GetEnvironmentOverride("SONYDEV_OFFICIAL_WEBSITE") ?? options.OfficialWebsite;
    }

    private static string? GetEnvironmentOverride(string variableName)
    {
        var value = Environment.GetEnvironmentVariable(variableName);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static Uri? CreateOptionalUri(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException($"Invalid runtime configuration URL: {value}");
        }

        var normalized = uri.AbsoluteUri.EndsWith("/", StringComparison.Ordinal)
            ? uri.AbsoluteUri
            : $"{uri.AbsoluteUri}/";

        return new Uri(normalized, UriKind.Absolute);
    }

    private static bool IsPlaceholderUri(Uri uri)
    {
        return string.Equals(uri.Host, "example.invalid", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class SonyDevRuntimeOptions
    {
        [JsonPropertyName("games_base_url")]
        public string GamesBaseUrl { get; set; } = "https://example.invalid/bypasses/";

        [JsonPropertyName("updates_base_url")]
        public string UpdatesBaseUrl { get; set; } = "https://example.invalid/SDBypass/update/";

        [JsonPropertyName("secret_key")]
        public string SecretKey { get; set; } = string.Empty;

        [JsonPropertyName("package_id")]
        public string PackageId { get; set; } = "SonyDevBypass";

        [JsonPropertyName("package_title")]
        public string PackageTitle { get; set; } = "SonyDev Bypass";

        [JsonPropertyName("update_channel")]
        public string UpdateChannel { get; set; } = "win-x64-beta";

        [JsonPropertyName("preferred_install_directory")]
        public string PreferredInstallDirectory { get; set; } = @"C:\SonyDev\Bypass";

        [JsonPropertyName("official_website")]
        public string OfficialWebsite { get; set; } = "https://sonydev.de/";
    }
}
