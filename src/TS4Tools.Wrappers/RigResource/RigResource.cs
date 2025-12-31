// Source: legacy_references/Sims4Tools/s4pi Wrappers/RigResource/RigResource.cs

using TS4Tools.Resources;

namespace TS4Tools.Wrappers;

/// <summary>
/// RIG resource containing skeleton bone hierarchies and IK chains.
/// Resource Type: 0x8EAF13DE
/// </summary>
/// <remarks>
/// Source: s4pi Wrappers/RigResource/RigResource.cs
/// Supports three formats:
/// - RawGranny: Passthrough for Granny2 binary data
/// - WrappedGranny: Treated as RawGranny (detected by magic 0x8EAF13DE)
/// - Clear: Native parsed format with bones and IK chains
/// </remarks>
public sealed class RigResource : TypedResource
{
    /// <summary>Resource type identifier.</summary>
    public const uint TypeId = 0x8EAF13DE;

    private byte[] _rawGrannyData = [];

    /// <summary>
    /// The format of this rig resource.
    /// </summary>
    public RigFormat Format { get; private set; } = RigFormat.Clear;

    /// <summary>
    /// Major version (typically 3 or 4 for Sims 4).
    /// Only valid for Clear format.
    /// </summary>
    public uint Major { get; set; } = 4;

    /// <summary>
    /// Minor version (typically 1 or 2).
    /// Only valid for Clear format.
    /// </summary>
    public uint Minor { get; set; } = 2;

    /// <summary>
    /// List of bones in this skeleton.
    /// Only valid for Clear format.
    /// </summary>
    public List<RigBone> Bones { get; } = [];

    /// <summary>
    /// Skeleton name (only present in version 4+ for non-TS4 or version &lt; 4 for TS4).
    /// </summary>
    public string? SkeletonName { get; set; }

    /// <summary>
    /// List of IK chains (only present in version 4+ or TS4).
    /// </summary>
    public List<IkChain> IkChains { get; } = [];

    /// <summary>
    /// Raw Granny2 data for RawGranny/WrappedGranny formats.
    /// </summary>
    public ReadOnlyMemory<byte> RawGrannyData => _rawGrannyData;

    /// <summary>
    /// Creates a new RIG resource by parsing data.
    /// </summary>
    public RigResource(ResourceKey key, ReadOnlyMemory<byte> data) : base(key, data)
    {
    }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < 8)
        {
            // Too short to determine format, treat as raw
            Format = RigFormat.RawGranny;
            _rawGrannyData = data.ToArray();
            return;
        }

        // Detect format based on first two DWORDs
        // Source: RigResource.cs lines 51-73
        uint dw1 = BinaryPrimitives.ReadUInt32LittleEndian(data);
        uint dw2 = BinaryPrimitives.ReadUInt32LittleEndian(data[4..]);

        if (dw1 == TypeId && dw2 == 0x00000000)
        {
            // WrappedGranny format - treat as raw
            Format = RigFormat.WrappedGranny;
            ParseRawGranny(data);
        }
        else if ((dw1 == 3 || dw1 == 4) && (dw2 == 1 || dw2 == 2))
        {
            // Clear format - native parsed
            Format = RigFormat.Clear;
            ParseClear(data);
        }
        else
        {
            // RawGranny format - passthrough
            Format = RigFormat.RawGranny;
            ParseRawGranny(data);
        }
    }

    /// <summary>
    /// Parses raw Granny2 data (passthrough).
    /// Source: RigResource.cs lines 76-80
    /// </summary>
    private void ParseRawGranny(ReadOnlySpan<byte> data)
    {
        _rawGrannyData = data.ToArray();
    }

    /// <summary>
    /// Parses Clear format with bones and IK chains.
    /// Source: RigResource.cs lines 87-97
    /// </summary>
    private void ParseClear(ReadOnlySpan<byte> data)
    {
        int position = 0;

        // Read version
        Major = BinaryPrimitives.ReadUInt32LittleEndian(data[position..]);
        position += 4;

        Minor = BinaryPrimitives.ReadUInt32LittleEndian(data[position..]);
        position += 4;

        // Read bone list (count-prefixed)
        int boneCount = BinaryPrimitives.ReadInt32LittleEndian(data[position..]);
        position += 4;

        if (boneCount < 0 || boneCount > 10000)
            throw new InvalidDataException($"Invalid bone count: {boneCount}");

        Bones.Clear();
        for (int i = 0; i < boneCount; i++)
        {
            Bones.Add(RigBone.Read(data, ref position));
        }

        // Read skeleton name (version 4+ in legacy)
        // Note: Legacy has isTS4 flag affecting this, we assume TS4
        if (Major >= 4)
        {
            int nameLength = BinaryPrimitives.ReadInt32LittleEndian(data[position..]);
            position += 4;

            if (nameLength < 0 || nameLength > 1024)
                throw new InvalidDataException($"Invalid skeleton name length: {nameLength}");

            if (nameLength > 0)
            {
                SkeletonName = Encoding.UTF8.GetString(data.Slice(position, nameLength));
                position += nameLength;
            }
        }

        // Read IK chains (version 4+ or TS4)
        // Note: Legacy has version/isTS4 logic, we read for version 4+
        if (Major >= 4 && position < data.Length)
        {
            int ikCount = BinaryPrimitives.ReadInt32LittleEndian(data[position..]);
            position += 4;

            if (ikCount < 0 || ikCount > 1000)
                throw new InvalidDataException($"Invalid IK chain count: {ikCount}");

            IkChains.Clear();
            for (int i = 0; i < ikCount; i++)
            {
                IkChains.Add(IkChain.Read(data, ref position, Major));
            }
        }
    }

    /// <inheritdoc/>
    protected override ReadOnlyMemory<byte> Serialize()
    {
        return Format switch
        {
            RigFormat.RawGranny => _rawGrannyData,
            RigFormat.WrappedGranny => _rawGrannyData,
            RigFormat.Clear => SerializeClear(),
            _ => throw new InvalidOperationException($"Unknown RIG format: {Format}")
        };
    }

    /// <summary>
    /// Serializes Clear format.
    /// Source: RigResource.cs lines 128-152
    /// </summary>
    private byte[] SerializeClear()
    {
        // Calculate total size
        int size = 8; // Major + Minor
        size += 4; // Bone count
        foreach (var bone in Bones)
        {
            size += bone.GetSerializedSize();
        }

        // Skeleton name (version 4+)
        int skeletonNameBytes = 0;
        if (Major >= 4)
        {
            size += 4; // Name length
            skeletonNameBytes = Encoding.UTF8.GetByteCount(SkeletonName ?? "");
            size += skeletonNameBytes;
        }

        // IK chains (version 4+)
        if (Major >= 4)
        {
            size += 4; // IK chain count
            foreach (var chain in IkChains)
            {
                size += chain.GetSerializedSize(Major);
            }
        }

        byte[] buffer = new byte[size];
        int position = 0;

        // Write version
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(position), Major);
        position += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(position), Minor);
        position += 4;

        // Write bones
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(position), Bones.Count);
        position += 4;

        foreach (var bone in Bones)
        {
            bone.Write(buffer.AsSpan(), ref position);
        }

        // Write skeleton name (version 4+)
        if (Major >= 4)
        {
            byte[] nameBytes = Encoding.UTF8.GetBytes(SkeletonName ?? "");
            BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(position), nameBytes.Length);
            position += 4;

            if (nameBytes.Length > 0)
            {
                nameBytes.CopyTo(buffer.AsSpan(position));
                position += nameBytes.Length;
            }
        }

        // Write IK chains (version 4+)
        if (Major >= 4)
        {
            BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(position), IkChains.Count);
            position += 4;

            foreach (var chain in IkChains)
            {
                chain.Write(buffer.AsSpan(), ref position, Major);
            }
        }

        return buffer;
    }

    /// <inheritdoc/>
    protected override void InitializeDefaults()
    {
        Format = RigFormat.Clear;
        Major = 4;
        Minor = 2;
        Bones.Clear();
        IkChains.Clear();
        SkeletonName = null;
        _rawGrannyData = [];
    }

    /// <summary>
    /// Adds a bone to the skeleton.
    /// Only valid for Clear format.
    /// </summary>
    public void AddBone(RigBone bone)
    {
        if (Format != RigFormat.Clear)
            throw new InvalidOperationException("Cannot modify bones in RawGranny format");

        Bones.Add(bone);
        OnChanged();
    }

    /// <summary>
    /// Adds an IK chain to the skeleton.
    /// Only valid for Clear format with version 4+.
    /// </summary>
    public void AddIkChain(IkChain chain)
    {
        if (Format != RigFormat.Clear)
            throw new InvalidOperationException("Cannot modify IK chains in RawGranny format");
        if (Major < 4)
            throw new InvalidOperationException("IK chains require version 4+");

        IkChains.Add(chain);
        OnChanged();
    }

    /// <summary>
    /// Sets raw Granny2 data (changes format to RawGranny).
    /// </summary>
    public void SetRawGrannyData(ReadOnlySpan<byte> data)
    {
        Format = RigFormat.RawGranny;
        _rawGrannyData = data.ToArray();
        Bones.Clear();
        IkChains.Clear();
        SkeletonName = null;
        OnChanged();
    }

    /// <summary>
    /// Finds a bone by name.
    /// </summary>
    public RigBone? FindBone(string name) =>
        Bones.FirstOrDefault(b => b.Name == name);

    /// <summary>
    /// Finds a bone by hash.
    /// </summary>
    public RigBone? FindBone(uint hash) =>
        Bones.FirstOrDefault(b => b.Hash == hash);
}
