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

namespace TS4Tools.Resources.Common;

/// <summary>
/// Represents a Type-Group-Instance reference used throughout The Sims 4 resource system.
/// TGI references are the primary mechanism for linking resources within .package files.
/// This is the canonical implementation shared across all resource types.
/// </summary>
/// <param name="TypeId">The resource type identifier (32-bit).</param>
/// <param name="GroupId">The resource group identifier (32-bit).</param>
/// <param name="InstanceId">The resource instance identifier (64-bit).</param>
public sealed record TgiReference(uint TypeId, uint GroupId, ulong InstanceId)
{
    /// <summary>Gets the resource type identifier.</summary>
    public uint ResourceType => TypeId;

    /// <summary>Gets the resource group identifier.</summary>
    public uint ResourceGroup => GroupId;

    /// <summary>Gets the resource instance identifier.</summary>
    public ulong ResourceInstance => InstanceId;

    /// <summary>
    /// Creates a new TGI reference with alternative parameter names for compatibility.
    /// </summary>
    /// <param name="resourceType">The resource type identifier.</param>
    /// <param name="resourceGroup">The resource group identifier.</param>
    /// <param name="resourceInstance">The resource instance identifier.</param>
    /// <returns>A new TgiReference instance.</returns>
    public static TgiReference Create(uint resourceType, uint resourceGroup, ulong resourceInstance)
        => new(resourceType, resourceGroup, resourceInstance);

    /// <summary>
    /// Formats the TGI reference as a standard string representation.
    /// </summary>
    /// <returns>String in format "T:0x{TypeId:X8}-G:0x{GroupId:X8}-I:0x{InstanceId:X16}".</returns>
    public override string ToString()
        => $"T:0x{TypeId:X8}-G:0x{GroupId:X8}-I:0x{InstanceId:X16}";

    /// <summary>
    /// Creates a TGI reference from a formatted string.
    /// </summary>
    /// <param name="value">String in TGI format.</param>
    /// <returns>Parsed TgiReference or null if parsing fails.</returns>
    public static TgiReference? TryParse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        try
        {
            // Expected format: "T:0x{TypeId:X8}-G:0x{GroupId:X8}-I:0x{InstanceId:X16}"
            var parts = value.Split('-');
            if (parts.Length != 3)
                return null;

            var typeStr = parts[0].Replace("T:0x", "", StringComparison.OrdinalIgnoreCase).Replace("T:", "", StringComparison.OrdinalIgnoreCase);
            var groupStr = parts[1].Replace("G:0x", "", StringComparison.OrdinalIgnoreCase).Replace("G:", "", StringComparison.OrdinalIgnoreCase);
            var instanceStr = parts[2].Replace("I:0x", "", StringComparison.OrdinalIgnoreCase).Replace("I:", "", StringComparison.OrdinalIgnoreCase);

            if (uint.TryParse(typeStr, System.Globalization.NumberStyles.HexNumber, null, out var type) &&
                uint.TryParse(groupStr, System.Globalization.NumberStyles.HexNumber, null, out var group) &&
                ulong.TryParse(instanceStr, System.Globalization.NumberStyles.HexNumber, null, out var instance))
            {
                return new TgiReference(type, group, instance);
            }
        }
        catch
        {
            // Parsing failed
        }

        return null;
    }

    /// <summary>
    /// Gets a value indicating whether this TGI reference represents a null/empty reference.
    /// </summary>
    public bool IsNull => TypeId == 0 && GroupId == 0 && InstanceId == 0;

    /// <summary>
    /// Gets a null TGI reference (all zeros).
    /// </summary>
    public static TgiReference Null => new(0, 0, 0);

    /// <summary>
    /// Checks if this TGI reference matches the specified type.
    /// </summary>
    /// <param name="typeId">The type identifier to check.</param>
    /// <returns>True if the type matches.</returns>
    public bool IsType(uint typeId) => TypeId == typeId;

    /// <summary>
    /// Checks if this TGI reference belongs to the specified group.
    /// </summary>
    /// <param name="groupId">The group identifier to check.</param>
    /// <returns>True if the group matches.</returns>
    public bool IsGroup(uint groupId) => GroupId == groupId;

    /// <summary>
    /// Creates a new TGI reference with the same Type and Group but different Instance.
    /// </summary>
    /// <param name="newInstance">The new instance identifier.</param>
    /// <returns>A new TgiReference with updated instance.</returns>
    public TgiReference WithInstance(ulong newInstance) => this with { InstanceId = newInstance };

    /// <summary>
    /// Creates a new TGI reference with the same Type and Instance but different Group.
    /// </summary>
    /// <param name="newGroup">The new group identifier.</param>
    /// <returns>A new TgiReference with updated group.</returns>
    public TgiReference WithGroup(uint newGroup) => this with { GroupId = newGroup };

    /// <summary>
    /// Creates a new TGI reference with the same Group and Instance but different Type.
    /// </summary>
    /// <param name="newType">The new type identifier.</param>
    /// <returns>A new TgiReference with updated type.</returns>
    public TgiReference WithType(uint newType) => this with { TypeId = newType };

    /// <summary>
    /// Compares this TGI reference with another for sorting purposes.
    /// Order: Type, Group, Instance.
    /// </summary>
    /// <param name="other">The other TGI reference to compare.</param>
    /// <returns>Comparison result.</returns>
    public int CompareTo(TgiReference? other)
    {
        if (other is null) return 1;

        var typeComparison = TypeId.CompareTo(other.TypeId);
        if (typeComparison != 0) return typeComparison;

        var groupComparison = GroupId.CompareTo(other.GroupId);
        if (groupComparison != 0) return groupComparison;

        return InstanceId.CompareTo(other.InstanceId);
    }
}
