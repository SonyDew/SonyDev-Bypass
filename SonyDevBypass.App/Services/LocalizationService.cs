using System.IO;
using System.Text.Json;

namespace SonyDevBypass.App.Services;

public sealed class LocalizationService
{
    private static readonly Dictionary<string, Dictionary<string, string>> BuiltInFallbacks =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["title"] = "SonyDev Bypass",
                ["hero_tagline"] = "Modern desktop client for catalog sync and recursive bypass downloads.",
                ["live_catalog_badge"] = "LIVE CATALOG",
                ["build_panel_title"] = "Current build",
                ["update_panel_title"] = "Updates",
                ["catalog_panel_title"] = "Catalog",
                ["directory_panel_title"] = "Destination",
                ["language_panel_title"] = "Language",
                ["status_panel_title"] = "Status",
                ["progress_panel_title"] = "Transfer",
                ["workflow_title"] = "Workflow",
                ["selected_game_label"] = "Selected Game",
                ["start_section_title"] = "Start Bypass",
                ["ready_when_selected"] = "Ready when folder and game are selected",
                ["start_action_hint"] = "Run selected bypass package",
                ["directory_hint"] = "Choose the folder where the selected files should be downloaded.",
                ["search_label"] = "Filter games by name",
                ["refresh_catalog"] = "Refresh list",
                ["loading_games"] = "Loading game catalog...",
                ["games_loaded"] = "Catalog loaded: {count} games.",
                ["games_load_failed"] = "Could not load the game catalog.",
                ["catalog_count"] = "{count} games available",
                ["select_game_first"] = "Select a game first.",
                ["select_folder_first"] = "Select a destination folder first.",
                ["download_confirmation_title"] = "Start download",
                ["download_confirmation_message"] = "Download files for \"{game}\" into:\n{path}",
                ["status_default"] = "Ready for the next action.",
                ["status_downloading"] = "Downloading...",
                ["status_success"] = "Completed successfully.",
                ["status_error"] = "Error: {error}",
                ["download_complete"] = "Download completed successfully.",
                ["check_updates_status"] = "Checking for updates...",
                ["update_available_short"] = "Update available: {version} ({release_date})",
                ["up_to_date_status"] = "You are using the latest build.",
                ["update_check_failed"] = "Could not check for updates.",
                ["download_update_button"] = "Download Update",
                ["restart_update_button"] = "Restart and Update",
                ["download_setup_button"] = "Download Installer",
                ["update_button"] = "Update",
                ["update_now_button"] = "Update Now",
                ["update_ready_restart_status"] = "Update ready: {version}. Restart to apply.",
                ["update_setup_required_status"] = "Update available: {version}. Download the new installer.",
                ["update_download_progress"] = "Downloading update... {progress}%",
                ["update_download_failed"] = "Could not download the update package.",
                ["applying_update_status"] = "Applying update {version}...",
                ["update_apply_failed"] = "Could not apply the downloaded update.",
                ["installer_download_progress"] = "Downloading installer... {progress}%",
                ["installer_started_status"] = "Installer launched. Finish setup to migrate to automatic updates.",
                ["installer_download_failed"] = "Could not download the installer.",
                ["installer_not_published"] = "Installer is not published on the update server yet.",
                ["unknown_release_date"] = "Unknown date",
                ["unknown_version"] = "Unknown version",
                ["update_popup_title"] = "Update Available",
                ["update_popup_subtitle"] = "A newer SonyDev Bypass build is ready.",
                ["update_popup_version_label"] = "Version",
                ["update_popup_date_label"] = "Release date",
                ["update_popup_notes_label"] = "What's new",
                ["update_popup_dismiss_button"] = "Later",
                ["update_popup_no_notes"] = "No release notes were provided for this update.",
                ["update_popup_download_hint"] = "The app will download the update package and prepare it automatically.",
                ["update_popup_installer_hint"] = "This build will download the new setup package and migrate the app to the latest updater flow.",
                ["update_popup_restart_hint"] = "The update package is ready. Restart the app to finish the installation.",
                ["progress_template"] = "{completed} of {total}\n{files_done}/{files_total} files",
                ["current_file_template"] = "Current file: {file}",
                ["footer_note"] = "The app downloads the remote directory structure as-is.",
                ["ready_to_start"] = "Ready to start.",
                ["language_saved"] = "Language changed to {language}.",
                ["download_cancelled"] = "Download cancelled.",
                ["update_available_dialog"] = "New version {version}\nReleased: {release_date}\n\n{changelog}",
                ["no_updates_dialog"] = "No new updates were found.",
                ["check_update_button"] = "Check for Updates",
                ["browse_button"] = "Browse...",
                ["start_button"] = "Start Bypass",
                ["cancel_button"] = "Cancel",
                ["discord_button"] = "Join Discord",
                ["select_language"] = "Select Language",
                ["selected_game"] = "Selected Game",
                ["version_label"] = "Version {version} | Release Date: {release_date}",
                ["warning_title"] = "Warning"
                ,
                ["close_during_download_title"] = "Exit",
                ["close_during_download_message"] = "Stop the download and exit?",
                ["close_during_update_title"] = "Exit",
                ["close_during_update_message"] = "Stop the update and exit?"
            },
            ["ru"] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["title"] = "SonyDev Bypass",
                ["hero_tagline"] = "Современный настольный клиент для синхронизации каталога и рекурсивной загрузки bypass-файлов.",
                ["live_catalog_badge"] = "ЖИВОЙ КАТАЛОГ",
                ["build_panel_title"] = "Текущая сборка",
                ["update_panel_title"] = "Обновления",
                ["catalog_panel_title"] = "Каталог",
                ["directory_panel_title"] = "Папка назначения",
                ["language_panel_title"] = "Язык",
                ["status_panel_title"] = "Статус",
                ["progress_panel_title"] = "Передача",
                ["workflow_title"] = "Порядок работы",
                ["selected_game_label"] = "Выбранная игра",
                ["start_section_title"] = "Начать обход",
                ["ready_when_selected"] = "Готово, когда выбраны папка и игра",
                ["start_action_hint"] = "Запустить выбранный bypass-пакет",
                ["directory_hint"] = "Выберите папку, в которую нужно загрузить файлы для выбранной игры.",
                ["search_label"] = "Фильтр игр по названию",
                ["refresh_catalog"] = "Обновить список",
                ["loading_games"] = "Загрузка каталога игр...",
                ["games_loaded"] = "Каталог загружен: {count} игр.",
                ["games_load_failed"] = "Не удалось загрузить каталог игр.",
                ["catalog_count"] = "Доступно игр: {count}",
                ["select_game_first"] = "Сначала выберите игру.",
                ["select_folder_first"] = "Сначала выберите папку назначения.",
                ["download_confirmation_title"] = "Начать загрузку",
                ["download_confirmation_message"] = "Скачать файлы для \"{game}\" в папку:\n{path}",
                ["status_default"] = "Готово к следующему действию.",
                ["status_downloading"] = "Загрузка...",
                ["status_success"] = "Операция успешно завершена.",
                ["status_error"] = "Ошибка: {error}",
                ["download_complete"] = "Загрузка успешно завершена.",
                ["check_updates_status"] = "Проверка обновлений...",
                ["update_available_short"] = "Доступно обновление: {version} ({release_date})",
                ["up_to_date_status"] = "У вас установлена актуальная сборка.",
                ["update_check_failed"] = "Не удалось проверить обновления.",
                ["download_update_button"] = "Скачать обновление",
                ["restart_update_button"] = "Перезапустить и обновить",
                ["download_setup_button"] = "Скачать установщик",
                ["update_button"] = "Обновить",
                ["update_now_button"] = "Обновить сейчас",
                ["update_ready_restart_status"] = "Обновление {version} готово. Перезапустите приложение для установки.",
                ["update_setup_required_status"] = "Доступно обновление {version}. Скачайте новый установщик.",
                ["update_download_progress"] = "Загрузка обновления... {progress}%",
                ["update_download_failed"] = "Не удалось загрузить пакет обновления.",
                ["applying_update_status"] = "Применение обновления {version}...",
                ["update_apply_failed"] = "Не удалось применить загруженное обновление.",
                ["installer_download_progress"] = "Загрузка установщика... {progress}%",
                ["installer_started_status"] = "Установщик запущен. Завершите установку, чтобы перейти на автообновления.",
                ["installer_download_failed"] = "Не удалось загрузить установщик.",
                ["installer_not_published"] = "Установщик ещё не опубликован на сервере обновлений.",
                ["unknown_release_date"] = "Дата неизвестна",
                ["unknown_version"] = "Неизвестная версия",
                ["update_popup_title"] = "Доступно обновление",
                ["update_popup_subtitle"] = "Для SonyDev Bypass доступна новая сборка.",
                ["update_popup_version_label"] = "Версия",
                ["update_popup_date_label"] = "Дата выпуска",
                ["update_popup_notes_label"] = "Что нового",
                ["update_popup_dismiss_button"] = "Позже",
                ["update_popup_no_notes"] = "Для этого обновления не указаны заметки о релизе.",
                ["update_popup_download_hint"] = "Приложение скачает пакет обновления и подготовит его автоматически.",
                ["update_popup_installer_hint"] = "Эта сборка скачает новый установщик и переведёт приложение на актуальный поток обновлений.",
                ["update_popup_restart_hint"] = "Пакет обновления уже готов. Перезапустите приложение для завершения установки.",
                ["progress_template"] = "{completed} из {total}\nФайлов: {files_done}/{files_total}",
                ["current_file_template"] = "Текущий файл: {file}",
                ["footer_note"] = "Программа сохраняет структуру каталогов сервера без изменений.",
                ["ready_to_start"] = "Готово к запуску.",
                ["language_saved"] = "Язык переключен: {language}.",
                ["download_cancelled"] = "Загрузка отменена.",
                ["update_available_dialog"] = "Новая версия {version}\nДата выхода: {release_date}\n\n{changelog}",
                ["no_updates_dialog"] = "Новых обновлений не найдено.",
                ["check_update_button"] = "Проверить обновления",
                ["browse_button"] = "Обзор...",
                ["start_button"] = "Начать обход",
                ["cancel_button"] = "Отмена",
                ["discord_button"] = "Присоединиться к Discord",
                ["select_language"] = "Выберите язык",
                ["selected_game"] = "Выбранная игра",
                ["version_label"] = "Версия {version} | Дата выпуска: {release_date}",
                ["warning_title"] = "Предупреждение"
                ,
                ["close_during_download_title"] = "Выход",
                ["close_during_download_message"] = "Остановить загрузку и выйти?",
                ["close_during_update_title"] = "Выход",
                ["close_during_update_message"] = "Остановить обновление и выйти?"
            }
        };

    private Dictionary<string, string> _translations = new(StringComparer.OrdinalIgnoreCase);

    public string CurrentLanguage { get; private set; } = "en";

    public async Task SetLanguageAsync(string languageCode, CancellationToken cancellationToken = default)
    {
        var normalizedCode = string.IsNullOrWhiteSpace(languageCode) ? "en" : languageCode;
        var filePath = Path.Combine(AppContext.BaseDirectory, "Resources", "Languages", $"{normalizedCode}.json");

        Dictionary<string, string>? loaded = null;
        if (File.Exists(filePath))
        {
            await using var stream = File.OpenRead(filePath);
            loaded = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream, cancellationToken: cancellationToken);
        }

        CurrentLanguage = loaded is null ? "en" : normalizedCode;
        _translations = loaded ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public string T(string key, IReadOnlyDictionary<string, string>? replacements = null)
    {
        var text = TryGetValue(CurrentLanguage, key)
            ?? TryGetValue("en", key)
            ?? key;

        if (replacements is null)
        {
            return text;
        }

        foreach (var replacement in replacements)
        {
            text = text.Replace($"{{{replacement.Key}}}", replacement.Value, StringComparison.Ordinal);
        }

        return text;
    }

    private string? TryGetValue(string languageCode, string key)
    {
        if (_translations.TryGetValue(key, out var liveValue) &&
            string.Equals(languageCode, CurrentLanguage, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(liveValue))
        {
            return liveValue;
        }

        if (BuiltInFallbacks.TryGetValue(languageCode, out var fallback) &&
            fallback.TryGetValue(key, out var fallbackValue))
        {
            return fallbackValue;
        }

        return null;
    }
}
