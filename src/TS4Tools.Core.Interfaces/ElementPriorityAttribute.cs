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

using System.Reflection;

namespace TS4Tools.Core.Interfaces;

/// <summary>
/// Element priority is used when displaying elements.
/// Lower values indicate higher priority.
/// </summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
public sealed class ElementPriorityAttribute : Attribute
{
    /// <summary>
    /// Element priority is used when displaying elements
    /// </summary>
    /// <param name="priority">Element priority, lower values are higher priority</param>
    public ElementPriorityAttribute(int priority) => Priority = priority;

    /// <summary>
    /// Element priority, lower values are higher priority
    /// </summary>
    public int Priority { get; }

    /// <summary>
    /// Return the ElementPriority value for a Content Field.
    /// </summary>
    /// <param name="type">Type on which Content Field exists.</param>
    /// <param name="fieldName">Content Field name.</param>
    /// <returns>The value of the ElementPriorityAttribute Priority field, if found;
    /// otherwise int.MaxValue.</returns>
    public static int GetPriority(Type type, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentException.ThrowIfNullOrEmpty(fieldName);

        var member = type.GetProperty(fieldName) ??
                    type.GetField(fieldName) as MemberInfo;

        if (member?.GetCustomAttribute<ElementPriorityAttribute>() is { } attribute)
        {
            return attribute.Priority;
        }

        return int.MaxValue;
    }
}
