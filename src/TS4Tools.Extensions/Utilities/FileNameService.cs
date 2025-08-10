using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using TS4Tools.Core.System.Platform;
using TS4Tools.Extensions.ResourceIdentification;
using TS4Tools.Extensions.ResourceTypes;

namespace TS4Tools.Extensions.Utilities;

/// <summary>
/// Default implementation of <see cref="IFileNameService"/> that generates appropriate filenames
/// for resources based on their type and properties.
/// </summary>
public sealed partial class FileNameService : IFileNameService
{
    private readonly IResourceTypeRegistry _typeRegistry;
    private readonly ILogger<FileNameService> _logger;
    private readonly IPlatformService _platformService;

    // Pre-compiled regex for filename sanitization
    [GeneratedRegex(@"[<>:""/\\|?*\x00-\x1f]", RegexOptions.Compiled)]
    private static partial Regex InvalidCharsRegex();

    /// <summary>
    /// Initializes a new instance of the <see cref="FileNameService"/> class.
    /// </summary>
    /// <param name="typeRegistry">The resource type registry.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="platformService">The platform service for platform-specific operations.</param>
    public FileNameService(IResourceTypeRegistry typeRegistry, ILogger<FileNameService> logger, IPlatformService? platformService = null)
    {
        _typeRegistry = typeRegistry ?? throw new ArgumentNullException(nameof(typeRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _platformService = platformService ?? PlatformService.Instance;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string GetFileName(IResourceKey resourceKey, string? baseName = null)
    {
        ArgumentNullException.ThrowIfNull(resourceKey);

        var identifier = ResourceIdentifier.FromResourceKey(resourceKey);
        return GetFileName(identifier, baseName);
    }

    /// <inheritdoc />
    public string GetFileName(ResourceIdentifier identifier, string? baseName = null)
    {
        var extension = _typeRegistry.GetExtension(identifier.ResourceType) ?? ".dat";

        if (!string.IsNullOrWhiteSpace(baseName))
        {
            var sanitizedBaseName = SanitizeFileName(baseName);
            return EnsureExtension(sanitizedBaseName, extension);
        }

        // Use name if available, otherwise generate from TGI
        string fileName;
        if (!string.IsNullOrWhiteSpace(identifier.Name))
        {
            fileName = SanitizeFileName(identifier.Name);
        }
        else
        {
            // Generate filename from TGI values
            var tag = _typeRegistry.GetTag(identifier.ResourceType) ?? "UNKN";
            fileName = $"{tag}_{identifier.ResourceType:X8}_{identifier.ResourceGroup:X8}_{identifier.Instance:X16}";
        }

        return EnsureExtension(fileName, extension);
    }

    /// <inheritdoc />
    public string SanitizeFileName(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        // Use platform service for cross-platform filename sanitization
        return _platformService.SanitizeFileName(fileName);
    }

    /// <inheritdoc />
    public string GetUniqueFileName(string baseFileName, string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        var fullPath = Path.Combine(directory, baseFileName);
        if (!File.Exists(fullPath))
        {
            return baseFileName;
        }

        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(baseFileName);
        var extension = Path.GetExtension(baseFileName);

        var counter = 1;
        string uniqueFileName;
        do
        {
            uniqueFileName = $"{fileNameWithoutExtension}_{counter:D3}{extension}";
            fullPath = Path.Combine(directory, uniqueFileName);
            counter++;
        }
        while (File.Exists(fullPath) && counter < 1000); // Prevent infinite loops

        if (counter >= 1000)
        {
            // Fallback to timestamp-based naming
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            uniqueFileName = $"{fileNameWithoutExtension}_{timestamp}{extension}";
            LogUniqueFileNameFallback(_logger, uniqueFileName);
        }

        return uniqueFileName;
    }

    /// <summary>
    /// Ensures the filename has the correct extension.
    /// </summary>
    /// <param name="fileName">The base filename.</param>
    /// <param name="expectedExtension">The expected extension (including the dot).</param>
    /// <returns>The filename with the correct extension.</returns>
    private static string EnsureExtension(string fileName, string expectedExtension)
    {
        var currentExtension = Path.GetExtension(fileName);

        if (string.Equals(currentExtension, expectedExtension, StringComparison.OrdinalIgnoreCase))
        {
            return fileName;
        }

        // Remove current extension if present and add the expected one
        if (!string.IsNullOrEmpty(currentExtension))
        {
            fileName = Path.GetFileNameWithoutExtension(fileName);
        }

        return fileName + expectedExtension;
    }

    /// <summary>
    /// LoggerMessage delegate for unique filename fallback.
    /// </summary>
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Could not generate unique filename after 999 attempts, using timestamp: {FileName}")]
    private static partial void LogUniqueFileNameFallback(ILogger logger, string fileName);
}
