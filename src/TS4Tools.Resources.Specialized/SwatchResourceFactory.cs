using TS4Tools.Core.Resources;
using TS4Tools.Core.Interfaces.Resources.Specialized;

namespace TS4Tools.Resources.Specialized;

/// <summary>
/// Factory for creating SwatchResource instances following the TS4Tools resource factory pattern.
/// </summary>
public sealed class SwatchResourceFactory : ResourceFactoryBase<ISwatchResource>
{
    /// <summary>
    /// Initializes a new instance of the SwatchResourceFactory class.
    /// </summary>
    public SwatchResourceFactory()
        : base(new[] { $"0x{SwatchResource.ResourceType:X8}" })
    {
    }

    /// <summary>
    /// Creates a new empty SwatchResource asynchronously.
    /// </summary>
    /// <param name="apiVersion">The API version to use</param>
    /// <param name="stream">Optional stream containing resource data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task containing the created SwatchResource</returns>
    public override Task<ISwatchResource> CreateResourceAsync(
        int apiVersion,
        Stream? stream = null,
        CancellationToken cancellationToken = default)
    {
        if (stream == null)
        {
            // Default swatch collection
            var defaultResource = new SwatchResource("Default Swatches", SwatchCategory.General, apiVersion);
            return Task.FromResult<ISwatchResource>(defaultResource);
        }

        // Create from stream data
        return CreateFromStreamAsync(apiVersion, stream, cancellationToken);
    }

    /// <summary>
    /// Gets the format information for this factory.
    /// </summary>
    public ResourceFormatInfo FormatInfo => new()
    {
        ResourceType = SwatchResource.ResourceType,
        Name = "SwatchResource",
        Description = "Color swatch definitions for character customization",
        SupportedVersion = 1,
        FileExtensions = new[] { ".swatch" },
        MimeType = "application/x-sims4-swatch"
    };

    /// <summary>
    /// Creates a SwatchResource from binary data asynchronously.
    /// </summary>
    /// <param name="apiVersion">The API version to use</param>
    /// <param name="data">The binary data to parse</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task containing the created SwatchResource</returns>
    public Task<ISwatchResource> CreateFromDataAsync(
        int apiVersion,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<ISwatchResource>(cancellationToken);
        }

        var resource = SwatchResource.FromData(apiVersion, data.Span);
        return Task.FromResult<ISwatchResource>(resource);
    }

    /// <summary>
    /// Creates a SwatchResource from a stream asynchronously.
    /// </summary>
    /// <param name="apiVersion">The API version to use</param>
    /// <param name="stream">The stream to read from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task containing the created SwatchResource</returns>
    public async Task<ISwatchResource> CreateFromStreamAsync(
        int apiVersion,
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        return await SwatchResource.FromStreamAsync(apiVersion, stream, cancellationToken);
    }

    /// <summary>
    /// Creates a SwatchResource with a specific name and category.
    /// </summary>
    /// <param name="apiVersion">The API version to use</param>
    /// <param name="name">The name for the swatch collection</param>
    /// <param name="category">The category for the swatch collection</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task containing the created SwatchResource</returns>
    public Task<ISwatchResource> CreateWithCategoryAsync(
        int apiVersion,
        string name,
        SwatchCategory category,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resource = new SwatchResource(name, category, apiVersion);
        return Task.FromResult<ISwatchResource>(resource);
    }
}
