using Microsoft.Extensions.Logging;
using TS4Tools.Core.Interfaces;
using TS4Tools.Resources.Common;
using TS4Tools.Resources.Common.Collections;

namespace TS4Tools.Resources.Catalog;

/// <summary>
/// Abstract base class for all catalog resource implementations.
/// Provides common functionality for version handling, validation, and disposal patterns.
/// </summary>
public abstract class AbstractCatalogResource : IAbstractCatalogResource
{
    private readonly ILogger _logger;
    private CatalogCommonBlock? _commonBlock;
    private uint _version = 1;
    private bool _isModified;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AbstractCatalogResource"/> class.
    /// </summary>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <param name="catalogType">The catalog type identifier.</param>
    protected AbstractCatalogResource(ILogger logger, CatalogType catalogType)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        CatalogType = catalogType;
    }

    /// <inheritdoc />
    public event EventHandler? ResourceChanged;

    /// <inheritdoc />
    public abstract Stream Stream { get; }

    /// <inheritdoc />
    public abstract byte[] AsBytes { get; }

    /// <inheritdoc />
    public abstract int RecommendedApiVersion { get; }

    /// <inheritdoc />
    public int RequestedApiVersion { get; set; } = 1;

    /// <inheritdoc />
    public abstract IReadOnlyList<string> ContentFields { get; }

    /// <inheritdoc />
    public abstract TypedValue this[int index] { get; set; }

    /// <inheritdoc />
    public abstract TypedValue this[string name] { get; set; }

    /// <inheritdoc />
    public CatalogCommonBlock? CommonBlock
    {
        get => _commonBlock;
        set
        {
            if (_commonBlock != value)
            {
                _commonBlock = value;
                OnResourceChanged();
            }
        }
    }

    /// <inheritdoc />
    public uint Version
    {
        get => _version;
        set
        {
            if (_version != value)
            {
                _version = value;
                OnResourceChanged();
            }
        }
    }

    /// <inheritdoc />
    public CatalogType CatalogType { get; }

    /// <inheritdoc />
    public bool IsModified => _isModified;

    /// <inheritdoc />
    public abstract uint MinimumSupportedVersion { get; }

    /// <inheritdoc />
    public abstract uint MaximumSupportedVersion { get; }

    /// <inheritdoc />
    public bool IsVersionSupported => Version >= MinimumSupportedVersion && Version <= MaximumSupportedVersion;

    /// <inheritdoc />
    public abstract Task LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task SaveToStreamAsync(Stream stream, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public virtual async Task<ValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        var result = new ValidationResult();

        // Version validation
        if (!IsVersionSupported)
        {
            result.AddError(
                $"Version {Version} is not supported. Supported range: {MinimumSupportedVersion}-{MaximumSupportedVersion}",
                nameof(Version),
                Version);
        }

        // Common block validation
        if (CommonBlock == null)
        {
            result.AddWarning("CommonBlock is null, which may indicate incomplete resource data", nameof(CommonBlock));
        }
        else
        {
            // Validate common block properties
            var commonValidation = await ValidateCommonBlockAsync(CommonBlock, cancellationToken).ConfigureAwait(false);
            result.Merge(commonValidation);
        }

        // Run resource-specific validation rules
        var rules = GetValidationRules();
        foreach (var rule in rules)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var ruleResult = await rule.ValidateAsync(this, cancellationToken).ConfigureAwait(false);
                result.Merge(ruleResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Validation rule {RuleName} failed with exception", rule.RuleName);
                result.AddError($"Validation rule '{rule.RuleName}' failed: {ex.Message}");
            }
        }

        _logger.LogDebug("Validation completed: {ErrorCount} errors, {WarningCount} warnings",
            result.Errors.Count, result.Warnings.Count);

        return result;
    }

    /// <inheritdoc />
    public virtual async Task MigrateVersionAsync(uint fromVersion, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        if (fromVersion == Version)
        {
            return; // No migration needed
        }

        _logger.LogDebug("Migrating catalog resource from version {FromVersion} to {ToVersion}", fromVersion, Version);

        // Default migration: just update the version
        // Derived classes should override this for specific migration logic
        OnResourceChanged();

        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual IEnumerable<IValidationRule> GetValidationRules()
    {
        // Base validation rules that apply to all catalog resources
        yield return new VersionValidationRule();
        yield return new CommonBlockValidationRule();
    }

    /// <inheritdoc />
    public virtual IEnumerable<string> Validate()
    {
        // Use Task.Run to avoid deadlocks in synchronization contexts
        var validationTask = Task.Run(async () => await ValidateAsync().ConfigureAwait(false));
        var result = validationTask.GetAwaiter().GetResult();

        return result.Errors.Select(e => e.Message);
    }

    /// <inheritdoc />
    public virtual async Task<ICatalogResource> CloneAsync()
    {
        var abstractClone = await CloneAsync(CancellationToken.None).ConfigureAwait(false);
        return abstractClone;
    }

    /// <inheritdoc />
    public virtual async Task<IAbstractCatalogResource> CloneAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        // Default implementation: serialize to bytes and recreate
        var bytes = AsBytes;
        using var stream = new MemoryStream(bytes);

        var clone = await CreateInstanceAsync(stream, cancellationToken).ConfigureAwait(false);
        return clone;
    }

    /// <summary>
    /// Creates a new instance of this catalog resource type from a stream.
    /// Must be implemented by derived classes for proper cloning support.
    /// </summary>
    /// <param name="stream">The stream to load from.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A new instance of the catalog resource.</returns>
    protected abstract Task<IAbstractCatalogResource> CreateInstanceAsync(Stream stream, CancellationToken cancellationToken);

    /// <summary>
    /// Validates the common block properties.
    /// </summary>
    /// <param name="commonBlock">The common block to validate.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The validation result.</returns>
    protected virtual async Task<ValidationResult> ValidateCommonBlockAsync(CatalogCommonBlock commonBlock, CancellationToken cancellationToken)
    {
        var result = new ValidationResult();

        // Basic common block validation
        if (commonBlock.CategoryId == 0)
        {
            result.AddWarning("CategoryId is 0, which may not be a valid catalog identifier", nameof(CatalogCommonBlock.CategoryId));
        }

        // Additional validation can be added here
        await Task.CompletedTask.ConfigureAwait(false);
        return result;
    }

    /// <summary>
    /// Notifies that the resource has changed.
    /// </summary>
    protected virtual void OnResourceChanged()
    {
        if (!_disposed)
        {
            _isModified = true;
            ResourceChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(false);
            GC.SuppressFinalize(this);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes of resources.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources - use Task.Run to avoid deadlocks
                Task.Run(async () => await DisposeAsyncCore().ConfigureAwait(false))
                    .GetAwaiter().GetResult();
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// Performs async disposal of resources.
    /// Override in derived classes to dispose of additional resources.
    /// </summary>
    /// <returns>A task representing the async disposal operation.</returns>
    protected virtual async ValueTask DisposeAsyncCore()
    {
        // Base implementation - derived classes should override
        await Task.CompletedTask.ConfigureAwait(false);
    }
}

/// <summary>
/// Validation rule for checking catalog resource version compatibility.
/// </summary>
internal sealed class VersionValidationRule : IValidationRule
{
    public string RuleName => "Version Compatibility";
    public string Description => "Validates that the catalog resource version is supported";

    public async Task<ValidationResult> ValidateAsync(ICatalogResource resource, CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult();

        if (resource is IAbstractCatalogResource abstractResource)
        {
            if (!abstractResource.IsVersionSupported)
            {
                result.AddError(
                    $"Version {abstractResource.Version} is not supported",
                    nameof(IAbstractCatalogResource.Version),
                    abstractResource.Version);
            }
        }

        await Task.CompletedTask.ConfigureAwait(false);
        return result;
    }
}

/// <summary>
/// Validation rule for checking common block integrity.
/// </summary>
internal sealed class CommonBlockValidationRule : IValidationRule
{
    public string RuleName => "Common Block Integrity";
    public string Description => "Validates catalog common block data integrity";

    public async Task<ValidationResult> ValidateAsync(ICatalogResource resource, CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult();

        if (resource.CommonBlock == null)
        {
            result.AddWarning("CommonBlock is null", nameof(ICatalogResource.CommonBlock));
        }

        await Task.CompletedTask.ConfigureAwait(false);
        return result;
    }
}
