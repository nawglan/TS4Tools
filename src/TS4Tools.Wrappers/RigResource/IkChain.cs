// Source: legacy_references/Sims4Tools/s4pi Wrappers/RigResource/RigResource.cs lines 295-520

namespace TS4Tools.Wrappers;

/// <summary>
/// Represents an IK (Inverse Kinematics) chain in a skeleton rig.
/// Contains bone indices and node references for IK solving.
/// </summary>
public sealed class IkChain : IEquatable<IkChain>
{
    /// <summary>Number of info node indices (version 4+).</summary>
    public const int InfoNodeCount = 11;

    /// <summary>List of bone indices in this IK chain.</summary>
    public List<int> Bones { get; } = [];

    /// <summary>
    /// Info node indices (11 values, only present in version 4+).
    /// Indices 0-10 are stored in legacy as infoNode0Index through infoNodeAIndex.
    /// </summary>
    public int[] InfoNodeIndices { get; } = new int[InfoNodeCount];

    /// <summary>Pole vector bone index.</summary>
    public int PoleVectorIndex { get; set; }

    /// <summary>Slot info bone index (version 4+ only).</summary>
    public int SlotInfoIndex { get; set; }

    /// <summary>Slot offset bone index.</summary>
    public int SlotOffsetIndex { get; set; }

    /// <summary>Root bone index.</summary>
    public int RootIndex { get; set; }

    /// <summary>
    /// Creates a new empty IK chain.
    /// </summary>
    public IkChain()
    {
    }

    /// <summary>
    /// Reads an IK chain from the data span.
    /// Source: RigResource.cs IKElement.Parse() lines 365-390
    /// </summary>
    /// <param name="data">The data span to read from.</param>
    /// <param name="position">Current position, updated after reading.</param>
    /// <param name="majorVersion">The rig major version (affects field presence).</param>
    public static IkChain Read(ReadOnlySpan<byte> data, ref int position, uint majorVersion)
    {
        var chain = new IkChain();

        // Read bone list (count-prefixed int list)
        int boneCount = BinaryPrimitives.ReadInt32LittleEndian(data[position..]);
        position += 4;

        if (boneCount < 0 || boneCount > 1000)
            throw new InvalidDataException($"Invalid IK chain bone count: {boneCount}");

        for (int i = 0; i < boneCount; i++)
        {
            chain.Bones.Add(BinaryPrimitives.ReadInt32LittleEndian(data[position..]));
            position += 4;
        }

        // Read info node indices (version 4+ only)
        if (majorVersion >= 4)
        {
            for (int i = 0; i < InfoNodeCount; i++)
            {
                chain.InfoNodeIndices[i] = BinaryPrimitives.ReadInt32LittleEndian(data[position..]);
                position += 4;
            }
        }

        // Read pole vector index (all versions)
        chain.PoleVectorIndex = BinaryPrimitives.ReadInt32LittleEndian(data[position..]);
        position += 4;

        // Read slot info index (version 4+ only)
        if (majorVersion >= 4)
        {
            chain.SlotInfoIndex = BinaryPrimitives.ReadInt32LittleEndian(data[position..]);
            position += 4;
        }

        // Read slot offset and root indices (all versions)
        chain.SlotOffsetIndex = BinaryPrimitives.ReadInt32LittleEndian(data[position..]);
        position += 4;

        chain.RootIndex = BinaryPrimitives.ReadInt32LittleEndian(data[position..]);
        position += 4;

        return chain;
    }

    /// <summary>
    /// Writes the IK chain to the buffer.
    /// Source: RigResource.cs IKElement.UnParse() lines 392-418
    /// </summary>
    /// <param name="buffer">The buffer to write to.</param>
    /// <param name="position">Current position, updated after writing.</param>
    /// <param name="majorVersion">The rig major version (affects field presence).</param>
    public void Write(Span<byte> buffer, ref int position, uint majorVersion)
    {
        // Write bone list
        BinaryPrimitives.WriteInt32LittleEndian(buffer[position..], Bones.Count);
        position += 4;

        foreach (int boneIndex in Bones)
        {
            BinaryPrimitives.WriteInt32LittleEndian(buffer[position..], boneIndex);
            position += 4;
        }

        // Write info node indices (version 4+ only)
        if (majorVersion >= 4)
        {
            for (int i = 0; i < InfoNodeCount; i++)
            {
                BinaryPrimitives.WriteInt32LittleEndian(buffer[position..], InfoNodeIndices[i]);
                position += 4;
            }
        }

        // Write pole vector index
        BinaryPrimitives.WriteInt32LittleEndian(buffer[position..], PoleVectorIndex);
        position += 4;

        // Write slot info index (version 4+ only)
        if (majorVersion >= 4)
        {
            BinaryPrimitives.WriteInt32LittleEndian(buffer[position..], SlotInfoIndex);
            position += 4;
        }

        // Write slot offset and root indices
        BinaryPrimitives.WriteInt32LittleEndian(buffer[position..], SlotOffsetIndex);
        position += 4;

        BinaryPrimitives.WriteInt32LittleEndian(buffer[position..], RootIndex);
        position += 4;
    }

    /// <summary>
    /// Calculates the serialized size of this IK chain.
    /// </summary>
    /// <param name="majorVersion">The rig major version.</param>
    /// <returns>The total size in bytes when serialized.</returns>
    public int GetSerializedSize(uint majorVersion)
    {
        int size = 4 + (Bones.Count * 4); // Bone list

        if (majorVersion >= 4)
        {
            size += InfoNodeCount * 4; // Info node indices
            size += 4; // Slot info index
        }

        size += 4; // Pole vector index
        size += 4; // Slot offset index
        size += 4; // Root index

        return size;
    }

    /// <summary>
    /// Determines whether this IK chain is equal to another IK chain.
    /// </summary>
    /// <param name="other">The IK chain to compare with.</param>
    /// <returns>True if the IK chains are equal; otherwise, false.</returns>
    public bool Equals(IkChain? other)
    {
        if (other is null) return false;
        if (Bones.Count != other.Bones.Count) return false;
        for (int i = 0; i < Bones.Count; i++)
        {
            if (Bones[i] != other.Bones[i]) return false;
        }
        for (int i = 0; i < InfoNodeCount; i++)
        {
            if (InfoNodeIndices[i] != other.InfoNodeIndices[i]) return false;
        }
        return PoleVectorIndex == other.PoleVectorIndex
            && SlotInfoIndex == other.SlotInfoIndex
            && SlotOffsetIndex == other.SlotOffsetIndex
            && RootIndex == other.RootIndex;
    }

    /// <summary>
    /// Determines whether this IK chain is equal to the specified object.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True if the objects are equal; otherwise, false.</returns>
    public override bool Equals(object? obj) => obj is IkChain other && Equals(other);

    /// <summary>
    /// Returns a hash code for this IK chain.
    /// </summary>
    /// <returns>A hash code for the current IK chain.</returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Bones.Count);
        foreach (var bone in Bones) hash.Add(bone);
        foreach (var node in InfoNodeIndices) hash.Add(node);
        hash.Add(PoleVectorIndex);
        hash.Add(SlotInfoIndex);
        hash.Add(SlotOffsetIndex);
        hash.Add(RootIndex);
        return hash.ToHashCode();
    }

    /// <summary>
    /// Returns a string representation of this IK chain.
    /// </summary>
    /// <returns>A string describing the IK chain.</returns>
    public override string ToString() => $"IkChain ({Bones.Count} bones)";
}
