using System.Reflection;
using System.Text.Json;

namespace TS4Tools.UI.Services;

/// <summary>
/// Checks for updates from GitHub releases.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/Settings/UpdateChecker.cs
/// Source: legacy_references/Sims4Tools/s4pe/Settings/GitHubVersion.cs
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", "CA1001:Types that own disposable fields should be disposable", Justification = "HttpClient is intentionally not disposed - singleton service reuses client")]
public sealed class UpdateCheckerService
{
    // TODO: Update this to the actual TS4Tools repository when available
    private const string GitHubApiUrl = "https://api.github.com/repos/anthropics/ts4tools/releases/latest";
    private const string DownloadPageUrl = "https://github.com/anthropics/ts4tools/releases";

    private static readonly Lazy<UpdateCheckerService> LazyInstance = new(() => new UpdateCheckerService());
    private readonly HttpClient _httpClient;

    public static UpdateCheckerService Instance => LazyInstance.Value;

    private UpdateCheckerService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "TS4Tools-UpdateChecker");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
    }

    /// <summary>
    /// Gets the current application version.
    /// </summary>
    public static string CurrentVersion
    {
        get
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "0.0.0";
        }
    }

    /// <summary>
    /// Gets the download page URL.
    /// </summary>
    public static string DownloadPage => DownloadPageUrl;

    /// <summary>
    /// Checks for updates and returns the result.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Update check result.</returns>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Settings/UpdateChecker.cs lines 82-129
    /// </remarks>
    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetStringAsync(GitHubApiUrl, cancellationToken);

            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;

            if (root.TryGetProperty("tag_name", out var tagNameElement))
            {
                var latestVersion = tagNameElement.GetString()?.TrimStart('v', 'V') ?? "0.0.0";

                var hasUpdate = CompareVersions(CurrentVersion, latestVersion) < 0;

                return new UpdateCheckResult
                {
                    Success = true,
                    CurrentVersion = CurrentVersion,
                    LatestVersion = latestVersion,
                    UpdateAvailable = hasUpdate,
                    DownloadUrl = DownloadPageUrl
                };
            }

            return new UpdateCheckResult
            {
                Success = false,
                CurrentVersion = CurrentVersion,
                ErrorMessage = "Could not parse version from GitHub response"
            };
        }
        catch (HttpRequestException ex)
        {
            return new UpdateCheckResult
            {
                Success = false,
                CurrentVersion = CurrentVersion,
                ErrorMessage = $"Network error: {ex.Message}"
            };
        }
        catch (JsonException ex)
        {
            return new UpdateCheckResult
            {
                Success = false,
                CurrentVersion = CurrentVersion,
                ErrorMessage = $"Failed to parse response: {ex.Message}"
            };
        }
        catch (TaskCanceledException)
        {
            return new UpdateCheckResult
            {
                Success = false,
                CurrentVersion = CurrentVersion,
                ErrorMessage = "Update check timed out"
            };
        }
    }

    /// <summary>
    /// Compares two version strings.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Settings/GitHubVersion.cs lines 59-62
    /// </remarks>
    private static int CompareVersions(string current, string latest)
    {
        if (Version.TryParse(current, out var currentVer) && Version.TryParse(latest, out var latestVer))
        {
            return currentVer.CompareTo(latestVer);
        }

        // Fallback to string comparison
        return string.Compare(current, latest, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Performs a daily check if enabled and enough time has passed.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Settings/UpdateChecker.cs lines 68-80
    /// </remarks>
    public async Task<UpdateCheckResult?> DailyCheckAsync(CancellationToken cancellationToken = default)
    {
        var settings = SettingsService.Instance.Settings;

        if (!settings.AutoCheckForUpdates)
            return null;

        var lastCheck = settings.LastUpdateCheck;
        if (DateTime.UtcNow - lastCheck < TimeSpan.FromDays(1))
            return null;

        var result = await CheckForUpdatesAsync(cancellationToken);

        if (result.Success)
        {
            settings.LastUpdateCheck = DateTime.UtcNow;
            SettingsService.Instance.Save();
        }

        return result;
    }
}

/// <summary>
/// Result of an update check.
/// </summary>
public sealed class UpdateCheckResult
{
    /// <summary>
    /// Whether the check was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The current application version.
    /// </summary>
    public string CurrentVersion { get; init; } = "";

    /// <summary>
    /// The latest available version (if check succeeded).
    /// </summary>
    public string LatestVersion { get; init; } = "";

    /// <summary>
    /// Whether an update is available.
    /// </summary>
    public bool UpdateAvailable { get; init; }

    /// <summary>
    /// URL to download the update.
    /// </summary>
    public string DownloadUrl { get; init; } = "";

    /// <summary>
    /// Error message if the check failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
}
