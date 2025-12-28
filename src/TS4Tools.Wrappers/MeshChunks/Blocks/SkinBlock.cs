
namespace TS4Tools.Wrappers.MeshChunks;

/// <summary>
/// Represents a bone in a skeleton with its inverse bind pose matrix.
/// Source: s4pi Wrappers/MeshChunks/SKIN.cs Bone class lines 51-110
/// </summary>
public sealed class Bone : IEquatable<Bone>
{
    /// <summary>FNV32 hash of the bone name.</summary>
    public uint NameHash { get; set; }

    /// <summary>Inverse bind pose transformation matrix.</summary>
    public Matrix43 InverseBindPose { get; set; }

    public Bone()
    {
        InverseBindPose = Matrix43.Identity;
    }

    public Bone(uint nameHash, Matrix43 inverseBindPose)
    {
        NameHash = nameHash;
        InverseBindPose = inverseBindPose;
    }

    public bool Equals(Bone? other) =>
        other is not null && NameHash == other.NameHash && InverseBindPose == other.InverseBindPose;

    public override bool Equals(object? obj) => obj is Bone other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(NameHash, InverseBindPose);

    public override string ToString() => $"Bone 0x{NameHash:X8}";
}

/// <summary>
/// SKIN block - contains skeleton bone data with inverse bind poses.
/// Resource Type: 0x01D0E76B
/// Source: s4pi Wrappers/MeshChunks/SKIN.cs
/// </summary>
public sealed class SkinBlock : RcolBlock
{
    /// <summary>Resource type identifier for SKIN.</summary>
    public const uint TypeId = 0x01D0E76B;

    /// <inheritdoc/>
    public override string Tag => "SKIN";

    /// <inheritdoc/>
    public override uint ResourceType => TypeId;

    /// <inheritdoc/>
    public override bool IsKnownType => true;

    /// <summary>Format version (typically 0x00000001).</summary>
    public uint Version { get; private set; } = 0x00000001;

    /// <summary>List of bones in this skeleton.</summary>
    public List<Bone> Bones { get; } = [];

    /// <summary>
    /// Creates an empty SKIN block.
    /// </summary>
    public SkinBlock() : base()
    {
    }

    /// <summary>
    /// Creates a SKIN block from raw data.
    /// </summary>
    public SkinBlock(ReadOnlySpan<byte> data) : base(data)
    {
    }

    /// <summary>
    /// Parses the SKIN block data.
    /// The format stores bone hashes first, then matrices.
    /// Source: s4pi Wrappers/MeshChunks/SKIN.cs Parse() lines 140-154
    /// </summary>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        int pos = 0;

        // Validate tag
        string tag = ExtractTag(data);
        if (tag != Tag)
            throw new InvalidDataException($"Invalid SKIN tag: expected '{Tag}', got '{tag}'");
        pos += 4;

        // Read header
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        int boneCount = BinaryPrimitives.ReadInt32LittleEndian(data[pos..]);
        pos += 4;

        // Validate
        if (boneCount < 0 || boneCount > 1000)
            throw new InvalidDataException($"Invalid SKIN bone count: {boneCount}");

        // Read bone name hashes first
        uint[] nameHashes = new uint[boneCount];
        for (int i = 0; i < boneCount; i++)
        {
            nameHashes[i] = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
            pos += 4;
        }

        // Then read matrices
        Bones.Clear();
        for (int i = 0; i < boneCount; i++)
        {
            var matrix = Matrix43.Read(data, ref pos);
            Bones.Add(new Bone(nameHashes[i], matrix));
        }
    }

    /// <summary>
    /// Serializes the SKIN block back to bytes.
    /// Source: s4pi Wrappers/MeshChunks/SKIN.cs UnParse() lines 156-167
    /// </summary>
    public override ReadOnlyMemory<byte> Serialize()
    {
        // Calculate size: tag(4) + version(4) + count(4) + hashes(count*4) + matrices(count*48)
        int size = 12 + (Bones.Count * 4) + (Bones.Count * Matrix43.Size);
        byte[] buffer = new byte[size];
        int pos = 0;

        // Write tag
        buffer[pos++] = (byte)'S';
        buffer[pos++] = (byte)'K';
        buffer[pos++] = (byte)'I';
        buffer[pos++] = (byte)'N';

        // Write header
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), Version);
        pos += 4;

        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(pos), Bones.Count);
        pos += 4;

        // Write bone name hashes
        foreach (var bone in Bones)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), bone.NameHash);
            pos += 4;
        }

        // Write matrices
        foreach (var bone in Bones)
        {
            bone.InverseBindPose.Write(buffer, ref pos);
        }

        return buffer;
    }

    /// <summary>
    /// Finds a bone by its name hash.
    /// </summary>
    public Bone? FindBone(uint nameHash) =>
        Bones.FirstOrDefault(b => b.NameHash == nameHash);
}
