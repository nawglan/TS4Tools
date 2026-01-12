// Source: legacy_references/Sims4Tools/s4pi Wrappers/JazzResource/JazzResource.cs

namespace TS4Tools.Wrappers.JazzChunks;

/// <summary>
/// Helper methods for Jazz chunk parsing and serialization.
/// Source: JazzResource.cs lines 33-66
/// </summary>
public static class JazzHelper
{
    /// <summary>
    /// Validates the DEADBEEF marker during parsing.
    /// </summary>
    public static void ParseDeadBeef(ReadOnlySpan<byte> data, ref int offset)
    {
        uint marker = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        if (marker != JazzConstants.DeadBeef)
            throw new InvalidDataException($"Invalid DEADBEEF marker: expected 0x{JazzConstants.DeadBeef:X8}, got 0x{marker:X8} at offset {offset}");
        offset += 4;
    }

    /// <summary>
    /// Writes the DEADBEEF marker during serialization.
    /// </summary>
    public static void WriteDeadBeef(BinaryWriter writer)
    {
        writer.Write(JazzConstants.DeadBeef);
    }

    /// <summary>
    /// Validates the CloseDGN ("/DGN") marker during parsing.
    /// </summary>
    public static void ParseCloseDgn(ReadOnlySpan<byte> data, ref int offset)
    {
        uint marker = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        if (marker != JazzConstants.CloseDgn)
            throw new InvalidDataException($"Invalid /DGN marker: expected 0x{JazzConstants.CloseDgn:X8}, got 0x{marker:X8} at offset {offset}");
        offset += 4;
    }

    /// <summary>
    /// Writes the CloseDGN ("/DGN") marker during serialization.
    /// </summary>
    public static void WriteCloseDgn(BinaryWriter writer)
    {
        writer.Write(JazzConstants.CloseDgn);
    }

    /// <summary>
    /// Reads padding bytes and validates they are zero.
    /// </summary>
    public static void ExpectZero(ReadOnlySpan<byte> data, ref int offset, int alignment = 4)
    {
        while (offset % alignment != 0 && offset < data.Length)
        {
            if (data[offset] != 0)
                throw new InvalidDataException($"Invalid padding: expected 0x00, got 0x{data[offset]:X2} at offset {offset}");
            offset++;
        }
    }

    /// <summary>
    /// Writes padding bytes to achieve alignment.
    /// </summary>
    public static void PadZero(BinaryWriter writer, int alignment = 4)
    {
        while (writer.BaseStream.Position % alignment != 0)
            writer.Write((byte)0);
    }

    /// <summary>
    /// Reads a length-prefixed Unicode string.
    /// Source: JazzResource.cs lines 939-940
    /// </summary>
    public static string ReadUnicodeString(ReadOnlySpan<byte> data, ref int offset)
    {
        int charCount = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
        offset += 4;

        if (charCount == 0)
            return string.Empty;

        int byteCount = charCount * 2;
        string result = Encoding.Unicode.GetString(data.Slice(offset, byteCount));
        offset += byteCount;

        // Read null terminator if present
        if (charCount > 0)
            offset += 2; // Skip null terminator

        return result;
    }

    /// <summary>
    /// Writes a length-prefixed Unicode string.
    /// Source: JazzResource.cs lines 987-989
    /// </summary>
    public static void WriteUnicodeString(BinaryWriter writer, string value)
    {
        writer.Write(value.Length);
        if (value.Length > 0)
        {
            writer.Write(Encoding.Unicode.GetBytes(value));
            writer.Write((ushort)0); // Null terminator
        }
    }

    /// <summary>
    /// Reads a TGI block in ITG order (Instance, Type, Group).
    /// </summary>
    public static ResourceKey ReadTgiItg(ReadOnlySpan<byte> data, ref int offset)
    {
        ulong instance = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
        offset += 8;
        uint type = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;
        uint group = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;
        return new ResourceKey(type, group, instance);
    }

    /// <summary>
    /// Writes a TGI block in ITG order (Instance, Type, Group).
    /// </summary>
    public static void WriteTgiItg(BinaryWriter writer, ResourceKey key)
    {
        writer.Write(key.Instance);
        writer.Write(key.ResourceType);
        writer.Write(key.ResourceGroup);
    }

    /// <summary>
    /// Reads a chunk reference (uint32 index).
    /// </summary>
    public static uint ReadChunkReference(ReadOnlySpan<byte> data, ref int offset)
    {
        uint value = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;
        return value;
    }

    /// <summary>
    /// Reads a chunk reference list (count + array of uint32 indices).
    /// </summary>
    public static List<uint> ReadChunkReferenceList(ReadOnlySpan<byte> data, ref int offset)
    {
        int count = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
        offset += 4;

        var list = new List<uint>(count);
        for (int i = 0; i < count; i++)
        {
            list.Add(BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]));
            offset += 4;
        }
        return list;
    }

    /// <summary>
    /// Writes a chunk reference list (count + array of uint32 indices).
    /// </summary>
    public static void WriteChunkReferenceList(BinaryWriter writer, List<uint> list)
    {
        writer.Write(list.Count);
        foreach (uint item in list)
            writer.Write(item);
    }
}

/// <summary>
/// Animation entry in a state machine.
/// Source: JazzResource.cs lines 267-338
/// </summary>
public sealed class JazzAnimation
{
    /// <summary>Name hash for the animation.</summary>
    public uint NameHash { get; set; }

    /// <summary>First actor hash.</summary>
    public uint Actor1Hash { get; set; }

    /// <summary>Second actor hash.</summary>
    public uint Actor2Hash { get; set; }

    public static JazzAnimation Parse(ReadOnlySpan<byte> data, ref int offset)
    {
        var anim = new JazzAnimation
        {
            NameHash = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]),
        };
        offset += 4;
        anim.Actor1Hash = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;
        anim.Actor2Hash = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;
        return anim;
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write(NameHash);
        writer.Write(Actor1Hash);
        writer.Write(Actor2Hash);
    }
}

/// <summary>
/// Actor slot entry in play animation node.
/// Source: JazzResource.cs lines 1024-1105
/// </summary>
public sealed class JazzActorSlot
{
    public uint ChainId { get; set; }
    public uint SlotId { get; set; }
    public uint ActorNameHash { get; set; }
    public uint SlotNameHash { get; set; }

    public static JazzActorSlot Parse(ReadOnlySpan<byte> data, ref int offset)
    {
        var slot = new JazzActorSlot
        {
            ChainId = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]),
        };
        offset += 4;
        slot.SlotId = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;
        slot.ActorNameHash = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;
        slot.SlotNameHash = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;
        return slot;
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write(ChainId);
        writer.Write(SlotId);
        writer.Write(ActorNameHash);
        writer.Write(SlotNameHash);
    }
}

/// <summary>
/// Actor suffix entry in play animation node.
/// Source: JazzResource.cs lines 1125-1190
/// </summary>
public sealed class JazzActorSuffix
{
    public uint ActorNameHash { get; set; }
    public uint SuffixHash { get; set; }

    public static JazzActorSuffix Parse(ReadOnlySpan<byte> data, ref int offset)
    {
        var suffix = new JazzActorSuffix
        {
            ActorNameHash = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]),
        };
        offset += 4;
        suffix.SuffixHash = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;
        return suffix;
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write(ActorNameHash);
        writer.Write(SuffixHash);
    }
}

/// <summary>
/// Outcome entry in random node.
/// Source: JazzResource.cs lines 1347-1411
/// </summary>
public sealed class JazzOutcome
{
    public float Weight { get; set; }
    public List<uint> DecisionGraphIndexes { get; set; } = [];

    public static JazzOutcome Parse(ReadOnlySpan<byte> data, ref int offset)
    {
        var outcome = new JazzOutcome
        {
            Weight = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]),
        };
        offset += 4;
        outcome.DecisionGraphIndexes = JazzHelper.ReadChunkReferenceList(data, ref offset);
        return outcome;
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Weight);
        JazzHelper.WriteChunkReferenceList(writer, DecisionGraphIndexes);
    }
}

/// <summary>
/// Match entry in select on parameter node.
/// Source: JazzResource.cs lines 1514-1578
/// </summary>
public sealed class JazzParameterMatch
{
    public uint TestValue { get; set; }
    public List<uint> DecisionGraphIndexes { get; set; } = [];

    public static JazzParameterMatch Parse(ReadOnlySpan<byte> data, ref int offset)
    {
        var match = new JazzParameterMatch
        {
            TestValue = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]),
        };
        offset += 4;
        match.DecisionGraphIndexes = JazzHelper.ReadChunkReferenceList(data, ref offset);
        return match;
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write(TestValue);
        JazzHelper.WriteChunkReferenceList(writer, DecisionGraphIndexes);
    }
}

/// <summary>
/// Match entry in select on destination node.
/// Source: JazzResource.cs lines 1674-1739
/// </summary>
public sealed class JazzDestinationMatch
{
    public uint StateIndex { get; set; }
    public List<uint> DecisionGraphIndexes { get; set; } = [];

    public static JazzDestinationMatch Parse(ReadOnlySpan<byte> data, ref int offset)
    {
        var match = new JazzDestinationMatch
        {
            StateIndex = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]),
        };
        offset += 4;
        match.DecisionGraphIndexes = JazzHelper.ReadChunkReferenceList(data, ref offset);
        return match;
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write(StateIndex);
        JazzHelper.WriteChunkReferenceList(writer, DecisionGraphIndexes);
    }
}
