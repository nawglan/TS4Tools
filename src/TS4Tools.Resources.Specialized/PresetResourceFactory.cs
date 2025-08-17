using TS4Tools.Core.Resources;
using TS4Tools.Core.Interfaces.Resources.Specialized;

namespace TS4Tools.Resources.Specialized;

/// <summary>
/// Factory for creating PresetResource instances following the TS4Tools resource factory pattern.
/// </summary>
public sealed class PresetResourceFactory : ResourceFactoryBase<IPresetResource>
{
    /// <summary>
    /// Initializes a new instance of the PresetResourceFactory class.
    /// </summary>
    public PresetResourceFactory()
        : base(new[] { $"0x{PresetResource.ResourceType:X8}" })
    {
    }

    /// <summary>
    /// Creates a new empty PresetResource asynchronously.
    /// </summary>
    /// <param name="apiVersion">The API version to use</param>
    /// <param name="stream">Optional stream containing resource data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task containing the created PresetResource</returns>
    public override Task<IPresetResource> CreateResourceAsync(
        int apiVersion,
        Stream? stream = null,
        CancellationToken cancellationToken = default)
    {
        if (stream == null)
        {
            // Default generic preset
            var defaultResource = new PresetResource("Generic", "Unnamed", apiVersion);
            return Task.FromResult<IPresetResource>(defaultResource);
        }

        // Create from stream data
        return CreateFromStreamAsync(apiVersion, "Generic", stream, cancellationToken);
    }

    /// <summary>
    /// Gets the format information for this factory.
    /// </summary>
    public ResourceFormatInfo FormatInfo => new()
    {
        ResourceType = PresetResource.ResourceType,
        Name = "PresetResource",
        Description = "Generic preset data storage and management",
        SupportedVersion = 1,
        FileExtensions = new[] { ".preset" },
        MimeType = "application/x-sims4-preset"
    };

    /// <summary>
    /// Creates a PresetResource from binary data asynchronously.
    /// </summary>
    /// <param name="apiVersion">The API version to use</param>
    /// <param name="presetType">The preset type identifier</param>
    /// <param name="data">The binary data to parse</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task containing the created PresetResource</returns>
    public Task<IPresetResource> CreateFromDataAsync(
        int apiVersion,
        string presetType,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IPresetResource>(cancellationToken);
        }

        var resource = PresetResource.FromData(presetType, apiVersion, data.Span);
        return Task.FromResult<IPresetResource>(resource);
    }

    /// <summary>
    /// Creates a PresetResource from a stream asynchronously.
    /// </summary>
    /// <param name="apiVersion">The API version to use</param>
    /// <param name="presetType">The preset type identifier</param>
    /// <param name="stream">The stream to read from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task containing the created PresetResource</returns>
    public async Task<IPresetResource> CreateFromStreamAsync(
        int apiVersion,
        string presetType,
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        return await PresetResource.FromStreamAsync(presetType, apiVersion, stream, cancellationToken);
    }
}
