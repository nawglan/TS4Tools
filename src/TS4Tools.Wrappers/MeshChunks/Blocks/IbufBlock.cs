
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
    /// Serializes the IBUF block back to bytes.
    /// Source: s4pi Wrappers/MeshChunks/IBUF.cs UnParse() lines 206-239
    /// </summary>
    public override ReadOnlyMemory<byte> Serialize()
    {
        // Determine if we need 32-bit indices
        bool is32Bit = Buffer.Length > ushort.MaxValue || Buffer.Any(i => i > ushort.MaxValue || i < short.MinValue);
        bool isDifferenced = (Flags & IbufFormat.DifferencedIndices) != 0;

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
    public int[] GetIndices(ModelPrimitiveType primitiveType, int startIndex, int primitiveCount)
    {
        int indexCount = primitiveCount * primitiveType.IndexCountPerPrimitive();
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

    public Ibuf2Block() : base() { }
    public Ibuf2Block(ReadOnlySpan<byte> data) : base(data) { }
}
