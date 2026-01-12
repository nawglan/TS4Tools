// Source: legacy_references/Sims4Tools/s4pi Wrappers/AnimationResources/IkConfiguration.cs

namespace TS4Tools.Wrappers;

/// <summary>
/// IK slot assignment for animation clips.
/// </summary>
/// <remarks>
/// Source: IkConfiguration.cs lines 68-161
/// </remarks>
public sealed class ClipSlotAssignment
{
    /// <summary>Chain identifier.</summary>
    public ushort ChainId { get; set; }

    /// <summary>Slot identifier within the chain.</summary>
    public ushort SlotId { get; set; }

    /// <summary>Target object namespace.</summary>
    public string TargetObjectNamespace { get; set; } = string.Empty;

    /// <summary>Target joint name.</summary>
    public string TargetJointName { get; set; } = string.Empty;

    /// <summary>
    /// Parses a slot assignment from binary data.
    /// </summary>
    internal static ClipSlotAssignment Parse(ReadOnlySpan<byte> data, ref int offset)
    {
        var assignment = new ClipSlotAssignment
        {
            ChainId = BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]),
            SlotId = BinaryPrimitives.ReadUInt16LittleEndian(data[(offset + 2)..])
        };
        offset += 4;

        assignment.TargetObjectNamespace = ReadString32(data, ref offset);
        assignment.TargetJointName = ReadString32(data, ref offset);

        return assignment;
    }

    /// <summary>
    /// Writes the slot assignment to a stream.
    /// </summary>
    internal void Write(BinaryWriter writer)
    {
        writer.Write(ChainId);
        writer.Write(SlotId);
        WriteString32(writer, TargetObjectNamespace);
        WriteString32(writer, TargetJointName);
    }

    private static string ReadString32(ReadOnlySpan<byte> data, ref int offset)
    {
        int length = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
        offset += 4;

        if (length <= 0)
            return string.Empty;

        if (offset + length > data.Length)
            throw new InvalidDataException($"String length {length} exceeds available data at offset {offset}");

        string result = Encoding.ASCII.GetString(data.Slice(offset, length));
        offset += length;
        return result;
    }

    private static void WriteString32(BinaryWriter writer, string value)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(value ?? string.Empty);
        writer.Write(bytes.Length);
        writer.Write(bytes);
    }
}

/// <summary>
/// Collection of IK slot assignments for an animation clip.
/// </summary>
/// <remarks>
/// Source: IkConfiguration.cs lines 27-67
/// </remarks>
public sealed class ClipIkConfiguration
{
    private readonly List<ClipSlotAssignment> _assignments = [];

    /// <summary>
    /// The slot assignments in this configuration.
    /// </summary>
    public IReadOnlyList<ClipSlotAssignment> Assignments => _assignments;

    /// <summary>
    /// Number of slot assignments.
    /// </summary>
    public int Count => _assignments.Count;

    /// <summary>
    /// Parses IK configuration from binary data.
    /// </summary>
    internal static ClipIkConfiguration Parse(ReadOnlySpan<byte> data, ref int offset)
    {
        var config = new ClipIkConfiguration();

        int count = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
        offset += 4;

        if (count < 0)
            throw new InvalidDataException($"Invalid slot assignment count: {count}");

        for (int i = 0; i < count; i++)
        {
            config._assignments.Add(ClipSlotAssignment.Parse(data, ref offset));
        }

        return config;
    }

    /// <summary>
    /// Writes the IK configuration to a stream.
    /// </summary>
    internal void Write(BinaryWriter writer)
    {
        writer.Write(_assignments.Count);
        foreach (var assignment in _assignments)
        {
            assignment.Write(writer);
        }
    }

    /// <summary>
    /// Adds a slot assignment to the configuration.
    /// </summary>
    public void Add(ClipSlotAssignment assignment)
    {
        _assignments.Add(assignment);
    }

    /// <summary>
    /// Clears all slot assignments.
    /// </summary>
    public void Clear()
    {
        _assignments.Clear();
    }
}
