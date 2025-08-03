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

namespace TS4Tools.Core.Interfaces;

/// <summary>
/// Minimal interface for content fields support
/// </summary>
public interface IContentFields
{
    /// <summary>
    /// The list of content field names
    /// </summary>
    IReadOnlyList<string> ContentFields { get; }

    /// <summary>
    /// Get the value of a content field by name
    /// </summary>
    /// <param name="index">The index of the field</param>
    /// <returns>The typed value of the field</returns>
    TypedValue this[int index] { get; set; }

    /// <summary>
    /// Get the value of a content field by name
    /// </summary>
    /// <param name="name">The name of the field</param>
    /// <returns>The typed value of the field</returns>
    TypedValue this[string name] { get; set; }
}
