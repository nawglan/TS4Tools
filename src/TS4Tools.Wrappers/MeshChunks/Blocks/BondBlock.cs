// Source: legacy_references/Sims4Tools/s4pi Wrappers/s4piRCOLChunks/SlotAdjust.cs

using TS4Tools.Wrappers.MeshChunks;

namespace TS4Tools.Wrappers;

/// <summary>
/// Represents a slot adjustment for mesh deformation.
/// Contains offset, scale, and rotation for a named slot.
/// Source: SlotAdjust.cs Adjustment class lines 89-245
/// </summary>
public sealed class SlotAdjustment : IEquatable<SlotAdjustment>
{
    /// <summary>FNV32 hash of the slot name.</summary>
    public uint SlotNameHash { get; set; }

    /// <summary>Position offset (X, Y, Z).</summary>
    public MeshVector3 Offset { get; set; }

    /// <summary>Scale factor (X, Y, Z).</summary>
    public MeshVector3 Scale { get; set; }

    /// <summary>Rotation quaternion (X, Y, Z, W).</summary>
    public MeshVector4 Quaternion { get; set; }

    /// <summary>
    /// Creates a new slot adjustment with identity transform.
    /// </summary>
    public SlotAdjustment()
    {
        Offset = MeshVector3.Zero;
        Scale = MeshVector3.One;
        Quaternion = new MeshVector4(0, 0, 0, 1); // Identity
    }

    /// <summary>
    /// Reads a slot adjustment from the data span.
    /// Source: SlotAdjust.cs Adjustment.Parse() lines 132-146
    /// </summary>
    public static SlotAdjustment Read(ReadOnlySpan<byte> data, ref int position)
    {
        var adj = new SlotAdjustment
        {
            SlotNameHash = BinaryPrimitives.ReadUInt32LittleEndian(data[position..])
        };
        position += 4;

        adj.Offset = MeshVector3.Read(data, ref position);
        adj.Scale = MeshVector3.Read(data, ref position);
        adj.Quaternion = MeshVector4.Read(data, ref position);

        return adj;
    }

    /// <summary>
    /// Writes the slot adjustment to the buffer.
    /// Source: SlotAdjust.cs Adjustment.UnParse() lines 148-162
    /// </summary>
    public void Write(Span<byte> buffer, ref int position)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[position..], SlotNameHash);
        position += 4;

        Offset.Write(buffer, ref position);
        Scale.Write(buffer, ref position);
        Quaternion.Write(buffer, ref position);
    }

    /// <summary>Size in bytes when serialized.</summary>
    public const int Size = 4 + MeshVector3.Size + MeshVector3.Size + MeshVector4.Size; // 4 + 12 + 12 + 16 = 44

    /// <inheritdoc/>
    public bool Equals(SlotAdjustment? other)
    {
        if (other is null) return false;
        return SlotNameHash == other.SlotNameHash
            && Offset == other.Offset
            && Scale == other.Scale
            && Quaternion == other.Quaternion;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is SlotAdjustment other && Equals(other);
    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(SlotNameHash, Offset, Scale, Quaternion);
    /// <inheritdoc/>
    public override string ToString() => $"SlotAdjustment 0x{SlotNameHash:X8}";
}

/// <summary>
/// BOND block - Slot adjustment/binding data for mesh deformation.
/// Resource Type: 0x0355E0A6
/// </summary>
/// <remarks>
/// Source: s4pi Wrappers/s4piRCOLChunks/SlotAdjust.cs
/// IMPORTANT: Unlike other RCOL blocks, this block has NO tag in the binary data.
/// The tag "BOND" is used for registration only.
/// </remarks>
public sealed class BondBlock : RcolBlock
{
    /// <summary>Resource type identifier for BOND (SlotAdjust).</summary>
    public const uint TypeId = 0x0355E0A6;

    /// <inheritdoc/>
    public override string Tag => "BOND";

    /// <inheritdoc/>
    public override uint ResourceType => TypeId;

    /// <inheritdoc/>
    public override bool IsKnownType => true;

    /// <summary>Format version (typically 4).</summary>
    public uint Version { get; set; } = 4;

    /// <summary>List of slot adjustments.</summary>
    public List<SlotAdjustment> Adjustments { get; } = [];

    /// <summary>
    /// Creates an empty BOND block.
    /// </summary>
    public BondBlock() : base()
    {
    }

    /// <summary>
    /// Creates a BOND block from raw data.
    /// </summary>
    public BondBlock(ReadOnlySpan<byte> data) : base(data)
    {
    }

    /// <summary>
    /// Parses the BOND block data.
    /// IMPORTANT: This block has NO tag in the binary data (unlike other RCOL blocks).
    /// Source: SlotAdjust.cs Parse() lines 58-69
    /// </summary>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < 4)
            throw new InvalidDataException("BOND data too short");

        int pos = 0;

        // NOTE: No tag to validate! This block starts directly with version.
        // Source: SlotAdjust.cs lines 61-65 shows the tag read is commented out

        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        // Read adjustment count
        if (pos + 4 > data.Length)
            throw new InvalidDataException("BOND data too short for adjustment count");

        int count = BinaryPrimitives.ReadInt32LittleEndian(data[pos..]);
        pos += 4;

        if (count < 0 || count > 10000)
            throw new InvalidDataException($"Invalid BOND adjustment count: {count}");

        // Read adjustments
        Adjustments.Clear();
        for (int i = 0; i < count; i++)
        {
            if (pos + SlotAdjustment.Size > data.Length)
                throw new InvalidDataException($"Unexpected end of BOND data at adjustment {i}");

            Adjustments.Add(SlotAdjustment.Read(data, ref pos));
        }
    }

    /// <summary>
    /// Serializes the BOND block back to bytes.
    /// Source: SlotAdjust.cs UnParse() lines 71-83
    /// </summary>
    public override ReadOnlyMemory<byte> Serialize()
    {
        // Calculate size: version(4) + count(4) + adjustments(count * 44)
        int size = 8 + (Adjustments.Count * SlotAdjustment.Size);
        byte[] buffer = new byte[size];
        int pos = 0;

        // NOTE: No tag written (unlike other RCOL blocks)
        // Source: SlotAdjust.cs line 76 shows w.Write(tag) is commented out

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), Version);
        pos += 4;

        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(pos), Adjustments.Count);
        pos += 4;

        foreach (var adj in Adjustments)
        {
            adj.Write(buffer.AsSpan(), ref pos);
        }

        return buffer;
    }

    /// <summary>
    /// Finds an adjustment by slot name hash.
    /// </summary>
    public SlotAdjustment? FindAdjustment(uint slotNameHash) =>
        Adjustments.FirstOrDefault(a => a.SlotNameHash == slotNameHash);

    /// <summary>
    /// Adds a new adjustment.
    /// </summary>
    public void AddAdjustment(SlotAdjustment adjustment)
    {
        Adjustments.Add(adjustment);
    }
}
