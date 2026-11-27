// Source: legacy_references/Sims4Tools/s4pi Wrappers/GenericRCOLResource/ARCOLBlock.cs (Parse(Stream) pattern)
// Legacy blocks read from a Stream with BinaryReader, where a truncated stream throws EndOfStreamException.
// This helper has no direct legacy equivalent: it gives the same fail-fast behaviour for span based parsing.

using TS4Tools.Wrappers.MeshChunks;

namespace TS4Tools.Wrappers;

/// <summary>
/// Bounds-checked little-endian reader over a span, used by RCOL block parsers.
/// Throws <see cref="InvalidDataException"/> instead of an index error when data is truncated.
/// </summary>
internal ref struct RcolSpanReader
{
    private readonly ReadOnlySpan<byte> _data;
    private int _pos;

    public RcolSpanReader(ReadOnlySpan<byte> data, int position = 0)
    {
        _data = data;
        _pos = position;
    }

    /// <summary>Current read position.</summary>
    public int Position
    {
        readonly get => _pos;
        set
        {
            if (value < 0 || value > _data.Length)
                throw new InvalidDataException($"Position {value} is outside the data (length {_data.Length})");
            _pos = value;
        }
    }

    /// <summary>Bytes left to read.</summary>
    public readonly int Remaining => _data.Length - _pos;

    /// <summary>Total length of the underlying data.</summary>
    public readonly int Length => _data.Length;

    private void Need(int count)
    {
        if (count < 0 || _pos > _data.Length - count)
            throw new InvalidDataException(
                $"Unexpected end of data: need {count} bytes at offset {_pos}, only {_data.Length - _pos} available");
    }

    public byte ReadByte()
    {
        Need(1);
        return _data[_pos++];
    }

    public uint ReadUInt32()
    {
        Need(4);
        uint v = BinaryPrimitives.ReadUInt32LittleEndian(_data[_pos..]);
        _pos += 4;
        return v;
    }

    public int ReadInt32()
    {
        Need(4);
        int v = BinaryPrimitives.ReadInt32LittleEndian(_data[_pos..]);
        _pos += 4;
        return v;
    }

    public ulong ReadUInt64()
    {
        Need(8);
        ulong v = BinaryPrimitives.ReadUInt64LittleEndian(_data[_pos..]);
        _pos += 8;
        return v;
    }

    public float ReadSingle()
    {
        Need(4);
        float v = BinaryPrimitives.ReadSingleLittleEndian(_data[_pos..]);
        _pos += 4;
        return v;
    }

    public byte[] ReadBytes(int count)
    {
        Need(count);
        byte[] v = _data.Slice(_pos, count).ToArray();
        _pos += count;
        return v;
    }

    /// <summary>
    /// Reads an element count and verifies that at least <paramref name="minElementSize"/> bytes per
    /// element remain, so a corrupt count cannot drive huge allocations.
    /// </summary>
    public int ReadCount(int minElementSize)
    {
        int count = ReadInt32();
        CheckCount(count, minElementSize);
        return count;
    }

    /// <summary>Validates a count read elsewhere (e.g. a byte count).</summary>
    public readonly void CheckCount(int count, int minElementSize)
    {
        if (count < 0 || (minElementSize > 0 && (long)count * minElementSize > Remaining))
            throw new InvalidDataException($"Invalid element count {count} at offset {_pos} ({Remaining} bytes remain)");
    }

    public MeshVector3 ReadVector3()
    {
        Need(MeshVector3.Size);
        return MeshVector3.Read(_data, ref _pos);
    }

    public MeshVector4 ReadVector4()
    {
        Need(MeshVector4.Size);
        return MeshVector4.Read(_data, ref _pos);
    }

    public BoundingBox ReadBoundingBox()
    {
        Need(BoundingBox.Size);
        return BoundingBox.Read(_data, ref _pos);
    }
}

/// <summary>
/// BinaryWriter helpers for the mesh math types used by RCOL blocks.
/// </summary>
internal static class RcolWriterExtensions
{
    public static void Write(this BinaryWriter w, MeshVector3 v)
    {
        w.Write(v.X);
        w.Write(v.Y);
        w.Write(v.Z);
    }

    public static void Write(this BinaryWriter w, MeshVector4 v)
    {
        w.Write(v.X);
        w.Write(v.Y);
        w.Write(v.Z);
        w.Write(v.W);
    }

    public static void Write(this BinaryWriter w, BoundingBox b)
    {
        w.Write(b.Min);
        w.Write(b.Max);
    }
}
