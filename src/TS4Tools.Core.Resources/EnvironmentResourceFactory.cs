using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Interfaces.Resources;

namespace TS4Tools.Core.Resources;

/// <summary>
/// Factory for creating EnvironmentResource instances.
/// </summary>
/// <remarks>
/// Handles creation of environment resources that manage weather patterns,
/// seasonal effects, and environmental systems including temperature, humidity,
/// wind conditions, and regional weather variations.
/// </remarks>
public sealed class EnvironmentResourceFactory : ResourceFactoryBase<IEnvironmentResource>
{
    /// <summary>
    /// Initializes a new instance of the EnvironmentResourceFactory class.
    /// </summary>
    public EnvironmentResourceFactory() : base(new[] { "0xE5A5C6D8", "0x8E6B4F2A", "0x7C9D3E1B" }, priority: 100)
    {
    }

    /// <inheritdoc />
    public override async Task<IEnvironmentResource> CreateResourceAsync(int apiVersion, Stream? stream = null, CancellationToken cancellationToken = default)
    {
        // For async consistency, but no actual async work needed for this resource
        await Task.CompletedTask;

        return stream != null ? new EnvironmentResource(stream) : new EnvironmentResource();
    }

    /// <summary>
    /// Creates a resource instance synchronously.
    /// </summary>
    /// <returns>New EnvironmentResource instance</returns>
    public IEnvironmentResource Create()
    {
        return new EnvironmentResource();
    }

    /// <summary>
    /// Creates a resource instance from a stream synchronously.
    /// </summary>
    /// <param name="stream">Stream to load from</param>
    /// <returns>New EnvironmentResource instance</returns>
    public IEnvironmentResource Create(Stream stream)
    {
        return new EnvironmentResource(stream);
    }

    /// <summary>
    /// Creates a resource instance asynchronously.
    /// </summary>
    /// <returns>Task containing new EnvironmentResource instance</returns>
    public async Task<IEnvironmentResource> CreateAsync()
    {
        await Task.CompletedTask;
        return new EnvironmentResource();
    }

    /// <summary>
    /// Creates a resource instance from a stream asynchronously.
    /// </summary>
    /// <param name="stream">Stream to load from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task containing new EnvironmentResource instance</returns>
    public async Task<IEnvironmentResource> CreateAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();
        return new EnvironmentResource(stream);
    }

    /// <summary>
    /// Creates a resource instance asynchronously with cancellation support.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task containing new EnvironmentResource instance</returns>
    public async Task<IEnvironmentResource> CreateAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();
        return new EnvironmentResource();
    }

    /// <summary>
    /// Checks if the factory can create a resource of the specified type.
    /// </summary>
    /// <param name="resourceType">Resource type ID</param>
    /// <returns>True if the resource type is supported</returns>
    public bool CanCreate(uint resourceType)
    {
        return CanCreateResource(resourceType);
    }

    /// <summary>
    /// Checks if the factory can create a resource of the specified type.
    /// </summary>
    /// <param name="resourceType">Resource type name</param>
    /// <returns>True if the resource type is supported</returns>
    public bool CanCreate(string resourceType)
    {
        ArgumentNullException.ThrowIfNull(resourceType);

        // Check if it's a supported hex resource type first
        if (SupportedResourceTypes.Contains(resourceType))
            return true;

        // Check if it's a supported friendly name
        return SupportedResourceTypeNames.Contains(resourceType, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the supported resource type names.
    /// </summary>
    public IEnumerable<string> SupportedResourceTypeNames => new[] { "ENVIRONMENT", "WEATHER", "SEASON" };

    /// <inheritdoc />
    protected override bool TryGetResourceTypeId(string resourceType, out uint id)
    {
        // Handle Environment specific mappings
        id = resourceType.ToUpperInvariant() switch
        {
            "ENVIRONMENT" => 0xE5A5C6D8,
            "0XE5A5C6D8" => 0xE5A5C6D8,
            "WEATHER" => 0x8E6B4F2A,
            "0X8E6B4F2A" => 0x8E6B4F2A,
            "SEASON" => 0x7C9D3E1B,
            "0X7C9D3E1B" => 0x7C9D3E1B,
            _ => 0
        };

        if (id != 0)
            return true;

        // Fall back to base implementation
        return base.TryGetResourceTypeId(resourceType, out id);
    }
}
