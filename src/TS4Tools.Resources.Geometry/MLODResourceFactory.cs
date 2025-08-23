using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Geometry;

/// <summary>
/// Factory for creating MLOD (Mesh Level of Detail) resources that handle 3D object geometry LODs.
/// </summary>
public sealed class MLODResourceFactory : ResourceFactoryBase<MLODResource>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MLODResourceFactory"/> class.
    /// </summary>
    public MLODResourceFactory() : base(new[] { "MLOD", "0x01D10F34" }, priority: 50)
    {
    }

    /// <inheritdoc/>
    public override async Task<MLODResource> CreateResourceAsync(
        int apiVersion,
        Stream? stream = null,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // For async compliance

        // Create a basic MLOD resource
        var key = new ResourceKey(0x01D10F34, 0x00000000, 0x0000000000000000);
        byte[] data;
        uint version = 1;
        int meshCount = 0;

        if (stream != null)
        {
            data = await ReadStreamAsync(stream, cancellationToken);
            
            // Try to parse basic MLOD structure if we have enough data
            if (data.Length >= 8)
            {
                try
                {
                    // Read MLOD header (simplified)
                    using var reader = new BinaryReader(new MemoryStream(data));
                    var tag = reader.ReadUInt32(); // Should be "MLOD"
                    version = reader.ReadUInt32();
                    
                    // Rough estimate of mesh count based on remaining data
                    // This is simplified - real MLOD parsing would be more complex
                    meshCount = Math.Max(0, (data.Length - 8) / 64); // Estimate
                }
                catch
                {
                    // If parsing fails, use defaults
                    version = 1;
                    meshCount = 0;
                }
            }
        }
        else
        {
            // Create minimal MLOD header
            data = new byte[8];
            using var writer = new BinaryWriter(new MemoryStream(data));
            writer.Write(0x444F4C4D); // "MLOD" in little-endian
            writer.Write(version);
        }

        return new MLODResource(key, data, version, meshCount);
    }

    private static async Task<byte[]> ReadStreamAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        return memoryStream.ToArray();
    }
}
