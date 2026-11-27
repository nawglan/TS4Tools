
namespace TS4Tools.Wrappers.MeshChunks;

/// <summary>
/// IBUF (Index Buffer) block - contains triangle/primitive indices for mesh rendering.
/// Resource Type: 0x01D0E70F
/// Source: s4pi Wrappers/MeshChunks/IBUF.cs
/// </summary>
public class IbufBlock : RcolBlock
{
    /// <summary>Resource type identifier for IBUF.</summary>
    public const uint TypeId = 0x01D0E70F;

    /// <summary>Resource type identifier for IBUF2 (shadow variant).</summary>
    public const uint TypeId2 = 0x0229684F;

    /// <inheritdoc/>
    public override string Tag => "IBUF";

    /// <inheritdoc/>
    public override uint ResourceType => TypeId;

    /// <inheritdoc/>
    public override bool IsKnownType => true;

    /// <summary>Format version.</summary>
    public uint Version { get; private set; }

    /// <summary>Format flags indicating compression and index size.</summary>
    public IbufFormat Flags { get; private set; }

    /// <summary>Display list usage value.</summary>
    public uint DisplayListUsage { get; private set; }

    /// <summary>The index buffer data (always stored as 32-bit internally).</summary>
    public int[] Buffer { get; private set; } = [];

    /// <summary>
    /// Creates an empty IBUF block.
    /// </summary>
    public IbufBlock() : base()
    {
    }

    /// <summary>
    /// Creates an IBUF block from raw data.
    /// </summary>
    public IbufBlock(ReadOnlySpan<byte> data) : base(data)
    {
    }

    /// <summary>
    /// Parses the IBUF block data.
    /// Handles differenced indices and 16/32-bit formats.
    /// Source: s4pi Wrappers/MeshChunks/IBUF.cs Parse() lines 177-204
    /// </summary>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        int pos = 0;

        // Validate tag
        string tag = ExtractTag(data);
        if (tag != Tag)
            throw new InvalidDataException($"Invalid IBUF tag: expected '{Tag}', got '{tag}'");
        pos += 4;

        // Read header
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        Flags = (IbufFormat)BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        DisplayListUsage = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        // Determine format and read indices
        bool is32Bit = (Flags & IbufFormat.Uses32BitIndices) != 0;
        bool isDifferenced = (Flags & IbufFormat.DifferencedIndices) != 0;
        int indexSize = is32Bit ? 4 : 2;
        int remainingBytes = data.Length - pos;
        int indexCount = remainingBytes / indexSize;

        // Validate
        if (indexCount < 0 || indexCount > 10_000_000)
            throw new InvalidDataException($"Invalid IBUF index count: {indexCount}");

        Buffer = new int[indexCount];
        int lastValue = 0;

        for (int i = 0; i < indexCount; i++)
        {
            int current;
            if (is32Bit)
            {
                current = BinaryPrimitives.ReadInt32LittleEndian(data[pos..]);
                pos += 4;
            }
            else
            {
                current = BinaryPrimitives.ReadInt16LittleEndian(data[pos..]);
                pos += 2;
            }

            // Apply delta decoding if differenced
            if (isDifferenced)
            {
                current += lastValue;
            }

            Buffer[i] = current;
            lastValue = current;
        }
    }

    /// <summary>
    /// Whether every value written (index, or delta when differenced) fits the 16-bit field, which legacy reads
    /// back sign-extended (IBUF.cs line 194) and writes as UInt16 (line 235).
    /// </summary>
    private bool StoredValuesFit16Bit(bool isDifferenced)
    {
        int last = 0;
        foreach (int current in Buffer)
        {
            int stored = isDifferenced ? current - last : current;
            last = current;
            if (stored < short.MinValue || stored > ushort.MaxValue)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Serializes the IBUF block back to bytes.
    /// Source: s4pi Wrappers/MeshChunks/IBUF.cs UnParse() lines 206-239
    /// </summary>
    public override ReadOnlyMemory<byte> Serialize()
    {
        // Determine if we need 32-bit indices.
        // Source: IBUF.cs line 210 - legacy switches to 32-bit exactly when there are more than 65535 indices
        // and otherwise writes every value (or delta) as UInt16. Real game/mod meshes contradict that rule in
        // both directions (a game IBUF with 74,832 16-bit differenced indices, a mod IBUF with 64,614 32-bit
        // indices), so legacy rewrites them with a different width. The port keeps the stored width and only
        // widens a 16-bit buffer when a stored value (index, or delta when differenced) no longer fits.
        bool isDifferenced = (Flags & IbufFormat.DifferencedIndices) != 0;
        bool is32Bit = (Flags & IbufFormat.Uses32BitIndices) != 0 || !StoredValuesFit16Bit(isDifferenced);

        // Update flags
        var flags = Flags;
        if (is32Bit)
            flags |= IbufFormat.Uses32BitIndices;
        else
            flags &= ~IbufFormat.Uses32BitIndices;

        int indexSize = is32Bit ? 4 : 2;
        int size = 16 + (Buffer.Length * indexSize);
        byte[] buffer = new byte[size];
        int pos = 0;

        // Write tag
        buffer[pos++] = (byte)'I';
        buffer[pos++] = (byte)'B';
        buffer[pos++] = (byte)'U';
        buffer[pos++] = (byte)'F';

        // Write header
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), Version);
        pos += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), (uint)flags);
        pos += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), DisplayListUsage);
        pos += 4;

        // Write indices with optional delta encoding
        int lastValue = 0;
        for (int i = 0; i < Buffer.Length; i++)
        {
            int current = Buffer[i];
            int toWrite = isDifferenced ? current - lastValue : current;
            lastValue = current;

            if (is32Bit)
            {
                BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(pos), toWrite);
                pos += 4;
            }
            else
            {
                BinaryPrimitives.WriteInt16LittleEndian(buffer.AsSpan(pos), (short)toWrite);
                pos += 2;
            }
        }

        return buffer;
    }

    /// <summary>
    /// Gets indices for a mesh's primitive range.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pi Wrappers/MeshChunks/IBUF.cs lines 77-85. The range comes from the
    /// MLOD/GEOM data, so it is checked against the buffer (in long arithmetic) before allocating; legacy relied on
    /// Array.Copy failing after allocating.
    /// </remarks>
    /// <exception cref="InvalidDataException">The range does not lie inside the index buffer.</exception>
    public int[] GetIndices(ModelPrimitiveType primitiveType, int startIndex, int primitiveCount)
    {
        long indexCountLong = (long)primitiveCount * primitiveType.IndexCountPerPrimitive();
        if (startIndex < 0 || indexCountLong < 0 || startIndex + indexCountLong > Buffer.Length)
            throw new InvalidDataException(
                $"Index range {startIndex}+{indexCountLong} is outside the index buffer ({Buffer.Length} indices).");
        int indexCount = (int)indexCountLong;
        int[] result = new int[indexCount];
        Array.Copy(Buffer, startIndex, result, 0, indexCount);
        return result;
    }
}

/// <summary>
/// IBUF2 (Shadow Index Buffer) variant - inherits all parsing from IBUF.
/// Resource Type: 0x0229684F
/// </summary>
public sealed class Ibuf2Block : IbufBlock
{
    /// <summary>Resource type identifier for IBUF2.</summary>
    public new const uint TypeId = 0x0229684F;

    /// <inheritdoc/>
    public override uint ResourceType => TypeId;

    /// <summary>
    /// Creates an empty IBUF2 block.
    /// </summary>
    public Ibuf2Block() : base() { }
    /// <summary>
    /// Creates an IBUF2 block from raw data.
    /// </summary>
    public Ibuf2Block(ReadOnlySpan<byte> data) : base(data) { }
}
