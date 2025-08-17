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
/// An index entry within a package
/// </summary>
public interface IResourceIndexEntry : IApiVersion, IContentFields, IResourceKey, IEquatable<IResourceIndexEntry>
{
    /// <summary>
    /// If the resource was read from a package, the location in the package the resource was read from
    /// </summary>
    uint ChunkOffset { get; set; }

    /// <summary>
    /// The number of bytes the resource uses within the package
    /// </summary>
    uint FileSize { get; set; }

    /// <summary>
    /// The number of bytes the resource uses in memory
    /// </summary>
    uint MemorySize { get; set; }

    /// <summary>
    /// 0xFFFF if FileSize != MemorySize (compressed), else 0x0000
    /// </summary>
    ushort Compressed { get; set; }

    /// <summary>
    /// Always 0x0001
    /// </summary>
    ushort Unknown2 { get; set; }

    /// <summary>
    /// A <see cref="Stream"/> covering the index entry bytes
    /// </summary>
    Stream Stream { get; }

    /// <summary>
    /// True if the index entry has been deleted from the package index
    /// </summary>
    bool IsDeleted { get; set; }
}
