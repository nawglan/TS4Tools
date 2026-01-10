// Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/BoneResource.cs

using TS4Tools.Resources;

namespace TS4Tools.Wrappers;

/// <summary>
/// Bone resource containing skeleton bone data with names and inverse bind pose matrices.
/// Resource Type: 0x00AE6C67
/// </summary>
/// <remarks>
/// Source: BoneResource.cs lines 28-321
/// Contains a list of bones, each with a name and a 4x3 inverse bind pose matrix.
/// Names are stored as 7-bit encoded length-prefixed BigEndianUnicode strings.
/// </remarks>
[ResourceHandler(TypeId)]
public sealed class BoneResource : TypedResource
{
    /// <summary>Resource type identifier.</summary>
    public const uint TypeId = 0x00AE6C67;

    private const int MinHeaderSize = 8; // version(4) + nameCount(4)
    private const int MatrixSize = 48; // 4 rows * 3 floats * 4 bytes

    private byte[] _rawData = [];
    private readonly List<BoneData> _bones = [];

    /// <summary>
    /// Resource version.
    /// </summary>
    public uint Version { get; set; }

    /// <summary>
    /// List of bones with names and inverse bind pose matrices.
    /// </summary>
    public IReadOnlyList<BoneData> Bones => _bones;

    /// <summary>
    /// Whether this resource was successfully parsed.
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Creates a new BoneResource by parsing data.
    /// </summary>
    public BoneResource(ResourceKey key, ReadOnlyMemory<byte> data) : base(key, data)
    {
    }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        _rawData = data.ToArray();
        _bones.Clear();
        IsValid = false;

        if (data.Length < MinHeaderSize)
            return;

        try
        {
            using var ms = new MemoryStream(data.ToArray());
            using var reader = new BinaryReader(ms, Encoding.BigEndianUnicode);

            // Read version
            // Source: BoneResource.cs line 46
            Version = reader.ReadUInt32();

            // Read bone names
            // Source: BoneResource.cs lines 48-50
            int nameCount = reader.ReadInt32();
            if (nameCount < 0 || nameCount > 10000)
                return;

            var names = new string[nameCount];
            for (int i = 0; i < nameCount; i++)
            {
                names[i] = ReadBigEndianUnicodeString(ms);
            }

            // Read matrix count and verify it matches name count
            // Source: BoneResource.cs lines 52-55
            int matrixCount = reader.ReadInt32();
            if (matrixCount != nameCount)
                return; // Mismatched counts

            // Verify we have enough data for all matrices
            long remainingBytes = ms.Length - ms.Position;
            if (remainingBytes < matrixCount * MatrixSize)
                return;

            // Read matrices and create bones
            // Source: BoneResource.cs lines 57-61
            for (int i = 0; i < matrixCount; i++)
            {
                var matrix = ReadMatrix4x3(reader);
                _bones.Add(new BoneData(names[i], matrix));
            }

            IsValid = true;
        }
        catch
        {
            IsValid = false;
        }
    }

    /// <summary>
    /// Reads a 7-bit encoded length-prefixed BigEndianUnicode string.
    /// Source: legacy_references/Sims4Tools/CS System Classes/SevenBitString.cs lines 36, 63
    /// </summary>
    private static string ReadBigEndianUnicodeString(Stream s)
    {
        // Read 7-bit encoded length (in bytes)
        int length = 0;
        int shift = 0;
        byte b;
        do
        {
            int read = s.ReadByte();
            if (read == -1)
                throw new EndOfStreamException();
            b = (byte)read;
            length |= (b & 0x7F) << shift;
            shift += 7;
        } while ((b & 0x80) != 0);

        if (length == 0)
            return string.Empty;

        // Read the bytes and decode as BigEndianUnicode (UTF-16BE)
        byte[] bytes = new byte[length];
        int bytesRead = s.Read(bytes, 0, length);
        if (bytesRead != length)
            throw new EndOfStreamException();

        return Encoding.BigEndianUnicode.GetString(bytes);
    }

    /// <summary>
    /// Reads a 4x3 matrix (4 rows of 3 floats each).
    /// Source: BoneResource.cs Matrix4x3.Parse lines 184-190
    /// </summary>
    private static BoneMatrix4x3 ReadMatrix4x3(BinaryReader reader)
    {
        return new BoneMatrix4x3(
            Right: new BoneMatrixRow(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
            Up: new BoneMatrixRow(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
            Back: new BoneMatrixRow(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
            Translate: new BoneMatrixRow(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())
        );
    }

    /// <inheritdoc/>
    protected override ReadOnlyMemory<byte> Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.BigEndianUnicode);

        // Write version
        // Source: BoneResource.cs line 69
        writer.Write(Version);

        // Write bone name count and names
        // Source: BoneResource.cs lines 73-75
        writer.Write(_bones.Count);
        foreach (var bone in _bones)
        {
            WriteBigEndianUnicodeString(ms, bone.Name);
        }

        // Write matrix count and matrices
        // Source: BoneResource.cs lines 77-79
        writer.Write(_bones.Count);
        foreach (var bone in _bones)
        {
            WriteMatrix4x3(writer, bone.InverseBindPose);
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Writes a 7-bit encoded length-prefixed BigEndianUnicode string.
    /// Source: legacy_references/Sims4Tools/CS System Classes/SevenBitString.cs lines 44-50, 70
    /// </summary>
    private static void WriteBigEndianUnicodeString(Stream s, string value)
    {
        byte[] bytes = Encoding.BigEndianUnicode.GetBytes(value ?? string.Empty);

        // Write 7-bit encoded length
        int length = bytes.Length;
        do
        {
            byte b = (byte)(length & 0x7F);
            length >>= 7;
            if (length > 0)
                b |= 0x80;
            s.WriteByte(b);
        } while (length > 0);

        // Write the bytes
        s.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// Writes a 4x3 matrix.
    /// Source: BoneResource.cs Matrix4x3.UnParse lines 192-198
    /// </summary>
    private static void WriteMatrix4x3(BinaryWriter writer, BoneMatrix4x3 matrix)
    {
        WriteMatrixRow(writer, matrix.Right);
        WriteMatrixRow(writer, matrix.Up);
        WriteMatrixRow(writer, matrix.Back);
        WriteMatrixRow(writer, matrix.Translate);
    }

    private static void WriteMatrixRow(BinaryWriter writer, BoneMatrixRow row)
    {
        writer.Write(row.X);
        writer.Write(row.Y);
        writer.Write(row.Z);
    }

    /// <summary>
    /// Adds a bone.
    /// </summary>
    public void AddBone(BoneData bone) => _bones.Add(bone);

    /// <summary>
    /// Adds a bone with name and identity matrix.
    /// </summary>
    public void AddBone(string name)
    {
        _bones.Add(new BoneData(name, BoneMatrix4x3.Identity));
    }

    /// <summary>
    /// Removes all bones.
    /// </summary>
    public void ClearBones() => _bones.Clear();

    /// <inheritdoc/>
    protected override void InitializeDefaults()
    {
        _rawData = [];
        Version = 0;
        _bones.Clear();
        IsValid = false;
    }
}

/// <summary>
/// A bone with a name and inverse bind pose matrix.
/// Source: BoneResource.cs Bone class, lines 247-296
/// </summary>
/// <param name="Name">The bone name.</param>
/// <param name="InverseBindPose">The 4x3 inverse bind pose matrix.</param>
public sealed record BoneData(string Name, BoneMatrix4x3 InverseBindPose);

/// <summary>
/// A row of a matrix (3 float values).
/// Source: BoneResource.cs MatrixRow class, lines 86-159
/// </summary>
/// <param name="X">X component.</param>
/// <param name="Y">Y component.</param>
/// <param name="Z">Z component.</param>
public sealed record BoneMatrixRow(float X, float Y, float Z)
{
    /// <summary>Zero row (0, 0, 0).</summary>
    public static BoneMatrixRow Zero => new(0f, 0f, 0f);

    /// <summary>Unit X row (1, 0, 0).</summary>
    public static BoneMatrixRow UnitX => new(1f, 0f, 0f);

    /// <summary>Unit Y row (0, 1, 0).</summary>
    public static BoneMatrixRow UnitY => new(0f, 1f, 0f);

    /// <summary>Unit Z row (0, 0, 1).</summary>
    public static BoneMatrixRow UnitZ => new(0f, 0f, 1f);
}

/// <summary>
/// A 4x3 transformation matrix (rotation/scale + translation).
/// Source: BoneResource.cs Matrix4x3 class, lines 161-245
/// </summary>
/// <param name="Right">Right vector row.</param>
/// <param name="Up">Up vector row.</param>
/// <param name="Back">Back vector row.</param>
/// <param name="Translate">Translation row.</param>
public sealed record BoneMatrix4x3(
    BoneMatrixRow Right,
    BoneMatrixRow Up,
    BoneMatrixRow Back,
    BoneMatrixRow Translate)
{
    /// <summary>
    /// Identity matrix.
    /// </summary>
    public static BoneMatrix4x3 Identity => new(
        BoneMatrixRow.UnitX,
        BoneMatrixRow.UnitY,
        BoneMatrixRow.UnitZ,
        BoneMatrixRow.Zero);
}
