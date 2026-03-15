using System.Net;
using System.Net.Http;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using SonyDevBypass.App.Models;

namespace SonyDevBypass.App.Services;

public sealed class SonyDevApiClient : IDisposable
{
    private static readonly Regex IndexEntryRegex = new(
        "<a href=\"(?<href>[^\"]+)\">.*?</a>\\s+\\d{2}-[A-Za-z]{3}-\\d{4}\\s+\\d{2}:\\d{2}\\s+(?<size>-|\\d+)",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private readonly HttpClient _httpClient;
    private readonly SonyDevApiConfiguration _configuration;

    public SonyDevApiClient(SonyDevApiConfiguration configuration)
    {
        _configuration = configuration;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(45)
        };
        if (_configuration.HasSecretKey)
        {
            _httpClient.DefaultRequestHeaders.Add("X-Secret-Key", _configuration.SecretKey);
        }
    }

    public bool HasCatalogEndpoint => _configuration.HasCatalogConfiguration;

    public bool HasUpdateEndpoint => _configuration.HasUpdateConfiguration;

    public string CatalogConfigurationMessage => _configuration.GetCatalogConfigurationMessage();

    public string UpdateConfigurationMessage => _configuration.GetUpdateConfigurationMessage();

    public async Task<IReadOnlyList<string>> GetGameNamesAsync(CancellationToken cancellationToken = default)
    {
        _configuration.EnsureCatalogConfigured();
        var entries = await ReadDirectoryEntriesAsync(_configuration.GamesBaseUri!, cancellationToken);
        return entries
            .Where(entry => entry.IsDirectory)
            .Select(entry => entry.Name)
            .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    public async Task<UpdateInfo?> GetLatestUpdateAsync(CancellationToken cancellationToken = default)
    {
        _configuration.EnsureUpdatesConfigured();
        using var response = await _httpClient.GetAsync(_configuration.LatestUpdateUri!, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<UpdateInfo>(stream, cancellationToken: cancellationToken);
    }

    public async Task DownloadGameAsync(
        string gameName,
        string destinationDirectory,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _configuration.EnsureCatalogConfigured();
        var gameUri = new Uri(_configuration.GamesBaseUri!, $"{Uri.EscapeDataString(gameName)}/");
        var remoteFiles = new List<RemoteFile>();

        await CollectFilesAsync(gameUri, string.Empty, remoteFiles, cancellationToken);

        if (remoteFiles.Count == 0)
        {
            throw new InvalidOperationException("No files were found in the selected game folder.");
        }

        var totalBytes = remoteFiles.Sum(file => file.Size);
        long completedBytes = 0;

        for (var index = 0; index < remoteFiles.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var remoteFile = remoteFiles[index];
            var targetPath = Path.Combine(destinationDirectory, remoteFile.RelativePath);
            var targetDirectory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrWhiteSpace(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            using var response = await _httpClient.GetAsync(remoteFile.Uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var destination = File.Create(targetPath);

            var buffer = new byte[81920];
            long fileBytesDownloaded = 0;

            while (true)
            {
                var bytesRead = await source.ReadAsync(buffer, cancellationToken);
                if (bytesRead == 0)
                {
                    break;
                }

                await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                fileBytesDownloaded += bytesRead;

                var aggregateBytes = completedBytes + fileBytesDownloaded;
                progress?.Report(new DownloadProgress
                {
                    CurrentFileName = remoteFile.RelativePath,
                    CompletedBytes = aggregateBytes,
                    TotalBytes = totalBytes,
                    CompletedFiles = index,
                    TotalFiles = remoteFiles.Count,
                    Fraction = CalculateFraction(aggregateBytes, totalBytes, index, remoteFiles.Count, fileBytesDownloaded, remoteFile.Size)
                });
            }

            completedBytes += fileBytesDownloaded;
            progress?.Report(new DownloadProgress
            {
                CurrentFileName = remoteFile.RelativePath,
                CompletedBytes = completedBytes,
                TotalBytes = totalBytes,
                CompletedFiles = index + 1,
                TotalFiles = remoteFiles.Count,
                Fraction = CalculateFraction(completedBytes, totalBytes, index + 1, remoteFiles.Count, 0, 0)
            });
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private async Task CollectFilesAsync(
        Uri directoryUri,
        string relativePath,
        ICollection<RemoteFile> files,
        CancellationToken cancellationToken)
    {
        var entries = await ReadDirectoryEntriesAsync(directoryUri, cancellationToken);

        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var entryPath = string.IsNullOrWhiteSpace(relativePath)
                ? entry.Name
                : Path.Combine(relativePath, entry.Name);

            if (entry.IsDirectory)
            {
                await CollectFilesAsync(entry.Uri, entryPath, files, cancellationToken);
                continue;
            }

            files.Add(new RemoteFile
            {
                RelativePath = entryPath,
                Uri = entry.Uri,
                Size = entry.Size ?? 0
            });
        }
    }

    private async Task<IReadOnlyList<RemoteDirectoryEntry>> ReadDirectoryEntriesAsync(Uri directoryUri, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(directoryUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        var entries = new List<RemoteDirectoryEntry>();

        foreach (Match match in IndexEntryRegex.Matches(html))
        {
            var href = match.Groups["href"].Value;
            if (string.IsNullOrWhiteSpace(href) ||
                href.StartsWith("?", StringComparison.Ordinal) ||
                href.StartsWith("/", StringComparison.Ordinal) ||
                href is "../" or "./")
            {
                continue;
            }

            var isDirectory = href.EndsWith("/", StringComparison.Ordinal);
            var decodedName = WebUtility.HtmlDecode(Uri.UnescapeDataString(href.TrimEnd('/')));
            var sizeGroup = match.Groups["size"].Value;
            long? size = long.TryParse(sizeGroup, out var parsedSize) ? parsedSize : null;

            entries.Add(new RemoteDirectoryEntry
            {
                Name = decodedName,
                Href = href,
                Uri = new Uri(directoryUri, href),
                IsDirectory = isDirectory,
                Size = size
            });
        }

        return entries;
    }

    private static double CalculateFraction(
        long completedBytes,
        long totalBytes,
        int completedFiles,
        int totalFiles,
        long currentFileBytes,
        long currentFileTotal)
    {
        if (totalBytes > 0)
        {
            return Math.Clamp((double)completedBytes / totalBytes, 0d, 1d);
        }

        if (totalFiles <= 0)
        {
            return 0d;
        }

        var fileFraction = currentFileTotal > 0
            ? (double)currentFileBytes / currentFileTotal
            : 0d;

        return Math.Clamp((completedFiles + fileFraction) / totalFiles, 0d, 1d);
    }
}
