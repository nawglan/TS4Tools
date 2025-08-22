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

// Disable code analysis for legacy compatibility interface
#pragma warning disable CA1002 // Do not expose generic lists
#pragma warning disable S1133 // Deprecated code is intentional for compatibility

namespace TS4Tools.Core.Interfaces;

/// <summary>
/// Legacy s4pi IPackage interface compatibility shim.
/// 
/// CRITICAL COMPATIBILITY REQUIREMENT: This interface provides 100% backward compatibility
/// with the legacy s4pi.Interfaces.IPackage interface that community plugins expect.
/// 
/// The WrapperDealer.GetResource() method signature MUST accept this interface type
/// to maintain compatibility with existing modding tools and plugins.
/// 
/// DESIGN PATTERN: This is a compatibility bridge that allows legacy plugins to pass
/// their IPackage objects to the modern TS4Tools WrapperDealer, which internally
/// converts them to work with the modern TS4Tools.Core.Package.IPackage system.
/// </summary>
public interface ILegacyPackage : IApiVersion, IContentFields, IDisposable
{
    #region Whole package
    
    /// <summary>
    /// Tell the package to save itself to wherever it believes it came from
    /// </summary>
    void SavePackage();
    
    /// <summary>
    /// Tell the package to save itself to the specified stream
    /// </summary>
    /// <param name="s">A stream to which the package should be saved</param>
    void SaveAs(Stream s);
    
    /// <summary>
    /// Tell the package to save itself to a file with the name in path
    /// </summary>
    /// <param name="path">A fully-qualified file name</param>
    void SaveAs(string path);
    
    #endregion

    #region Package header
    
    /// <summary>
    /// Package header: "DBPF" bytes
    /// </summary>
    [ElementPriority(1)]
    byte[] Magic { get; }
    
    /// <summary>
    /// Package header: 0x00000002
    /// </summary>
    [ElementPriority(2)]
    int Major { get; }
    
    /// <summary>
    /// Package header: 0x00000000
    /// </summary>
    [ElementPriority(3)]
    int Minor { get; }
    
    /// <summary>
    /// Package header: 0x00000000
    /// </summary>
    [ElementPriority(4)]
    int UserVersionMajor { get; }
    
    /// <summary>
    /// Package header: 0x00000000
    /// </summary>
    [ElementPriority(5)]
    int UserVersionMinor { get; }
    
    /// <summary>
    /// Package header: unused
    /// </summary>
    [ElementPriority(6)]
    int Unused1 { get; }
    
    /// <summary>
    /// Package header: typically, not set
    /// </summary>
    [ElementPriority(7)]
    int CreationTime { get; }
    
    /// <summary>
    /// Package header: typically, not set
    /// </summary>
    [ElementPriority(8)]
    int UpdatedTime { get; }
    
    /// <summary>
    /// Package header: unused
    /// </summary>
    [ElementPriority(9)]
    int Unused2 { get; }
    
    /// <summary>
    /// Package header: number of entries in the package index
    /// </summary>
    [ElementPriority(10)]
    int Indexcount { get; }
    
    /// <summary>
    /// Package header: unused
    /// </summary>
    [ElementPriority(11)]
    int IndexRecordPositionLow { get; }
    
    /// <summary>
    /// Package header: index size on disk in bytes
    /// </summary>
    [ElementPriority(12)]
    int Indexsize { get; }
    
    /// <summary>
    /// Package header: unused
    /// </summary>
    [ElementPriority(13)]
    int Unused3 { get; }
    
    /// <summary>
    /// Package header: always 3 for historical reasons
    /// </summary>
    [ElementPriority(14)]
    int Unused4 { get; }
    
    /// <summary>
    /// Package header: index position in file
    /// </summary>
    [ElementPriority(15)]
    int Indexposition { get; }
    
    /// <summary>
    /// Package header: unused
    /// </summary>
    [ElementPriority(16)]
    byte[] Unused5 { get; }

    /// <summary>
    /// A MemoryStream covering the package header bytes
    /// </summary>
    [ElementPriority(17)]
    Stream HeaderStream { get; }
    
    #endregion

    #region Package index
    
    /// <summary>
    /// Package index: raised when the result of a previous call to GetResourceList becomes invalid 
    /// </summary>
    event EventHandler? ResourceIndexInvalidated;

    /// <summary>
    /// Package index: the index format in use
    /// </summary>
    [ElementPriority(13)]
    uint Indextype { get; }

    /// <summary>
    /// Package index: the index as a ILegacyResourceIndexEntry list
    /// </summary>
    [ElementPriority(14)]
    List<ILegacyResourceIndexEntry> GetResourceList { get; }

    /// <summary>
    /// Searches the entire package for the first IResourceIndexEntry that matches the conditions
    /// </summary>
    /// <param name="flags">True bits enable matching against numerically equivalent values entry</param>
    /// <param name="values">Field values to compare against</param>
    /// <returns>The first matching IResourceIndexEntry, if any; otherwise null</returns>
    [Obsolete("Please use Find(Predicate<IResourceIndexEntry> Match)")]
    ILegacyResourceIndexEntry? Find(uint flags, ILegacyResourceIndexEntry values);

    /// <summary>
    /// Searches the entire package for the first IResourceIndexEntry that matches the conditions
    /// </summary>
    /// <param name="names">Names of IResourceIndexEntry fields to compare</param>
    /// <param name="values">Field values to compare against</param>
    /// <returns>The first matching IResourceIndexEntry, if any; otherwise null</returns>
    [Obsolete("Please use Find(Predicate<IResourceIndexEntry> Match)")]
    ILegacyResourceIndexEntry? Find(string[] names, TypedValue[] values);

    /// <summary>
    /// Searches the entire package for the first IResourceIndexEntry that matches the conditions
    /// </summary>
    /// <param name="Match">Predicate defining matching conditions</param>
    /// <returns>The first matching IResourceIndexEntry, if any; otherwise null</returns>
    ILegacyResourceIndexEntry? Find(Predicate<ILegacyResourceIndexEntry> Match);

    /// <summary>
    /// Searches the entire package for all IResourceIndexEntrys that matches the conditions
    /// </summary>
    /// <param name="flags">True bits enable matching against numerically equivalent values entry</param>
    /// <param name="values">Field values to compare against</param>
    /// <returns>An IList of zero or more matches</returns>
    [Obsolete("Please use FindAll(Predicate<IResourceIndexEntry> Match)")]
    List<IResourceIndexEntry> FindAll(uint flags, IResourceIndexEntry values);

    /// <summary>
    /// Searches the entire package for all IResourceIndexEntrys that matches the conditions
    /// </summary>
    /// <param name="names">Names of IResourceIndexEntry fields to compare</param>
    /// <param name="values">Field values to compare against</param>
    /// <returns>An IList of zero or more matches</returns>
    [Obsolete("Please use FindAll(Predicate<IResourceIndexEntry> Match)")]
    List<ILegacyResourceIndexEntry> FindAll(string[] names, TypedValue[] values);

    /// <summary>
    /// Searches the entire package for all IResourceIndexEntrys that matches the conditions
    /// </summary>
    /// <param name="Match">Predicate defining matching conditions</param>
    /// <returns>Zero or more matches</returns>
    List<ILegacyResourceIndexEntry> FindAll(Predicate<ILegacyResourceIndexEntry> Match);
    
    #endregion

    #region Package content
    
    /// <summary>
    /// Add a resource to the package
    /// </summary>
    /// <param name="rk">The resource's IResourceKey</param>
    /// <param name="stream">The stream that contains the resource's data</param>
    /// <param name="rejectDups">If true, fail if the IResourceKey already exists</param>
    /// <returns>Null if rejectDups and the IResourceKey exists; else the new IResourceIndexEntry</returns>
    IResourceIndexEntry? AddResource(IResourceKey rk, Stream stream, bool rejectDups);
    
    /// <summary>
    /// Tell the package to replace the data for the resource indexed by IResourceIndexEntry rc
    /// with the data from the IResource res
    /// </summary>
    /// <param name="rc">Target IResourceIndexEntry</param>
    /// <param name="res">Source IResource</param>
    void ReplaceResource(IResourceIndexEntry rc, IResource res);
    
    /// <summary>
    /// Tell the package to delete the resource indexed by IResourceIndexEntry rc
    /// </summary>
    /// <param name="rc">Target IResourceIndexEntry</param>
    void DeleteResource(IResourceIndexEntry rc);
    
    #endregion

    #region Internal API - Used by WrapperDealer
    
    /// <summary>
    /// Required internally by s4pi - not for use in user tools.
    /// Please use WrapperDealer.GetResource(int, ILegacyPackage, ILegacyResourceIndexEntry) instead.
    /// </summary>
    /// <param name="rie">ILegacyResourceIndexEntry of resource</param>
    /// <returns>The resource data (uncompressed, if necessary)</returns>
    /// <remarks>Used by WrapperDealer to get the data for a resource.</remarks>
    Stream GetResource(ILegacyResourceIndexEntry rie);
    
    #endregion
}
