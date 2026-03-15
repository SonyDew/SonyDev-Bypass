using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Forms;
using SonyDevBypass.App.Infrastructure;
using SonyDevBypass.App.Models;
using SonyDevBypass.App.Services;
using Velopack;
using AppUpdateInfo = SonyDevBypass.App.Models.UpdateInfo;
using VelopackUpdateInfo = Velopack.UpdateInfo;

namespace SonyDevBypass.App.ViewModels;

public sealed class MainViewModel : ObservableObject, IDisposable
{
    private const string DiscordInviteUrl = "https://discord.gg/6QBqSTQErK";
    private const string InternalVersion = "1.2.4-beta";
    private const string DisplayVersion = "1.2.4 beta";
    private const string ReleaseDate = "2026-03-15";

    private readonly SonyDevApiClient _apiClient;
    private readonly AppUpdateService _appUpdateService;
    private readonly AppSettingsService _settingsService;
    private readonly LocalizationService _localizationService;
    private readonly DialogService _dialogService;

    private readonly AsyncRelayCommand _downloadCommand;
    private readonly RelayCommand _cancelDownloadCommand;
    private readonly RelayCommand _browseFolderCommand;
    private readonly AsyncRelayCommand _refreshGamesCommand;
    private readonly AsyncRelayCommand _checkForUpdatesCommand;
    private readonly AsyncRelayCommand _executeUpdatePromptPrimaryCommand;
    private readonly RelayCommand _openDiscordCommand;
    private readonly RelayCommand _dismissUpdatePromptCommand;

    private AppSettings _settings = new();
    private CancellationTokenSource? _downloadCancellationTokenSource;
    private CancellationTokenSource? _updateCancellationTokenSource;
    private DownloadProgress? _lastDownloadProgress;
    private AppUpdateInfo? _lastUpdateInfo;
    private VelopackUpdateInfo? _availableAppUpdate;
    private string _lastStatusKey = "status_default";
    private IReadOnlyDictionary<string, string>? _lastStatusArgs;
    private string _lastCatalogStatusKey = "loading_games";
    private IReadOnlyDictionary<string, string>? _lastCatalogStatusArgs;
    private string _searchText = string.Empty;
    private string _downloadDirectory = string.Empty;
    private string _statusMessage = string.Empty;
    private string _catalogStatusMessage = string.Empty;
    private string _progressText = string.Empty;
    private string _progressPrimaryText = string.Empty;
    private string _progressSecondaryText = string.Empty;
    private string _currentFileText = string.Empty;
    private string _updateSummary = string.Empty;
    private string _updatePromptVersion = string.Empty;
    private string _updatePromptReleaseDate = string.Empty;
    private string _updatePromptChangelog = string.Empty;
    private string? _selectedGame;
    private LanguageOption? _selectedLanguage;
    private bool _isBusy;
    private bool _isLoadingGames;
    private bool _isInitializing;
    private bool _isUpdateActionBusy;
    private bool _isUpdatePromptVisible;
    private bool _allowImmediateClose;
    private double _progressValue;
    private UpdateActionMode _updateActionMode = UpdateActionMode.Check;

    public MainViewModel(
        SonyDevApiClient apiClient,
        AppUpdateService appUpdateService,
        AppSettingsService settingsService,
        LocalizationService localizationService,
        DialogService dialogService)
    {
        _apiClient = apiClient;
        _appUpdateService = appUpdateService;
        _settingsService = settingsService;
        _localizationService = localizationService;
        _dialogService = dialogService;

        Games = new ObservableCollection<string>();
        FilteredGames = CollectionViewSource.GetDefaultView(Games);
        FilteredGames.Filter = FilterGames;

        Languages = new ObservableCollection<LanguageOption>(CreateLanguages());

        _browseFolderCommand = new RelayCommand(BrowseFolder, () => CanChangeInputs);
        _refreshGamesCommand = new AsyncRelayCommand(LoadGamesAsync, () => CanRefreshGames);
        _downloadCommand = new AsyncRelayCommand(DownloadSelectedGameAsync, () => CanDownload);
        _cancelDownloadCommand = new RelayCommand(CancelDownload, () => CanCancelDownload);
        _checkForUpdatesCommand = new AsyncRelayCommand(HandleUpdateActionAsync, () => !IsBusy && !IsUpdateActionBusy);
        _executeUpdatePromptPrimaryCommand = new AsyncRelayCommand(ExecuteUpdatePromptPrimaryActionAsync, () => !IsBusy && !IsUpdateActionBusy);
        _openDiscordCommand = new RelayCommand(OpenDiscord);
        _dismissUpdatePromptCommand = new RelayCommand(HideUpdatePrompt, () => CanDismissUpdatePrompt);
    }

    public ObservableCollection<string> Games { get; }

    public ICollectionView FilteredGames { get; }

    public ObservableCollection<LanguageOption> Languages { get; }

    public string WindowTitle => T("title");

    public string TitleText => T("title");

    public string HeroTagline => T("hero_tagline");

    public string LiveCatalogBadgeText => T("live_catalog_badge");

    public string BuildPanelTitle => T("build_panel_title");

    public string UpdatePanelTitle => T("update_panel_title");

    public string CatalogPanelTitle => T("catalog_panel_title");

    public string DirectoryPanelTitle => T("directory_panel_title");

    public string LanguagePanelTitle => T("language_panel_title");

    public string StatusPanelTitle => T("status_panel_title");

    public string ProgressPanelTitle => T("progress_panel_title");

    public string WorkflowTitle => T("workflow_title");

    public string SelectedGameLabel => T("selected_game_label");

    public string StartSectionTitle => T("start_section_title");

    public string ReadyWhenSelectedText => T("ready_when_selected");

    public string StartActionHintText => T("start_action_hint");

    public string DirectoryHint => T("directory_hint");

    public string SearchLabelText => T("search_label");

    public string RefreshCatalogText => T("refresh_catalog");

    public string LoadingGamesText => T("loading_games");

    public string CheckForUpdatesText => _updateActionMode switch
    {
        UpdateActionMode.Download => T("update_button"),
        UpdateActionMode.Restart => T("update_button"),
        UpdateActionMode.InstallSetup => T("update_button"),
        _ => T("check_update_button")
    };

    public string BrowseButtonText => T("browse_button");

    public string StartButtonText => T("start_button");

    public string CancelButtonText => T("cancel_button");

    public string DiscordButtonText => T("discord_button");

    public string SelectLanguageText => T("select_language");

    public string FooterNoteText => T("footer_note");

    public string UpdatePromptTitle => T("update_popup_title");

    public string UpdatePromptSubtitle => T("update_popup_subtitle");

    public string UpdatePromptVersionLabel => T("update_popup_version_label");

    public string UpdatePromptDateLabel => T("update_popup_date_label");

    public string UpdatePromptNotesLabel => T("update_popup_notes_label");

    public string UpdatePromptDismissText => T("update_popup_dismiss_button");

    public string UpdatePromptPrimaryButtonText => _updateActionMode switch
    {
        UpdateActionMode.Restart => T("restart_update_button"),
        _ => T("update_now_button")
    };

    public string UpdatePromptActionHintText => _updateActionMode switch
    {
        UpdateActionMode.InstallSetup => T("update_popup_installer_hint"),
        UpdateActionMode.Restart => T("update_popup_restart_hint"),
        _ => T("update_popup_download_hint")
    };

    public string CurrentVersionText => T("version_label", Args(
        ("version", DisplayVersion),
        ("release_date", ReleaseDate)));

    public string GameCountText => T("catalog_count", Args(("count", Games.Count.ToString(CultureInfo.InvariantCulture))));

    public string SelectedGameSummary => !string.IsNullOrWhiteSpace(SelectedGame)
        ? $"{T("selected_game")}: {SelectedGame}"
        : $"{T("selected_game")}: -";

    public string SelectedGameDisplayText => !string.IsNullOrWhiteSpace(SelectedGame)
        ? SelectedGame
        : "-";

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetProperty(ref _searchText, value))
            {
                return;
            }

            FilteredGames.Refresh();
        }
    }

    public string DownloadDirectory
    {
        get => _downloadDirectory;
        set
        {
            if (!SetProperty(ref _downloadDirectory, value))
            {
                return;
            }

            _settings.DownloadDirectory = value;
            OnPropertyChanged(nameof(CanDownload));
            NotifyCommandState();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string CatalogStatusMessage
    {
        get => _catalogStatusMessage;
        private set => SetProperty(ref _catalogStatusMessage, value);
    }

    public string ProgressText
    {
        get => _progressText;
        private set => SetProperty(ref _progressText, value);
    }

    public string ProgressPrimaryText
    {
        get => _progressPrimaryText;
        private set => SetProperty(ref _progressPrimaryText, value);
    }

    public string ProgressSecondaryText
    {
        get => _progressSecondaryText;
        private set => SetProperty(ref _progressSecondaryText, value);
    }

    public string CurrentFileText
    {
        get => _currentFileText;
        private set
        {
            if (SetProperty(ref _currentFileText, value))
            {
                OnPropertyChanged(nameof(HasCurrentFile));
            }
        }
    }

    public bool HasCurrentFile => !string.IsNullOrWhiteSpace(CurrentFileText);

    public string UpdateSummary
    {
        get => _updateSummary;
        private set
        {
            if (SetProperty(ref _updateSummary, value))
            {
                OnPropertyChanged(nameof(UpdatePromptStatusText));
            }
        }
    }

    public bool IsUpdatePromptVisible
    {
        get => _isUpdatePromptVisible;
        private set => SetProperty(ref _isUpdatePromptVisible, value);
    }

    public string UpdatePromptVersion
    {
        get => _updatePromptVersion;
        private set => SetProperty(ref _updatePromptVersion, value);
    }

    public string UpdatePromptReleaseDate
    {
        get => _updatePromptReleaseDate;
        private set => SetProperty(ref _updatePromptReleaseDate, value);
    }

    public string UpdatePromptChangelog
    {
        get => _updatePromptChangelog;
        private set => SetProperty(ref _updatePromptChangelog, value);
    }

    public string UpdatePromptStatusText => UpdateSummary;

    public string? SelectedGame
    {
        get => _selectedGame;
        set
        {
            if (!SetProperty(ref _selectedGame, value))
            {
                return;
            }

            _settings.LastSelectedGame = value;
            OnPropertyChanged(nameof(SelectedGameSummary));
            OnPropertyChanged(nameof(SelectedGameDisplayText));
            OnPropertyChanged(nameof(CanDownload));
            NotifyCommandState();
        }
    }

    public LanguageOption? SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (!SetProperty(ref _selectedLanguage, value))
            {
                return;
            }

            if (_isInitializing || value is null)
            {
                return;
            }

            _ = ApplyLanguageAsync(value.Code, value.DisplayName, true);
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value))
            {
                return;
            }

            OnPropertyChanged(nameof(CanChangeInputs));
            OnPropertyChanged(nameof(CanRefreshGames));
            OnPropertyChanged(nameof(CanDownload));
            OnPropertyChanged(nameof(CanCancelDownload));
            NotifyCommandState();
        }
    }

    public bool IsLoadingGames
    {
        get => _isLoadingGames;
        private set
        {
            if (!SetProperty(ref _isLoadingGames, value))
            {
                return;
            }

            OnPropertyChanged(nameof(CanRefreshGames));
            OnPropertyChanged(nameof(CanDownload));
            NotifyCommandState();
        }
    }

    public double ProgressValue
    {
        get => _progressValue;
        private set => SetProperty(ref _progressValue, value);
    }

    public bool CanChangeInputs => !IsBusy && !IsUpdateActionBusy;

    public bool CanRefreshGames => !IsBusy && !IsLoadingGames && !IsUpdateActionBusy;

    public bool CanDownload =>
        !IsBusy &&
        !IsLoadingGames &&
        !IsUpdateActionBusy &&
        !string.IsNullOrWhiteSpace(SelectedGame) &&
        !string.IsNullOrWhiteSpace(DownloadDirectory);

    public bool CanCancelDownload => IsBusy;

    public bool CanDismissUpdatePrompt => !IsUpdateActionBusy;

    public AsyncRelayCommand DownloadCommand => _downloadCommand;

    public RelayCommand CancelDownloadCommand => _cancelDownloadCommand;

    public RelayCommand BrowseFolderCommand => _browseFolderCommand;

    public AsyncRelayCommand RefreshGamesCommand => _refreshGamesCommand;

    public AsyncRelayCommand CheckForUpdatesCommand => _checkForUpdatesCommand;

    public AsyncRelayCommand ExecuteUpdatePromptPrimaryCommand => _executeUpdatePromptPrimaryCommand;

    public RelayCommand OpenDiscordCommand => _openDiscordCommand;

    public RelayCommand DismissUpdatePromptCommand => _dismissUpdatePromptCommand;

    public bool ConfirmClose()
    {
        if (_allowImmediateClose)
        {
            return true;
        }

        if (CanCancelDownload)
        {
            var confirmedDownloadClose = _dialogService.Confirm(
                T("close_during_download_title"),
                T("close_during_download_message"));

            if (confirmedDownloadClose)
            {
                _downloadCancellationTokenSource?.Cancel();
            }

            return confirmedDownloadClose;
        }

        if (!IsUpdateActionBusy)
        {
            return true;
        }

        var confirmedUpdateClose = _dialogService.Confirm(
            T("close_during_update_title"),
            T("close_during_update_message"));

        if (confirmedUpdateClose)
        {
            _updateCancellationTokenSource?.Cancel();
        }

        return confirmedUpdateClose;
    }

    public async Task InitializeAsync()
    {
        _isInitializing = true;
        _settings = await _settingsService.LoadAsync();
        await ApplyLanguageAsync(_settings.Language, persist: false);

        DownloadDirectory = _settings.DownloadDirectory;
        SelectedLanguage = Languages.FirstOrDefault(language => language.Code == _localizationService.CurrentLanguage)
            ?? Languages.FirstOrDefault(language => language.Code == "en");

        _isInitializing = false;

        SetStatus("status_default");
        SetCatalogStatus("loading_games");
        ResetProgressPresentation();
        UpdateSummary = T("check_updates_status");

        await LoadGamesAsync();

        if (_settings.AutoCheckUpdates)
        {
            await CheckForUpdatesAsync(true);
        }
        else
        {
            UpdateSummary = T("up_to_date_status");
        }
    }

    public void Dispose()
    {
        _downloadCancellationTokenSource?.Cancel();
        _downloadCancellationTokenSource?.Dispose();
        _updateCancellationTokenSource?.Cancel();
        _updateCancellationTokenSource?.Dispose();
        _apiClient.Dispose();
    }

    private async Task LoadGamesAsync()
    {
        if (IsLoadingGames)
        {
            return;
        }

        IsLoadingGames = true;
        SetCatalogStatus("loading_games");

        try
        {
            if (!_apiClient.HasCatalogEndpoint)
            {
                SetCatalogStatus("games_load_failed");
                SetStatus("status_error", Args(("error", _apiClient.CatalogConfigurationMessage)));
                UpdateSummary = _apiClient.CatalogConfigurationMessage;
                return;
            }

            var gameNames = await _apiClient.GetGameNamesAsync();
            Games.Clear();

            foreach (var gameName in gameNames)
            {
                Games.Add(gameName);
            }

            SelectedGame = SelectPreferredGame(gameNames);
            OnPropertyChanged(nameof(GameCountText));
            SetCatalogStatus("games_loaded", Args(("count", Games.Count.ToString(CultureInfo.InvariantCulture))));
        }
        catch (Exception ex)
        {
            SetCatalogStatus("games_load_failed");
            SetStatus("status_error", Args(("error", ex.Message)));
            UpdateSummary = T("games_load_failed");
        }
        finally
        {
            IsLoadingGames = false;
        }
    }

    private async Task DownloadSelectedGameAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedGame))
        {
            SetStatus("status_error", Args(("error", T("select_game_first"))));
            return;
        }

        if (string.IsNullOrWhiteSpace(DownloadDirectory))
        {
            SetStatus("status_error", Args(("error", T("select_folder_first"))));
            return;
        }

        var confirmed = _dialogService.Confirm(
            T("download_confirmation_title"),
            T("download_confirmation_message", Args(
                ("game", SelectedGame),
                ("path", DownloadDirectory))));

        if (!confirmed)
        {
            return;
        }

        _downloadCancellationTokenSource?.Dispose();
        _downloadCancellationTokenSource = new CancellationTokenSource();

        IsBusy = true;
        SetStatus("status_downloading");
        ResetProgressPresentation();

        var progress = new Progress<DownloadProgress>(ApplyProgress);

        try
        {
            await _apiClient.DownloadGameAsync(
                SelectedGame,
                DownloadDirectory,
                progress,
                _downloadCancellationTokenSource.Token);

            SetStatus("status_success");
            ProgressValue = 1d;
            ProgressText = T("download_complete");
            await PersistSettingsAsync();
        }
        catch (OperationCanceledException)
        {
            SetStatus("status_cancelled");
            ProgressText = T("download_cancelled");
        }
        catch (Exception ex)
        {
            SetStatus("status_error", Args(("error", ex.Message)));
        }
        finally
        {
            IsBusy = false;
            _downloadCancellationTokenSource?.Dispose();
            _downloadCancellationTokenSource = null;
        }
    }

    private void ApplyProgress(DownloadProgress progress)
    {
        _lastDownloadProgress = progress;
        ProgressValue = progress.Fraction;

        var totalBytes = progress.TotalBytes > 0 ? FormatBytes(progress.TotalBytes) : "?";
        ProgressText = T("progress_template", Args(
            ("completed", FormatBytes(progress.CompletedBytes)),
            ("total", totalBytes),
            ("files_done", progress.CompletedFiles.ToString(CultureInfo.InvariantCulture)),
            ("files_total", progress.TotalFiles.ToString(CultureInfo.InvariantCulture))));
        ApplyProgressLines(ProgressText);

        CurrentFileText = T("current_file_template", Args(("file", progress.CurrentFileName)));
    }

    private async Task HandleUpdateActionAsync()
    {
        if (_updateActionMode is not UpdateActionMode.Check)
        {
            ShowUpdatePromptForCurrentState();
            return;
        }

        await CheckForUpdatesAsync(false);
    }

    private async Task ExecuteUpdatePromptPrimaryActionAsync()
    {
        switch (_updateActionMode)
        {
            case UpdateActionMode.Download:
                await DownloadAvailableAppUpdateAsync();
                break;
            case UpdateActionMode.Restart:
                await RestartToApplyUpdateAsync();
                break;
            case UpdateActionMode.InstallSetup:
                await DownloadAndLaunchInstallerAsync();
                break;
            default:
                await CheckForUpdatesAsync(false);
                break;
        }
    }

    private async Task CheckForUpdatesAsync(bool silent)
    {
        if (IsUpdateActionBusy)
        {
            return;
        }

        IsUpdateActionBusy = true;

        try
        {
            if (!_apiClient.HasUpdateEndpoint && !_appUpdateService.HasUpdateEndpoint)
            {
                SetUpdateActionMode(UpdateActionMode.Check);
                UpdateSummary = _apiClient.UpdateConfigurationMessage;
                HideUpdatePrompt();
                return;
            }

            _availableAppUpdate = null;
            SetUpdateActionMode(UpdateActionMode.Check);
            UpdateSummary = T("check_updates_status");

            AppUpdateInfo? latestInfo = null;
            Exception? latestInfoError = null;

            try
            {
                latestInfo = await _apiClient.GetLatestUpdateAsync();
                _lastUpdateInfo = latestInfo;
            }
            catch (Exception ex)
            {
                latestInfoError = ex;
                _lastUpdateInfo = null;
            }

            Exception? packageUpdateError = null;

            if (_appUpdateService.IsInstalled)
            {
                try
                {
                    _availableAppUpdate = await _appUpdateService.CheckForUpdatesAsync();
                }
                catch (Exception ex)
                {
                    packageUpdateError = ex;
                }
            }

            var pendingRestart = _appUpdateService.PendingRestartUpdate;
            if (_appUpdateService.RequiresInstallerMigration)
            {
                var migrationVersion = pendingRestart is not null
                    ? GetReleaseVersion(pendingRestart)
                    : _availableAppUpdate is not null
                        ? GetReleaseVersion(_availableAppUpdate.TargetFullRelease)
                        : latestInfo?.Version;

                var hasMigrationCandidate =
                    pendingRestart is not null ||
                    _availableAppUpdate is not null ||
                    (latestInfo is not null && HasAvailableUpdate(latestInfo, GetCurrentBuildVersion()));

                if (hasMigrationCandidate)
                {
                    ShowInstallerMigrationPrompt(latestInfo, migrationVersion, latestInfo?.ReleaseDate);
                    return;
                }
            }

            if (pendingRestart is not null)
            {
                SetUpdateActionMode(UpdateActionMode.Restart);
                UpdateSummary = T("update_ready_restart_status", Args(("version", GetReleaseVersion(pendingRestart))));
                ShowUpdatePrompt(latestInfo, GetReleaseVersion(pendingRestart));
                return;
            }

            if (_availableAppUpdate is not null)
            {
                SetUpdateActionMode(UpdateActionMode.Download);
                UpdateSummary = T("update_available_short", Args(
                    ("version", GetReleaseVersion(_availableAppUpdate.TargetFullRelease)),
                    ("release_date", latestInfo?.ReleaseDate ?? T("unknown_release_date"))));
                ShowUpdatePrompt(latestInfo, GetReleaseVersion(_availableAppUpdate.TargetFullRelease));
                return;
            }

            if (latestInfo is not null && HasAvailableUpdate(latestInfo, GetCurrentBuildVersion()))
            {
                SetUpdateActionMode(UpdateActionMode.InstallSetup);
                UpdateSummary = T("update_setup_required_status", Args(
                    ("version", latestInfo.Version),
                    ("release_date", latestInfo.ReleaseDate)));
                ShowUpdatePrompt(latestInfo);
                return;
            }

            var anyCheckSucceeded = latestInfo is not null || (_appUpdateService.IsInstalled && packageUpdateError is null);
            var configurationMessage = TryGetConfigurationMessage(latestInfoError, packageUpdateError);
            SetUpdateActionMode(UpdateActionMode.Check);
            UpdateSummary = !string.IsNullOrWhiteSpace(configurationMessage)
                ? configurationMessage
                : anyCheckSucceeded || silent
                    ? T("up_to_date_status")
                    : T("update_check_failed");
            HideUpdatePrompt();
        }
        catch (Exception ex)
        {
            SetUpdateActionMode(UpdateActionMode.Check);
            UpdateSummary = ex is InvalidOperationException ? ex.Message : T("update_check_failed");
            HideUpdatePrompt();
        }
        finally
        {
            IsUpdateActionBusy = false;
        }
    }

    private async Task DownloadAvailableAppUpdateAsync()
    {
        if (_availableAppUpdate is null)
        {
            await CheckForUpdatesAsync(false);
            return;
        }

        _updateCancellationTokenSource?.Dispose();
        _updateCancellationTokenSource = new CancellationTokenSource();
        IsUpdateActionBusy = true;

        try
        {
            UpdateSummary = T("update_download_progress", Args(("progress", "0")));

            await _appUpdateService.DownloadUpdateAsync(
                _availableAppUpdate,
                progress => RunOnUiThread(() =>
                    UpdateSummary = T("update_download_progress", Args(("progress", progress.ToString(CultureInfo.InvariantCulture))))),
                _updateCancellationTokenSource.Token);

            var pendingRestart = _appUpdateService.PendingRestartUpdate ?? _availableAppUpdate.TargetFullRelease;
            SetUpdateActionMode(UpdateActionMode.Restart);
            UpdateSummary = T("update_ready_restart_status", Args(("version", GetReleaseVersion(pendingRestart))));
            ShowUpdatePrompt(_lastUpdateInfo, GetReleaseVersion(pendingRestart));
        }
        catch (OperationCanceledException)
        {
            RefreshUpdatePresentation();
        }
        catch (Exception)
        {
            UpdateSummary = T("update_download_failed");
        }
        finally
        {
            _updateCancellationTokenSource?.Dispose();
            _updateCancellationTokenSource = null;
            IsUpdateActionBusy = false;
        }
    }

    private async Task RestartToApplyUpdateAsync()
    {
        var pendingRestart = _appUpdateService.PendingRestartUpdate;
        if (pendingRestart is null)
        {
            await CheckForUpdatesAsync(false);
            return;
        }

        IsUpdateActionBusy = true;

        try
        {
            UpdateSummary = T("applying_update_status", Args(("version", GetReleaseVersion(pendingRestart))));
            await PersistSettingsAsync();
            await _appUpdateService.PrepareApplyAndRestartAsync(pendingRestart);
            _allowImmediateClose = true;
            System.Windows.Application.Current?.Shutdown();
        }
        catch (Exception)
        {
            _allowImmediateClose = false;
            UpdateSummary = T("update_apply_failed");
        }
        finally
        {
            IsUpdateActionBusy = false;
        }
    }

    private async Task DownloadAndLaunchInstallerAsync()
    {
        _updateCancellationTokenSource?.Dispose();
        _updateCancellationTokenSource = new CancellationTokenSource();
        IsUpdateActionBusy = true;

        try
        {
            if (!_appUpdateService.HasUpdateEndpoint)
            {
                UpdateSummary = _apiClient.UpdateConfigurationMessage;
                return;
            }

            UpdateSummary = T("installer_download_progress", Args(("progress", "0")));

            var installerPath = await _appUpdateService.DownloadInstallerAsync(
                progress => RunOnUiThread(() =>
                    UpdateSummary = T("installer_download_progress", Args(("progress", progress.ToString(CultureInfo.InvariantCulture))))),
                _updateCancellationTokenSource.Token);

            _appUpdateService.LaunchInstaller(installerPath);
            UpdateSummary = T("installer_started_status");
            HideUpdatePrompt();
        }
        catch (OperationCanceledException)
        {
            RefreshUpdatePresentation();
        }
        catch (Exception ex)
        {
            UpdateSummary = IsMissingRemoteResource(ex)
                ? T("installer_not_published")
                : T("installer_download_failed");
        }
        finally
        {
            _updateCancellationTokenSource?.Dispose();
            _updateCancellationTokenSource = null;
            IsUpdateActionBusy = false;
        }
    }

    private static string? TryGetConfigurationMessage(params Exception?[] exceptions)
    {
        return exceptions
            .OfType<InvalidOperationException>()
            .Select(exception => exception.Message)
            .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message));
    }

    private async Task ApplyLanguageAsync(string languageCode, string? displayName = null, bool persist = true)
    {
        await _localizationService.SetLanguageAsync(languageCode);
        RaiseLocalizedPropertyChanges();
        ReapplyStatus();
        ReapplyCatalogStatus();
        RefreshProgressPresentation();
        RefreshUpdatePresentation();

        if (persist)
        {
            _settings.Language = _localizationService.CurrentLanguage;
            await PersistSettingsAsync();
            SetStatus("language_saved", Args(("language", displayName ?? languageCode)));
        }
    }

    private async Task PersistSettingsAsync()
    {
        _settings.DownloadDirectory = DownloadDirectory;
        _settings.LastSelectedGame = SelectedGame;
        await _settingsService.SaveAsync(_settings);
    }

    private string? SelectPreferredGame(IEnumerable<string> gameNames)
    {
        if (!string.IsNullOrWhiteSpace(_settings.LastSelectedGame) &&
            gameNames.Contains(_settings.LastSelectedGame, StringComparer.OrdinalIgnoreCase))
        {
            return gameNames.First(game => string.Equals(game, _settings.LastSelectedGame, StringComparison.OrdinalIgnoreCase));
        }

        return gameNames.FirstOrDefault();
    }

    private void BrowseFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            SelectedPath = string.IsNullOrWhiteSpace(DownloadDirectory)
                ? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
                : DownloadDirectory,
            ShowNewFolderButton = true
        };

        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        DownloadDirectory = dialog.SelectedPath;
        _ = PersistSettingsAsync();
    }

    private void CancelDownload()
    {
        _downloadCancellationTokenSource?.Cancel();
    }

    private void OpenDiscord()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = DiscordInviteUrl,
            UseShellExecute = true
        });
    }

    private void ShowUpdatePrompt(AppUpdateInfo? updateInfo, string? fallbackVersion = null, string? fallbackDate = null)
    {
        var resolvedVersion = !string.IsNullOrWhiteSpace(updateInfo?.Version)
            ? updateInfo.Version
            : fallbackVersion ?? T("unknown_version");
        var resolvedDate = !string.IsNullOrWhiteSpace(updateInfo?.ReleaseDate)
            ? updateInfo.ReleaseDate
            : fallbackDate ?? T("unknown_release_date");

        UpdatePromptVersion = resolvedVersion;
        UpdatePromptReleaseDate = resolvedDate;
        UpdatePromptChangelog = FormatUpdateChangelog(updateInfo?.Changelog);
        IsUpdatePromptVisible = true;
    }

    private void ShowInstallerMigrationPrompt(AppUpdateInfo? updateInfo, string? fallbackVersion = null, string? fallbackDate = null)
    {
        var resolvedVersion = !string.IsNullOrWhiteSpace(updateInfo?.Version)
            ? updateInfo.Version
            : fallbackVersion ?? T("unknown_version");
        var resolvedDate = !string.IsNullOrWhiteSpace(updateInfo?.ReleaseDate)
            ? updateInfo.ReleaseDate
            : fallbackDate ?? T("unknown_release_date");

        SetUpdateActionMode(UpdateActionMode.InstallSetup);
        UpdateSummary = T("update_setup_required_status", Args(
            ("version", resolvedVersion),
            ("release_date", resolvedDate)));
        ShowUpdatePrompt(updateInfo, resolvedVersion, resolvedDate);
    }

    private void ShowUpdatePromptForCurrentState()
    {
        switch (_updateActionMode)
        {
            case UpdateActionMode.Download when _availableAppUpdate is not null:
                ShowUpdatePrompt(_lastUpdateInfo, GetReleaseVersion(_availableAppUpdate.TargetFullRelease));
                break;

            case UpdateActionMode.Restart:
                var pendingRestart = _appUpdateService.PendingRestartUpdate;
                if (pendingRestart is not null)
                {
                    ShowUpdatePrompt(_lastUpdateInfo, GetReleaseVersion(pendingRestart));
                }
                break;

            case UpdateActionMode.InstallSetup:
                ShowUpdatePrompt(_lastUpdateInfo, UpdatePromptVersion, UpdatePromptReleaseDate);
                break;
        }
    }

    private void HideUpdatePrompt()
    {
        if (IsUpdateActionBusy)
        {
            return;
        }

        IsUpdatePromptVisible = false;
    }

    private void NotifyCommandState()
    {
        _downloadCommand.RaiseCanExecuteChanged();
        _cancelDownloadCommand.RaiseCanExecuteChanged();
        _browseFolderCommand.RaiseCanExecuteChanged();
        _refreshGamesCommand.RaiseCanExecuteChanged();
        _checkForUpdatesCommand.RaiseCanExecuteChanged();
        _executeUpdatePromptPrimaryCommand.RaiseCanExecuteChanged();
        _dismissUpdatePromptCommand.RaiseCanExecuteChanged();
    }

    private void SetStatus(string key, IReadOnlyDictionary<string, string>? replacements = null)
    {
        _lastStatusKey = key;
        _lastStatusArgs = replacements;
        StatusMessage = T(key, replacements);
    }

    private void SetCatalogStatus(string key, IReadOnlyDictionary<string, string>? replacements = null)
    {
        _lastCatalogStatusKey = key;
        _lastCatalogStatusArgs = replacements;
        CatalogStatusMessage = T(key, replacements);
    }

    private void ReapplyStatus()
    {
        StatusMessage = T(_lastStatusKey, _lastStatusArgs);
    }

    private void ReapplyCatalogStatus()
    {
        CatalogStatusMessage = T(_lastCatalogStatusKey, _lastCatalogStatusArgs);
    }

    private void ResetProgressPresentation()
    {
        _lastDownloadProgress = null;
        ProgressValue = 0d;
        ProgressText = T("ready_to_start");
        ApplyProgressLines(ProgressText);
        CurrentFileText = string.Empty;
    }

    private void RefreshProgressPresentation()
    {
        if (_lastDownloadProgress is null)
        {
            ProgressText = T("ready_to_start");
            ApplyProgressLines(ProgressText);
            CurrentFileText = string.Empty;
            return;
        }

        ApplyProgress(_lastDownloadProgress);
    }

    private void RefreshUpdatePresentation()
    {
        switch (_updateActionMode)
        {
            case UpdateActionMode.Download when _availableAppUpdate is not null:
                UpdateSummary = T("update_available_short", Args(
                    ("version", GetReleaseVersion(_availableAppUpdate.TargetFullRelease)),
                    ("release_date", _lastUpdateInfo?.ReleaseDate ?? T("unknown_release_date"))));
                break;

            case UpdateActionMode.Restart:
                var pendingRestart = _appUpdateService.PendingRestartUpdate;
                if (pendingRestart is null)
                {
                    SetUpdateActionMode(UpdateActionMode.Check);
                    UpdateSummary = T("up_to_date_status");
                    break;
                }

                UpdateSummary = T("update_ready_restart_status", Args(("version", GetReleaseVersion(pendingRestart))));
                break;

            case UpdateActionMode.InstallSetup when _lastUpdateInfo is not null:
                UpdateSummary = T("update_setup_required_status", Args(
                    ("version", _lastUpdateInfo.Version),
                    ("release_date", _lastUpdateInfo.ReleaseDate)));
                break;

            case UpdateActionMode.InstallSetup:
                UpdateSummary = T("update_setup_required_status", Args(
                    ("version", string.IsNullOrWhiteSpace(UpdatePromptVersion) ? T("unknown_version") : UpdatePromptVersion),
                    ("release_date", string.IsNullOrWhiteSpace(UpdatePromptReleaseDate) ? T("unknown_release_date") : UpdatePromptReleaseDate)));
                break;

            default:
                UpdateSummary = _lastUpdateInfo is not null && HasAvailableUpdate(_lastUpdateInfo, GetCurrentBuildVersion())
                    ? T("update_setup_required_status", Args(
                        ("version", _lastUpdateInfo.Version),
                        ("release_date", _lastUpdateInfo.ReleaseDate)))
                    : T("up_to_date_status");
                break;
        }
    }

    private void RaiseLocalizedPropertyChanges()
    {
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(TitleText));
        OnPropertyChanged(nameof(HeroTagline));
        OnPropertyChanged(nameof(LiveCatalogBadgeText));
        OnPropertyChanged(nameof(BuildPanelTitle));
        OnPropertyChanged(nameof(UpdatePanelTitle));
        OnPropertyChanged(nameof(CatalogPanelTitle));
        OnPropertyChanged(nameof(DirectoryPanelTitle));
        OnPropertyChanged(nameof(LanguagePanelTitle));
        OnPropertyChanged(nameof(StatusPanelTitle));
        OnPropertyChanged(nameof(ProgressPanelTitle));
        OnPropertyChanged(nameof(WorkflowTitle));
        OnPropertyChanged(nameof(SelectedGameLabel));
        OnPropertyChanged(nameof(StartSectionTitle));
        OnPropertyChanged(nameof(ReadyWhenSelectedText));
        OnPropertyChanged(nameof(StartActionHintText));
        OnPropertyChanged(nameof(DirectoryHint));
        OnPropertyChanged(nameof(SearchLabelText));
        OnPropertyChanged(nameof(RefreshCatalogText));
        OnPropertyChanged(nameof(LoadingGamesText));
        OnPropertyChanged(nameof(CheckForUpdatesText));
        OnPropertyChanged(nameof(BrowseButtonText));
        OnPropertyChanged(nameof(StartButtonText));
        OnPropertyChanged(nameof(CancelButtonText));
        OnPropertyChanged(nameof(DiscordButtonText));
        OnPropertyChanged(nameof(SelectLanguageText));
        OnPropertyChanged(nameof(FooterNoteText));
        OnPropertyChanged(nameof(CurrentVersionText));
        OnPropertyChanged(nameof(GameCountText));
        OnPropertyChanged(nameof(SelectedGameSummary));
        OnPropertyChanged(nameof(SelectedGameDisplayText));
        OnPropertyChanged(nameof(UpdatePromptTitle));
        OnPropertyChanged(nameof(UpdatePromptSubtitle));
        OnPropertyChanged(nameof(UpdatePromptVersionLabel));
        OnPropertyChanged(nameof(UpdatePromptDateLabel));
        OnPropertyChanged(nameof(UpdatePromptNotesLabel));
        OnPropertyChanged(nameof(UpdatePromptDismissText));
        OnPropertyChanged(nameof(UpdatePromptPrimaryButtonText));
        OnPropertyChanged(nameof(UpdatePromptActionHintText));
        OnPropertyChanged(nameof(UpdatePromptStatusText));
    }

    private bool FilterGames(object obj)
    {
        if (obj is not string gameName)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        return gameName.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase);
    }

    private string T(string key, IReadOnlyDictionary<string, string>? replacements = null)
    {
        return _localizationService.T(key, replacements);
    }

    private void ApplyProgressLines(string text)
    {
        var lines = text.Split(["\r\n", "\n"], StringSplitOptions.None);
        ProgressPrimaryText = lines.Length > 0 ? lines[0] : string.Empty;
        ProgressSecondaryText = lines.Length > 1 ? lines[1] : string.Empty;
    }

    private string FormatUpdateChangelog(string? changelog)
    {
        if (string.IsNullOrWhiteSpace(changelog))
        {
            return T("update_popup_no_notes");
        }

        return changelog.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\n", Environment.NewLine, StringComparison.Ordinal)
            .Trim();
    }

    private void SetUpdateActionMode(UpdateActionMode mode)
    {
        if (_updateActionMode == mode)
        {
            return;
        }

        _updateActionMode = mode;
        OnPropertyChanged(nameof(CheckForUpdatesText));
        OnPropertyChanged(nameof(UpdatePromptPrimaryButtonText));
        OnPropertyChanged(nameof(UpdatePromptActionHintText));
    }

    private string GetCurrentBuildVersion()
    {
        return _appUpdateService.CurrentInstalledVersion ?? InternalVersion;
    }

    private static string GetReleaseVersion(VelopackAsset asset)
    {
        return Convert.ToString(asset.Version, CultureInfo.InvariantCulture)
            ?? asset.FileName
            ?? "unknown";
    }

    private void RunOnUiThread(Action action)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;

        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.Invoke(action);
    }

    private static bool IsMissingRemoteResource(Exception exception)
    {
        return exception.ToString().Contains("404", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsUpdateActionBusy
    {
        get => _isUpdateActionBusy;
        set
        {
            if (_isUpdateActionBusy == value)
            {
                return;
            }

            _isUpdateActionBusy = value;
            OnPropertyChanged(nameof(CanChangeInputs));
            OnPropertyChanged(nameof(CanRefreshGames));
            OnPropertyChanged(nameof(CanDownload));
            OnPropertyChanged(nameof(CanDismissUpdatePrompt));
            NotifyCommandState();
        }
    }

    private static Dictionary<string, string> Args(params (string Key, string Value)[] replacements)
    {
        return replacements.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static bool HasAvailableUpdate(AppUpdateInfo? updateInfo, string currentVersion)
    {
        if (updateInfo is null || string.IsNullOrWhiteSpace(updateInfo.Version))
        {
            return false;
        }

        return !string.Equals(NormalizeVersion(updateInfo.Version), NormalizeVersion(currentVersion), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeVersion(string version)
    {
        return string.Concat(version.Where(char.IsLetterOrDigit)).ToLowerInvariant();
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        double value = bytes;
        var unitIndex = 0;

        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return $"{value:0.##} {units[unitIndex]}";
    }

    private static IEnumerable<LanguageOption> CreateLanguages()
    {
        return
        [
            new("en", "English"),
            new("ru", "Russian"),
            new("ua", "Ukrainian"),
            new("es", "Spanish"),
            new("zh", "Chinese"),
            new("fr", "French"),
            new("de", "German"),
            new("it", "Italian"),
            new("pt", "Portuguese"),
            new("ja", "Japanese"),
            new("ko", "Korean"),
            new("pl", "Polish"),
            new("tr", "Turkish"),
            new("ar", "Arabic")
        ];
    }

    private enum UpdateActionMode
    {
        Check,
        Download,
        Restart,
        InstallSetup
    }
}
