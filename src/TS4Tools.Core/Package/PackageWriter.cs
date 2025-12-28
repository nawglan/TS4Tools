using TS4Tools.Compression;

namespace TS4Tools.Package;

/// <summary>
/// Writes a package to a stream.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs
///
/// Saving logic - SaveAs(Stream) (lines 96-174):
/// - Write header first (line 99)
/// - Write all resource data with updated offsets (lines 102-147)
/// - Write index at current position (lines 149-156)
/// - Update header fields: indexcount, indexsize, indexposition (lines 158-166)
///
/// Index type optimization (lines 101-111):
/// - Analyzes unique Type/Group/InstanceHigh values
/// - Sets flags to store common values in index header
/// - 0x01: Type shared, 0x02: Group shared, 0x04: InstanceHigh shared
///
/// FileSize bit 31 must always be set when writing (ResourceIndexEntry.cs line 99).
/// </remarks>
internal sealed class PackageWriter
{
    private readonly DbpfPackage _package;
    private readonly List<ResourceIndexEntry> _resources;
    private readonly Stream? _sourceStream;

    /// <summary>
    /// Converts a long value to uint with overflow checking for DBPF 4GB limit.
    /// </summary>
    private static uint ToUInt32Checked(long value, string fieldName)
    {
        if (value < 0 || value > uint.MaxValue)
            throw new PackageFormatException(
                $"{fieldName} ({value:N0}) exceeds 32-bit limit. DBPF format is limited to 4GB.");
        return (uint)value;
    }

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
            newEntry.ChunkOffset = ToUInt32Checked(output.Position, "Chunk offset");
            newEntry.MemorySize = ToUInt32Checked(resourceData.Length, "Memory size");
            newEntry.FileSize = ToUInt32Checked(dataToWrite.Length, "File size");

            if (isCompressed)
            {
                newEntry.CompressionType = Compressor.ZlibCompressionType;
            }
            else
            {
                newEntry.CompressionType = Compressor.NoCompression;
            }

            // Write resource data
            await output.WriteAsync(dataToWrite, cancellationToken).ConfigureAwait(false);
            newEntries.Add((newEntry, dataToWrite));
        }

        // Reuse a single buffer for all 4-byte writes to reduce allocations
        var buffer = new byte[4];

        // Write index
        long indexPosition = output.Position;
        int indexSize = await WriteIndexAsync(output, indexType, newEntries.Select(x => x.Entry).ToList(), buffer, cancellationToken).ConfigureAwait(false);

        // Update header with final values
        output.Position = 36; // Index count
        BinaryPrimitives.WriteInt32LittleEndian(buffer, newEntries.Count);
        await output.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);

        output.Position = 44; // Index size
        BinaryPrimitives.WriteInt32LittleEndian(buffer, indexSize);
        await output.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);

        output.Position = 40; // Index position low (set to 0)
        BinaryPrimitives.WriteInt32LittleEndian(buffer, 0);
        await output.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);

        output.Position = 64; // Index position
        if (indexPosition > int.MaxValue)
            throw new PackageFormatException(
                $"Index position ({indexPosition:N0}) exceeds 32-bit limit. DBPF format is limited to 4GB.");
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
        byte[] buffer,
        CancellationToken cancellationToken)
    {
        long startPosition = output.Position;

        // Get shared header values
        uint headerType = entries.Count > 0 ? entries[0].Key.ResourceType : 0;
        uint headerGroup = entries.Count > 0 ? entries[0].Key.ResourceGroup : 0;
        uint headerInstanceHigh = entries.Count > 0 ? (uint)(entries[0].Key.Instance >> 32) : 0;

        // Write index type
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
            await WriteEntryAsync(output, entry, indexType, buffer, cancellationToken).ConfigureAwait(false);
        }

        return (int)(output.Position - startPosition);
    }

    private static async Task WriteEntryAsync(
        Stream output,
        ResourceIndexEntry entry,
        uint indexType,
        byte[] buffer,
        CancellationToken cancellationToken)
    {
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

        // Compressed + Unknown2 (reuse buffer for 2 uint16 values)
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, entry.CompressionType);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(2), entry.Unknown2);
        await output.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
    }
}
