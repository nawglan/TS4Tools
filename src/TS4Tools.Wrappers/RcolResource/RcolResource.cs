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
    private uint _version;
    private int _publicChunksCount;
    private uint _unused;
    private bool _structureEdited;

    /// <summary>
    /// The RCOL version.
    /// Source: GenericRCOLResource.cs line 42
    /// </summary>
    public uint Version
    {
        get => _version;
        set
        {
            if (_version == value) return;
            _version = value;
            OnChanged();
        }
    }

    /// <summary>
    /// The number of "public" chunks in the resource.
    /// Public chunks are accessible externally, private chunks are internal.
    /// Source: GenericRCOLResource.cs lines 45-46
    /// </summary>
    public int PublicChunksCount
    {
        get => _publicChunksCount;
        set
        {
            if (_publicChunksCount == value) return;
            _publicChunksCount = value;
            OnChanged();
        }
    }

    /// <summary>
    /// Unknown/unused field, preserved for round-trip.
    /// Source: GenericRCOLResource.cs lines 48-50
    /// </summary>
    public uint Unused
    {
        get => _unused;
        set
        {
            if (_unused == value) return;
            _unused = value;
            OnChanged();
        }
    }

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
            _version = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
            pos += 4;

            _publicChunksCount = BinaryPrimitives.ReadInt32LittleEndian(data[pos..]);
            pos += 4;

            _unused = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
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

            // Single chunk: legacy ignores the stored index entry and always takes everything after the
            // index as the chunk (GenericRCOLResource.cs lines 86-89: Position = 0x2c + countResources * 16,
            // Length = s.Length - Position). Real mod files exist whose stored length is short of the data.
            if (chunkCount == 1)
            {
                index[0].Position = (uint)pos; // == 0x2C + resourceCount * 16
                index[0].Length = data.Length - pos;
            }

            // Legacy infers the type of a single chunk with a zero type from the tag in its data
            // (GenericRCOLResource.cs lines 90-94), whatever the index said about its position.
            if (chunkCount == 1 && chunkTgis[0].ResourceType == 0
                && index[0].Length >= 4 && index[0].Position <= (uint)(data.Length - 4))
            {
                string tag = RcolBlock.ExtractTag(data[(int)index[0].Position..]);
                uint inferredType = GetTypeFromTag(tag);
                if (inferredType != 0)
                {
                    chunkTgis[0] = new RcolTgiBlock(
                        chunkTgis[0].Instance,
                        inferredType,
                        chunkTgis[0].ResourceGroup);
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
        // Raw data is returned when nothing was parsed, or when a parsed resource with no chunks has not been
        // edited (keeps the original bytes of an empty RCOL intact).
        if (!IsValid || (_chunks.Count == 0 && !_structureEdited))
            return _rawData;

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

            // Keep the entries' recorded location in step with the written layout
            _chunks[i].Position = chunkIndex[i].Position;
            _chunks[i].Length = chunkIndex[i].Length;
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

    #region Mutation API

    /// <summary>
    /// Marks the resource as modified. Call after changing a chunk block in place
    /// (legacy blocks raise OnRCOLChanged for the same purpose).
    /// </summary>
    public void MarkChanged() => OnChanged();

    /// <summary>
    /// Appends a chunk. The chunk is written after the existing ones; offsets and lengths are recomputed
    /// on serialization. Chunk references inside other blocks are not rewritten (legacy does not either).
    /// Source: GenericRCOLResource.cs ChunkEntryList.Add (lines 245-248), Resources/ChunkEntries (lines 676-704)
    /// </summary>
    /// <returns>The index of the new chunk.</returns>
    public int AddChunk(RcolTgiBlock tgi, RcolBlock block)
    {
        InsertChunk(_chunks.Count, tgi, block);
        return _chunks.Count - 1;
    }

    /// <summary>
    /// Inserts a chunk at the given index.
    /// </summary>
    public void InsertChunk(int index, RcolTgiBlock tgi, RcolBlock block)
    {
        EnsureEditable();
        ArgumentNullException.ThrowIfNull(block);
        if (index < 0 || index > _chunks.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        if (_chunks.Count >= MaxChunkCount)
            throw new InvalidOperationException($"An RCOL resource can hold at most {MaxChunkCount} chunks");

        _chunks.Insert(index, new RcolChunkEntry(tgi, block));
        MarkEdited();
    }

    /// <summary>
    /// Removes the chunk at the given index.
    /// </summary>
    public void RemoveChunkAt(int index)
    {
        EnsureEditable();
        if (index < 0 || index >= _chunks.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        _chunks.RemoveAt(index);
        MarkEdited();
    }

    /// <summary>
    /// Removes the given chunk entry.
    /// </summary>
    /// <returns>True if the entry was part of this resource.</returns>
    public bool RemoveChunk(RcolChunkEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        int index = _chunks.IndexOf(entry);
        if (index < 0)
            return false;
        RemoveChunkAt(index);
        return true;
    }

    /// <summary>
    /// Replaces the chunk at the given index with a new TGI and block.
    /// Source: GenericRCOLResource.cs ChunkEntry.TGIBlock/RCOLBlock setters (lines 214-215)
    /// </summary>
    public void ReplaceChunk(int index, RcolTgiBlock tgi, RcolBlock block)
    {
        EnsureEditable();
        ArgumentNullException.ThrowIfNull(block);
        if (index < 0 || index >= _chunks.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        _chunks[index] = new RcolChunkEntry(tgi, block);
        MarkEdited();
    }

    /// <summary>
    /// Appends an external resource reference.
    /// Source: GenericRCOLResource.cs Resources property (lines 664-680)
    /// </summary>
    /// <returns>The index of the new reference.</returns>
    public int AddExternalResource(RcolTgiBlock tgi)
    {
        InsertExternalResource(_externalResources.Count, tgi);
        return _externalResources.Count - 1;
    }

    /// <summary>
    /// Inserts an external resource reference at the given index. Delayed chunk references that point at
    /// later entries are not rewritten.
    /// </summary>
    public void InsertExternalResource(int index, RcolTgiBlock tgi)
    {
        EnsureEditable();
        if (index < 0 || index > _externalResources.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        if (_externalResources.Count >= MaxExternalCount)
            throw new InvalidOperationException($"An RCOL resource can reference at most {MaxExternalCount} external resources");

        _externalResources.Insert(index, tgi);
        MarkEdited();
    }

    /// <summary>
    /// Removes the external resource reference at the given index.
    /// </summary>
    public void RemoveExternalResourceAt(int index)
    {
        EnsureEditable();
        if (index < 0 || index >= _externalResources.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        _externalResources.RemoveAt(index);
        MarkEdited();
    }

    /// <summary>
    /// Replaces the external resource reference at the given index.
    /// </summary>
    public void ReplaceExternalResource(int index, RcolTgiBlock tgi)
    {
        EnsureEditable();
        if (index < 0 || index >= _externalResources.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        _externalResources[index] = tgi;
        MarkEdited();
    }

    private void EnsureEditable()
    {
        if (!IsValid)
            throw new InvalidOperationException("The RCOL data could not be parsed and cannot be edited");
    }

    private void MarkEdited()
    {
        _structureEdited = true;
        OnChanged();
    }

    /// <summary>
    /// Sets the block of a chunk without marking the resource dirty. Used by wrappers (GEOM) that
    /// post-process freshly parsed data, as legacy GeometryResource does in its constructor.
    /// </summary>
    internal void SetChunkSilently(int index, RcolTgiBlock tgi, RcolBlock block)
    {
        _chunks[index] = new RcolChunkEntry(tgi, block, _chunks[index].Position, _chunks[index].Length);
    }

    /// <summary>
    /// Adds a chunk without marking the resource dirty, for wrappers that build a default resource.
    /// </summary>
    internal void AddChunkSilently(RcolTgiBlock tgi, RcolBlock block)
    {
        _chunks.Add(new RcolChunkEntry(tgi, block));
        _structureEdited = true;
    }

    /// <summary>
    /// Sets the header fields without marking the resource dirty.
    /// </summary>
    internal void SetHeaderSilently(uint version, int publicChunks, uint unused)
    {
        _version = version;
        _publicChunksCount = publicChunks;
        _unused = unused;
    }

    #endregion

    /// <inheritdoc/>
    protected override void InitializeDefaults()
    {
        _version = 3; // Common version
        _publicChunksCount = 0;
        _unused = 0;
        _externalResources.Clear();
        _chunks.Clear();
        IsValid = true;
    }

    /// <summary>
    /// Creates an RCOL block from raw data using the block registry.
    /// Source: GenericRCOLResourceHandler.RCOLDealer pattern from legacy s4pi.
    /// </summary>
    private static RcolBlock CreateBlock(uint resourceType, ReadOnlySpan<byte> data)
    {
        return RcolBlockRegistry.CreateBlock(resourceType, data);
    }

    /// <summary>
    /// Tries to get a resource type from a 4-character tag.
    /// Source: GenericRCOLResourceHandler.RCOLTypesForTag (GenericRCOLResource.cs lines 877-885): the legacy
    /// lookup runs over every registered block (including Jazz, BOND/slot adjust and VBSI).
    /// </summary>
    private static uint GetTypeFromTag(string tag)
    {
        uint? registered = RcolBlockRegistry.GetTypeForTag(tag);
        if (registered.HasValue)
            return registered.Value;

        // Types listed in RCOLResources.txt that have no dedicated block class.
        return tag switch
        {
            "TREE" => RcolConstants.Tree,
            "ANIM" => RcolConstants.Anim,
            _ => 0
        };
    }
}
