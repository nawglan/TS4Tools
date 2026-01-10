// Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/GEOMListResource.cs

using System.Buffers.Binary;
using TS4Tools.Resources;
using TS4Tools.Wrappers.CasPartResource;

namespace TS4Tools.Wrappers;

/// <summary>
/// GEOM List resource containing geometry reference blocks.
/// Resource Type: 0xAC16FBEC
/// </summary>
/// <remarks>
/// Source: GEOMListResource.cs lines 32-241
/// Contains arrays of TGI keys (public, external, delay-load) and
/// reference blocks linking CAS part regions to geometry resources.
/// </remarks>
[ResourceHandler(TypeId)]
public sealed class GeomListResource : TypedResource
{
    /// <summary>Resource type identifier.</summary>
    public const uint TypeId = 0xAC16FBEC;

    private const int MinHeaderSize = 20; // 5 x uint32 for counts
    private const int TgiBlockSize = 16; // Type (4) + Group (4) + Instance (8) in ITG order

    private byte[] _rawData = [];
    private readonly List<ResourceKey> _publicKeys = [];
    private readonly List<ResourceKey> _externalKeys = [];
    private readonly List<ResourceKey> _delayLoadKeys = [];
    private readonly List<GeomReferenceBlock> _referenceBlocks = [];

    /// <summary>
    /// Context version.
    /// </summary>
    public uint ContextVersion { get; set; }

    /// <summary>
    /// Object position in bytes.
    /// </summary>
    public uint ObjectPosition { get; set; }

    /// <summary>
    /// Object length in bytes.
    /// </summary>
    public uint ObjectLength { get; set; }

    /// <summary>
    /// Object version.
    /// </summary>
    public uint ObjectVersion { get; set; }

    /// <summary>
    /// Public TGI keys.
    /// </summary>
    public IReadOnlyList<ResourceKey> PublicKeys => _publicKeys;

    /// <summary>
    /// External TGI keys.
    /// </summary>
    public IReadOnlyList<ResourceKey> ExternalKeys => _externalKeys;

    /// <summary>
    /// Delay-load TGI keys.
    /// </summary>
    public IReadOnlyList<ResourceKey> DelayLoadKeys => _delayLoadKeys;

    /// <summary>
    /// Reference blocks linking regions to geometry.
    /// </summary>
    public IReadOnlyList<GeomReferenceBlock> ReferenceBlocks => _referenceBlocks;

    /// <summary>
    /// Whether this resource was successfully parsed.
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Creates a new GeomListResource by parsing data.
    /// </summary>
    public GeomListResource(ResourceKey key, ReadOnlyMemory<byte> data) : base(key, data)
    {
    }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        _rawData = data.ToArray();
        _publicKeys.Clear();
        _externalKeys.Clear();
        _delayLoadKeys.Clear();
        _referenceBlocks.Clear();
        IsValid = false;

        if (data.Length < MinHeaderSize)
            return;

        try
        {
            int offset = 0;

            // Read header counts
            // Source: GEOMListResource.cs lines 63-67
            ContextVersion = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;

            uint publicKeyCount = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;

            uint externalKeyCount = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;

            uint delayLoadKeyCount = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;

            // Object count (stored but not directly used - it's for the reference block list)
            uint objectCount = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;

            // Validate counts
            // Security: prevent unreasonable array sizes
            if (publicKeyCount > 10000 || externalKeyCount > 10000 || delayLoadKeyCount > 10000 || objectCount > 10000)
                return;

            int expectedKeySize = (int)(publicKeyCount + externalKeyCount + delayLoadKeyCount) * TgiBlockSize;
            if (offset + expectedKeySize > data.Length)
                return;

            // Read public keys (ITG order)
            // Source: GEOMListResource.cs lines 68-72
            for (int i = 0; i < publicKeyCount; i++)
            {
                var key = ReadTgiBlock(data, ref offset);
                _publicKeys.Add(key);
            }

            // Read external keys
            // Source: GEOMListResource.cs lines 73-77
            for (int i = 0; i < externalKeyCount; i++)
            {
                var key = ReadTgiBlock(data, ref offset);
                _externalKeys.Add(key);
            }

            // Read delay-load keys
            // Source: GEOMListResource.cs lines 78-82
            for (int i = 0; i < delayLoadKeyCount; i++)
            {
                var key = ReadTgiBlock(data, ref offset);
                _delayLoadKeys.Add(key);
            }

            // Read object position, length, version
            // Source: GEOMListResource.cs lines 83-86
            if (offset + 12 > data.Length)
                return;

            ObjectPosition = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;

            ObjectLength = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;

            ObjectVersion = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;

            // Read reference block list
            // Source: GEOMListResource.cs lines 196-205
            if (offset + 4 > data.Length)
                return;

            int blockCount = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
            offset += 4;

            if (blockCount < 0 || blockCount > 10000)
                return;

            for (int i = 0; i < blockCount; i++)
            {
                var block = ParseReferenceBlock(data, ref offset);
                if (block == null)
                    return; // Parse failed
                _referenceBlocks.Add(block);
            }

            IsValid = true;
        }
        catch
        {
            IsValid = false;
        }
    }

    /// <summary>
    /// Parses a single reference block.
    /// Source: GEOMListResource.cs lines 136-150
    /// </summary>
    private static GeomReferenceBlock? ParseReferenceBlock(ReadOnlySpan<byte> data, ref int offset)
    {
        // Minimum: region(4) + layer(4) + isReplacement(1) + count(4) = 13 bytes
        if (offset + 13 > data.Length)
            return null;

        var region = (CASPartRegion)BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        float layer = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
        offset += 4;

        bool isReplacement = data[offset] != 0;
        offset += 1;

        int tgiCount = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
        offset += 4;

        if (tgiCount < 0 || tgiCount > 10000)
            return null;

        if (offset + (tgiCount * TgiBlockSize) > data.Length)
            return null;

        var tgiList = new List<ResourceKey>(tgiCount);
        for (int i = 0; i < tgiCount; i++)
        {
            var key = ReadTgiBlock(data, ref offset);
            tgiList.Add(key);
        }

        return new GeomReferenceBlock
        {
            Region = region,
            Layer = layer,
            IsReplacement = isReplacement,
            TgiList = tgiList
        };
    }

    /// <summary>
    /// Reads a TGI block in ITG order (Instance, Type, Group stored as Type, Group, Instance in file).
    /// Source: GEOMListResource.cs line 71 uses "ITG" order
    /// </summary>
    private static ResourceKey ReadTgiBlock(ReadOnlySpan<byte> data, ref int offset)
    {
        // ITG order: Instance (8 bytes), Type (4 bytes), Group (4 bytes)
        ulong instance = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
        offset += 8;

        uint type = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        uint group = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        return new ResourceKey(type, group, instance);
    }

    /// <inheritdoc/>
    protected override ReadOnlyMemory<byte> Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Write header
        // Source: GEOMListResource.cs lines 94-98
        writer.Write(ContextVersion);
        writer.Write((uint)_publicKeys.Count);
        writer.Write((uint)_externalKeys.Count);
        writer.Write((uint)_delayLoadKeys.Count);
        writer.Write((uint)_referenceBlocks.Count); // objectCount

        // Write public keys
        // Source: GEOMListResource.cs lines 100-103
        foreach (var key in _publicKeys)
        {
            WriteTgiBlock(writer, key);
        }

        // Write external keys
        // Source: GEOMListResource.cs lines 104-107
        foreach (var key in _externalKeys)
        {
            WriteTgiBlock(writer, key);
        }

        // Write delay-load keys
        // Source: GEOMListResource.cs lines 108-111
        foreach (var key in _delayLoadKeys)
        {
            WriteTgiBlock(writer, key);
        }

        // Calculate and write object position, length
        // Source: GEOMListResource.cs lines 112-117
        uint objectPos = (uint)(ms.Position + 8); // After position and length fields
        uint objectLen = CalculateReferenceBlockListSize() + 4; // +4 for the count

        writer.Write(objectPos);
        writer.Write(objectLen);
        writer.Write(ObjectVersion);

        // Write reference block list
        // Source: GEOMListResource.cs lines 207-214
        writer.Write(_referenceBlocks.Count);
        foreach (var block in _referenceBlocks)
        {
            WriteReferenceBlock(writer, block);
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Writes a TGI block in ITG order.
    /// </summary>
    private static void WriteTgiBlock(BinaryWriter writer, ResourceKey key)
    {
        // ITG order: Instance (8), Type (4), Group (4)
        writer.Write(key.Instance);
        writer.Write(key.ResourceType);
        writer.Write(key.ResourceGroup);
    }

    /// <summary>
    /// Writes a reference block.
    /// Source: GEOMListResource.cs lines 152-162
    /// </summary>
    private static void WriteReferenceBlock(BinaryWriter writer, GeomReferenceBlock block)
    {
        writer.Write((uint)block.Region);
        writer.Write(block.Layer);
        writer.Write(block.IsReplacement);
        writer.Write(block.TgiList.Count);
        foreach (var tgi in block.TgiList)
        {
            WriteTgiBlock(writer, tgi);
        }
    }

    /// <summary>
    /// Calculates the total size of the reference block list (excluding count).
    /// Source: GEOMListResource.cs lines 183-194
    /// </summary>
    private uint CalculateReferenceBlockListSize()
    {
        uint size = 0;
        foreach (var block in _referenceBlocks)
        {
            // region(4) + layer(4) + isReplacement(1) + count(4) + tgis(16 each)
            size += 13 + (uint)(block.TgiList.Count * TgiBlockSize);
        }
        return size;
    }

    /// <summary>
    /// Adds a public key.
    /// </summary>
    public void AddPublicKey(ResourceKey key) => _publicKeys.Add(key);

    /// <summary>
    /// Adds an external key.
    /// </summary>
    public void AddExternalKey(ResourceKey key) => _externalKeys.Add(key);

    /// <summary>
    /// Adds a delay-load key.
    /// </summary>
    public void AddDelayLoadKey(ResourceKey key) => _delayLoadKeys.Add(key);

    /// <summary>
    /// Adds a reference block.
    /// </summary>
    public void AddReferenceBlock(GeomReferenceBlock block) => _referenceBlocks.Add(block);

    /// <summary>
    /// Clears all keys and reference blocks.
    /// </summary>
    public void Clear()
    {
        _publicKeys.Clear();
        _externalKeys.Clear();
        _delayLoadKeys.Clear();
        _referenceBlocks.Clear();
    }

    /// <inheritdoc/>
    protected override void InitializeDefaults()
    {
        _rawData = [];
        ContextVersion = 0;
        ObjectPosition = 0;
        ObjectLength = 0;
        ObjectVersion = 0;
        _publicKeys.Clear();
        _externalKeys.Clear();
        _delayLoadKeys.Clear();
        _referenceBlocks.Clear();
        IsValid = false;
    }
}

/// <summary>
/// A reference block linking a CAS part region to geometry resources.
/// Source: GEOMListResource.cs ReferenceBlock class, lines 127-175
/// </summary>
public sealed class GeomReferenceBlock
{
    /// <summary>
    /// CAS part region this block applies to.
    /// </summary>
    public CASPartRegion Region { get; init; }

    /// <summary>
    /// Layer value for sorting.
    /// </summary>
    public float Layer { get; init; }

    /// <summary>
    /// Whether this is a replacement for existing geometry.
    /// </summary>
    public bool IsReplacement { get; init; }

    /// <summary>
    /// List of TGI references to geometry resources.
    /// </summary>
    public IReadOnlyList<ResourceKey> TgiList { get; init; } = [];
}
