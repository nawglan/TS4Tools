namespace TS4Tools.Wrappers.MeshChunks;

/// <summary>
/// Unknown data structure used in GEOM version 0x0C.
/// Contains an unknown uint and a list of Vector2 values.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MeshChunks/GEOM.cs lines 1011-1085
/// </summary>
public sealed class GeomUnknownThing
{
    /// <summary>Unknown uint value.</summary>
    public uint Unknown1 { get; set; }

    /// <summary>List of Vector2 values.</summary>
    public List<MeshVector2> Unknown2 { get; } = [];

    /// <summary>
    /// Creates an empty UnknownThing.
    /// </summary>
    public GeomUnknownThing()
    {
    }

    /// <summary>
    /// Creates an UnknownThing with the specified values.
    /// </summary>
    public GeomUnknownThing(uint unknown1, IEnumerable<MeshVector2> unknown2)
    {
        Unknown1 = unknown1;
        Unknown2.AddRange(unknown2);
    }

    /// <summary>
    /// Reads an UnknownThing from the span.
    /// Source: GEOM.cs UnknownThing.Parse lines 1030-1035
    /// </summary>
    public static GeomUnknownThing Read(ReadOnlySpan<byte> data, ref int position)
    {
        var thing = new GeomUnknownThing
        {
            Unknown1 = BinaryPrimitives.ReadUInt32LittleEndian(data[position..])
        };
        position += 4;

        // Read Vector2 list
        int count = BinaryPrimitives.ReadInt32LittleEndian(data[position..]);
        position += 4;

        for (int i = 0; i < count; i++)
        {
            thing.Unknown2.Add(MeshVector2.Read(data, ref position));
        }

        return thing;
    }

    /// <summary>
    /// Writes the UnknownThing to a binary writer.
    /// Source: GEOM.cs UnknownThing.UnParse lines 1036-1042
    /// </summary>
    public void Write(BinaryWriter writer)
    {
        writer.Write(Unknown1);
        writer.Write(Unknown2.Count);

        var buffer = new byte[MeshVector2.Size];
        foreach (var vec in Unknown2)
        {
            int pos = 0;
            vec.Write(buffer, ref pos);
            writer.Write(buffer);
        }
    }
}

/// <summary>
/// List of UnknownThing entries.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MeshChunks/GEOM.cs lines 1086-1096
/// </summary>
public sealed class GeomUnknownThingList
{
    private readonly List<GeomUnknownThing> _things = [];

    /// <summary>The entries in this list.</summary>
    public IReadOnlyList<GeomUnknownThing> Things => _things;

    /// <summary>Number of entries.</summary>
    public int Count => _things.Count;

    /// <summary>
    /// Creates an empty list.
    /// </summary>
    public GeomUnknownThingList()
    {
    }

    /// <summary>
    /// Reads the list from the span.
    /// </summary>
    public static GeomUnknownThingList Read(ReadOnlySpan<byte> data, ref int position)
    {
        int count = BinaryPrimitives.ReadInt32LittleEndian(data[position..]);
        position += 4;

        var list = new GeomUnknownThingList();
        for (int i = 0; i < count; i++)
        {
            list._things.Add(GeomUnknownThing.Read(data, ref position));
        }

        return list;
    }

    /// <summary>
    /// Writes the list to a binary writer.
    /// </summary>
    public void Write(BinaryWriter writer)
    {
        writer.Write(_things.Count);
        foreach (var thing in _things)
        {
            thing.Write(writer);
        }
    }
}

/// <summary>
/// Second unknown data structure used in GEOM version 0x0C.
/// Contains 18 fields of various types (53 bytes total).
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MeshChunks/GEOM.cs lines 1099-1277
/// </summary>
public sealed class GeomUnknownThing2
{
    /// <summary>Size in bytes when serialized (4+2+2+2+13*4+1 = 63 bytes).</summary>
    public const int Size = 63;

    /// <summary>Unknown field 1 (uint).</summary>
    public uint Unknown1 { get; set; }

    /// <summary>Unknown field 2 (ushort).</summary>
    public ushort Unknown2 { get; set; }

    /// <summary>Unknown field 3 (ushort).</summary>
    public ushort Unknown3 { get; set; }

    /// <summary>Unknown field 4 (ushort).</summary>
    public ushort Unknown4 { get; set; }

    /// <summary>Unknown field 5 (float).</summary>
    public float Unknown5 { get; set; }

    /// <summary>Unknown field 6 (float).</summary>
    public float Unknown6 { get; set; }

    /// <summary>Unknown field 7 (float).</summary>
    public float Unknown7 { get; set; }

    /// <summary>Unknown field 8 (float).</summary>
    public float Unknown8 { get; set; }

    /// <summary>Unknown field 9 (float).</summary>
    public float Unknown9 { get; set; }

    /// <summary>Unknown field 10 (float).</summary>
    public float Unknown10 { get; set; }

    /// <summary>Unknown field 11 (float).</summary>
    public float Unknown11 { get; set; }

    /// <summary>Unknown field 12 (float).</summary>
    public float Unknown12 { get; set; }

    /// <summary>Unknown field 13 (float).</summary>
    public float Unknown13 { get; set; }

    /// <summary>Unknown field 14 (float).</summary>
    public float Unknown14 { get; set; }

    /// <summary>Unknown field 15 (float).</summary>
    public float Unknown15 { get; set; }

    /// <summary>Unknown field 16 (float).</summary>
    public float Unknown16 { get; set; }

    /// <summary>Unknown field 17 (float).</summary>
    public float Unknown17 { get; set; }

    /// <summary>Unknown field 18 (byte).</summary>
    public byte Unknown18 { get; set; }

    /// <summary>
    /// Creates an empty UnknownThing2.
    /// </summary>
    public GeomUnknownThing2()
    {
    }

    /// <summary>
    /// Reads an UnknownThing2 from the span.
    /// Source: GEOM.cs UnknownThing2.Parse lines 1161-1182
    /// </summary>
    public static GeomUnknownThing2 Read(ReadOnlySpan<byte> data, ref int position)
    {
        var thing = new GeomUnknownThing2
        {
            Unknown1 = BinaryPrimitives.ReadUInt32LittleEndian(data[position..])
        };
        position += 4;

        thing.Unknown2 = BinaryPrimitives.ReadUInt16LittleEndian(data[position..]);
        position += 2;

        thing.Unknown3 = BinaryPrimitives.ReadUInt16LittleEndian(data[position..]);
        position += 2;

        thing.Unknown4 = BinaryPrimitives.ReadUInt16LittleEndian(data[position..]);
        position += 2;

        thing.Unknown5 = BinaryPrimitives.ReadSingleLittleEndian(data[position..]);
        position += 4;

        thing.Unknown6 = BinaryPrimitives.ReadSingleLittleEndian(data[position..]);
        position += 4;

        thing.Unknown7 = BinaryPrimitives.ReadSingleLittleEndian(data[position..]);
        position += 4;

        thing.Unknown8 = BinaryPrimitives.ReadSingleLittleEndian(data[position..]);
        position += 4;

        thing.Unknown9 = BinaryPrimitives.ReadSingleLittleEndian(data[position..]);
        position += 4;

        thing.Unknown10 = BinaryPrimitives.ReadSingleLittleEndian(data[position..]);
        position += 4;

        thing.Unknown11 = BinaryPrimitives.ReadSingleLittleEndian(data[position..]);
        position += 4;

        thing.Unknown12 = BinaryPrimitives.ReadSingleLittleEndian(data[position..]);
        position += 4;

        thing.Unknown13 = BinaryPrimitives.ReadSingleLittleEndian(data[position..]);
        position += 4;

        thing.Unknown14 = BinaryPrimitives.ReadSingleLittleEndian(data[position..]);
        position += 4;

        thing.Unknown15 = BinaryPrimitives.ReadSingleLittleEndian(data[position..]);
        position += 4;

        thing.Unknown16 = BinaryPrimitives.ReadSingleLittleEndian(data[position..]);
        position += 4;

        thing.Unknown17 = BinaryPrimitives.ReadSingleLittleEndian(data[position..]);
        position += 4;

        thing.Unknown18 = data[position++];

        return thing;
    }

    /// <summary>
    /// Writes the UnknownThing2 to a binary writer.
    /// Source: GEOM.cs UnknownThing2.UnParse lines 1183-1204
    /// </summary>
    public void Write(BinaryWriter writer)
    {
        writer.Write(Unknown1);
        writer.Write(Unknown2);
        writer.Write(Unknown3);
        writer.Write(Unknown4);
        writer.Write(Unknown5);
        writer.Write(Unknown6);
        writer.Write(Unknown7);
        writer.Write(Unknown8);
        writer.Write(Unknown9);
        writer.Write(Unknown10);
        writer.Write(Unknown11);
        writer.Write(Unknown12);
        writer.Write(Unknown13);
        writer.Write(Unknown14);
        writer.Write(Unknown15);
        writer.Write(Unknown16);
        writer.Write(Unknown17);
        writer.Write(Unknown18);
    }
}

/// <summary>
/// List of UnknownThing2 entries.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MeshChunks/GEOM.cs lines 1278-1288
/// </summary>
public sealed class GeomUnknownThing2List
{
    private readonly List<GeomUnknownThing2> _things = [];

    /// <summary>The entries in this list.</summary>
    public IReadOnlyList<GeomUnknownThing2> Things => _things;

    /// <summary>Number of entries.</summary>
    public int Count => _things.Count;

    /// <summary>
    /// Creates an empty list.
    /// </summary>
    public GeomUnknownThing2List()
    {
    }

    /// <summary>
    /// Reads the list from the span.
    /// </summary>
    public static GeomUnknownThing2List Read(ReadOnlySpan<byte> data, ref int position)
    {
        int count = BinaryPrimitives.ReadInt32LittleEndian(data[position..]);
        position += 4;

        var list = new GeomUnknownThing2List();
        for (int i = 0; i < count; i++)
        {
            list._things.Add(GeomUnknownThing2.Read(data, ref position));
        }

        return list;
    }

    /// <summary>
    /// Writes the list to a binary writer.
    /// </summary>
    public void Write(BinaryWriter writer)
    {
        writer.Write(_things.Count);
        foreach (var thing in _things)
        {
            thing.Write(writer);
        }
    }
}

/// <summary>
/// A list of bone hash values (uint32).
/// Source: UIntList in legacy, used for bone references.
/// </summary>
public sealed class GeomBoneHashList
{
    private readonly List<uint> _hashes = [];

    /// <summary>The bone hashes in this list.</summary>
    public IReadOnlyList<uint> Hashes => _hashes;

    /// <summary>Number of hashes.</summary>
    public int Count => _hashes.Count;

    /// <summary>
    /// Creates an empty list.
    /// </summary>
    public GeomBoneHashList()
    {
    }

    /// <summary>
    /// Reads the list from the span.
    /// </summary>
    public static GeomBoneHashList Read(ReadOnlySpan<byte> data, ref int position)
    {
        int count = BinaryPrimitives.ReadInt32LittleEndian(data[position..]);
        position += 4;

        var list = new GeomBoneHashList();
        for (int i = 0; i < count; i++)
        {
            list._hashes.Add(BinaryPrimitives.ReadUInt32LittleEndian(data[position..]));
            position += 4;
        }

        return list;
    }

    /// <summary>
    /// Writes the list to a binary writer.
    /// </summary>
    public void Write(BinaryWriter writer)
    {
        writer.Write(_hashes.Count);
        foreach (var hash in _hashes)
        {
            writer.Write(hash);
        }
    }
}
