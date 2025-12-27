using System.Buffers.Binary;

namespace TS4Tools.Wrappers;

/// <summary>
/// TGI block for RCOL resources, stored in ITG order (Instance, Type, Group).
/// Source: GenericRCOLResource.cs line 81 - TGIBlock with "ITG" order
/// </summary>
public readonly struct RcolTgiBlock : IEquatable<RcolTgiBlock>
{
    /// <summary>
    /// Size of a TGI block in bytes.
    /// </summary>
    public const int Size = 16;

    /// <summary>
    /// The instance ID.
    /// </summary>
    public ulong Instance { get; }

    /// <summary>
    /// The resource type.
    /// </summary>
    public uint ResourceType { get; }

    /// <summary>
    /// The resource group.
    /// </summary>
    public uint ResourceGroup { get; }

    /// <summary>
    /// Creates a new TGI block.
    /// </summary>
    public RcolTgiBlock(ulong instance, uint resourceType, uint resourceGroup)
    {
        Instance = instance;
        ResourceType = resourceType;
        ResourceGroup = resourceGroup;
    }

    /// <summary>
    /// Reads a TGI block from a span in ITG order.
    /// Source: GenericRCOLResource.cs line 81 - "ITG" order
    /// </summary>
    public static RcolTgiBlock Read(ReadOnlySpan<byte> data)
    {
        if (data.Length < Size)
            throw new ArgumentException($"Data too short for TGI block: expected {Size} bytes, got {data.Length}");

        // ITG order: Instance (8 bytes), Type (4 bytes), Group (4 bytes)
        ulong instance = BinaryPrimitives.ReadUInt64LittleEndian(data);
        uint type = BinaryPrimitives.ReadUInt32LittleEndian(data[8..]);
        uint group = BinaryPrimitives.ReadUInt32LittleEndian(data[12..]);

        return new RcolTgiBlock(instance, type, group);
    }

    /// <summary>
    /// Writes this TGI block to a span in ITG order.
    /// </summary>
    public void Write(Span<byte> destination)
    {
        if (destination.Length < Size)
            throw new ArgumentException($"Destination too short for TGI block: need {Size} bytes, got {destination.Length}");

        BinaryPrimitives.WriteUInt64LittleEndian(destination, Instance);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[8..], ResourceType);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[12..], ResourceGroup);
    }

    /// <summary>
    /// Converts to a ResourceKey.
    /// </summary>
    public ResourceKey ToResourceKey() => new(ResourceType, ResourceGroup, Instance);

    /// <summary>
    /// Creates from a ResourceKey.
    /// </summary>
    public static RcolTgiBlock FromResourceKey(ResourceKey key) =>
        new(key.Instance, key.ResourceType, key.ResourceGroup);

    /// <inheritdoc/>
    public bool Equals(RcolTgiBlock other) =>
        Instance == other.Instance &&
        ResourceType == other.ResourceType &&
        ResourceGroup == other.ResourceGroup;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is RcolTgiBlock other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Instance, ResourceType, ResourceGroup);

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(RcolTgiBlock left, RcolTgiBlock right) => left.Equals(right);

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(RcolTgiBlock left, RcolTgiBlock right) => !left.Equals(right);

    /// <inheritdoc/>
    public override string ToString() =>
        $"0x{ResourceType:X8}:0x{ResourceGroup:X8}:0x{Instance:X16}";
}
