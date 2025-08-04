using TS4Tools.Core.Interfaces;

namespace TS4Tools.Core.Package;

/// <summary>
/// High-level package management service providing common package operations.
/// </summary>
public interface IPackageService
{
    /// <summary>
    /// Validates a package file for integrity and format compliance.
    /// </summary>
    /// <param name="filePath">Path to the package file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with details</returns>
    Task<PackageValidationResult> ValidatePackageAsync(string filePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets package information without fully loading the package.
    /// </summary>
    /// <param name="filePath">Path to the package file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Package metadata information</returns>
    Task<PackageInfo> GetPackageInfoAsync(string filePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Compacts a package by removing deleted entries and optimizing layout.
    /// </summary>
    /// <param name="package">Package to compact</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Compaction result with statistics</returns>
    Task<PackageCompactionResult> CompactPackageAsync(IPackage package, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a backup of a package file.
    /// </summary>
    /// <param name="filePath">Original package file path</param>
    /// <param name="backupPath">Backup file path (optional, auto-generated if null)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Path to the created backup file</returns>
    Task<string> CreateBackupAsync(string filePath, string? backupPath = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Results of package validation operation.
/// </summary>
public record PackageValidationResult
{
    /// <summary>
    /// Whether the package is valid.
    /// </summary>
    public bool IsValid { get; init; }
    
    /// <summary>
    /// List of validation errors, if any.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    
    /// <summary>
    /// List of validation warnings, if any.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
    
    /// <summary>
    /// Package file size in bytes.
    /// </summary>
    public long FileSizeBytes { get; init; }
    
    /// <summary>
    /// Number of resources in the package.
    /// </summary>
    public int ResourceCount { get; init; }
}

/// <summary>
/// Basic package information.
/// </summary>
public record PackageInfo
{
    /// <summary>
    /// Package file path.
    /// </summary>
    public string FilePath { get; init; } = string.Empty;
    
    /// <summary>
    /// Package file size in bytes.
    /// </summary>
    public long FileSizeBytes { get; init; }
    
    /// <summary>
    /// Number of resources in the package.
    /// </summary>
    public int ResourceCount { get; init; }
    
    /// <summary>
    /// Package major version.
    /// </summary>
    public int MajorVersion { get; init; }
    
    /// <summary>
    /// Package minor version.
    /// </summary>
    public int MinorVersion { get; init; }
    
    /// <summary>
    /// Package creation time (if available).
    /// </summary>
    public DateTime? CreationTime { get; init; }
    
    /// <summary>
    /// Package last update time (if available).
    /// </summary>
    public DateTime? UpdatedTime { get; init; }
}

/// <summary>
/// Results of package compaction operation.
/// </summary>
public record PackageCompactionResult
{
    /// <summary>
    /// Whether the compaction was successful.
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// Original package size in bytes.
    /// </summary>
    public long OriginalSizeBytes { get; init; }
    
    /// <summary>
    /// New package size in bytes after compaction.
    /// </summary>
    public long NewSizeBytes { get; init; }
    
    /// <summary>
    /// Number of deleted entries removed.
    /// </summary>
    public int DeletedEntriesRemoved { get; init; }
    
    /// <summary>
    /// Space saved in bytes.
    /// </summary>
    public long SpaceSaved => OriginalSizeBytes - NewSizeBytes;
    
    /// <summary>
    /// Percentage of space saved.
    /// </summary>
    public double SpaceSavedPercentage => OriginalSizeBytes > 0 ? (double)SpaceSaved / OriginalSizeBytes * 100 : 0;
}
