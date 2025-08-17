using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Visual;

/// <summary>
/// Factory for creating mask resources that handle alpha masks and overlays for transparency effects.
/// </summary>
public sealed class MaskResourceFactory : ResourceFactoryBase<MaskResource>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MaskResourceFactory"/> class.
    /// </summary>
    public MaskResourceFactory() : base(new[] { "MASK", "0x00B2D882" }, priority: 50)
    {
    }

    /// <inheritdoc/>
    public override async Task<MaskResource> CreateResourceAsync(
        int apiVersion,
        Stream? stream = null,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // For async compliance

        // Create a basic mask resource - real implementation would parse dimensions from stream
        var key = new ResourceKey(0x00B2D882, 0x00000000, 0x0000000000000000);
        var data = stream != null ? await ReadStreamAsync(stream, cancellationToken) : new byte[100 * 100];

        return new MaskResource(key, data, 100, 100, 8, 1, MaskFormat.Alpha);
    }

    private static async Task<byte[]> ReadStreamAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        return memoryStream.ToArray();
    }
}

/// <summary>
/// Factory for creating thumbnail resources that handle preview and thumbnail generation.
/// </summary>
public sealed class ThumbnailResourceFactory : ResourceFactoryBase<ThumbnailResource>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ThumbnailResourceFactory"/> class.
    /// </summary>
    public ThumbnailResourceFactory() : base(new[] { "THUM", "THUMB", "0x3453CF95" }, priority: 50)
    {
    }

    /// <inheritdoc/>
    public override async Task<ThumbnailResource> CreateResourceAsync(
        int apiVersion,
        Stream? stream = null,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // For async compliance

        // Create a basic thumbnail resource
        var key = new ResourceKey(0x3453CF95, 0x00000000, 0x0000000000000000);
        var data = stream != null ? await ReadStreamAsync(stream, cancellationToken) : new byte[8]; // Minimal header
        var imageData = data.Length > 8 ? data[8..] : Array.Empty<byte>();

        return new ThumbnailResource(key, data, 100, 100, ThumbnailFormat.PNG, 85, false, imageData);
    }

    private static async Task<byte[]> ReadStreamAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        return memoryStream.ToArray();
    }
}

/// <summary>
/// Factory for creating material resources that handle material definitions with shader parameters.
/// </summary>
public sealed class MaterialResourceFactory : ResourceFactoryBase<MaterialResource>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MaterialResourceFactory"/> class.
    /// </summary>
    public MaterialResourceFactory() : base(new[] { "MATD", "MATERIAL", "0x015A1849" }, priority: 50)
    {
    }

    /// <inheritdoc/>
    protected override void ValidateApiVersion(int apiVersion)
    {
        base.ValidateApiVersion(apiVersion);

        if (apiVersion > 10) // Reasonable upper bound for API versions
        {
            throw new ArgumentException($"API version {apiVersion} is not supported by {nameof(MaterialResourceFactory)}", nameof(apiVersion));
        }
    }

    /// <inheritdoc/>
    public override async Task<MaterialResource> CreateResourceAsync(
        int apiVersion,
        Stream? stream = null,
        CancellationToken cancellationToken = default)
    {
        ValidateApiVersion(apiVersion);

        await Task.CompletedTask; // For async compliance

        // Create a basic material resource
        var key = new ResourceKey(0x015A1849, 0x00000000, 0x0000000000000000);
        return new MaterialResource(key, "DefaultMaterial", MaterialType.Standard);
    }
}
