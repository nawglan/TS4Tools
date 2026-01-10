// Source: legacy_references/Sims4Tools/s4pi Wrappers/AnimationResources/ClipEvents.cs lines 504-628

namespace TS4Tools.Wrappers;

/// <summary>
/// Fixed string length for clip event strings (0x80 = 128 bytes).
/// </summary>
internal static class ClipEventConstants
{
    public const int FixedStringLength = 0x80;
}

/// <summary>
/// Base class for animation clip events.
/// </summary>
/// <remarks>
/// Source: ClipEvents.cs lines 504-628
/// </remarks>
public abstract class ClipEvent
{
    /// <summary>Event type identifier.</summary>
    public ClipEventType TypeId { get; }

    /// <summary>Unknown value 1.</summary>
    public uint Unknown1 { get; set; }

    /// <summary>Unknown value 2.</summary>
    public uint Unknown2 { get; set; }

    /// <summary>Timecode when this event occurs during animation playback.</summary>
    public float Timecode { get; set; }

    /// <summary>
    /// Creates a new clip event with the specified type.
    /// </summary>
    protected ClipEvent(ClipEventType typeId)
    {
        TypeId = typeId;
    }

    /// <summary>
    /// Gets the size of type-specific data for this event.
    /// </summary>
    protected abstract int TypeDataSize { get; }

    /// <summary>
    /// Total size of this event including header (type, size) and common fields.
    /// </summary>
    public int Size => TypeDataSize + 12; // 3 x uint32 (unknown1, unknown2, timecode)

    /// <summary>
    /// Reads type-specific data from the span.
    /// </summary>
    protected abstract void ReadTypeData(ReadOnlySpan<byte> data, int offset);

    /// <summary>
    /// Writes type-specific data to the writer.
    /// </summary>
    protected abstract void WriteTypeData(BinaryWriter writer);

    /// <summary>
    /// Parses the common event fields from binary data.
    /// </summary>
    internal void ParseCommon(ReadOnlySpan<byte> data, int offset)
    {
        Unknown1 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        Unknown2 = BinaryPrimitives.ReadUInt32LittleEndian(data[(offset + 4)..]);
        Timecode = BinaryPrimitives.ReadSingleLittleEndian(data[(offset + 8)..]);
        ReadTypeData(data, offset + 12);
    }

    /// <summary>
    /// Writes the event to a stream.
    /// </summary>
    internal void Write(BinaryWriter writer)
    {
        writer.Write((uint)TypeId);
        writer.Write((uint)Size);
        writer.Write(Unknown1);
        writer.Write(Unknown2);
        writer.Write(Timecode);
        WriteTypeData(writer);
    }

    /// <summary>
    /// Creates a clip event from binary data based on type.
    /// </summary>
    /// <remarks>
    /// Source: ClipEvents.cs lines 608-627
    /// </remarks>
    public static ClipEvent Create(ClipEventType typeId, uint size, ReadOnlySpan<byte> data, int offset)
    {
        ClipEvent evt = typeId switch
        {
            ClipEventType.Sound => new ClipEventSound(),
            ClipEventType.Script => new ClipEventScript((int)size - 12),
            ClipEventType.Effect => new ClipEventEffect(),
            ClipEventType.Snap => new ClipEventSnap((int)size - 12),
            ClipEventType.DoubleModifierSound => new ClipEventDoubleModifierSound(),
            ClipEventType.Censor => new ClipEventCensor(),
            _ => new ClipEventUnknown(typeId, (int)size - 12)
        };

        evt.ParseCommon(data, offset);
        return evt;
    }

    /// <summary>
    /// Reads a fixed-length string (128 bytes, null-padded).
    /// </summary>
    protected static string ReadStringFixed(ReadOnlySpan<byte> data, int offset, int length = ClipEventConstants.FixedStringLength)
    {
        var span = data.Slice(offset, length);
        int nullIndex = span.IndexOf((byte)0);
        if (nullIndex >= 0)
            span = span[..nullIndex];

        return Encoding.ASCII.GetString(span).Trim();
    }

    /// <summary>
    /// Writes a fixed-length string (128 bytes, null-padded).
    /// </summary>
    protected static void WriteStringFixed(BinaryWriter writer, string value, int length = ClipEventConstants.FixedStringLength)
    {
        byte[] buffer = new byte[length];
        if (!string.IsNullOrEmpty(value))
        {
            byte[] bytes = Encoding.ASCII.GetBytes(value);
            Array.Copy(bytes, buffer, Math.Min(bytes.Length, length - 1));
        }
        writer.Write(buffer);
    }
}
