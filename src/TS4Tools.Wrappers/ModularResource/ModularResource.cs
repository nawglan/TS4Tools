using TS4Tools.Resources;

namespace TS4Tools.Wrappers;

/// <summary>
/// Modular resource containing TGI indexes and TGI blocks.
/// Resource Type: 0xCF9A4ACE
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/ModularResource/ModularResource.cs
/// </summary>
public sealed class ModularResource : TypedResource
{
    private readonly List<short> _tgiIndexes = [];
    private readonly List<ResourceKey> _tgiBlocks = [];

    /// <summary>
    /// Unknown field 1.
    /// </summary>
    public ushort Unknown1 { get; set; }

    /// <summary>
    /// Unknown field 2.
    /// </summary>
    public ushort Unknown2 { get; set; }

    /// <summary>
    /// The TGI index list - indexes into TgiBlocks.
    /// </summary>
    public IReadOnlyList<short> TgiIndexes => _tgiIndexes;

    /// <summary>
    /// The TGI block list.
    /// </summary>
    public IReadOnlyList<ResourceKey> TgiBlocks => _tgiBlocks;

    /// <summary>
    /// Creates a new ModularResource by parsing data.
    /// </summary>
    public ModularResource(ResourceKey key, ReadOnlyMemory<byte> data) : base(key, data)
    {
    }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        // Minimum: unknown1(2) + tgiOffset(4) + tgiSize(4) + unknown2(2) + indexCount(2) = 14 bytes
        if (data.Length < 14)
            throw new ResourceFormatException("Modular resource data too short for header.");

        int offset = 0;

        // Read unknown1
        Unknown1 = BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]);
        offset += 2;

        // Read TGI offset (relative to current position after reading the offset)
        uint tgiOffset = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;
        long tgiPosn = offset + tgiOffset; // Absolute position of TGI blocks

        // Read TGI size
        uint tgiSize = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Read unknown2
        Unknown2 = BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]);
        offset += 2;

        // Read index count (Int16 count per legacy)
        int indexCount = BinaryPrimitives.ReadInt16LittleEndian(data[offset..]);
        offset += 2;

        if (indexCount < 0 || indexCount > short.MaxValue)
            throw new ResourceFormatException($"Invalid TGI index count: {indexCount}");

        // Validate we have enough space for indexes before TGI blocks
        int requiredForIndexes = offset + (indexCount * 2);
        if (requiredForIndexes > data.Length)
            throw new ResourceFormatException($"Modular resource data too short for indexes. Need {requiredForIndexes}, have {data.Length}.");

        // Read TGI indexes
        _tgiIndexes.Clear();
        _tgiIndexes.EnsureCapacity(indexCount);
        for (int i = 0; i < indexCount; i++)
        {
            short index = BinaryPrimitives.ReadInt16LittleEndian(data[offset..]);
            offset += 2;
            _tgiIndexes.Add(index);
        }

        // Read TGI blocks from tgiPosn
        _tgiBlocks.Clear();
        if (tgiSize > 0 && tgiPosn < data.Length)
        {
            // Each TGI block is 16 bytes (4 type + 4 group + 8 instance)
            int tgiBlockCount = (int)(tgiSize / 16);
            _tgiBlocks.EnsureCapacity(tgiBlockCount);

            int tgiReadOffset = (int)tgiPosn;
            for (int i = 0; i < tgiBlockCount && tgiReadOffset + 16 <= data.Length; i++)
            {
                uint type = BinaryPrimitives.ReadUInt32LittleEndian(data[tgiReadOffset..]);
                tgiReadOffset += 4;
                uint group = BinaryPrimitives.ReadUInt32LittleEndian(data[tgiReadOffset..]);
                tgiReadOffset += 4;
                ulong instance = BinaryPrimitives.ReadUInt64LittleEndian(data[tgiReadOffset..]);
                tgiReadOffset += 8;

                _tgiBlocks.Add(new ResourceKey(type, group, instance));
            }
        }
    }

    /// <inheritdoc/>
    protected override ReadOnlyMemory<byte> Serialize()
    {
        // Calculate sizes
        int headerSize = 2 + 4 + 4 + 2 + 2; // unknown1 + tgiOffset + tgiSize + unknown2 + count
        int indexesSize = _tgiIndexes.Count * 2;
        int tgiBlocksSize = _tgiBlocks.Count * 16;
        int totalSize = headerSize + indexesSize + tgiBlocksSize;

        var buffer = new byte[totalSize];
        int offset = 0;

        // Write unknown1
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(offset), Unknown1);
        offset += 2;

        // Calculate TGI offset (from after the offset field to the TGI blocks)
        // TGI blocks start after: tgiSize(4) + unknown2(2) + count(2) + indexes(indexesSize)
        uint tgiOffset = (uint)(4 + 2 + 2 + indexesSize);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), tgiOffset);
        offset += 4;

        // Write TGI size
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), (uint)tgiBlocksSize);
        offset += 4;

        // Write unknown2
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(offset), Unknown2);
        offset += 2;

        // Write index count
        BinaryPrimitives.WriteInt16LittleEndian(buffer.AsSpan(offset), (short)_tgiIndexes.Count);
        offset += 2;

        // Write indexes
        foreach (short index in _tgiIndexes)
        {
            BinaryPrimitives.WriteInt16LittleEndian(buffer.AsSpan(offset), index);
            offset += 2;
        }

        // Write TGI blocks
        foreach (var tgi in _tgiBlocks)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), tgi.ResourceType);
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), tgi.ResourceGroup);
            offset += 4;
            BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(offset), tgi.Instance);
            offset += 8;
        }

        return buffer;
    }

    /// <inheritdoc/>
    protected override void InitializeDefaults()
    {
        Unknown1 = 0;
        Unknown2 = 0;
        _tgiIndexes.Clear();
        _tgiBlocks.Clear();
    }

    /// <summary>
    /// Adds a TGI index.
    /// </summary>
    public void AddIndex(short index)
    {
        _tgiIndexes.Add(index);
        OnChanged();
    }

    /// <summary>
    /// Adds a TGI block.
    /// </summary>
    public void AddTgiBlock(ResourceKey block)
    {
        _tgiBlocks.Add(block);
        OnChanged();
    }

    /// <summary>
    /// Clears all indexes.
    /// </summary>
    public void ClearIndexes()
    {
        _tgiIndexes.Clear();
        OnChanged();
    }

    /// <summary>
    /// Clears all TGI blocks.
    /// </summary>
    public void ClearTgiBlocks()
    {
        _tgiBlocks.Clear();
        OnChanged();
    }

    /// <summary>
    /// Gets the TGI block at the specified index.
    /// </summary>
    public ResourceKey? GetTgiBlockAtIndex(int index)
    {
        if (index < 0 || index >= _tgiIndexes.Count)
            return null;

        short blockIndex = _tgiIndexes[index];
        if (blockIndex < 0 || blockIndex >= _tgiBlocks.Count)
            return null;

        return _tgiBlocks[blockIndex];
    }
}
