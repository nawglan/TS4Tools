
namespace TS4Tools.Wrappers.MeshChunks;

/// <summary>
/// RSLT (Result/Slot) block - defines slot placement for object parts.
/// Resource Type: 0xD3044521
/// Source: s4pi Wrappers/s4piRCOLChunks/RSLT.cs
///
/// Contains 5 part types: routes, containers, effects, IK targets, cones.
/// Each part type has a list of parts and a list of slot offsets.
/// </summary>
public sealed class RsltBlock : RcolBlock
{
    /// <summary>Resource type identifier for RSLT.</summary>
    public const uint TypeId = 0xD3044521;

    /// <inheritdoc/>
    public override string Tag => "RSLT";

    /// <inheritdoc/>
    public override uint ResourceType => TypeId;

    /// <inheritdoc/>
    public override bool IsKnownType => true;

    /// <summary>Format version (typically 4).</summary>
    public uint Version { get; private set; } = 4;

    /// <summary>Route parts.</summary>
    public List<RsltPart> Routes { get; } = [];

    /// <summary>Route slot offsets.</summary>
    public List<RsltSlotOffset> RouteOffsets { get; } = [];

    /// <summary>Container parts (with slot placement info).</summary>
    public List<RsltSlottedPart> Containers { get; } = [];

    /// <summary>Container slot offsets.</summary>
    public List<RsltSlotOffset> ContainerOffsets { get; } = [];

    /// <summary>Effect parts.</summary>
    public List<RsltPart> Effects { get; } = [];

    /// <summary>Effect slot offsets.</summary>
    public List<RsltSlotOffset> EffectOffsets { get; } = [];

    /// <summary>Inverse kinematics target parts.</summary>
    public List<RsltPart> InverseKineticsTargets { get; } = [];

    /// <summary>IK target slot offsets.</summary>
    public List<RsltSlotOffset> InverseKineticsTargetOffsets { get; } = [];

    /// <summary>Cone parts.</summary>
    public List<RsltConePart> Cones { get; } = [];

    /// <summary>Cone slot offsets.</summary>
    public List<RsltSlotOffset> ConeOffsets { get; } = [];

    /// <summary>
    /// Creates an empty RSLT block.
    /// </summary>
    public RsltBlock() : base()
    {
    }

    /// <summary>
    /// Creates an RSLT block from raw data.
    /// </summary>
    public RsltBlock(ReadOnlySpan<byte> data) : base(data)
    {
    }

    /// <summary>
    /// Parses the RSLT block data.
    /// Source: RSLT.cs lines 76-121
    /// </summary>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        int pos = 0;

        // Validate tag
        string tag = ExtractTag(data);
        if (tag != Tag)
            throw new InvalidDataException($"Invalid RSLT tag: expected '{Tag}', got '{tag}'");
        pos += 4;

        // Read header
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        int nRouteSlots = BinaryPrimitives.ReadInt32LittleEndian(data[pos..]);
        pos += 4;
        int nContainers = BinaryPrimitives.ReadInt32LittleEndian(data[pos..]);
        pos += 4;
        int nEffects = BinaryPrimitives.ReadInt32LittleEndian(data[pos..]);
        pos += 4;
        int nInverseKineticsTargets = BinaryPrimitives.ReadInt32LittleEndian(data[pos..]);
        pos += 4;
        int nConeSlots = BinaryPrimitives.ReadInt32LittleEndian(data[pos..]);
        pos += 4;

        // Clear lists
        Routes.Clear();
        RouteOffsets.Clear();
        Containers.Clear();
        ContainerOffsets.Clear();
        Effects.Clear();
        EffectOffsets.Clear();
        InverseKineticsTargets.Clear();
        InverseKineticsTargetOffsets.Clear();
        Cones.Clear();
        ConeOffsets.Clear();

        // Parse routes
        // Source: RSLT.cs lines 92-96
        ParsePartList(data, ref pos, nRouteSlots, Routes);
        if (nRouteSlots > 0)
            ParseSlotOffsets(data, ref pos, RouteOffsets);

        // Parse containers (slotted parts)
        // Source: RSLT.cs lines 98-102
        ParseSlottedPartList(data, ref pos, nContainers, Containers);
        if (nContainers > 0)
            ParseSlotOffsets(data, ref pos, ContainerOffsets);

        // Parse effects
        // Source: RSLT.cs lines 104-108
        ParsePartList(data, ref pos, nEffects, Effects);
        if (nEffects > 0)
            ParseSlotOffsets(data, ref pos, EffectOffsets);

        // Parse IK targets
        // Source: RSLT.cs lines 110-114
        ParsePartList(data, ref pos, nInverseKineticsTargets, InverseKineticsTargets);
        if (nInverseKineticsTargets > 0)
            ParseSlotOffsets(data, ref pos, InverseKineticsTargetOffsets);

        // Parse cones
        // Source: RSLT.cs lines 116-120
        ParseConePartList(data, ref pos, nConeSlots, Cones);
        if (nConeSlots > 0)
            ParseSlotOffsets(data, ref pos, ConeOffsets);
    }

    /// <summary>
    /// Parses a list of Parts (routes, effects, IK targets).
    /// Source: RSLT.cs PartList.Parse lines 505-528
    /// Data is stored in "structure of arrays" format.
    /// </summary>
    private static void ParsePartList(ReadOnlySpan<byte> data, ref int pos, int count, List<RsltPart> list)
    {
        if (count == 0) return;

        // Read all slot names
        var slotNames = new uint[count];
        for (int i = 0; i < count; i++)
        {
            slotNames[i] = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
            pos += 4;
        }

        // Read all bone names
        var boneNames = new uint[count];
        for (int i = 0; i < count; i++)
        {
            boneNames[i] = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
            pos += 4;
        }

        // Read transform matrices and coordinates (interleaved)
        // For each part: matrixX(12) + coordX(4) + matrixY(12) + coordY(4) + matrixZ(12) + coordZ(4) = 48 bytes
        for (int i = 0; i < count; i++)
        {
            var matrixX = ReadMatrixRow(data, ref pos);
            float coordX = BinaryPrimitives.ReadSingleLittleEndian(data[pos..]);
            pos += 4;

            var matrixY = ReadMatrixRow(data, ref pos);
            float coordY = BinaryPrimitives.ReadSingleLittleEndian(data[pos..]);
            pos += 4;

            var matrixZ = ReadMatrixRow(data, ref pos);
            float coordZ = BinaryPrimitives.ReadSingleLittleEndian(data[pos..]);
            pos += 4;

            list.Add(new RsltPart(slotNames[i], boneNames[i], matrixX, matrixY, matrixZ,
                new MeshVector3(coordX, coordY, coordZ)));
        }
    }

    /// <summary>
    /// Parses a list of SlottedParts (containers).
    /// Source: RSLT.cs SlottedPartList.Parse lines 647-678
    /// </summary>
    private static void ParseSlottedPartList(ReadOnlySpan<byte> data, ref int pos, int count, List<RsltSlottedPart> list)
    {
        if (count == 0) return;

        // Read all slot names
        var slotNames = new uint[count];
        for (int i = 0; i < count; i++)
        {
            slotNames[i] = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
            pos += 4;
        }

        // Read all bone names
        var boneNames = new uint[count];
        for (int i = 0; i < count; i++)
        {
            boneNames[i] = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
            pos += 4;
        }

        // Read slot sizes (bytes)
        var slotSizes = new byte[count];
        for (int i = 0; i < count; i++)
        {
            slotSizes[i] = data[pos++];
        }

        // Read slot type sets (ulong)
        var slotTypeSets = new ulong[count];
        for (int i = 0; i < count; i++)
        {
            slotTypeSets[i] = BinaryPrimitives.ReadUInt64LittleEndian(data[pos..]);
            pos += 8;
        }

        // Read slot direction locked flags (bool/byte)
        var slotDirectionLocked = new bool[count];
        for (int i = 0; i < count; i++)
        {
            slotDirectionLocked[i] = data[pos++] != 0;
        }

        // Read slot legacy hashes
        var slotLegacyHashes = new uint[count];
        for (int i = 0; i < count; i++)
        {
            slotLegacyHashes[i] = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
            pos += 4;
        }

        // Read transform matrices and coordinates (interleaved)
        for (int i = 0; i < count; i++)
        {
            var matrixX = ReadMatrixRow(data, ref pos);
            float coordX = BinaryPrimitives.ReadSingleLittleEndian(data[pos..]);
            pos += 4;

            var matrixY = ReadMatrixRow(data, ref pos);
            float coordY = BinaryPrimitives.ReadSingleLittleEndian(data[pos..]);
            pos += 4;

            var matrixZ = ReadMatrixRow(data, ref pos);
            float coordZ = BinaryPrimitives.ReadSingleLittleEndian(data[pos..]);
            pos += 4;

            list.Add(new RsltSlottedPart(slotNames[i], boneNames[i], matrixX, matrixY, matrixZ,
                new MeshVector3(coordX, coordY, coordZ),
                slotSizes[i], slotTypeSets[i], slotDirectionLocked[i], slotLegacyHashes[i]));
        }
    }

    /// <summary>
    /// Parses a list of ConeParts.
    /// Source: RSLT.cs ConePartList.Parse lines 857-878
    /// </summary>
    private static void ParseConePartList(ReadOnlySpan<byte> data, ref int pos, int count, List<RsltConePart> list)
    {
        if (count == 0) return;

        // Read all slot names
        var slotNames = new uint[count];
        for (int i = 0; i < count; i++)
        {
            slotNames[i] = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
            pos += 4;
        }

        // Read all bone names
        var boneNames = new uint[count];
        for (int i = 0; i < count; i++)
        {
            boneNames[i] = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
            pos += 4;
        }

        // Read transform matrices (3x3 per part, all in a row)
        var matrices = new RsltMatrixRow[count][];
        for (int i = 0; i < count; i++)
        {
            matrices[i] = new RsltMatrixRow[3];
            matrices[i][0] = ReadMatrixRow(data, ref pos);
            matrices[i][1] = ReadMatrixRow(data, ref pos);
            matrices[i][2] = ReadMatrixRow(data, ref pos);
        }

        // Read cone elements (radius + angle per part)
        for (int i = 0; i < count; i++)
        {
            float radius = BinaryPrimitives.ReadSingleLittleEndian(data[pos..]);
            pos += 4;
            float angle = BinaryPrimitives.ReadSingleLittleEndian(data[pos..]);
            pos += 4;

            list.Add(new RsltConePart(slotNames[i], boneNames[i],
                matrices[i][0], matrices[i][1], matrices[i][2],
                new RsltConeElement(radius, angle)));
        }
    }

    /// <summary>
    /// Parses a list of SlotOffsets.
    /// Source: RSLT.cs SlotOffsetList - reads count then entries
    /// </summary>
    private static void ParseSlotOffsets(ReadOnlySpan<byte> data, ref int pos, List<RsltSlotOffset> list)
    {
        int count = BinaryPrimitives.ReadInt32LittleEndian(data[pos..]);
        pos += 4;

        for (int i = 0; i < count; i++)
        {
            int slotIndex = BinaryPrimitives.ReadInt32LittleEndian(data[pos..]);
            pos += 4;

            var position = MeshVector3.Read(data, ref pos);
            var rotation = MeshVector3.Read(data, ref pos);

            list.Add(new RsltSlotOffset(slotIndex, position, rotation));
        }
    }

    private static RsltMatrixRow ReadMatrixRow(ReadOnlySpan<byte> data, ref int pos)
    {
        float r1 = BinaryPrimitives.ReadSingleLittleEndian(data[pos..]);
        pos += 4;
        float r2 = BinaryPrimitives.ReadSingleLittleEndian(data[pos..]);
        pos += 4;
        float r3 = BinaryPrimitives.ReadSingleLittleEndian(data[pos..]);
        pos += 4;
        return new RsltMatrixRow(r1, r2, r3);
    }

    /// <summary>
    /// Serializes the RSLT block back to bytes.
    /// Source: RSLT.cs lines 123-154
    /// </summary>
    public override ReadOnlyMemory<byte> Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Write tag
        writer.Write((byte)'R');
        writer.Write((byte)'S');
        writer.Write((byte)'L');
        writer.Write((byte)'T');

        // Write header
        writer.Write(Version);
        writer.Write(Routes.Count);
        writer.Write(Containers.Count);
        writer.Write(Effects.Count);
        writer.Write(InverseKineticsTargets.Count);
        writer.Write(Cones.Count);

        // Write routes
        SerializePartList(writer, Routes);
        if (Routes.Count > 0)
            SerializeSlotOffsets(writer, RouteOffsets);

        // Write containers
        SerializeSlottedPartList(writer, Containers);
        if (Containers.Count > 0)
            SerializeSlotOffsets(writer, ContainerOffsets);

        // Write effects
        SerializePartList(writer, Effects);
        if (Effects.Count > 0)
            SerializeSlotOffsets(writer, EffectOffsets);

        // Write IK targets
        SerializePartList(writer, InverseKineticsTargets);
        if (InverseKineticsTargets.Count > 0)
            SerializeSlotOffsets(writer, InverseKineticsTargetOffsets);

        // Write cones
        SerializeConePartList(writer, Cones);
        if (Cones.Count > 0)
            SerializeSlotOffsets(writer, ConeOffsets);

        return ms.ToArray();
    }

    /// <summary>
    /// Serializes a list of Parts.
    /// Source: RSLT.cs PartList.UnParse lines 530-546
    /// </summary>
    private static void SerializePartList(BinaryWriter writer, List<RsltPart> list)
    {
        // Write all slot names
        foreach (var part in list)
            writer.Write(part.SlotNameHash);

        // Write all bone names
        foreach (var part in list)
            writer.Write(part.BoneNameHash);

        // Write transform matrices and coordinates (interleaved)
        foreach (var part in list)
        {
            WriteMatrixRow(writer, part.MatrixX);
            writer.Write(part.Coordinates.X);
            WriteMatrixRow(writer, part.MatrixY);
            writer.Write(part.Coordinates.Y);
            WriteMatrixRow(writer, part.MatrixZ);
            writer.Write(part.Coordinates.Z);
        }
    }

    /// <summary>
    /// Serializes a list of SlottedParts.
    /// Source: RSLT.cs SlottedPartList.UnParse lines 680-701
    /// </summary>
    private static void SerializeSlottedPartList(BinaryWriter writer, List<RsltSlottedPart> list)
    {
        // Write all slot names
        foreach (var part in list)
            writer.Write(part.SlotNameHash);

        // Write all bone names
        foreach (var part in list)
            writer.Write(part.BoneNameHash);

        // Write slot sizes
        foreach (var part in list)
            writer.Write(part.SlotSize);

        // Write slot type sets
        foreach (var part in list)
            writer.Write(part.SlotTypeSet);

        // Write slot direction locked flags
        foreach (var part in list)
            writer.Write(part.SlotDirectionLocked);

        // Write slot legacy hashes
        foreach (var part in list)
            writer.Write(part.SlotLegacyHash);

        // Write transform matrices and coordinates (interleaved)
        foreach (var part in list)
        {
            WriteMatrixRow(writer, part.MatrixX);
            writer.Write(part.Coordinates.X);
            WriteMatrixRow(writer, part.MatrixY);
            writer.Write(part.Coordinates.Y);
            WriteMatrixRow(writer, part.MatrixZ);
            writer.Write(part.Coordinates.Z);
        }
    }

    /// <summary>
    /// Serializes a list of ConeParts.
    /// Source: RSLT.cs ConePartList.UnParse lines 879-898
    /// </summary>
    private static void SerializeConePartList(BinaryWriter writer, List<RsltConePart> list)
    {
        // Write all slot names
        foreach (var part in list)
            writer.Write(part.SlotNameHash);

        // Write all bone names
        foreach (var part in list)
            writer.Write(part.BoneNameHash);

        // Write all transform matrices
        foreach (var part in list)
        {
            WriteMatrixRow(writer, part.MatrixX);
            WriteMatrixRow(writer, part.MatrixY);
            WriteMatrixRow(writer, part.MatrixZ);
        }

        // Write all cone elements
        foreach (var part in list)
        {
            writer.Write(part.Cone.Radius);
            writer.Write(part.Cone.Angle);
        }
    }

    /// <summary>
    /// Serializes a list of SlotOffsets.
    /// </summary>
    private static void SerializeSlotOffsets(BinaryWriter writer, List<RsltSlotOffset> list)
    {
        writer.Write(list.Count);
        foreach (var offset in list)
        {
            writer.Write(offset.SlotIndex);
            writer.Write(offset.Position.X);
            writer.Write(offset.Position.Y);
            writer.Write(offset.Position.Z);
            writer.Write(offset.Rotation.X);
            writer.Write(offset.Rotation.Y);
            writer.Write(offset.Rotation.Z);
        }
    }

    private static void WriteMatrixRow(BinaryWriter writer, RsltMatrixRow row)
    {
        writer.Write(row.R1);
        writer.Write(row.R2);
        writer.Write(row.R3);
    }
}

/// <summary>
/// A 3x1 rotation matrix row.
/// Source: RSLT.cs MatrixRow class
/// </summary>
public readonly struct RsltMatrixRow
{
    public float R1 { get; }
    public float R2 { get; }
    public float R3 { get; }

    public RsltMatrixRow(float r1, float r2, float r3)
    {
        R1 = r1;
        R2 = r2;
        R3 = r3;
    }
}

/// <summary>
/// Slot offset entry.
/// Source: RSLT.cs SlotOffset class
/// </summary>
public sealed class RsltSlotOffset
{
    public int SlotIndex { get; }
    public MeshVector3 Position { get; }
    public MeshVector3 Rotation { get; }

    public RsltSlotOffset(int slotIndex, MeshVector3 position, MeshVector3 rotation)
    {
        SlotIndex = slotIndex;
        Position = position;
        Rotation = rotation;
    }
}

/// <summary>
/// Basic slot part (for routes, effects, IK targets).
/// Source: RSLT.cs Part class
/// </summary>
public class RsltPart
{
    public uint SlotNameHash { get; }
    public uint BoneNameHash { get; }
    public RsltMatrixRow MatrixX { get; }
    public RsltMatrixRow MatrixY { get; }
    public RsltMatrixRow MatrixZ { get; }
    public MeshVector3 Coordinates { get; }

    public RsltPart(uint slotNameHash, uint boneNameHash,
        RsltMatrixRow matrixX, RsltMatrixRow matrixY, RsltMatrixRow matrixZ,
        MeshVector3 coordinates)
    {
        SlotNameHash = slotNameHash;
        BoneNameHash = boneNameHash;
        MatrixX = matrixX;
        MatrixY = matrixY;
        MatrixZ = matrixZ;
        Coordinates = coordinates;
    }
}

/// <summary>
/// Slotted part with slot placement info (for containers).
/// Source: RSLT.cs SlottedPart class
/// </summary>
public sealed class RsltSlottedPart : RsltPart
{
    public byte SlotSize { get; }
    public ulong SlotTypeSet { get; }
    public bool SlotDirectionLocked { get; }
    public uint SlotLegacyHash { get; }

    public RsltSlottedPart(uint slotNameHash, uint boneNameHash,
        RsltMatrixRow matrixX, RsltMatrixRow matrixY, RsltMatrixRow matrixZ,
        MeshVector3 coordinates,
        byte slotSize, ulong slotTypeSet, bool slotDirectionLocked, uint slotLegacyHash)
        : base(slotNameHash, boneNameHash, matrixX, matrixY, matrixZ, coordinates)
    {
        SlotSize = slotSize;
        SlotTypeSet = slotTypeSet;
        SlotDirectionLocked = slotDirectionLocked;
        SlotLegacyHash = slotLegacyHash;
    }
}

/// <summary>
/// Cone element with radius and angle.
/// Source: RSLT.cs ConeElement class
/// </summary>
public readonly struct RsltConeElement
{
    public float Radius { get; }
    public float Angle { get; }

    public RsltConeElement(float radius, float angle)
    {
        Radius = radius;
        Angle = angle;
    }
}

/// <summary>
/// Cone part.
/// Source: RSLT.cs ConePart class
/// </summary>
public sealed class RsltConePart
{
    public uint SlotNameHash { get; }
    public uint BoneNameHash { get; }
    public RsltMatrixRow MatrixX { get; }
    public RsltMatrixRow MatrixY { get; }
    public RsltMatrixRow MatrixZ { get; }
    public RsltConeElement Cone { get; }

    public RsltConePart(uint slotNameHash, uint boneNameHash,
        RsltMatrixRow matrixX, RsltMatrixRow matrixY, RsltMatrixRow matrixZ,
        RsltConeElement cone)
    {
        SlotNameHash = slotNameHash;
        BoneNameHash = boneNameHash;
        MatrixX = matrixX;
        MatrixY = matrixY;
        MatrixZ = matrixZ;
        Cone = cone;
    }
}
