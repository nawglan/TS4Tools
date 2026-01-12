// Source: legacy_references/Sims4Tools/s4pi Wrappers/RigResource/RigResource.cs lines 163-283

using TS4Tools.Wrappers.MeshChunks;

namespace TS4Tools.Wrappers;

/// <summary>
/// Represents a bone in a skeleton rig.
/// Contains position, orientation, scaling, name, and parent/opposing bone indices.
/// </summary>
public sealed class RigBone : IEquatable<RigBone>
{
    /// <summary>Position in local space (3 floats).</summary>
    public MeshVector3 Position { get; set; }

    /// <summary>Orientation quaternion (4 floats: X, Y, Z, W).</summary>
    public MeshVector4 Orientation { get; set; }

    /// <summary>Scaling factor (3 floats).</summary>
    public MeshVector3 Scaling { get; set; }

    /// <summary>Bone name (length-prefixed string).</summary>
    public string Name { get; set; } = "";

    /// <summary>Index of the opposing bone (-1 if none).</summary>
    public int OpposingBoneIndex { get; set; }

    /// <summary>Index of the parent bone (-1 for root).</summary>
    public int ParentBoneIndex { get; set; }

    /// <summary>FNV32 hash of the bone name.</summary>
    public uint Hash { get; set; }

    /// <summary>Unknown field (preserved for round-trip).</summary>
    public uint Unknown2 { get; set; }

    /// <summary>
    /// Creates a new empty bone with identity transform.
    /// </summary>
    public RigBone()
    {
        Position = MeshVector3.Zero;
        Orientation = new MeshVector4(0, 0, 0, 1); // Identity quaternion
        Scaling = MeshVector3.One;
    }

    /// <summary>
    /// Reads a bone from the data span.
    /// Source: RigResource.cs Bone.Parse() lines 207-218
    /// </summary>
    public static RigBone Read(ReadOnlySpan<byte> data, ref int position)
    {
        var bone = new RigBone
        {
            Position = MeshVector3.Read(data, ref position),
            Orientation = MeshVector4.Read(data, ref position),
            Scaling = MeshVector3.Read(data, ref position)
        };

        // Read length-prefixed string
        int nameLength = BinaryPrimitives.ReadInt32LittleEndian(data[position..]);
        position += 4;

        if (nameLength < 0 || nameLength > 1024)
            throw new InvalidDataException($"Invalid bone name length: {nameLength}");

        if (nameLength > 0)
        {
            bone.Name = Encoding.UTF8.GetString(data.Slice(position, nameLength));
            position += nameLength;
        }

        bone.OpposingBoneIndex = BinaryPrimitives.ReadInt32LittleEndian(data[position..]);
        position += 4;

        bone.ParentBoneIndex = BinaryPrimitives.ReadInt32LittleEndian(data[position..]);
        position += 4;

        bone.Hash = BinaryPrimitives.ReadUInt32LittleEndian(data[position..]);
        position += 4;

        bone.Unknown2 = BinaryPrimitives.ReadUInt32LittleEndian(data[position..]);
        position += 4;

        return bone;
    }

    /// <summary>
    /// Writes the bone to the buffer.
    /// Source: RigResource.cs Bone.UnParse() lines 220-236
    /// </summary>
    public void Write(Span<byte> buffer, ref int position)
    {
        Position.Write(buffer, ref position);
        Orientation.Write(buffer, ref position);
        Scaling.Write(buffer, ref position);

        // Write length-prefixed string
        byte[] nameBytes = Encoding.UTF8.GetBytes(Name ?? "");
        BinaryPrimitives.WriteInt32LittleEndian(buffer[position..], nameBytes.Length);
        position += 4;

        if (nameBytes.Length > 0)
        {
            nameBytes.CopyTo(buffer[position..]);
            position += nameBytes.Length;
        }

        BinaryPrimitives.WriteInt32LittleEndian(buffer[position..], OpposingBoneIndex);
        position += 4;

        BinaryPrimitives.WriteInt32LittleEndian(buffer[position..], ParentBoneIndex);
        position += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer[position..], Hash);
        position += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer[position..], Unknown2);
        position += 4;
    }

    /// <summary>
    /// Calculates the serialized size of this bone.
    /// </summary>
    public int GetSerializedSize()
    {
        // Position(12) + Orientation(16) + Scaling(12) + NameLength(4) + Name + indices(16)
        int nameBytes = Encoding.UTF8.GetByteCount(Name ?? "");
        return 12 + 16 + 12 + 4 + nameBytes + 16;
    }

    public bool Equals(RigBone? other)
    {
        if (other is null) return false;
        return Position == other.Position
            && Orientation == other.Orientation
            && Scaling == other.Scaling
            && Name == other.Name
            && OpposingBoneIndex == other.OpposingBoneIndex
            && ParentBoneIndex == other.ParentBoneIndex
            && Hash == other.Hash
            && Unknown2 == other.Unknown2;
    }

    public override bool Equals(object? obj) => obj is RigBone other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(Position, Orientation, Scaling, Name, OpposingBoneIndex, ParentBoneIndex, Hash, Unknown2);

    public override string ToString() => $"Bone '{Name}' (0x{Hash:X8})";
}
