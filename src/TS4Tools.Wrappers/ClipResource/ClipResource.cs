// Source: legacy_references/Sims4Tools/s4pi Wrappers/AnimationResources/ClipResource.cs

using TS4Tools.Resources;

namespace TS4Tools.Wrappers;

/// <summary>
/// Animation clip resource (CLIP).
/// Resource Type: 0x6B20C4F3
/// </summary>
/// <remarks>
/// Source: ClipResource.cs lines 27-280
/// Contains animation metadata, IK configuration, events, and S3CLIP codec data.
/// Supports versions 4-14+.
/// </remarks>
public sealed class ClipResource : TypedResource
{
    /// <summary>Resource type identifier.</summary>
    public const uint TypeId = 0x6B20C4F3;

    private byte[] _rawData = [];
    private readonly List<ClipEvent> _clipEvents = [];
    private readonly List<string> _explicitNamespaces = [];

    /// <summary>
    /// Clip format version (typically 4-14+).
    /// </summary>
    public uint Version { get; set; } = 14;

    /// <summary>
    /// Clip flags.
    /// </summary>
    public uint Flags { get; set; }

    /// <summary>
    /// Duration of the animation in seconds.
    /// </summary>
    public float Duration { get; set; }

    /// <summary>
    /// Initial rotation offset (quaternion: X, Y, Z, W).
    /// </summary>
    public (float X, float Y, float Z, float W) InitialOffsetQ { get; set; } = (0f, 0f, 0f, 1f);

    /// <summary>
    /// Initial translation offset (X, Y, Z).
    /// </summary>
    public (float X, float Y, float Z) InitialOffsetT { get; set; }

    /// <summary>
    /// Reference namespace hash (version 5+).
    /// </summary>
    public uint ReferenceNamespaceHash { get; set; }

    /// <summary>
    /// Surface namespace hash (version 10+).
    /// </summary>
    public uint SurfaceNamespaceHash { get; set; }

    /// <summary>
    /// Surface joint name hash (version 10+).
    /// </summary>
    public uint SurfaceJointNameHash { get; set; }

    /// <summary>
    /// Surface child namespace hash (version 11+).
    /// </summary>
    public uint SurfaceChildNamespaceHash { get; set; }

    /// <summary>
    /// Clip name (version 7+).
    /// </summary>
    public string ClipName { get; set; } = string.Empty;

    /// <summary>
    /// Rig namespace.
    /// </summary>
    public string RigNamespace { get; set; } = string.Empty;

    /// <summary>
    /// Explicit namespaces (version 4+).
    /// </summary>
    public IReadOnlyList<string> ExplicitNamespaces => _explicitNamespaces;

    /// <summary>
    /// IK slot assignments.
    /// </summary>
    public ClipIkConfiguration SlotAssignments { get; private set; } = new();

    /// <summary>
    /// Animation events.
    /// </summary>
    public IReadOnlyList<ClipEvent> ClipEvents => _clipEvents;

    /// <summary>
    /// S3CLIP codec data.
    /// </summary>
    public ClipCodecData CodecData { get; private set; } = new();

    /// <summary>
    /// Whether this resource was successfully parsed.
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Creates a new clip resource by parsing data.
    /// </summary>
    public ClipResource(ResourceKey key, ReadOnlyMemory<byte> data) : base(key, data)
    {
    }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        _rawData = data.ToArray();
        _clipEvents.Clear();
        _explicitNamespaces.Clear();
        IsValid = false;

        if (data.Length < 36) // Minimum: version + flags + duration + quaternion + vector3
            return;

        try
        {
            int offset = 0;

            // Version and flags
            Version = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;

            Flags = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;

            // Duration
            Duration = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
            offset += 4;

            // Initial offset quaternion (X, Y, Z, W)
            float qx = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
            float qy = BinaryPrimitives.ReadSingleLittleEndian(data[(offset + 4)..]);
            float qz = BinaryPrimitives.ReadSingleLittleEndian(data[(offset + 8)..]);
            float qw = BinaryPrimitives.ReadSingleLittleEndian(data[(offset + 12)..]);
            InitialOffsetQ = (qx, qy, qz, qw);
            offset += 16;

            // Initial offset translation (X, Y, Z)
            float tx = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
            float ty = BinaryPrimitives.ReadSingleLittleEndian(data[(offset + 4)..]);
            float tz = BinaryPrimitives.ReadSingleLittleEndian(data[(offset + 8)..]);
            InitialOffsetT = (tx, ty, tz);
            offset += 12;

            // Version-specific fields
            if (Version >= 5)
            {
                ReferenceNamespaceHash = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
                offset += 4;
            }

            if (Version >= 10)
            {
                SurfaceNamespaceHash = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
                offset += 4;

                SurfaceJointNameHash = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
                offset += 4;
            }

            if (Version >= 11)
            {
                SurfaceChildNamespaceHash = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
                offset += 4;
            }

            if (Version >= 7)
            {
                ClipName = ReadString32(data, ref offset);
            }

            // Rig namespace (always present)
            RigNamespace = ReadString32(data, ref offset);

            // Explicit namespaces (version 4+)
            if (Version >= 4)
            {
                int nsCount = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
                offset += 4;

                for (int i = 0; i < nsCount; i++)
                {
                    _explicitNamespaces.Add(ReadString32(data, ref offset));
                }
            }

            // IK configuration
            SlotAssignments = ClipIkConfiguration.Parse(data, ref offset);

            // Clip events
            int eventCount = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
            offset += 4;

            for (int i = 0; i < eventCount; i++)
            {
                var typeId = (ClipEventType)BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
                offset += 4;

                uint size = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
                offset += 4;

                var evt = ClipEvent.Create(typeId, size, data, offset);
                _clipEvents.Add(evt);

                offset += (int)size;
            }

            // Codec data length and content
            int codecDataLength = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
            offset += 4;

            if (codecDataLength > 0 && offset + codecDataLength <= data.Length)
            {
                CodecData = ClipCodecData.Parse(data[offset..], codecDataLength);
            }

            IsValid = true;
        }
        catch
        {
            IsValid = false;
        }
    }

    /// <inheritdoc/>
    protected override ReadOnlyMemory<byte> Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Version and flags
        writer.Write(Version);
        writer.Write(Flags);

        // Duration
        writer.Write(Duration);

        // Initial offset quaternion
        writer.Write(InitialOffsetQ.X);
        writer.Write(InitialOffsetQ.Y);
        writer.Write(InitialOffsetQ.Z);
        writer.Write(InitialOffsetQ.W);

        // Initial offset translation
        writer.Write(InitialOffsetT.X);
        writer.Write(InitialOffsetT.Y);
        writer.Write(InitialOffsetT.Z);

        // Version-specific fields
        if (Version >= 5)
            writer.Write(ReferenceNamespaceHash);

        if (Version >= 10)
        {
            writer.Write(SurfaceNamespaceHash);
            writer.Write(SurfaceJointNameHash);
        }

        if (Version >= 11)
            writer.Write(SurfaceChildNamespaceHash);

        if (Version >= 7)
            WriteString32(writer, ClipName);

        WriteString32(writer, RigNamespace);

        // Explicit namespaces (version 4+)
        if (Version >= 4)
        {
            writer.Write(_explicitNamespaces.Count);
            foreach (var ns in _explicitNamespaces)
            {
                WriteString32(writer, ns);
            }
        }

        // IK configuration
        SlotAssignments.Write(writer);

        // Clip events
        writer.Write(_clipEvents.Count);
        foreach (var evt in _clipEvents)
        {
            evt.Write(writer);
        }

        // Codec data
        long codecLengthPos = ms.Position;
        writer.Write(0); // Placeholder for length

        long codecStart = ms.Position;
        CodecData.Write(writer);
        long codecEnd = ms.Position;

        // Patch codec data length
        int codecLength = (int)(codecEnd - codecStart);
        ms.Position = codecLengthPos;
        writer.Write(codecLength);

        return ms.ToArray();
    }

    /// <summary>
    /// Adds a clip event.
    /// </summary>
    public void AddEvent(ClipEvent evt)
    {
        _clipEvents.Add(evt);
    }

    /// <summary>
    /// Removes all clip events.
    /// </summary>
    public void ClearEvents()
    {
        _clipEvents.Clear();
    }

    /// <summary>
    /// Adds an explicit namespace.
    /// </summary>
    public void AddExplicitNamespace(string ns)
    {
        _explicitNamespaces.Add(ns);
    }

    /// <summary>
    /// Removes all explicit namespaces.
    /// </summary>
    public void ClearExplicitNamespaces()
    {
        _explicitNamespaces.Clear();
    }

    private static string ReadString32(ReadOnlySpan<byte> data, ref int offset)
    {
        int length = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
        offset += 4;

        if (length <= 0)
            return string.Empty;

        if (offset + length > data.Length)
            throw new InvalidDataException($"String length {length} exceeds available data at offset {offset}");

        string result = Encoding.ASCII.GetString(data.Slice(offset, length));
        offset += length;
        return result;
    }

    private static void WriteString32(BinaryWriter writer, string value)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(value ?? string.Empty);
        writer.Write(bytes.Length);
        writer.Write(bytes);
    }
}
