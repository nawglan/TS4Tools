using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
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

    // Pre-compiled regex for filename sanitization
    [GeneratedRegex(@"[<>:""/\\|?*\x00-\x1f]", RegexOptions.Compiled)]
    private static partial Regex InvalidCharsRegex();

    /// <summary>
    /// Initializes a new instance of the <see cref="FileNameService"/> class.
    /// </summary>
    /// <param name="typeRegistry">The resource type registry.</param>
    /// <param name="logger">The logger instance.</param>
    public FileNameService(IResourceTypeRegistry typeRegistry, ILogger<FileNameService> logger)
    {
        _typeRegistry = typeRegistry ?? throw new ArgumentNullException(nameof(typeRegistry));
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

        // Replace invalid characters with underscores
        var sanitized = InvalidCharsRegex().Replace(fileName, "_");
        
        // Ensure the filename isn't too long (most file systems support 255 chars)
        const int maxLength = 240; // Leave room for extension
        if (sanitized.Length > maxLength)
        {
            sanitized = sanitized[..maxLength];
        }

        // Ensure it doesn't end with a space or period (Windows restriction)
        sanitized = sanitized.TrimEnd(' ', '.');

        // Ensure it's not empty or only replacement characters after sanitization
        if (string.IsNullOrWhiteSpace(sanitized) || sanitized.All(c => c == '_'))
        {
            sanitized = "unnamed";
        }

        // Check for reserved names on Windows
        if (IsReservedName(sanitized))
        {
            sanitized = $"_{sanitized}";
        }

        return sanitized;
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
    /// Checks if the given name is a reserved filename on Windows.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <returns>True if the name is reserved; otherwise, false.</returns>
    private static bool IsReservedName(string name)
    {
        var upperName = name.ToUpperInvariant();
        return upperName is "CON" or "PRN" or "AUX" or "NUL" or
               "COM1" or "COM2" or "COM3" or "COM4" or "COM5" or "COM6" or "COM7" or "COM8" or "COM9" or
               "LPT1" or "LPT2" or "LPT3" or "LPT4" or "LPT5" or "LPT6" or "LPT7" or "LPT8" or "LPT9";
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
