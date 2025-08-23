using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Catalog;

/// <summary>
/// Factory for creating CWAL (Catalog Wall Pattern) resources that handle wall pattern catalog entries.
/// </summary>
public sealed class CWALResourceFactory : ResourceFactoryBase<CWALResource>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CWALResourceFactory"/> class.
    /// </summary>
    public CWALResourceFactory() : base(new[] { "CWAL", "0xD5F0F921" }, priority: 50)
    {
    }

    /// <inheritdoc/>
    public override async Task<CWALResource> CreateResourceAsync(
        int apiVersion,
        Stream? stream = null,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // For async compliance

        // Create a basic CWAL resource
        var key = new ResourceKey(0xD5F0F921, 0x00000000, 0x0000000000000000);
        byte[] data;

        if (stream != null)
        {
            data = await ReadStreamAsync(stream, cancellationToken);
        }
        else
        {
            // Create minimal CWAL header
            data = CreateMinimalCWALData();
        }

        return new CWALResource(key, data);
    }

    /// <summary>
    /// Creates minimal CWAL data with basic structure.
    /// </summary>
    /// <returns>The minimal CWAL data.</returns>
    private static byte[] CreateMinimalCWALData()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        
        // Write version (CWAL typically uses version 7)
        writer.Write((uint)7);
        
        // Write minimal catalog common section
        writer.Write((uint)0); // Name length (empty)
        
        // Write placeholder for material entries
        writer.Write((uint)0); // Material entry count
        
        // Write placeholder for image groups
        writer.Write((uint)0); // Image group count
        
        // Pad to minimum size
        while (stream.Length < 64)
        {
            writer.Write((byte)0);
        }
        
        return stream.ToArray();
    }

    private static async Task<byte[]> ReadStreamAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        return memoryStream.ToArray();
    }
}
