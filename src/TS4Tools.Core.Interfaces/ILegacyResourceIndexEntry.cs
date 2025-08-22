//
// Copyright (c) 2024 TS4Tools Contributors
//
// This file is part of TS4Tools.
//
// TS4Tools is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// TS4Tools is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with TS4Tools. If not, see <http://www.gnu.org/licenses/>.
//

#pragma warning disable CA1002 // Do not expose generic lists - Required for s4pi compatibility
#pragma warning disable CA1005 // Avoid excessive parameters on generic types - Required for s4pi compatibility  
#pragma warning disable CA1720 // Identifier contains type name - Required for s4pi compatibility
#pragma warning disable IDE1006 // Naming Styles - Required for s4pi compatibility

using System;
using System.Collections.Generic;
using System.IO;

namespace TS4Tools.Core.Interfaces
{
    /// <summary>
    /// Legacy s4pi compatibility interface for IResourceIndexEntry.
    /// This interface provides the exact API surface expected by legacy WrapperDealer.GetResource() methods.
    /// It bridges to the modern TS4Tools resource management system.
    /// </summary>
    public interface ILegacyResourceIndexEntry : IEquatable<ILegacyResourceIndexEntry>
    {
        /// <summary>
        /// The version of the API in use
        /// </summary>
        int RequestedApiVersion { get; }

        /// <summary>
        /// The best supported version of the API available
        /// </summary>
        int RecommendedApiVersion { get; }

        /// <summary>
        /// The list of available field names on this API object
        /// </summary>
        List<string> ContentFields { get; }

        /// <summary>
        /// The "type" of the resource
        /// </summary>
        uint ResourceType { get; set; }

        /// <summary>
        /// The "group" the resource is part of
        /// </summary>
        uint ResourceGroup { get; set; }

        /// <summary>
        /// The "instance" number of the resource
        /// </summary>
        ulong Instance { get; set; }

        /// <summary>
        /// If the resource was read from a package, the location in the package the resource was read from
        /// </summary>
        uint Chunkoffset { get; set; }

        /// <summary>
        /// The number of bytes the resource uses within the package
        /// </summary>
        uint Filesize { get; set; }

        /// <summary>
        /// The number of bytes the resource uses in memory
        /// </summary>
        uint Memsize { get; set; }

        /// <summary>
        /// 0xFFFF if Filesize != Memsize, else 0x0000
        /// </summary>
        ushort Compressed { get; set; }

        /// <summary>
        /// Always 0x0001
        /// </summary>
        ushort Unknown2 { get; set; }

        /// <summary>
        /// A MemoryStream covering the index entry bytes
        /// </summary>
        Stream Stream { get; }

        /// <summary>
        /// True if the index entry has been deleted from the package index
        /// </summary>
        bool IsDeleted { get; set; }

        /// <summary>
        /// Get value by field name for legacy compatibility
        /// </summary>
        /// <param name="index">Field name</param>
        /// <returns>Field value wrapped in TypedValue</returns>
        object this[string index] { get; set; }
    }
}
