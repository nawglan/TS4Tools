using Microsoft.Extensions.Logging;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package.Compression;

namespace TS4Tools.Core.Package;

/// <summary>
/// Factory implementation for creating package instances.
/// </summary>
internal sealed class PackageFactory : IPackageFactory
{
    private readonly ILogger<PackageFactory> _logger;
    private readonly ICompressionService _compressionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="PackageFactory"/> class.
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="compressionService">Compression service for package operations</param>
    public PackageFactory(ILogger<PackageFactory> logger, ICompressionService compressionService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _compressionService = compressionService ?? throw new ArgumentNullException(nameof(compressionService));
    }

    /// <inheritdoc />
    public async Task<IPackage> CreateEmptyPackageAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating new empty package");

        try
        {
            var package = await Task.FromResult(new Package(_compressionService));

            _logger.LogInformation("Created new empty package");
            return package;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create empty package");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IPackage> LoadFromFileAsync(string filePath, bool readOnly = false, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Package file not found: {filePath}");
        }

        _logger.LogDebug("Loading package from file: {FilePath} (ReadOnly: {ReadOnly})", filePath, readOnly);

        try
        {
            var package = await Package.LoadFromFileAsync(filePath, _compressionService, readOnly, cancellationToken);

            _logger.LogInformation("Successfully loaded package from {FilePath} (ReadOnly: {ReadOnly})", filePath, readOnly);
            return package;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load package from file: {FilePath}", filePath);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IPackage> LoadFromStreamAsync(Stream stream, bool readOnly = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanRead)
        {
            throw new ArgumentException("Stream must be readable", nameof(stream));
        }

        _logger.LogDebug("Loading package from stream (ReadOnly: {ReadOnly})", readOnly);

        try
        {
            var package = await Package.LoadFromStreamAsync(stream, _compressionService, readOnly, null, cancellationToken);

            _logger.LogInformation("Successfully loaded package from stream (ReadOnly: {ReadOnly})", readOnly);
            return package;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load package from stream");
            throw;
        }
    }
}
