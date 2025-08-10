using Microsoft.Extensions.Logging;
using TS4Tools.Core.Interfaces;

namespace TS4Tools.Core.Package;

/// <summary>
/// High-level package management service implementation.
/// </summary>
internal sealed class PackageService : IPackageService
{
    private readonly IPackageFactory _packageFactory;
    private readonly ILogger<PackageService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PackageService"/> class.
    /// </summary>
    /// <param name="packageFactory">Package factory instance</param>
    /// <param name="logger">Logger instance</param>
    public PackageService(IPackageFactory packageFactory, ILogger<PackageService> logger)
    {
        _packageFactory = packageFactory ?? throw new ArgumentNullException(nameof(packageFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<PackageValidationResult> ValidatePackageAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            return new PackageValidationResult
            {
                IsValid = false,
                Errors = new[] { $"File not found: {filePath}" }
            };
        }

        _logger.LogDebug("Validating package: {FilePath}", filePath);

        var errors = new List<string>();
        var warnings = new List<string>();
        var fileInfo = new FileInfo(filePath);

        try
        {
            using var package = await _packageFactory.LoadFromFileAsync(filePath, readOnly: true, cancellationToken);

            // Basic validation checks
            var resourceList = package.ResourceIndex;

            // Check for reasonable resource count
            if (resourceList.Count == 0)
            {
                warnings.Add("Package contains no resources");
            }
            else if (resourceList.Count > 10000)
            {
                warnings.Add($"Package contains a large number of resources ({resourceList.Count:N0})");
            }

            // Check for duplicate resources
            var duplicates = resourceList
                .GroupBy(r => new { r.ResourceType, r.ResourceGroup, r.Instance })
                .Where(g => g.Count() > 1)
                .ToList();

            if (duplicates.Any())
            {
                warnings.Add($"Found {duplicates.Count} duplicate resource keys");
            }

            _logger.LogInformation("Package validation completed for {FilePath}: {ResourceCount} resources, {ErrorCount} errors, {WarningCount} warnings",
                filePath, resourceList.Count, errors.Count, warnings.Count);

            return new PackageValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors,
                Warnings = warnings,
                FileSizeBytes = fileInfo.Length,
                ResourceCount = resourceList.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate package: {FilePath}", filePath);

            return new PackageValidationResult
            {
                IsValid = false,
                Errors = new[] { $"Validation failed: {ex.Message}" },
                FileSizeBytes = fileInfo.Length,
                ResourceCount = 0
            };
        }
    }

    /// <inheritdoc />
    public async Task<PackageInfo> GetPackageInfoAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Package file not found: {filePath}");
        }

        _logger.LogDebug("Getting package info for: {FilePath}", filePath);

        var fileInfo = new FileInfo(filePath);

        try
        {
            using var package = await _packageFactory.LoadFromFileAsync(filePath, readOnly: true, cancellationToken);

            var info = new PackageInfo
            {
                FilePath = filePath,
                FileSizeBytes = fileInfo.Length,
                ResourceCount = package.ResourceIndex.Count,
                MajorVersion = package.Major,
                MinorVersion = package.Minor,
                CreationTime = package.CreatedDate,
                UpdatedTime = package.ModifiedDate
            };

            _logger.LogInformation("Retrieved package info for {FilePath}: {ResourceCount} resources, {FileSize:N0} bytes",
                filePath, info.ResourceCount, info.FileSizeBytes);

            return info;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get package info for: {FilePath}", filePath);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<PackageCompactionResult> CompactPackageAsync(IPackage package, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(package);

        _logger.LogDebug("Starting package compaction");

        try
        {
            var originalResourceCount = package.ResourceIndex.Count;

            // Use the built-in compaction method
            await package.CompactAsync(cancellationToken);

            var newResourceCount = package.ResourceIndex.Count;
            var deletedEntriesRemoved = originalResourceCount - newResourceCount;

            _logger.LogInformation("Package compaction completed: removed {DeletedEntries} deleted entries",
                deletedEntriesRemoved);

            return new PackageCompactionResult
            {
                Success = true,
                DeletedEntriesRemoved = deletedEntriesRemoved,
                // Note: Size information would need to be tracked separately
                // as the package interface doesn't provide size metrics
                OriginalSizeBytes = 0,
                NewSizeBytes = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compact package");

            return new PackageCompactionResult
            {
                Success = false,
                DeletedEntriesRemoved = 0,
                OriginalSizeBytes = 0,
                NewSizeBytes = 0
            };
        }
    }

    /// <inheritdoc />
    public async Task<string> CreateBackupAsync(string filePath, string? backupPath = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Source file not found: {filePath}");
        }

        // Generate backup path if not provided
        if (string.IsNullOrWhiteSpace(backupPath))
        {
            var directory = Path.GetDirectoryName(filePath) ?? string.Empty;
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            var extension = Path.GetExtension(filePath);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            backupPath = Path.Combine(directory, $"{fileNameWithoutExtension}_backup_{timestamp}{extension}");
        }

        _logger.LogDebug("Creating backup: {SourceFile} -> {BackupFile}", filePath, backupPath);

        try
        {
            // Ensure backup directory exists
            var backupDirectory = Path.GetDirectoryName(backupPath);
            if (!string.IsNullOrEmpty(backupDirectory))
            {
                Directory.CreateDirectory(backupDirectory);
            }

            // Copy the file asynchronously
            using var sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
            using var destinationStream = new FileStream(backupPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);

            await sourceStream.CopyToAsync(destinationStream, cancellationToken);

            _logger.LogInformation("Successfully created backup: {BackupFile}", backupPath);
            return backupPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backup from {SourceFile} to {BackupFile}", filePath, backupPath);
            throw;
        }
    }
}
