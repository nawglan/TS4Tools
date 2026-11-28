namespace TS4Tools.UI.Services;

/// <summary>
/// Service for managing application settings.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/Properties/Settings.settings
/// </remarks>
public sealed class SettingsService
{
    private static readonly string DefaultSettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TS4Tools", "settings.json");

    // The settings file this instance reads and writes (the user's file, or a test's).
    private readonly string _settingsPath;

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static SettingsService? _instance;
    private AppSettings _settings;
    // Process-wide: there is one settings file. Static, so it lives as long as the process (no Dispose needed).
    private static readonly SemaphoreSlim SaveLock = new(1, 1);

    /// <summary>
    /// Gets the settings service. Returns defaults until <see cref="InitializeAsync"/> has loaded
    /// the persisted settings, so accessing it never performs blocking file I/O.
    /// </summary>
    public static SettingsService Instance => _instance ??= new SettingsService(DefaultSettingsPath, new AppSettings());

    public AppSettings Settings => _settings;

    private SettingsService(string settingsPath, AppSettings settings)
    {
        _settingsPath = settingsPath;
        _settings = settings;
    }

    /// <summary>
    /// Creates a service over <paramref name="settingsPath"/> (not the user's settings file), so tests can exercise
    /// loading and saving.
    /// </summary>
    internal static SettingsService CreateForTesting(string settingsPath, AppSettings? settings = null) =>
        new(settingsPath, settings ?? new AppSettings());

    /// <summary>Loads the settings file of a test instance (see <see cref="CreateForTesting"/>).</summary>
    internal async Task LoadForTestingAsync(CancellationToken cancellationToken = default)
    {
        RemoveStaleTempFiles(_settingsPath);
        _settings = await LoadSettingsAsync(_settingsPath, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Loads persisted settings asynchronously and installs them as the current instance.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Properties/Settings.settings (loaded once at startup)
    /// </remarks>
    public static async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        RemoveStaleTempFiles(DefaultSettingsPath);
        var settings = await LoadSettingsAsync(DefaultSettingsPath, cancellationToken).ConfigureAwait(false);
        if (_instance == null)
        {
            _instance = new SettingsService(DefaultSettingsPath, settings);
        }
        else
        {
            _instance._settings = settings;
        }
    }

    /// <summary>
    /// Deletes temp files left by saves that were interrupted (process killed between writing and moving), which
    /// would otherwise accumulate because every save uses its own temp name.
    /// </summary>
    private static void RemoveStaleTempFiles(string settingsPath)
    {
        string? dir = Path.GetDirectoryName(settingsPath);
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return;
        try
        {
            foreach (var stale in Directory.EnumerateFiles(dir, Path.GetFileName(settingsPath) + ".tmp-*"))
            {
                try { File.Delete(stale); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { /* in use or locked */ }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Cannot list the folder: nothing to clean up that we could delete anyway.
        }
    }

    private static async Task<AppSettings> LoadSettingsAsync(string settingsPath, CancellationToken cancellationToken)
    {
        try
        {
            if (File.Exists(settingsPath))
            {
                await using var stream = new FileStream(settingsPath, FileMode.Open, FileAccess.Read,
                    FileShare.Read, 4096, FileOptions.Asynchronous);
                var settings = await System.Text.Json.JsonSerializer
                    .DeserializeAsync<AppSettings>(stream, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                return settings ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TS4Tools] Failed to load settings: {ex.Message}");
        }

        return new AppSettings();
    }

    /// <summary>
    /// Raised (possibly on a background thread) when <see cref="SaveAsync"/> could not write the settings file, so the
    /// app can tell the user instead of losing their changes silently.
    /// </summary>
    public event EventHandler<Exception>? SaveFailed;

    /// <summary>
    /// Persists the current settings asynchronously.
    /// </summary>
    /// <remarks>
    /// The file is written to a temporary file next to it and moved into place, so a crash or full disk during the
    /// write never leaves a truncated settings.json (which would reset every setting on the next start).
    /// </remarks>
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        // Saves can overlap (several UI handlers and the background update check call this without coordinating).
        // Run them one at a time, each with its own temp file, so they cannot collide and the last one wins.
        // Snapshot the settings on the caller's thread (the UI thread, which is the only one that changes them),
        // before waiting: after the wait this method may continue on a thread-pool thread while the UI keeps editing
        // the lists. The lock hands out turns in call order, so the newest snapshot is still written last.
        string json;
        try
        {
            json = System.Text.Json.JsonSerializer.Serialize(_settings, JsonOptions);
        }
        catch (NotSupportedException ex)
        {
            SaveFailed?.Invoke(this, ex);
            return;
        }
        await SaveLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        string tempPath = _settingsPath + ".tmp-" + Guid.NewGuid().ToString("N")[..8];
        try
        {
            var dir = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            await File.WriteAllTextAsync(tempPath, json, cancellationToken).ConfigureAwait(false);
            File.Move(tempPath, _settingsPath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            System.Diagnostics.Debug.WriteLine($"[TS4Tools] Failed to save settings: {ex.Message}");
            SaveFailed?.Invoke(this, ex);
        }
        finally
        {
            try { if (File.Exists(tempPath)) File.Delete(tempPath); }
            catch (Exception cleanup) when (cleanup is IOException or UnauthorizedAccessException) { }
            SaveLock.Release();
        }
    }

    /// <summary>
    /// Resets all settings to defaults and persists them.
    /// </summary>
    public Task ResetAsync()
    {
        _settings = new AppSettings();
        return SaveAsync();
    }
}

/// <summary>
/// Application settings model.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/Properties/Settings.settings
/// </remarks>
public sealed class AppSettings
{
    // Display options
    // Source: Settings.settings lines 38-39, 44-45
    public bool UseNames { get; set; } = true;
    public bool UseTags { get; set; } = true;
    public bool HexOnly { get; set; }
    public bool EnableDDSPreview { get; set; } = true;
    public bool EnableFallbackTextPreview { get; set; }
    public bool EnableFallbackHexPreview { get; set; }
    public float PreviewZoomFactor { get; set; } = 1.0f;

    /// <summary>
    /// When true, always show hex view instead of automatic preview selection.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe Settings - hex viewing mode
    /// </remarks>
    public bool ForceHexView { get; set; }

    // External editors
    // Source: Settings.settings lines 56-64, 86-94
    public string? HexEditorCommand { get; set; }
    public bool HexEditorIgnoreTimestamp { get; set; }
    public bool HexEditorWantsQuotes { get; set; }
    public string? TextEditorCommand { get; set; }
    public bool TextEditorIgnoreTimestamp { get; set; }
    public bool TextEditorWantsQuotes { get; set; }

    // Sort options
    // Source: Settings.settings lines 26-34
    public int ColumnToSort { get; set; }
    public int SortOrder { get; set; }
    public bool Sort { get; set; } = true;

    // Hex viewer
    // Source: Settings.settings lines 41-42
    public int HexRowSize { get; set; } = 16;

    // MRU/Bookmarks
    // Source: Settings.settings lines 50-55. The MRU list itself is kept in recent.json by
    // MainWindowViewModel; a "RecentFiles" key in older settings files is ignored on load.
    public int MaxRecentFiles { get; set; } = 10;
    public List<string> Bookmarks { get; set; } = [];
    public int MaxBookmarks { get; set; } = 10;

    // Legacy property for backward compatibility (renamed to MaxBookmarks)
    [System.Text.Json.Serialization.JsonIgnore]
    [Obsolete("Use MaxBookmarks instead")]
    public int BookmarkSize { get => MaxBookmarks; set => MaxBookmarks = value; }

    // Custom Places (folder shortcuts in file dialogs)
    // Source: Settings.settings CustomPlaces/CustomPlacesCount
    public List<string> CustomPlaces { get; set; } = [];
    public int CustomPlacesCount { get; set; } = 5;

    // Browser column widths, ':'-separated, in column order (Name, Tag, Type, ...)
    // Source: Settings.settings line 47 (ColumnWidths); BrowserWidget.cs lines 46-89
    public string ColumnWidths { get; set; } = "";

    // Window state
    // Source: Settings.settings lines 17-25
    public int WindowWidth { get; set; } = -1;
    public int WindowHeight { get; set; } = -1;
    public int WindowX { get; set; } = -1;
    public int WindowY { get; set; } = -1;
    public int WindowState { get; set; } = 1;

    // Splitter positions: Splitter1 is the width of the browser column, Splitter2 the height of
    // the package information panel (-1 = default)
    // Source: Settings.settings lines 68-73
    public int Splitter1Position { get; set; } = -1;
    public int Splitter2Position { get; set; } = -1;

    /// <summary>
    /// Version of the main-window layout the splitter positions and column widths were saved for.
    /// Saved layout values from an older layout are discarded so they cannot squeeze the new one.
    /// </summary>
    public int LayoutVersion { get; set; }

    // Last paths
    // Source: Settings.settings lines 74-76; used as the start folder of the export pickers
    // (MainForm.cs lines 1773-1781, 1798-1804)
    public string? LastExportPath { get; set; }

    // Disabled features
    // Source: Settings.settings lines 83-85, 113-115
    public List<string> DisabledHelpers { get; set; } = [];
    public List<string> DisabledWrappers { get; set; } = [];

    // Auto display mode (0=Off, 1=Hex, 2=Preview)
    // Note: Legacy s4pe used AutoUpdateChoice for this, but we renamed it for clarity
    public int AutoDisplayMode { get; set; } = 2;

    // Legacy property for backward compatibility (renamed to AutoDisplayMode)
    [System.Text.Json.Serialization.JsonIgnore]
    [Obsolete("Use AutoDisplayMode instead")]
    public int AutoUpdateChoice { get => AutoDisplayMode; set => AutoDisplayMode = value; }

    // Auto update checking
    // Source: Settings.settings lines 95-103
    // Source: legacy_references/Sims4Tools/s4pe/Settings/UpdateChecker.cs
    public bool AutoCheckForUpdates { get; set; } = true;
    public DateTime LastUpdateCheck { get; set; } = DateTime.MinValue;

    // DBC import settings
    // Source: legacy_references/Sims4Tools/s4pe/Properties/Settings.settings AskDBCAutoSave
    // When true, prompts for autosave during DBC package import
    public bool PromptDbcAutosave { get; set; }
}
