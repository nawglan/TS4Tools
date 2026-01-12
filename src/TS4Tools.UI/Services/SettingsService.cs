namespace TS4Tools.UI.Services;

/// <summary>
/// Service for managing application settings.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/Properties/Settings.settings
/// </remarks>
public sealed class SettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TS4Tools", "settings.json");

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static SettingsService? _instance;
    private AppSettings _settings;

    public static SettingsService Instance => _instance ??= new SettingsService();

    public AppSettings Settings => _settings;

    private SettingsService()
    {
        _settings = LoadSettings();
    }

    private static AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var settings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json);
                return settings ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TS4Tools] Failed to load settings: {ex.Message}");
        }

        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = System.Text.Json.JsonSerializer.Serialize(_settings, JsonOptions);
            File.WriteAllText(SettingsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TS4Tools] Failed to save settings: {ex.Message}");
        }
    }

    public void Reset()
    {
        _settings = new AppSettings();
        Save();
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
    // Source: Settings.settings lines 50-55
    public List<string> RecentFiles { get; set; } = [];
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

    // Window state
    // Source: Settings.settings lines 17-25
    public int WindowWidth { get; set; } = -1;
    public int WindowHeight { get; set; } = -1;
    public int WindowX { get; set; } = -1;
    public int WindowY { get; set; } = -1;
    public int WindowState { get; set; } = 1;

    // Splitter positions
    // Source: Settings.settings lines 68-73
    public int Splitter1Position { get; set; } = -1;
    public int Splitter2Position { get; set; } = -1;

    // Last paths
    // Source: Settings.settings lines 74-76
    public string? LastExportPath { get; set; }

    // Disabled features
    // Source: Settings.settings lines 83-85, 113-115
    public List<string> DisabledHelpers { get; set; } = [];
    public List<string> DisabledWrappers { get; set; } = [];

    // Auto display mode (0=Off, 1=Hex, 2=Preview)
    // Note: Legacy s4pe used AutoUpdateChoice for this, but we renamed it for clarity
    public int AutoDisplayMode { get; set; }

    // Legacy property for backward compatibility (renamed to AutoDisplayMode)
    [System.Text.Json.Serialization.JsonIgnore]
    [Obsolete("Use AutoDisplayMode instead")]
    public int AutoUpdateChoice { get => AutoDisplayMode; set => AutoDisplayMode = value; }

    // Auto update checking
    // Source: Settings.settings lines 95-103
    // Source: legacy_references/Sims4Tools/s4pe/Settings/UpdateChecker.cs
    public bool AutoCheckForUpdates { get; set; } = true;
    public DateTime LastUpdateCheck { get; set; } = DateTime.MinValue;
}
