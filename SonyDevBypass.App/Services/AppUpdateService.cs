using System.Diagnostics;
using System.IO;
using System.Net.Http;
using Velopack;
using Velopack.Sources;

namespace SonyDevBypass.App.Services;

public sealed class AppUpdateService
{
    private readonly SonyDevApiConfiguration _configuration;
    private readonly SonyDevVelopackDownloader _downloader;
    private readonly Lazy<UpdateManager?> _updateManager;

    public AppUpdateService(SonyDevApiConfiguration configuration)
    {
        _configuration = configuration;
        _downloader = new SonyDevVelopackDownloader(_configuration);
        _updateManager = new Lazy<UpdateManager?>(CreateUpdateManager);
    }

    public bool HasUpdateEndpoint => _configuration.HasUpdateConfiguration;

    public bool IsInstalled => TryGetUpdateManager()?.IsInstalled ?? false;

    public string? CurrentInstalledVersion => TryGetUpdateManager()?.CurrentVersion?.ToString();

    public string? CurrentInstallRootDirectory => GetCurrentInstallRootDirectory();

    public bool IsInstalledInPreferredDirectory =>
        IsInstalled &&
        PathsEqual(CurrentInstallRootDirectory, _configuration.PreferredInstallDirectory);

    public bool RequiresInstallerMigration => IsInstalled && !IsInstalledInPreferredDirectory;

    public VelopackAsset? PendingRestartUpdate => TryGetUpdateManager()?.UpdatePendingRestart;

    public string InstallerFileName => _configuration.SetupInstallerUri is null
        ? $"{_configuration.PackageId}-{_configuration.UpdateChannel}-Setup.exe"
        : Path.GetFileName(_configuration.SetupInstallerUri.LocalPath);

    public async Task<UpdateInfo?> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        _configuration.EnsureUpdatesConfigured();
        if (!IsInstalled)
        {
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return await TryGetUpdateManager()!.CheckForUpdatesAsync();
    }

    public async Task DownloadUpdateAsync(
        UpdateInfo updateInfo,
        Action<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateInfo);

        if (!IsInstalled)
        {
            throw new InvalidOperationException("Auto-update is available only for installed builds.");
        }

        await TryGetUpdateManager()!.DownloadUpdatesAsync(
            updateInfo,
            progress ?? (_ => { }),
            cancellationToken);
    }

    public async Task PrepareApplyAndRestartAsync(
        VelopackAsset? asset = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsInstalled)
        {
            throw new InvalidOperationException("There is no installed Velopack package to update.");
        }

        var targetAsset = asset ?? PendingRestartUpdate;
        if (targetAsset is null)
        {
            throw new InvalidOperationException("There is no downloaded update ready to be applied.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        await TryGetUpdateManager()!.WaitExitThenApplyUpdatesAsync(targetAsset, silent: true, restart: true, restartArgs: []);
    }

    public async Task<string> DownloadInstallerAsync(
        Action<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _configuration.EnsureUpdatesConfigured();
        var targetPath = Path.Combine(
            Path.GetTempPath(),
            $"{_configuration.PackageId}-{_configuration.UpdateChannel}-Setup-{Guid.NewGuid():N}.exe");

        await _downloader.DownloadFile(
            _configuration.SetupInstallerUri!.AbsoluteUri,
            targetPath,
            progress ?? (_ => { }),
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            timeout: 5,
            cancellationToken);

        return targetPath;
    }

    public void LaunchInstaller(string installerPath)
    {
        if (string.IsNullOrWhiteSpace(installerPath))
        {
            throw new ArgumentException("Installer path cannot be empty.", nameof(installerPath));
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = installerPath,
            Arguments = $"--installto \"{_configuration.PreferredInstallDirectory}\"",
            UseShellExecute = true
        });
    }

    private UpdateManager? TryGetUpdateManager()
    {
        return _updateManager.Value;
    }

    private UpdateManager? CreateUpdateManager()
    {
        if (!_configuration.HasUpdateConfiguration)
        {
            return null;
        }

        try
        {
            return new UpdateManager(
                new SimpleWebSource(_configuration.UpdatesBaseUri!, _downloader, 5),
                new UpdateOptions
                {
                    ExplicitChannel = _configuration.UpdateChannel
                },
                locator: null);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private string? GetCurrentInstallRootDirectory()
    {
        if (!IsInstalled)
        {
            return null;
        }

        var appDirectory = AppContext.BaseDirectory
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (string.IsNullOrWhiteSpace(appDirectory))
        {
            return null;
        }

        var directoryInfo = new DirectoryInfo(appDirectory);
        if (string.Equals(directoryInfo.Name, "current", StringComparison.OrdinalIgnoreCase) &&
            directoryInfo.Parent is not null)
        {
            return directoryInfo.Parent.FullName;
        }

        return directoryInfo.FullName;
    }

    private static bool PathsEqual(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        static string Normalize(string path)
        {
            return Path.GetFullPath(path)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        return string.Equals(
            Normalize(left),
            Normalize(right),
            StringComparison.OrdinalIgnoreCase);
    }

    private sealed class SonyDevVelopackDownloader : HttpClientFileDownloader
    {
        private readonly SonyDevApiConfiguration _configuration;

        public SonyDevVelopackDownloader(SonyDevApiConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override HttpClient CreateHttpClient(IDictionary<string, string>? headers, double timeout)
        {
            var mergedHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (_configuration.HasSecretKey)
            {
                mergedHeaders["X-Secret-Key"] = _configuration.SecretKey;
            }

            foreach (var header in headers ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase))
            {
                mergedHeaders[header.Key] = header.Value;
            }

            return base.CreateHttpClient(mergedHeaders, timeout);
        }
    }
}
