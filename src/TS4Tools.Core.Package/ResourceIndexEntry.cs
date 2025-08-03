/***************************************************************************
 *  Copyright (C) 2025 TS4Tools Project                                    *
 *                                                                         *
 *  This file is part of TS4Tools                                         *
 *                                                                         *
 *  TS4Tools is free software: you can redistribute it and/or modify      *
 *  it under the terms of the GNU General Public License as published by   *
 *  the Free Software Foundation, either version 3 of the License, or      *
 *  (at your option) any later version.                                    *
 *                                                                         *
 *  TS4Tools is distributed in the hope that it will be useful,           *
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of         *
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the          *
 *  GNU General Public License for more details.                           *
 *                                                                         *
 *  You should have received a copy of the GNU General Public License      *
 *  along with TS4Tools.  If not, see <http://www.gnu.org/licenses/>.     *
 ***************************************************************************/

namespace TS4Tools.Core.Package;

/// <summary>
/// Implementation of a resource index entry for package files
/// </summary>
internal sealed class ResourceIndexEntry : IResourceIndexEntry
{
    /// <summary>
    /// Size of a resource index entry in bytes
    /// </summary>
    public const int EntrySize = 32; // 8 fields * 4 bytes each
    
    private const int ApiVersion = 1;
    private IResourceKey _resourceKey;
    
    /// <inheritdoc />
    public int RequestedApiVersion => ApiVersion;
    
    /// <inheritdoc />
    public int RecommendedApiVersion => ApiVersion;
    
    /// <inheritdoc />
    public IReadOnlyList<string> ContentFields { get; } = new[]
    {
        "ResourceType", "ResourceGroup", "Instance", "ChunkOffset",
        "FileSize", "MemorySize", "Compressed", "Unknown2"
    };
    
    /// <inheritdoc />
    public uint ResourceType { get; set; }
    
    /// <inheritdoc />
    public uint ResourceGroup { get; set; }
    
    /// <inheritdoc />
    public ulong Instance { get; set; }
    
    /// <inheritdoc />
    public uint ChunkOffset { get; set; }
    
    /// <inheritdoc />
    public uint FileSize { get; set; }
    
    /// <inheritdoc />
    public uint MemorySize { get; set; }
    
    /// <inheritdoc />
    public ushort Compressed { get; set; }
    
    /// <inheritdoc />
    public ushort Unknown2 { get; set; } = 1;
    
    /// <inheritdoc />
    public Stream Stream => throw new NotImplementedException("Stream access not yet implemented");
    
    /// <inheritdoc />
    public bool IsDeleted { get; set; }
    
    /// <inheritdoc />
    public IResourceKey ResourceKey => _resourceKey;
    
    /// <summary>
    /// Check if the resource is compressed
    /// </summary>
    public bool IsCompressed => Compressed == 0xFFFF;
    
    /// <inheritdoc />
    public TypedValue this[int index]
    {
        get => index switch
        {
            0 => TypedValue.Create(ResourceType),
            1 => TypedValue.Create(ResourceGroup),
            2 => TypedValue.Create(Instance),
            3 => TypedValue.Create(ChunkOffset),
            4 => TypedValue.Create(FileSize),
            5 => TypedValue.Create(MemorySize),
            6 => TypedValue.Create(Compressed),
            7 => TypedValue.Create(Unknown2),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
        set => throw new NotSupportedException("Resource index entry fields are read-only");
    }
    
    /// <inheritdoc />
    public TypedValue this[string name]
    {
        get => name switch
        {
            "ResourceType" => TypedValue.Create(ResourceType),
            "ResourceGroup" => TypedValue.Create(ResourceGroup),
            "Instance" => TypedValue.Create(Instance),
            "ChunkOffset" => TypedValue.Create(ChunkOffset),
            "FileSize" => TypedValue.Create(FileSize),
            "MemorySize" => TypedValue.Create(MemorySize),
            "Compressed" => TypedValue.Create(Compressed),
            "Unknown2" => TypedValue.Create(Unknown2),
            _ => throw new ArgumentException($"Unknown field: {name}", nameof(name))
        };
        set => throw new NotSupportedException("Resource index entry fields are read-only");
    }
    
    /// <summary>
    /// Creates a new resource index entry
    /// </summary>
    /// <param name="resourceKey">Resource key</param>
    /// <param name="fileSize">File size in bytes</param>
    /// <param name="memorySize">Memory size in bytes (0 if same as file size)</param>
    /// <param name="chunkOffset">Offset in package file</param>
    /// <param name="compressed">Compression flag</param>
    /// <param name="unknown2">Unknown field (typically 1)</param>
    public ResourceIndexEntry(
        IResourceKey resourceKey,
        uint fileSize,
        uint memorySize = 0,
        uint chunkOffset = 0,
        ushort compressed = 0,
        ushort unknown2 = 1)
    {
        ArgumentNullException.ThrowIfNull(resourceKey);
        
        _resourceKey = resourceKey;
        ResourceType = resourceKey.ResourceType;
        ResourceGroup = resourceKey.ResourceGroup;
        Instance = resourceKey.Instance;
        ChunkOffset = chunkOffset;
        FileSize = fileSize;
        MemorySize = memorySize == 0 ? fileSize : memorySize;
        Compressed = compressed;
        Unknown2 = unknown2;
    }
    
    /// <summary>
    /// Creates a resource index entry from raw values
    /// </summary>
    /// <param name="resourceType">Resource type</param>
    /// <param name="resourceGroup">Resource group</param>
    /// <param name="instance">Instance ID</param>
    /// <param name="chunkOffset">Offset in package file</param>
    /// <param name="fileSize">File size in bytes</param>
    /// <param name="memorySize">Memory size in bytes</param>
    /// <param name="compressed">Compression flag</param>
    /// <param name="unknown2">Unknown field</param>
    public ResourceIndexEntry(
        uint resourceType,
        uint resourceGroup,
        ulong instance,
        uint chunkOffset,
        uint fileSize,
        uint memorySize,
        ushort compressed,
        ushort unknown2)
    {
        ResourceType = resourceType;
        ResourceGroup = resourceGroup;
        Instance = instance;
        ChunkOffset = chunkOffset;
        FileSize = fileSize;
        MemorySize = memorySize;
        Compressed = compressed;
        Unknown2 = unknown2;
        
        _resourceKey = new ResourceKey(resourceType, resourceGroup, instance);
    }
    
    /// <summary>
    /// Read a resource index entry from a binary reader
    /// </summary>
    /// <param name="reader">Binary reader</param>
    /// <param name="indexType">Index type information</param>
    /// <returns>Resource index entry</returns>
    public static ResourceIndexEntry Read(BinaryReader reader, uint indexType)
    {
        ArgumentNullException.ThrowIfNull(reader);
        
        var resourceType = reader.ReadUInt32();
        var resourceGroup = reader.ReadUInt32();
        var instanceHigh = reader.ReadUInt32();
        var instanceLow = reader.ReadUInt32();
        var instance = ((ulong)instanceHigh << 32) | instanceLow;
        var chunkOffset = reader.ReadUInt32();
        var fileSize = reader.ReadUInt32();
        var memorySize = reader.ReadUInt32();
        var compressedAndUnknown = reader.ReadUInt32();
        
        // Split the compressed and unknown2 fields
        var compressed = (ushort)(compressedAndUnknown & 0xFFFF);
        var unknown2 = (ushort)((compressedAndUnknown >> 16) & 0xFFFF);
        
        return new ResourceIndexEntry(
            resourceType,
            resourceGroup,
            instance,
            chunkOffset,
            fileSize,
            memorySize,
            compressed,
            unknown2);
    }
    
    /// <summary>
    /// Write the resource index entry to a binary writer
    /// </summary>
    /// <param name="writer">Binary writer</param>
    public void Write(BinaryWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        
        writer.Write(ResourceType);
        writer.Write(ResourceGroup);
        writer.Write((uint)(Instance >> 32)); // High 32 bits
        writer.Write((uint)(Instance & 0xFFFFFFFF)); // Low 32 bits
        writer.Write(ChunkOffset);
        writer.Write(FileSize);
        writer.Write(MemorySize);
        
        // Combine compressed and unknown2 into a single uint
        var compressedAndUnknown = (uint)((Unknown2 << 16) | Compressed);
        writer.Write(compressedAndUnknown);
    }
    
    /// <inheritdoc />
    public bool Equals(IResourceKey? x, IResourceKey? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        
        return x.ResourceType == y.ResourceType &&
               x.ResourceGroup == y.ResourceGroup &&
               x.Instance == y.Instance;
    }
    
    /// <inheritdoc />
    public int GetHashCode(IResourceKey obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        return HashCode.Combine(obj.ResourceType, obj.ResourceGroup, obj.Instance);
    }
    
    /// <inheritdoc />
    public bool Equals(IResourceKey? other)
    {
        return Equals(this, other);
    }
    
    /// <inheritdoc />
    public int CompareTo(IResourceKey? other)
    {
        if (other is null) return 1;
        
        var typeComparison = ResourceType.CompareTo(other.ResourceType);
        if (typeComparison != 0) return typeComparison;
        
        var groupComparison = ResourceGroup.CompareTo(other.ResourceGroup);
        if (groupComparison != 0) return groupComparison;
        
        return Instance.CompareTo(other.Instance);
    }
    
    /// <inheritdoc />
    public bool Equals(IResourceIndexEntry? other)
    {
        return other != null &&
               ResourceType == other.ResourceType &&
               ResourceGroup == other.ResourceGroup &&
               Instance == other.Instance;
    }
    
    /// <inheritdoc />
    public override string ToString()
    {
        return $"ResourceIndexEntry: Type=0x{ResourceType:X8}, Group=0x{ResourceGroup:X8}, Instance=0x{Instance:X16}, Size={FileSize}";
    }
    
    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is IResourceIndexEntry other && Equals(other);
    }
    
    /// <inheritdoc />
    public override int GetHashCode()
    {
        return GetHashCode(this);
    }
    
    /// <inheritdoc />
    public static bool operator ==(ResourceIndexEntry? left, ResourceIndexEntry? right)
    {
        return EqualityComparer<ResourceIndexEntry>.Default.Equals(left, right);
    }
    
    /// <inheritdoc />
    public static bool operator !=(ResourceIndexEntry? left, ResourceIndexEntry? right)
    {
        return !(left == right);
    }
}
