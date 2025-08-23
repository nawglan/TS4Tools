using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Materials;

/// <summary>
/// Factory for creating MTBL (Material Table) resources that handle material definitions and model references.
/// </summary>
public sealed class MTBLResourceFactory : ResourceFactoryBase<MTBLResource>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MTBLResourceFactory"/> class.
    /// </summary>
    public MTBLResourceFactory() : base(new[] { "MTBL", "0x81CA1A10" }, priority: 50)
    {
    }

    /// <inheritdoc/>
    public override async Task<MTBLResource> CreateResourceAsync(
        int apiVersion,
        Stream? stream = null,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // For async compliance

        // Create a basic MTBL resource
        var key = new ResourceKey(0x81CA1A10, 0x00000000, 0x0000000000000000);
        byte[] data;

        if (stream != null)
        {
            data = await ReadStreamAsync(stream, cancellationToken);
        }
        else
        {
            // Create minimal MTBL header
            data = new byte[8];
            using var writer = new BinaryWriter(new MemoryStream(data));
            writer.Write((uint)1); // Version
            writer.Write((uint)0); // Entry count
        }

        return new MTBLResource(key, data);
    }

    private static async Task<byte[]> ReadStreamAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        return memoryStream.ToArray();
    }
}
