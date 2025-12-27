using System.Buffers.Binary;
using TS4Tools.Resources;

namespace TS4Tools.Wrappers;

/// <summary>
/// Generic RCOL (Resource Container Object Layout) resource.
/// RCOL resources contain one or more chunks, each identified by a TGI block.
/// Source: GenericRCOLResource.cs from legacy s4pi Wrappers/GenericRCOLResource/
/// </summary>
public sealed class RcolResource : TypedResource
{
    /// <summary>
    /// Minimum header size in bytes (5 x uint32 = 20 bytes).
    /// </summary>
    private const int MinHeaderSize = 20;

    /// <summary>
    /// Maximum reasonable chunk count for validation.
    /// </summary>
    private const int MaxChunkCount = 10000;

    /// <summary>
    /// Maximum reasonable external resource count for validation.
    /// </summary>
    private const int MaxExternalCount = 10000;

    private byte[] _rawData = [];
    private readonly List<RcolTgiBlock> _externalResources = [];
    private readonly List<RcolChunkEntry> _chunks = [];

    /// <summary>
    /// The RCOL version.
    /// Source: GenericRCOLResource.cs line 42
    /// </summary>
    public uint Version { get; private set; }

    /// <summary>
    /// The number of "public" chunks in the resource.
    /// Public chunks are accessible externally, private chunks are internal.
    /// Source: GenericRCOLResource.cs lines 45-46
    /// </summary>
    public int PublicChunksCount { get; private set; }

    /// <summary>
    /// Unknown/unused field, preserved for round-trip.
    /// Source: GenericRCOLResource.cs lines 48-50
    /// </summary>
    public uint Unused { get; private set; }

    /// <summary>
    /// External resources referenced by this RCOL.
    /// </summary>
    public IReadOnlyList<RcolTgiBlock> ExternalResources => _externalResources;

    /// <summary>
    /// The chunks in this RCOL resource.
    /// </summary>
    public IReadOnlyList<RcolChunkEntry> Chunks => _chunks;

    /// <summary>
    /// Number of chunks in this resource.
    /// </summary>
    public int ChunkCount => _chunks.Count;

    /// <summary>
    /// Number of external resource references.
    /// </summary>
    public int ExternalResourceCount => _externalResources.Count;

    /// <summary>
    /// Whether this resource was parsed successfully.
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Creates a new RCOL resource by parsing data.
    /// </summary>
    public RcolResource(ResourceKey key, ReadOnlyMemory<byte> data) : base(key, data)
    {
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Source: GenericRCOLResource.cs lines 71-98 (Parse method)
    /// </remarks>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        _rawData = data.ToArray();
        _externalResources.Clear();
        _chunks.Clear();
        IsValid = false;

        if (data.Length < MinHeaderSize)
        {
            // Too short, keep as raw data only
            return;
        }

        try
        {
            int pos = 0;

            // Read header
            // Source: GenericRCOLResource.cs lines 75-79
            Version = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
            pos += 4;

            PublicChunksCount = BinaryPrimitives.ReadInt32LittleEndian(data[pos..]);
            pos += 4;

            Unused = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
            pos += 4;

            int resourceCount = BinaryPrimitives.ReadInt32LittleEndian(data[pos..]);
            pos += 4;

            int chunkCount = BinaryPrimitives.ReadInt32LittleEndian(data[pos..]);
            pos += 4;

            // Validate counts
            if (chunkCount < 0 || chunkCount > MaxChunkCount)
                throw new ResourceFormatException($"Invalid RCOL chunk count: {chunkCount}");

            if (resourceCount < 0 || resourceCount > MaxExternalCount)
                throw new ResourceFormatException($"Invalid RCOL resource count: {resourceCount}");

            // Calculate minimum required size
            int minRequired = MinHeaderSize +
                (chunkCount * RcolTgiBlock.Size) +
                (resourceCount * RcolTgiBlock.Size) +
                (chunkCount * 8); // Index entries

            if (data.Length < minRequired)
                throw new ResourceFormatException($"RCOL data too short: need {minRequired} bytes, got {data.Length}");

            // Read chunk TGI blocks
            // Source: GenericRCOLResource.cs lines 80-81
            var chunkTgis = new RcolTgiBlock[chunkCount];
            for (int i = 0; i < chunkCount; i++)
            {
                chunkTgis[i] = RcolTgiBlock.Read(data[pos..]);
                pos += RcolTgiBlock.Size;
            }

            // Read external resource TGI blocks
            // Source: GenericRCOLResource.cs line 82
            for (int i = 0; i < resourceCount; i++)
            {
                _externalResources.Add(RcolTgiBlock.Read(data[pos..]));
                pos += RcolTgiBlock.Size;
            }

            // Read chunk index
            // Source: GenericRCOLResource.cs lines 84-85
            var index = new (uint Position, int Length)[chunkCount];
            for (int i = 0; i < chunkCount; i++)
            {
                index[i].Position = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
                index[i].Length = BinaryPrimitives.ReadInt32LittleEndian(data[(pos + 4)..]);
                pos += 8;
            }

            // Special case: single chunk with zero position
            // Source: GenericRCOLResource.cs lines 86-95
            if (chunkCount == 1 && index[0].Position == 0)
            {
                // Position 0 means data starts immediately after the index
                // Formula from legacy: 0x2c + (uint)countResources * 16
                // But we can just use current position since we're at the end of the index
                index[0].Position = (uint)pos;
                index[0].Length = data.Length - pos;

                // Try to determine resource type from tag if TGI has type 0
                if (chunkTgis[0].ResourceType == 0 && index[0].Length >= 4)
                {
                    string tag = RcolBlock.ExtractTag(data[pos..]);
                    uint inferredType = GetTypeFromTag(tag);
                    if (inferredType != 0)
                    {
                        chunkTgis[0] = new RcolTgiBlock(
                            chunkTgis[0].Instance,
                            inferredType,
                            chunkTgis[0].ResourceGroup);
                    }
                }
            }

            // Parse each chunk
            // Source: GenericRCOLResource.cs lines 97, 301-311
            for (int i = 0; i < chunkCount; i++)
            {
                int chunkPos = (int)index[i].Position;
                int chunkLen = index[i].Length;

                // Validate chunk bounds
                if (chunkPos < 0 || chunkPos > data.Length)
                    throw new ResourceFormatException($"Invalid chunk position: {chunkPos}");

                if (chunkLen < 0 || chunkPos + chunkLen > data.Length)
                    throw new ResourceFormatException($"Invalid chunk length: {chunkLen} at position {chunkPos}");

                var chunkData = data.Slice(chunkPos, chunkLen);
                var block = CreateBlock(chunkTgis[i].ResourceType, chunkData);

                _chunks.Add(new RcolChunkEntry(
                    chunkTgis[i],
                    block,
                    index[i].Position,
                    index[i].Length));
            }

            IsValid = true;
        }
        catch (Exception ex) when (ex is not ResourceFormatException)
        {
            // Keep raw data, parsing failed
            throw new ResourceFormatException($"Failed to parse RCOL resource: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Source: GenericRCOLResource.cs lines 104-140 (UnParse method)
    /// </remarks>
    protected override ReadOnlyMemory<byte> Serialize()
    {
        if (!IsValid || _chunks.Count == 0)
        {
            // Return raw data if not parsed
            return _rawData;
        }

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Write header
        // Source: GenericRCOLResource.cs lines 111-117
        writer.Write(Version);
        writer.Write(PublicChunksCount);
        writer.Write(Unused);
        writer.Write(_externalResources.Count);
        writer.Write(_chunks.Count);

        // Write chunk TGI blocks
        // Source: GenericRCOLResource.cs line 118
        var tgiBuffer = new byte[RcolTgiBlock.Size];
        foreach (var chunk in _chunks)
        {
            chunk.TgiBlock.Write(tgiBuffer);
            writer.Write(tgiBuffer);
        }

        // Write external resource TGI blocks
        // Source: GenericRCOLResource.cs line 119
        foreach (var external in _externalResources)
        {
            external.Write(tgiBuffer);
            writer.Write(tgiBuffer);
        }

        // Reserve space for index (patched later)
        // Source: GenericRCOLResource.cs lines 121-123
        long indexPos = ms.Position;
        for (int i = 0; i < _chunks.Count; i++)
        {
            writer.Write(0u);  // Position placeholder
            writer.Write(0);   // Length placeholder
        }

        // Write chunk data with 4-byte alignment
        // Source: GenericRCOLResource.cs lines 125-134
        var chunkIndex = new (uint Position, int Length)[_chunks.Count];
        for (int i = 0; i < _chunks.Count; i++)
        {
            // 4-byte alignment
            // Source: GenericRCOLResource.cs line 129
            while (ms.Position % 4 != 0)
                writer.Write((byte)0);

            chunkIndex[i].Position = (uint)ms.Position;
            var data = _chunks[i].Block.Serialize();
            writer.Write(data.Span);
            chunkIndex[i].Length = data.Length;
        }

        // Patch index with actual positions
        // Source: GenericRCOLResource.cs lines 136-137
        ms.Position = indexPos;
        foreach (var entry in chunkIndex)
        {
            writer.Write(entry.Position);
            writer.Write(entry.Length);
        }

        return ms.ToArray();
    }

    /// <inheritdoc/>
    protected override void InitializeDefaults()
    {
        Version = 3; // Common version
        PublicChunksCount = 0;
        Unused = 0;
        _externalResources.Clear();
        _chunks.Clear();
        IsValid = true;
    }

    /// <summary>
    /// Creates an RCOL block from raw data.
    /// Currently returns UnknownRcolBlock; can be extended with a registry for specific types.
    /// </summary>
    private static UnknownRcolBlock CreateBlock(uint resourceType, ReadOnlySpan<byte> data)
    {
        // For now, all blocks are unknown. A future RcolBlockRegistry could
        // return specific block types based on resourceType or tag.
        return new UnknownRcolBlock(resourceType, data);
    }

    /// <summary>
    /// Tries to get a resource type from a 4-character tag.
    /// Source: GenericRCOLResourceHandler.RCOLTypesForTag (lines 877-885)
    /// </summary>
    private static uint GetTypeFromTag(string tag) => tag switch
    {
        "MODL" => RcolConstants.Modl,
        "MLOD" => RcolConstants.Mlod,
        "MATD" => RcolConstants.Matd,
        "MTST" => RcolConstants.Mtst,
        "GEOM" => RcolConstants.Geom,
        "VBUF" => RcolConstants.Vbuf,
        "IBUF" => RcolConstants.Ibuf,
        "VRTF" => RcolConstants.Vrtf,
        "SKIN" => RcolConstants.Skin,
        "TREE" => RcolConstants.Tree,
        "TkMk" => RcolConstants.TkMk,
        "LITE" => RcolConstants.Lite,
        "ANIM" => RcolConstants.Anim,
        "VPXY" => RcolConstants.Vpxy,
        "RSLT" => RcolConstants.Rslt,
        "FTPT" => RcolConstants.Ftpt,
        _ => 0
    };
}
