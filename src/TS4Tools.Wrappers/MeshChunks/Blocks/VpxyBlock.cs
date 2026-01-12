
namespace TS4Tools.Wrappers.MeshChunks;

/// <summary>
/// VPXY (Vertex Proxy) block - contains model links and bounding box information.
/// Resource Type: 0x736884F1
/// Source: s4pi Wrappers/s4piRCOLChunks/VPXY.cs
/// </summary>
public sealed class VpxyBlock : RcolBlock
{
    /// <summary>Resource type identifier for VPXY.</summary>
    public const uint TypeId = 0x736884F1;

    /// <summary>Expected version number.</summary>
    private const uint ExpectedVersion = 4;

    /// <summary>Expected TC02 marker value.</summary>
    private const byte ExpectedTc02 = 0x02;

    /// <inheritdoc/>
    public override string Tag => "VPXY";

    /// <inheritdoc/>
    public override uint ResourceType => TypeId;

    /// <inheritdoc/>
    public override bool IsKnownType => true;

    /// <summary>Format version (expected to be 4).</summary>
    public uint Version { get; private set; } = ExpectedVersion;

    /// <summary>List of entries referencing TGI blocks.</summary>
    public List<VpxyEntry> Entries { get; } = [];

    /// <summary>TC02 marker (expected to be 0x02).</summary>
    public byte Tc02 { get; private set; } = ExpectedTc02;

    /// <summary>Bounding box for the model.</summary>
    public BoundingBox Bounds { get; private set; }

    /// <summary>Unused 4 bytes, preserved for round-trip.</summary>
    public byte[] Unused { get; private set; } = new byte[4];

    /// <summary>Whether this VPXY is modular (references an FTPT block).</summary>
    public bool IsModular { get; private set; }

    /// <summary>Index into FTPT block if modular.</summary>
    public int FtptIndex { get; private set; }

    /// <summary>TGI block list for external resource references.</summary>
    public List<RcolTgiBlock> TgiBlocks { get; } = [];

    /// <summary>
    /// Creates an empty VPXY block.
    /// </summary>
    public VpxyBlock() : base()
    {
    }

    /// <summary>
    /// Creates a VPXY block from raw data.
    /// </summary>
    public VpxyBlock(ReadOnlySpan<byte> data) : base(data)
    {
    }

    /// <summary>
    /// Parses the VPXY block data.
    /// Source: VPXY.cs lines 83-113
    /// </summary>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        int pos = 0;

        // Validate tag
        string tag = ExtractTag(data);
        if (tag != Tag)
            throw new InvalidDataException($"Invalid VPXY tag: expected '{Tag}', got '{tag}'");
        pos += 4;

        // Read version
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        if (Version != ExpectedVersion)
            throw new InvalidDataException($"Invalid VPXY version: expected 0x{ExpectedVersion:X8}, got 0x{Version:X8}");
        pos += 4;

        // TGI offset and size (relative to current position after reading these two values)
        // Source: VPXY.cs line 93: long tgiPosn = r.ReadUInt32() + s.Position;
        uint tgiOffset = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;
        uint tgiSize = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        // Calculate absolute TGI position (offset is relative to position after reading offset)
        int tgiAbsolutePos = (int)(pos - 4 + tgiOffset);

        // Read entry list
        // Source: VPXY.cs line 96: entryList = new EntryList(OnRCOLChanged, s);
        ParseEntries(data, ref pos);

        // Read TC02 marker
        // Source: VPXY.cs lines 97-99
        Tc02 = data[pos++];
        if (Tc02 != ExpectedTc02)
            throw new InvalidDataException($"Invalid VPXY TC02: expected 0x{ExpectedTc02:X2}, got 0x{Tc02:X2}");

        // Read bounding box (6 floats = 24 bytes)
        // Source: VPXY.cs line 100
        Bounds = BoundingBox.Read(data, ref pos);

        // Read unused bytes (4 bytes)
        // Source: VPXY.cs lines 101-103
        if (pos + 4 > data.Length)
            throw new InvalidDataException("VPXY data too short for unused bytes");
        Unused = data.Slice(pos, 4).ToArray();
        pos += 4;

        // Read modular flag
        // Source: VPXY.cs lines 104-108
        byte modularByte = data[pos++];
        IsModular = modularByte != 0;
        if (IsModular)
        {
            FtptIndex = BinaryPrimitives.ReadInt32LittleEndian(data[pos..]);
            pos += 4;
        }
        else
        {
            FtptIndex = 0;
        }

        // Read TGI block list
        // Source: VPXY.cs line 110
        ParseTgiBlocks(data, tgiAbsolutePos, tgiSize);
    }

    /// <summary>
    /// Parses the entry list.
    /// Source: VPXY.cs EntryList class, lines 272-294
    /// </summary>
    private void ParseEntries(ReadOnlySpan<byte> data, ref int pos)
    {
        Entries.Clear();

        // Read count (byte)
        byte entryCount = data[pos++];

        for (int i = 0; i < entryCount; i++)
        {
            // Read entry type (first byte determines type)
            // Source: VPXY.cs lines 158-164
            byte entryType = data[pos++];

            if (entryType == 0x00)
            {
                // Entry00: entryID (1 byte) + tgiIndexes list
                // Source: VPXY.cs lines 210-211
                byte entryId = data[pos++];
                byte indexCount = data[pos++];

                var indices = new List<int>(indexCount);
                for (int j = 0; j < indexCount; j++)
                {
                    indices.Add(data[pos++]);
                }

                Entries.Add(new VpxyEntry00(entryId, indices));
            }
            else if (entryType == 0x01)
            {
                // Entry01: tgiIndex (4 bytes)
                // Source: VPXY.cs lines 251-252
                int tgiIndex = BinaryPrimitives.ReadInt32LittleEndian(data[pos..]);
                pos += 4;

                Entries.Add(new VpxyEntry01(tgiIndex));
            }
            else
            {
                throw new InvalidDataException($"Unknown VPXY entry type: 0x{entryType:X2}");
            }
        }
    }

    /// <summary>
    /// Parses the TGI block list from the specified position.
    /// </summary>
    private void ParseTgiBlocks(ReadOnlySpan<byte> data, int tgiPos, uint tgiSize)
    {
        TgiBlocks.Clear();

        if (tgiPos >= data.Length)
            return;

        // TGI blocks are 16 bytes each (Instance:8, Type:4, Group:4)
        int blockCount = (int)(tgiSize / RcolTgiBlock.Size);

        for (int i = 0; i < blockCount && tgiPos + RcolTgiBlock.Size <= data.Length; i++)
        {
            TgiBlocks.Add(RcolTgiBlock.Read(data[tgiPos..]));
            tgiPos += RcolTgiBlock.Size;
        }
    }

    /// <summary>
    /// Serializes the VPXY block back to bytes.
    /// Source: VPXY.cs lines 115-146
    /// </summary>
    public override ReadOnlyMemory<byte> Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Write tag
        writer.Write((byte)'V');
        writer.Write((byte)'P');
        writer.Write((byte)'X');
        writer.Write((byte)'Y');

        // Write version
        writer.Write(Version);

        // Reserve space for TGI offset and size (will be patched later)
        long tgiOffsetPos = ms.Position;
        writer.Write(0u); // TGI offset placeholder
        writer.Write(0u); // TGI size placeholder

        // Write entry list
        // Source: VPXY.cs line 128
        SerializeEntries(writer);

        // Write TC02 marker
        writer.Write(Tc02);

        // Write bounding box
        var boundsBuffer = new byte[BoundingBox.Size];
        int boundsPos = 0;
        Bounds.Write(boundsBuffer, ref boundsPos);
        writer.Write(boundsBuffer);

        // Write unused bytes
        writer.Write(Unused);

        // Write modular flag and FTPT index
        writer.Write(IsModular ? (byte)1 : (byte)0);
        if (IsModular)
        {
            writer.Write(FtptIndex);
        }

        // Record current position for TGI offset calculation
        long tgiStartPos = ms.Position;

        // Write TGI blocks
        // Source: VPXY.cs line 141
        var tgiBuffer = new byte[RcolTgiBlock.Size];
        foreach (var tgi in TgiBlocks)
        {
            tgi.Write(tgiBuffer);
            writer.Write(tgiBuffer);
        }

        // Patch TGI offset and size
        // Offset is relative to position after reading the offset value (which is at tgiOffsetPos + 4)
        uint tgiOffset = (uint)(tgiStartPos - (tgiOffsetPos + 4));
        uint tgiSize = (uint)(TgiBlocks.Count * RcolTgiBlock.Size);

        ms.Position = tgiOffsetPos;
        writer.Write(tgiOffset);
        writer.Write(tgiSize);

        return ms.ToArray();
    }

    /// <summary>
    /// Serializes the entry list.
    /// </summary>
    private void SerializeEntries(BinaryWriter writer)
    {
        // Write count (byte)
        writer.Write((byte)Entries.Count);

        foreach (var entry in Entries)
        {
            entry.Write(writer);
        }
    }
}

/// <summary>
/// Base class for VPXY entries.
/// Source: VPXY.cs Entry class, lines 150-193
/// </summary>
public abstract class VpxyEntry
{
    /// <summary>The entry type (0x00 or 0x01).</summary>
    public abstract byte EntryType { get; }

    /// <summary>Writes the entry to the writer.</summary>
    internal abstract void Write(BinaryWriter writer);
}

/// <summary>
/// VPXY Entry type 0x00 - contains an entry ID and list of TGI indices.
/// Source: VPXY.cs Entry00 class, lines 194-241
/// </summary>
public sealed class VpxyEntry00 : VpxyEntry
{
    /// <inheritdoc/>
    public override byte EntryType => 0x00;

    /// <summary>Entry identifier.</summary>
    public byte EntryId { get; }

    /// <summary>List of TGI indices (each stored as a byte in legacy).</summary>
    public IReadOnlyList<int> TgiIndices { get; }

    public VpxyEntry00(byte entryId, IReadOnlyList<int> tgiIndices)
    {
        EntryId = entryId;
        TgiIndices = tgiIndices;
    }

    /// <inheritdoc/>
    internal override void Write(BinaryWriter writer)
    {
        writer.Write(EntryType);
        writer.Write(EntryId);
        writer.Write((byte)TgiIndices.Count);
        foreach (var index in TgiIndices)
        {
            writer.Write((byte)index);
        }
    }
}

/// <summary>
/// VPXY Entry type 0x01 - contains a single TGI index.
/// Source: VPXY.cs Entry01 class, lines 242-270
/// </summary>
public sealed class VpxyEntry01 : VpxyEntry
{
    /// <inheritdoc/>
    public override byte EntryType => 0x01;

    /// <summary>TGI index reference.</summary>
    public int TgiIndex { get; }

    public VpxyEntry01(int tgiIndex)
    {
        TgiIndex = tgiIndex;
    }

    /// <inheritdoc/>
    internal override void Write(BinaryWriter writer)
    {
        writer.Write(EntryType);
        writer.Write(TgiIndex);
    }
}
