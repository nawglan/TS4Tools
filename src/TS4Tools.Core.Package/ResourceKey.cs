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
/// Implementation of a resource key for identifying resources in packages
/// </summary>
public sealed class ResourceKey : IResourceKey
{
    /// <inheritdoc />
    public uint ResourceType { get; set; }

    /// <inheritdoc />
    public uint ResourceGroup { get; set; }

    /// <inheritdoc />
    public ulong Instance { get; set; }

    /// <summary>
    /// Creates a new resource key
    /// </summary>
    /// <param name="resourceType">Resource type</param>
    /// <param name="resourceGroup">Resource group</param>
    /// <param name="instance">Instance ID</param>
    public ResourceKey(uint resourceType, uint resourceGroup, ulong instance)
    {
        ResourceType = resourceType;
        ResourceGroup = resourceGroup;
        Instance = instance;
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
    public override bool Equals(object? obj)
    {
        return obj is IResourceKey other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return GetHashCode(this);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"ResourceKey: Type=0x{ResourceType:X8}, Group=0x{ResourceGroup:X8}, Instance=0x{Instance:X16}";
    }

    /// <inheritdoc />
    public static bool operator ==(ResourceKey? left, ResourceKey? right)
    {
        return EqualityComparer<ResourceKey>.Default.Equals(left, right);
    }

    /// <inheritdoc />
    public static bool operator !=(ResourceKey? left, ResourceKey? right)
    {
        return !(left == right);
    }

    /// <inheritdoc />
    public static bool operator <(ResourceKey? left, ResourceKey? right)
    {
        return left is null ? right is not null : left.CompareTo(right) < 0;
    }

    /// <inheritdoc />
    public static bool operator <=(ResourceKey? left, ResourceKey? right)
    {
        return left is null || left.CompareTo(right) <= 0;
    }

    /// <inheritdoc />
    public static bool operator >(ResourceKey? left, ResourceKey? right)
    {
        return left is not null && left.CompareTo(right) > 0;
    }

    /// <inheritdoc />
    public static bool operator >=(ResourceKey? left, ResourceKey? right)
    {
        return left is null ? right is null : left.CompareTo(right) >= 0;
    }
}
