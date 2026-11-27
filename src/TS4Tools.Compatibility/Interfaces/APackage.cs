// Source: legacy_references/Sims4Tools/s4pi/Interfaces/APackage.cs lines 24-316

namespace s4pi.Interfaces;

/// <summary>
/// Abstract definition of a package.
/// </summary>
/// <remarks>The concrete implementation is <see cref="s4pi.Package.Package"/>.</remarks>
public abstract class APackage : AApiVersionedFields, IPackage
{
    #region AApiVersionedFields
    /// <summary>
    /// A <see cref="List{T}"/> of available field names on this API object.
    /// </summary>
    public override List<string> ContentFields => GetContentFields(requestedApiVersion, GetType());
    #endregion

    #region IPackage Members

    #region Whole package
    /// <summary>Tell the package to save itself to wherever it believes it came from.</summary>
    public abstract void SavePackage();

    /// <summary>Tell the package to save itself to the <see cref="System.IO.Stream"/> <paramref name="s"/>.</summary>
    /// <param name="s">A stream to which the package should be saved.</param>
    public abstract void SaveAs(Stream s);

    /// <summary>Tell the package to save itself to a file with the name in <paramref name="path"/>.</summary>
    /// <param name="path">A fully-qualified file name.</param>
    public abstract void SaveAs(string path);
    #endregion

    #region Package header
    /// <summary>Package header: "DBPF" bytes.</summary>
    [ElementPriority(1)] public abstract byte[] Magic { get; }
    /// <summary>Package header: 0x00000002.</summary>
    [ElementPriority(2)] public abstract int Major { get; }
    /// <summary>Package header: 0x00000001.</summary>
    [ElementPriority(3)] public abstract int Minor { get; }
    /// <summary>Package header: user version major.</summary>
    [ElementPriority(4)] public abstract int UserVersionMajor { get; }
    /// <summary>Package header: user version minor.</summary>
    [ElementPriority(5)] public abstract int UserVersionMinor { get; }
    /// <summary>Package header: unused.</summary>
    [ElementPriority(6)] public abstract int Unused1 { get; }
    /// <summary>Package header: typically, not set.</summary>
    [ElementPriority(7)] public abstract int CreationTime { get; }
    /// <summary>Package header: typically, not set.</summary>
    [ElementPriority(8)] public abstract int UpdatedTime { get; }
    /// <summary>Package header: unused.</summary>
    [ElementPriority(9)] public abstract int Unused2 { get; }
    /// <summary>Package header: number of entries in the package index.</summary>
    [ElementPriority(10)] public abstract int Indexcount { get; }
    /// <summary>Package header: index position in file (low).</summary>
    [ElementPriority(11)] public abstract int IndexRecordPositionLow { get; }
    /// <summary>Package header: index size on disk in bytes.</summary>
    [ElementPriority(12)] public abstract int Indexsize { get; }
    /// <summary>Package header: unused.</summary>
    [ElementPriority(13)] public abstract int Unused3 { get; }
    /// <summary>Package header: unused (always 3).</summary>
    [ElementPriority(14)] public abstract int Unused4 { get; }
    /// <summary>Package header: index position in file.</summary>
    [ElementPriority(15)] public abstract int Indexposition { get; }
    /// <summary>Package header: unused.</summary>
    [ElementPriority(16)] public abstract byte[] Unused5 { get; }

    /// <summary>A MemoryStream covering the package header bytes.</summary>
    [ElementPriority(12)] public abstract Stream HeaderStream { get; }
    #endregion

    #region Package index
    /// <summary>Event indicating the package index has been invalidated.</summary>
    public event EventHandler? ResourceIndexInvalidated;

    /// <summary>Package index: the index format in use.</summary>
    [ElementPriority(13)] public abstract uint Indextype { get; }

    /// <summary>Package index: the index entries (including those marked deleted).</summary>
    [ElementPriority(14)] public abstract List<IResourceIndexEntry> GetResourceList { get; }

    /// <summary>Searches the package for the first resource matching <paramref name="flags"/> and <paramref name="values"/>.</summary>
    /// <param name="flags">True bits enable matching against numerically equivalent <paramref name="values"/> entry.</param>
    /// <param name="values">Fields to compare against.</param>
    /// <returns>The first match, if any; otherwise null.</returns>
    [Obsolete("Please use Find(Predicate<IResourceIndexEntry> Match)")]
    public abstract IResourceIndexEntry? Find(uint flags, IResourceIndexEntry values);

    /// <summary>Searches the package for the first resource matching <paramref name="names"/> and <paramref name="values"/>.</summary>
    /// <param name="names">Names of fields to compare.</param>
    /// <param name="values">Values to compare against.</param>
    /// <returns>The first match, if any; otherwise null.</returns>
    [Obsolete("Please use Find(Predicate<IResourceIndexEntry> Match)")]
    public abstract IResourceIndexEntry? Find(string[] names, TypedValue[] values);

    /// <summary>Searches the package for the first resource matching <paramref name="Match"/>.</summary>
    /// <param name="Match">Predicate defining matching conditions.</param>
    /// <returns>The first matching (non-deleted) entry, if any; otherwise null.</returns>
    public abstract IResourceIndexEntry? Find(Predicate<IResourceIndexEntry> Match);

    /// <summary>Searches the package for all resources matching <paramref name="flags"/> and <paramref name="values"/>.</summary>
    /// <param name="flags">True bits enable matching against numerically equivalent <paramref name="values"/> entry.</param>
    /// <param name="values">Fields to compare against.</param>
    /// <returns>All matching entries.</returns>
    [Obsolete("Please use FindAll(Predicate<IResourceIndexEntry> Match)")]
    public abstract List<IResourceIndexEntry> FindAll(uint flags, IResourceIndexEntry values);

    /// <summary>Searches the package for all resources matching <paramref name="names"/> and <paramref name="values"/>.</summary>
    /// <param name="names">Names of fields to compare.</param>
    /// <param name="values">Values to compare against.</param>
    /// <returns>All matching entries.</returns>
    [Obsolete("Please use FindAll(Predicate<IResourceIndexEntry> Match)")]
    public abstract List<IResourceIndexEntry> FindAll(string[] names, TypedValue[] values);

    /// <summary>Searches the package for all resources matching <paramref name="Match"/>.</summary>
    /// <param name="Match">Predicate defining matching conditions.</param>
    /// <returns>All matching (non-deleted) entries.</returns>
    public abstract List<IResourceIndexEntry> FindAll(Predicate<IResourceIndexEntry> Match);
    #endregion

    #region Package content
    /// <summary>Add a resource to the package.</summary>
    /// <param name="rk">The resource key.</param>
    /// <param name="stream">The stream that contains the resource data (null for an empty resource).</param>
    /// <param name="rejectDups">If true, fail if the resource key already exists.</param>
    /// <returns>Null if rejectDups and the resource key exists; else the new IResourceIndexEntry.</returns>
    public abstract IResourceIndexEntry? AddResource(IResourceKey rk, Stream? stream, bool rejectDups);

    /// <summary>Replace the data for the resource indexed by <paramref name="rc"/> with the data from <paramref name="res"/>.</summary>
    /// <param name="rc">Target resource index.</param>
    /// <param name="res">Source resource.</param>
    public abstract void ReplaceResource(IResourceIndexEntry rc, IResource res);

    /// <summary>Delete the resource indexed by <paramref name="rc"/>.</summary>
    /// <param name="rc">Target resource index.</param>
    public abstract void DeleteResource(IResourceIndexEntry rc);
    #endregion

    #region IDisposable
    /// <summary>Releases the package and its backing stream.</summary>
    public abstract void Dispose();
    #endregion

    #endregion

    // Static so cannot be defined on the interface; the concrete Package class hides these.

    /// <summary>Initialise a new, empty package (implemented by <see cref="s4pi.Package.Package"/>).</summary>
    /// <param name="APIversion">The requested API version.</param>
    /// <returns>An empty package.</returns>
    public static IPackage NewPackage(int APIversion) => throw new NotImplementedException();

    /// <summary>Initialise a new, empty package (implemented by <see cref="s4pi.Package.Package"/>).</summary>
    /// <param name="APIversion">The requested API version.</param>
    /// <param name="major">The major version of the package format.</param>
    /// <returns>An empty package.</returns>
    public static IPackage NewPackage(int APIversion, int major) => throw new NotImplementedException();

    /// <summary>Open an existing package by filename, read only (implemented by <see cref="s4pi.Package.Package"/>).</summary>
    /// <param name="APIversion">The requested API version.</param>
    /// <param name="packagePath">The fully qualified filename of the package.</param>
    /// <returns>The package.</returns>
    public static IPackage OpenPackage(int APIversion, string packagePath) => throw new NotImplementedException();

    /// <summary>Open an existing package by filename, optionally read/write (implemented by <see cref="s4pi.Package.Package"/>).</summary>
    /// <param name="APIversion">The requested API version.</param>
    /// <param name="packagePath">The fully qualified filename of the package.</param>
    /// <param name="readwrite">True to open for update.</param>
    /// <returns>The package.</returns>
    public static IPackage OpenPackage(int APIversion, string packagePath, bool readwrite) => throw new NotImplementedException();

    /// <summary>Releases any internal references associated with the given package (implemented by <see cref="s4pi.Package.Package"/>).</summary>
    /// <param name="APIversion">The requested API version.</param>
    /// <param name="pkg">The package to close.</param>
    public static void ClosePackage(int APIversion, IPackage pkg) => throw new NotImplementedException();

    // Required by API, not user tools

    /// <summary>
    /// Required internally by s4pi - <b>not</b> for use in user tools.
    /// Please use <c>WrapperDealer.GetResource(int, IPackage, IResourceIndexEntry)</c> instead.
    /// </summary>
    /// <param name="rie">IResourceIndexEntry of resource.</param>
    /// <returns>The resource data (uncompressed), or null for a resource without data.</returns>
    public abstract Stream? GetResource(IResourceIndexEntry rie);

    /// <summary>
    /// Used to indicate a resource index has been invalidated.
    /// </summary>
    /// <param name="sender">Object causing event to be raised.</param>
    /// <param name="e">Event arguments object.</param>
    protected virtual void OnResourceIndexInvalidated(object? sender, EventArgs e) => ResourceIndexInvalidated?.Invoke(sender, e);
}
