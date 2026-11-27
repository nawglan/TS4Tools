// Source: legacy_references/Sims4Tools/s4pi/Interfaces/IPackage.cs lines 24-244

namespace s4pi.Interfaces;

/// <summary>
/// Representation of a Sims 4 Package.
/// </summary>
public interface IPackage : IApiVersion, IContentFields, IDisposable
{
    #region Whole package
    /// <summary>
    /// Tell the package to save itself to wherever it believes it came from.
    /// </summary>
    void SavePackage();

    /// <summary>
    /// Tell the package to save itself to the <see cref="System.IO.Stream"/> <paramref name="s"/>.
    /// </summary>
    /// <param name="s">A stream to which the package should be saved.</param>
    void SaveAs(Stream s);

    /// <summary>
    /// Tell the package to save itself to a file with the name in <paramref name="path"/>.
    /// </summary>
    /// <param name="path">A fully-qualified file name.</param>
    void SaveAs(string path);
    #endregion

    #region Package header
    /// <summary>Package header: "DBPF" bytes.</summary>
    [ElementPriority(1)] byte[] Magic { get; }
    /// <summary>Package header: 0x00000002.</summary>
    [ElementPriority(2)] int Major { get; }
    /// <summary>Package header: 0x00000001.</summary>
    [ElementPriority(3)] int Minor { get; }
    /// <summary>Package header: user version major.</summary>
    [ElementPriority(4)] int UserVersionMajor { get; }
    /// <summary>Package header: user version minor.</summary>
    [ElementPriority(5)] int UserVersionMinor { get; }
    /// <summary>Package header: unused.</summary>
    [ElementPriority(6)] int Unused1 { get; }
    /// <summary>Package header: typically, not set.</summary>
    [ElementPriority(7)] int CreationTime { get; }
    /// <summary>Package header: typically, not set.</summary>
    [ElementPriority(8)] int UpdatedTime { get; }
    /// <summary>Package header: unused.</summary>
    [ElementPriority(9)] int Unused2 { get; }
    /// <summary>Package header: number of entries in the package index.</summary>
    [ElementPriority(10)] int Indexcount { get; }
    /// <summary>Package header: index position in file (low).</summary>
    [ElementPriority(11)] int IndexRecordPositionLow { get; }
    /// <summary>Package header: index size on disk in bytes.</summary>
    [ElementPriority(12)] int Indexsize { get; }
    /// <summary>Package header: unused.</summary>
    [ElementPriority(13)] int Unused3 { get; }
    /// <summary>Package header: unused (always 3).</summary>
    [ElementPriority(14)] int Unused4 { get; }
    /// <summary>Package header: index position in file.</summary>
    [ElementPriority(15)] int Indexposition { get; }
    /// <summary>Package header: unused.</summary>
    [ElementPriority(16)] byte[] Unused5 { get; }

    /// <summary>
    /// A MemoryStream covering the package header bytes.
    /// </summary>
    [ElementPriority(17)] Stream HeaderStream { get; }
    #endregion

    #region Package index
    /// <summary>
    /// Event indicating the package index has been invalidated.
    /// </summary>
    event EventHandler ResourceIndexInvalidated;

    /// <summary>
    /// Package index: the index format in use.
    /// </summary>
    [ElementPriority(13)] uint Indextype { get; }

    /// <summary>
    /// Package index: the index entries (including those marked deleted).
    /// </summary>
    [ElementPriority(14)] List<IResourceIndexEntry> GetResourceList { get; }

    /// <summary>
    /// Searches the entire package for a resource that matches the conditions defined by
    /// <paramref name="flags"/> and <paramref name="values"/>.
    /// </summary>
    /// <param name="flags">True bits enable matching against numerically equivalent <paramref name="values"/> entry.</param>
    /// <param name="values">Fields to compare against.</param>
    /// <returns>The first match, if any; otherwise null.</returns>
    [Obsolete("Please use Find(Predicate<IResourceIndexEntry> Match)")]
    IResourceIndexEntry? Find(uint flags, IResourceIndexEntry values);

    /// <summary>
    /// Searches the entire package for a resource that matches the conditions defined by
    /// <paramref name="names"/> and <paramref name="values"/>.
    /// </summary>
    /// <param name="names">Names of fields to compare.</param>
    /// <param name="values">Values to compare against.</param>
    /// <returns>The first match, if any; otherwise null.</returns>
    [Obsolete("Please use Find(Predicate<IResourceIndexEntry> Match)")]
    IResourceIndexEntry? Find(string[] names, TypedValue[] values);

    /// <summary>
    /// Searches the entire package for a resource that matches the conditions defined by <paramref name="Match"/>.
    /// </summary>
    /// <param name="Match">Predicate defining matching conditions.</param>
    /// <returns>The first matching (non-deleted) resource index entry, if any; otherwise null.</returns>
    IResourceIndexEntry? Find(Predicate<IResourceIndexEntry> Match);

    /// <summary>
    /// Searches the entire package for all resources that match the conditions defined by
    /// <paramref name="flags"/> and <paramref name="values"/>.
    /// </summary>
    /// <param name="flags">True bits enable matching against numerically equivalent <paramref name="values"/> entry.</param>
    /// <param name="values">Fields to compare against.</param>
    /// <returns>All matching entries.</returns>
    [Obsolete("Please use FindAll(Predicate<IResourceIndexEntry> Match)")]
    List<IResourceIndexEntry> FindAll(uint flags, IResourceIndexEntry values);

    /// <summary>
    /// Searches the entire package for all resources that match the conditions defined by
    /// <paramref name="names"/> and <paramref name="values"/>.
    /// </summary>
    /// <param name="names">Names of fields to compare.</param>
    /// <param name="values">Values to compare against.</param>
    /// <returns>All matching entries.</returns>
    [Obsolete("Please use FindAll(Predicate<IResourceIndexEntry> Match)")]
    List<IResourceIndexEntry> FindAll(string[] names, TypedValue[] values);

    /// <summary>
    /// Searches the entire package for all resources that match the conditions defined by <paramref name="Match"/>.
    /// </summary>
    /// <param name="Match">Predicate defining matching conditions.</param>
    /// <returns>All matching (non-deleted) resource index entries.</returns>
    List<IResourceIndexEntry> FindAll(Predicate<IResourceIndexEntry> Match);
    #endregion

    #region Package content
    /// <summary>
    /// Add a resource to the package.
    /// </summary>
    /// <param name="rk">The resource key.</param>
    /// <param name="stream">The stream that contains the resource data (null for an empty resource).</param>
    /// <param name="rejectDups">If true, fail if the resource key already exists.</param>
    /// <returns>Null if rejectDups and the resource key exists; else the new IResourceIndexEntry.</returns>
    IResourceIndexEntry? AddResource(IResourceKey rk, Stream? stream, bool rejectDups);

    /// <summary>
    /// Tell the package to replace the data for the resource indexed by <paramref name="rc"/>
    /// with the data from the resource <paramref name="res"/>.
    /// </summary>
    /// <param name="rc">Target resource index.</param>
    /// <param name="res">Source resource.</param>
    void ReplaceResource(IResourceIndexEntry rc, IResource res);

    /// <summary>
    /// Tell the package to delete the resource indexed by <paramref name="rc"/>.
    /// </summary>
    /// <param name="rc">Target resource index.</param>
    void DeleteResource(IResourceIndexEntry rc);
    #endregion
}
