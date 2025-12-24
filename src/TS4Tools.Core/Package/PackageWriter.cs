using System.Buffers.Binary;
using TS4Tools.Compression;

namespace TS4Tools.Package;

/// <summary>
/// Writes a package to a stream.
/// </summary>
internal sealed class PackageWriter
{
    private readonly DbpfPackage _package;
    private readonly List<ResourceIndexEntry> _resources;
    private readonly Stream? _sourceStream;

    public PackageWriter(DbpfPackage package, List<ResourceIndexEntry> resources, Stream? sourceStream)
    {
        _package = package;
        _resources = resources;
        _sourceStream = sourceStream;
    }

    public async Task WriteAsync(Stream output, CancellationToken cancellationToken)
    {
        // Write header (will update index fields later)
        var header = (byte[])_package.Header.Clone();
        await output.WriteAsync(header, cancellationToken).ConfigureAwait(false);

        // Collect unique values to determine optimal index type
        var types = new HashSet<uint>();
        var groups = new HashSet<uint>();
        var instanceHighs = new HashSet<uint>();

        foreach (var entry in _resources)
        {
            if (entry.IsDeleted) continue;
            types.Add(entry.Key.ResourceType);
            groups.Add(entry.Key.ResourceGroup);
            instanceHighs.Add((uint)(entry.Key.Instance >> 32));
        }

        // Calculate index type flags
        uint indexType = 0;
        if (types.Count <= 1) indexType |= 0x01;
        if (groups.Count <= 1) indexType |= 0x02;
        if (instanceHighs.Count <= 1) indexType |= 0x04;

        // Prepare new entries with updated offsets
        var newEntries = new List<(ResourceIndexEntry Entry, byte[] Data)>();

        foreach (var entry in _resources)
        {
            if (entry.IsDeleted) continue;

            byte[] resourceData = await GetResourceDataAsync(entry, cancellationToken).ConfigureAwait(false);
            byte[] dataToWrite;
            bool isCompressed;

            if (entry.CompressionType != 0 || entry.IsDirty)
            {
                // Compress if previously compressed or if dirty (new/modified data)
                (dataToWrite, isCompressed) = Compressor.Compress(resourceData);
            }
            else
            {
                // Keep uncompressed
                dataToWrite = resourceData;
                isCompressed = false;
            }

            var newEntry = entry.Clone();
            newEntry.ChunkOffset = (uint)output.Position;
            newEntry.MemorySize = (uint)resourceData.Length;

            if (isCompressed)
            {
                newEntry.FileSize = (uint)dataToWrite.Length;
                newEntry.CompressionType = Compressor.ZlibCompressionType;
            }
            else
            {
                newEntry.FileSize = (uint)dataToWrite.Length;
                newEntry.CompressionType = Compressor.NoCompression;
            }

            // Write resource data
            await output.WriteAsync(dataToWrite, cancellationToken).ConfigureAwait(false);
            newEntries.Add((newEntry, dataToWrite));
        }

        // Write index
        long indexPosition = output.Position;
        int indexSize = await WriteIndexAsync(output, indexType, newEntries.Select(x => x.Entry).ToList(), cancellationToken).ConfigureAwait(false);

        // Update header with final values
        output.Position = 36; // Index count
        var buffer = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(buffer, newEntries.Count);
        await output.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);

        output.Position = 44; // Index size
        BinaryPrimitives.WriteInt32LittleEndian(buffer, indexSize);
        await output.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);

        output.Position = 40; // Index position low (set to 0)
        BinaryPrimitives.WriteInt32LittleEndian(buffer, 0);
        await output.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);

        output.Position = 64; // Index position
        BinaryPrimitives.WriteInt32LittleEndian(buffer, (int)indexPosition);
        await output.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);

        await output.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<byte[]> GetResourceDataAsync(ResourceIndexEntry entry, CancellationToken cancellationToken)
    {
        // If we have cached/modified data, use it
        if (entry.ResourceData.HasValue)
            return entry.ResourceData.Value.ToArray();

        // Otherwise read from source stream
        if (_sourceStream == null)
            throw new InvalidOperationException("No source stream for clean resource.");

        _sourceStream.Position = entry.ChunkOffset;
        var data = new byte[entry.FileSize];
        int bytesRead = await _sourceStream.ReadAsync(data, cancellationToken).ConfigureAwait(false);
        if (bytesRead != entry.FileSize)
            throw new PackageFormatException("Unexpected end of file reading resource for save.");

        // If compressed, decompress first
        if (entry.IsCompressed)
        {
            return Decompressor.Decompress(data, (int)entry.MemorySize);
        }

        return data;
    }

    private static async Task<int> WriteIndexAsync(
        Stream output,
        uint indexType,
        List<ResourceIndexEntry> entries,
        CancellationToken cancellationToken)
    {
        long startPosition = output.Position;

        // Get shared header values
        uint headerType = entries.Count > 0 ? entries[0].Key.ResourceType : 0;
        uint headerGroup = entries.Count > 0 ? entries[0].Key.ResourceGroup : 0;
        uint headerInstanceHigh = entries.Count > 0 ? (uint)(entries[0].Key.Instance >> 32) : 0;

        // Write index type
        var buffer = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, indexType);
        await output.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);

        // Write header fields based on index type
        if ((indexType & 0x01) != 0)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, headerType);
            await output.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        }
        if ((indexType & 0x02) != 0)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, headerGroup);
            await output.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        }
        if ((indexType & 0x04) != 0)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, headerInstanceHigh);
            await output.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        }

        // Write entries
        foreach (var entry in entries)
        {
            await WriteEntryAsync(output, entry, indexType, cancellationToken).ConfigureAwait(false);
        }

        return (int)(output.Position - startPosition);
    }

    private static async Task WriteEntryAsync(
        Stream output,
        ResourceIndexEntry entry,
        uint indexType,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[4];

        // Write Type if not in header
        if ((indexType & 0x01) == 0)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, entry.Key.ResourceType);
            await output.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        }

        // Write Group if not in header
        if ((indexType & 0x02) == 0)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, entry.Key.ResourceGroup);
            await output.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        }

        // Write InstanceHigh if not in header
        if ((indexType & 0x04) == 0)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, (uint)(entry.Key.Instance >> 32));
            await output.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        }

        // InstanceLow (always in entry)
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, (uint)(entry.Key.Instance & 0xFFFFFFFF));
        await output.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);

        // ChunkOffset
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, entry.ChunkOffset);
        await output.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);

        // FileSize (with bit 31 set)
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, entry.FileSize | 0x80000000);
        await output.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);

        // MemSize
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, entry.MemorySize);
        await output.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);

        // Compressed + Unknown2
        var buffer2 = new byte[4];
        BinaryPrimitives.WriteUInt16LittleEndian(buffer2, entry.CompressionType);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer2.AsSpan(2), entry.Unknown2);
        await output.WriteAsync(buffer2, cancellationToken).ConfigureAwait(false);
    }
}
