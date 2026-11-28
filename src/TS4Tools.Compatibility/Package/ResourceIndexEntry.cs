// Source: legacy_references/Sims4Tools/s4pi/Package/ResourceIndexEntry.cs lines 27-264

using s4pi.Interfaces;
using CoreEntry = TS4Tools.IResourceIndexEntry;
using CoreKey = TS4Tools.ResourceKey;
using MutableCoreEntry = TS4Tools.IMutableResourceIndexEntry;

namespace s4pi.Package;

/// <summary>
/// Implementation of an index entry: a legacy view of a TS4Tools.Core package index entry.
/// </summary>
/// <remarks>
/// <para>Reads and writes go straight through to the TS4Tools entry, so the TS4Tools package and this view never
/// disagree. ResourceType/ResourceGroup/Instance, <see cref="Compressed"/> and <see cref="IsDeleted"/> are writable
/// (as in legacy, each set raises <see cref="AResourceIndexEntry.ResourceIndexEntryChanged"/>).</para>
/// <para><see cref="Chunkoffset"/>, <see cref="Filesize"/>, <see cref="Memsize"/> and <see cref="Unknown2"/> are owned by
/// TS4Tools.Core (assigned when resources are added, replaced and saved); legacy only set them from inside the package
/// implementation, and setting them here throws <see cref="NotSupportedException"/>.</para>
/// </remarks>
public class ResourceIndexEntry : AResourceIndexEntry
{
    private const int recommendedApiVersion = 2;

    private static readonly PropertyInfo? Unknown2Property =
        typeof(TS4Tools.Package.DbpfPackage).Assembly.GetType("TS4Tools.Package.ResourceIndexEntry")?.GetProperty("Unknown2");

    private readonly CoreEntry core;
    private readonly uint indexType;

    internal ResourceIndexEntry(CoreEntry core, uint indexType)
    {
        this.core = core;
        this.indexType = indexType;
        DataSource = core;
    }

    private ResourceIndexEntry(DetachedEntry copy, CoreEntry dataSource, uint indexType)
    {
        core = copy;
        this.indexType = indexType;
        DataSource = dataSource;
    }

    /// <summary>The TS4Tools.Core entry this view wraps (a private copy for an entry made by <see cref="Clone"/>).</summary>
    internal CoreEntry Core => core;

    /// <summary>True for an entry made by <see cref="Clone"/>: a copy that is not part of any package index.</summary>
    internal bool IsDetached => core is DetachedEntry;

    /// <summary>
    /// The package entry whose data this entry reads: the wrapped entry, or for a clone the entry it was copied from
    /// (legacy clones keep the chunk offset, so reading through them still reads the original data).
    /// </summary>
    internal CoreEntry DataSource { get; } = null!;

    /// <summary>
    /// True for an entry added with a null stream (legacy <c>AddResource(rk, null, ...)</c>) whose data has not been
    /// replaced; <see cref="Package.GetResource"/> returns null for it, as legacy does.
    /// </summary>
    internal bool HasNoData { get; set; }

    #region AApiVersionedFields
    /// <summary>The best supported version of the API available.</summary>
    public override int RecommendedApiVersion => recommendedApiVersion;
    #endregion

    private MutableCoreEntry Mutable => core as MutableCoreEntry
        ?? throw new NotSupportedException("The underlying TS4Tools index entry is read-only.");

    #region AResourceIndexEntry
    /// <summary>The "type" of the resource.</summary>
    [MinimumVersion(1)]
    [MaximumVersion(recommendedApiVersion)]
    public override uint ResourceType
    {
        get => core.Key.ResourceType;
        set
        {
            Mutable.Key = core.Key with { ResourceType = value };
            OnElementChanged();
        }
    }

    /// <summary>The "group" the resource is part of.</summary>
    [MinimumVersion(1)]
    [MaximumVersion(recommendedApiVersion)]
    public override uint ResourceGroup
    {
        get => core.Key.ResourceGroup;
        set
        {
            Mutable.Key = core.Key with { ResourceGroup = value };
            OnElementChanged();
        }
    }

    /// <summary>The "instance" number of the resource.</summary>
    [MinimumVersion(1)]
    [MaximumVersion(recommendedApiVersion)]
    public override ulong Instance
    {
        get => core.Key.Instance;
        set
        {
            Mutable.Key = core.Key with { Instance = value };
            OnElementChanged();
        }
    }

    /// <summary>If the resource was read from a package, the location in the package the resource was read from (0xFFFFFFFF if not yet written).</summary>
    /// <exception cref="NotSupportedException">On set: offsets are assigned by TS4Tools.Core when saving.</exception>
    [MinimumVersion(1)]
    [MaximumVersion(recommendedApiVersion)]
    public override uint Chunkoffset
    {
        get => core.ChunkOffset;
        set => throw new NotSupportedException("Chunkoffset is assigned by the package when saving.");
    }

    /// <summary>The number of bytes the resource uses within the package.</summary>
    /// <exception cref="NotSupportedException">On set: sizes are assigned by TS4Tools.Core when saving.</exception>
    [MinimumVersion(1)]
    [MaximumVersion(recommendedApiVersion)]
    public override uint Filesize
    {
        get => core.FileSize & 0x7fffffff;
        set => throw new NotSupportedException("Filesize is assigned by the package when saving.");
    }

    /// <summary>The number of bytes the resource uses in memory.</summary>
    /// <exception cref="NotSupportedException">On set: use <see cref="Package.ReplaceResource"/> to change the data.</exception>
    [MinimumVersion(1)]
    [MaximumVersion(recommendedApiVersion)]
    public override uint Memsize
    {
        get => core.MemorySize;
        set => throw new NotSupportedException("Memsize is assigned by the package from the resource data.");
    }

    /// <summary>
    /// The compression flag: 0x0000 stores the resource uncompressed; non-zero (e.g. 0x5A42, 0xFFFF) compresses on save.
    /// </summary>
    [MinimumVersion(1)]
    [MaximumVersion(recommendedApiVersion)]
    public override ushort Compressed
    {
        get => core.CompressionType;
        set
        {
            Mutable.CompressionType = value;
            OnElementChanged();
        }
    }

    /// <summary>Always 0x0001.</summary>
    /// <exception cref="NotSupportedException">On set: the field is preserved by TS4Tools.Core.</exception>
    [MinimumVersion(1)]
    [MaximumVersion(recommendedApiVersion)]
    public override ushort Unknown2
    {
        get => core is DetachedEntry d ? d.Unknown2 : Unknown2Property?.GetValue(core) is ushort u ? u : (ushort)1;
        set => throw new NotSupportedException("Unknown2 is preserved by the package.");
    }

    /// <summary>
    /// A <see cref="MemoryStream"/> covering the 36-byte index entry: index type, Type, Group, Instance (high, low),
    /// Chunkoffset, Filesize | 0x80000000, Memsize, Compressed, Unknown2.
    /// </summary>
    /// <remarks>Source: ResourceIndexEntry.cs lines 137, 205-225 (layout of the legacy indexEntry buffer)</remarks>
    [MinimumVersion(1)]
    [MaximumVersion(recommendedApiVersion)]
    public override Stream Stream => new MemoryStream(EntryBytes());

    private byte[] EntryBytes()
    {
        var b = new byte[36];
        var span = b.AsSpan();
        BinaryPrimitives.WriteUInt32LittleEndian(span, indexType);
        BinaryPrimitives.WriteUInt32LittleEndian(span[4..], ResourceType);
        BinaryPrimitives.WriteUInt32LittleEndian(span[8..], ResourceGroup);
        BinaryPrimitives.WriteUInt32LittleEndian(span[12..], (uint)(Instance >> 32));
        BinaryPrimitives.WriteUInt32LittleEndian(span[16..], (uint)(Instance & 0xffffffff));
        BinaryPrimitives.WriteUInt32LittleEndian(span[20..], Chunkoffset);
        BinaryPrimitives.WriteUInt32LittleEndian(span[24..], Filesize | 0x80000000);
        BinaryPrimitives.WriteUInt32LittleEndian(span[28..], Memsize);
        BinaryPrimitives.WriteUInt16LittleEndian(span[32..], Compressed);
        BinaryPrimitives.WriteUInt16LittleEndian(span[34..], Unknown2);
        return b;
    }

    /// <summary>True if the index entry has been deleted from the package index.</summary>
    [MinimumVersion(1)]
    [MaximumVersion(recommendedApiVersion)]
    public override bool IsDeleted
    {
        get => core.IsDeleted;
        set
        {
            if (core.IsDeleted != value)
            {
                Mutable.IsDeleted = value;
                OnElementChanged();
            }
        }
    }

    /// <summary>
    /// Returns an independent copy of this index entry.
    /// </summary>
    /// <param name="handler">Ignored (index entries report changes through <see cref="AResourceIndexEntry.ResourceIndexEntryChanged"/>).</param>
    /// <returns>A new <see cref="ResourceIndexEntry"/> holding a copy of the entry's values.</returns>
    /// <remarks>
    /// Source: ResourceIndexEntry.cs lines 153, 198 (legacy copies its private 32-byte buffer). Changing the copy's
    /// key, compression or deleted flag does not change the package; the copy is not in the package index, so
    /// <see cref="Package.ReplaceResource"/> and <see cref="Package.DeleteResource"/> reject it, while
    /// <see cref="Package.GetResource"/> still reads the original entry's data.
    /// </remarks>
    [MinimumVersion(1)]
    [MaximumVersion(recommendedApiVersion)]
    public override AHandlerElement Clone(EventHandler? handler) =>
        new ResourceIndexEntry(new DetachedEntry(core, Unknown2), DataSource, indexType) { HasNoData = HasNoData };

    /// <summary>A snapshot of an index entry's values that belongs to no package.</summary>
    private sealed class DetachedEntry(CoreEntry source, ushort unknown2) : MutableCoreEntry
    {
        public TS4Tools.ResourceKey Key { get; set; } = source.Key;
        public uint ChunkOffset { get; } = source.ChunkOffset;
        public uint FileSize { get; } = source.FileSize;
        public uint MemorySize { get; } = source.MemorySize;
        public ushort CompressionType { get; set; } = source.CompressionType;
        public bool IsCompressed { get; } = source.IsCompressed;
        public bool IsDeleted { get; set; } = source.IsDeleted;
        public ushort Unknown2 { get; } = unknown2;
    }
    #endregion

    #region IEquatable<IResourceIndexEntry>
    /// <summary>
    /// Indicates whether the current instance has the same index entry values as <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The other entry.</param>
    /// <returns>true if <paramref name="other"/> is a <see cref="ResourceIndexEntry"/> with identical index entry bytes.</returns>
    /// <remarks>Source: ResourceIndexEntry.cs lines 162-165</remarks>
    public override bool Equals(IResourceIndexEntry? other)
        => other is ResourceIndexEntry rie && EntryBytes().AsSpan().SequenceEqual(rie.EntryBytes());

    /// <summary>
    /// Returns the hash code for this instance (consistent with <see cref="Equals(IResourceIndexEntry)"/>).
    /// </summary>
    /// <returns>A hash of the Type, Group and Instance.</returns>
    public override int GetHashCode() => HashCode.Combine(ResourceType, ResourceGroup, Instance);
    #endregion

    /// <summary>The key of the wrapped entry.</summary>
    internal CoreKey Key => core.Key;
}
