using System.Buffers.Binary;
using System.Text;
using TS4Tools.Resources;

namespace TS4Tools.Wrappers;

/// <summary>
/// Object Key resource containing components and component data.
/// Resource Type: 0x02DC343F
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/ObjKeyResource/ObjKeyResource.cs
/// </summary>
[ResourceHandler(0x02DC343F)]
public sealed class ObjKeyResource : TypedResource
{
    /// <summary>
    /// Default format version.
    /// </summary>
    public const uint DefaultFormat = 7;

    private readonly List<ObjKeyComponent> _components = [];
    private readonly List<ComponentData> _componentData = [];
    private readonly List<ResourceKey> _tgiBlocks = [];

    /// <summary>
    /// Format version.
    /// </summary>
    public uint Format { get; set; } = DefaultFormat;

    /// <summary>
    /// Unknown byte field.
    /// </summary>
    public byte Unknown1 { get; set; }

    /// <summary>
    /// The components list.
    /// </summary>
    public IReadOnlyList<ObjKeyComponent> Components => _components;

    /// <summary>
    /// The component data list.
    /// </summary>
    public IReadOnlyList<ComponentData> ComponentData => _componentData;

    /// <summary>
    /// The TGI block list.
    /// </summary>
    public IReadOnlyList<ResourceKey> TgiBlocks => _tgiBlocks;

    /// <summary>
    /// Creates a new ObjKeyResource by parsing data.
    /// </summary>
    public ObjKeyResource(ResourceKey key, ReadOnlyMemory<byte> data) : base(key, data)
    {
    }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        // Minimum: format(4) + tgiOffset(4) + tgiSize(4) + componentCount(1) + componentDataCount(1) + unknown1(1) = 15
        if (data.Length < 15)
            throw new ResourceFormatException("ObjKey resource data too short for header.");

        int offset = 0;

        // Read format
        Format = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Read TGI offset and size
        uint tgiOffset = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;
        long tgiPosn = offset + tgiOffset;

        uint tgiSize = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Read components
        int componentCount = data[offset++];
        if (componentCount < 0 || componentCount > byte.MaxValue)
            throw new ResourceFormatException($"Invalid component count: {componentCount}");

        _components.Clear();
        _components.EnsureCapacity(componentCount);
        for (int i = 0; i < componentCount; i++)
        {
            if (offset + 4 > data.Length)
                throw new ResourceFormatException("Unexpected end of data reading components.");
            uint componentValue = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;
            _components.Add((ObjKeyComponent)componentValue);
        }

        // Read component data
        int componentDataCount = data[offset++];
        if (componentDataCount < 0 || componentDataCount > byte.MaxValue)
            throw new ResourceFormatException($"Invalid component data count: {componentDataCount}");

        _componentData.Clear();
        _componentData.EnsureCapacity(componentDataCount);
        for (int i = 0; i < componentDataCount; i++)
        {
            var (compData, bytesRead) = ParseComponentData(data[offset..]);
            if (compData == null)
                throw new ResourceFormatException($"Failed to parse component data at index {i}.");
            _componentData.Add(compData);
            offset += bytesRead;
        }

        // Read unknown1
        if (offset >= data.Length)
            throw new ResourceFormatException("Unexpected end of data reading unknown1.");
        Unknown1 = data[offset++];

        // Read TGI blocks
        _tgiBlocks.Clear();
        if (tgiSize > 0 && tgiPosn < data.Length)
        {
            int tgiBlockCount = (int)(tgiSize / 16);
            _tgiBlocks.EnsureCapacity(tgiBlockCount);

            int tgiReadOffset = (int)tgiPosn;
            for (int i = 0; i < tgiBlockCount && tgiReadOffset + 16 <= data.Length; i++)
            {
                uint type = BinaryPrimitives.ReadUInt32LittleEndian(data[tgiReadOffset..]);
                tgiReadOffset += 4;
                uint group = BinaryPrimitives.ReadUInt32LittleEndian(data[tgiReadOffset..]);
                tgiReadOffset += 4;
                ulong instance = BinaryPrimitives.ReadUInt64LittleEndian(data[tgiReadOffset..]);
                tgiReadOffset += 8;
                _tgiBlocks.Add(new ResourceKey(type, group, instance));
            }
        }
    }

    private static (ComponentData? data, int bytesRead) ParseComponentData(ReadOnlySpan<byte> data)
    {
        if (data.Length < 6) // key length(4) + at least 1 char + controlCode(1)
            return (null, 0);

        int offset = 0;

        // Read key string
        int keyLength = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
        offset += 4;

        if (keyLength < 0 || offset + keyLength > data.Length)
            return (null, 0);

        string key = Encoding.ASCII.GetString(data.Slice(offset, keyLength));
        offset += keyLength;

        if (offset >= data.Length)
            return (null, 0);

        // Read control code
        byte controlCode = data[offset++];

        ComponentData result;
        switch (controlCode)
        {
            case 0x00: // CDTString
            case 0x03: // CDTSteeringInstance (same format as string)
            {
                if (offset + 4 > data.Length)
                    return (null, 0);
                int strLength = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
                offset += 4;

                if (strLength < 0 || offset + strLength > data.Length)
                    return (null, 0);

                string strValue = Encoding.ASCII.GetString(data.Slice(offset, strLength));
                offset += strLength;

                result = controlCode == 0x00
                    ? new ComponentDataString(key, strValue)
                    : new ComponentDataSteeringInstance(key, strValue);
                break;
            }

            case 0x01: // CDTResourceKey
            case 0x02: // CDTAssetResourceName (same format as resource key)
            {
                if (offset + 4 > data.Length)
                    return (null, 0);
                int tgiIndex = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
                offset += 4;

                result = controlCode == 0x01
                    ? new ComponentDataResourceKey(key, tgiIndex)
                    : new ComponentDataAssetResourceName(key, tgiIndex);
                break;
            }

            case 0x04: // CDTUInt32
            {
                if (offset + 4 > data.Length)
                    return (null, 0);
                uint uintValue = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
                offset += 4;

                result = new ComponentDataUInt32(key, uintValue);
                break;
            }

            default:
                throw new ResourceFormatException($"Unknown component data control code: 0x{controlCode:X2}");
        }

        return (result, offset);
    }

    /// <inheritdoc/>
    protected override ReadOnlyMemory<byte> Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.ASCII, leaveOpen: true);

        // Write format
        writer.Write(Format);

        // Placeholder for tgiOffset and tgiSize
        long offsetPos = ms.Position;
        writer.Write(0u); // tgiOffset
        writer.Write(0u); // tgiSize

        // Write components
        writer.Write((byte)_components.Count);
        foreach (var component in _components)
        {
            writer.Write((uint)component);
        }

        // Write component data
        writer.Write((byte)_componentData.Count);
        foreach (var compData in _componentData)
        {
            WriteComponentData(writer, compData);
        }

        // Write unknown1
        writer.Write(Unknown1);

        // Calculate and write TGI offset
        long tgiStart = ms.Position;
        uint tgiOffset = (uint)(tgiStart - offsetPos - 4); // Offset from after the offset field

        // Write TGI blocks
        foreach (var tgi in _tgiBlocks)
        {
            writer.Write(tgi.ResourceType);
            writer.Write(tgi.ResourceGroup);
            writer.Write(tgi.Instance);
        }

        uint tgiSize = (uint)(ms.Position - tgiStart);

        // Go back and write offset/size
        ms.Position = offsetPos;
        writer.Write(tgiOffset);
        writer.Write(tgiSize);

        return ms.ToArray();
    }

    private static void WriteComponentData(BinaryWriter writer, ComponentData compData)
    {
        // Write key
        byte[] keyBytes = Encoding.ASCII.GetBytes(compData.Key);
        writer.Write(keyBytes.Length);
        writer.Write(keyBytes);

        // Write control code and data
        switch (compData)
        {
            case ComponentDataString str:
                writer.Write((byte)0x00);
                byte[] strBytes = Encoding.ASCII.GetBytes(str.Data);
                writer.Write(strBytes.Length);
                writer.Write(strBytes);
                break;

            case ComponentDataSteeringInstance steering:
                writer.Write((byte)0x03);
                byte[] steerBytes = Encoding.ASCII.GetBytes(steering.Data);
                writer.Write(steerBytes.Length);
                writer.Write(steerBytes);
                break;

            case ComponentDataAssetResourceName asset:
                writer.Write((byte)0x02);
                writer.Write(asset.TgiIndex);
                break;

            case ComponentDataResourceKey resKey:
                writer.Write((byte)0x01);
                writer.Write(resKey.TgiIndex);
                break;

            case ComponentDataUInt32 uint32:
                writer.Write((byte)0x04);
                writer.Write(uint32.Data);
                break;
        }
    }

    /// <inheritdoc/>
    protected override void InitializeDefaults()
    {
        Format = DefaultFormat;
        Unknown1 = 0;
        _components.Clear();
        _componentData.Clear();
        _tgiBlocks.Clear();
    }

    /// <summary>
    /// Adds a component.
    /// </summary>
    public void AddComponent(ObjKeyComponent component)
    {
        _components.Add(component);
        OnChanged();
    }

    /// <summary>
    /// Adds component data.
    /// </summary>
    public void AddComponentData(ComponentData data)
    {
        _componentData.Add(data);
        OnChanged();
    }

    /// <summary>
    /// Adds a TGI block.
    /// </summary>
    public void AddTgiBlock(ResourceKey block)
    {
        _tgiBlocks.Add(block);
        OnChanged();
    }

    /// <summary>
    /// Checks if a specific component exists.
    /// </summary>
    public bool HasComponent(ObjKeyComponent component) => _components.Contains(component);

    /// <summary>
    /// Gets component data by key.
    /// </summary>
    public ComponentData? GetComponentData(string key) => _componentData.FirstOrDefault(cd => cd.Key == key);

    /// <summary>
    /// Gets the TGI block referenced by component data.
    /// </summary>
    public ResourceKey? GetReferencedTgiBlock(ComponentData data)
    {
        if (data is ComponentDataResourceKey resKey)
        {
            if (resKey.TgiIndex >= 0 && resKey.TgiIndex < _tgiBlocks.Count)
                return _tgiBlocks[resKey.TgiIndex];
        }
        else if (data is ComponentDataAssetResourceName asset)
        {
            if (asset.TgiIndex >= 0 && asset.TgiIndex < _tgiBlocks.Count)
                return _tgiBlocks[asset.TgiIndex];
        }
        return null;
    }

    /// <summary>
    /// Clears all components.
    /// </summary>
    public void ClearComponents()
    {
        _components.Clear();
        OnChanged();
    }

    /// <summary>
    /// Clears all component data.
    /// </summary>
    public void ClearComponentData()
    {
        _componentData.Clear();
        OnChanged();
    }

    /// <summary>
    /// Clears all TGI blocks.
    /// </summary>
    public void ClearTgiBlocks()
    {
        _tgiBlocks.Clear();
        OnChanged();
    }
}

/// <summary>
/// Known object key component types.
/// </summary>
public enum ObjKeyComponent : uint
{
    Animation = 0xee17c6ad,
    Effect = 0x80d91e9e,
    Footprint = 0xc807312a,
    Lighting = 0xda6c50fd,
    Location = 0x461922c8,
    LotObject = 0x6693c8b3,
    Model = 0x2954e734,
    Physics = 0x1a8feb14,
    Sacs = 0x3ae9a8e7,
    Script = 0x23177498,
    Sim = 0x22706efa,
    Slot = 0x2ef1e401,
    Steering = 0x61bd317c,
    Transform = 0x54cb7ebb,
    Tree = 0xc602cd31,
    VisualState = 0x50b3d17c,
}

/// <summary>
/// Base class for component data entries.
/// </summary>
public abstract record ComponentData(string Key);

/// <summary>
/// Component data containing a string value.
/// </summary>
public sealed record ComponentDataString(string Key, string Data) : ComponentData(Key);

/// <summary>
/// Component data containing a steering instance string.
/// </summary>
public sealed record ComponentDataSteeringInstance(string Key, string Data) : ComponentData(Key);

/// <summary>
/// Component data containing a TGI block index.
/// </summary>
public sealed record ComponentDataResourceKey(string Key, int TgiIndex) : ComponentData(Key);

/// <summary>
/// Component data containing an asset resource TGI block index.
/// </summary>
public sealed record ComponentDataAssetResourceName(string Key, int TgiIndex) : ComponentData(Key);

/// <summary>
/// Component data containing a UInt32 value.
/// </summary>
public sealed record ComponentDataUInt32(string Key, uint Data) : ComponentData(Key);
