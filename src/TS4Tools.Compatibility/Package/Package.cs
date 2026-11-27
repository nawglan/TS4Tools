// Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs lines 27-676

using s4pi.Interfaces;
using CoreEntry = TS4Tools.IResourceIndexEntry;
using DbpfPackage = TS4Tools.Package.DbpfPackage;

namespace s4pi.Package;

/// <summary>
/// Implementation of a package: the legacy s4pi API over a TS4Tools.Core <see cref="DbpfPackage"/>.
/// </summary>
/// <remarks>
/// <para>Parsing, compression and saving are done by <see cref="DbpfPackage"/>; this class only adapts the
/// API. Differences from legacy, all at the API boundary:</para>
/// <list type="bullet">
/// <item>Resource data passed to <see cref="AddResource"/> / <see cref="ReplaceResource"/> is copied when the call is made
/// (legacy kept the caller's stream and read it on save).</item>
/// <item><see cref="GetResourceList"/> returns a new list of entry views on each call; the views are stable
/// (the same object for the same entry) until the index is invalidated by <see cref="SavePackage"/>.</item>
/// <item>Header/format errors are reported as <see cref="InvalidDataException"/> as in legacy, with the TS4Tools
/// exception as the inner exception.</item>
/// </list>
/// </remarks>
public class Package : APackage
{
    private const int recommendedApiVersion = 1;

    // Source: Package.cs line 594
    private static readonly int[] majors = [2];

    private DbpfPackage? core;
    private readonly bool writable;
    private readonly bool hasFile;
    private readonly ConditionalWeakTable<CoreEntry, ResourceIndexEntry> views = new();
    private bool disposed;

    #region AApiVersionedFields
    /// <summary>The best supported version of the API available.</summary>
    public override int RecommendedApiVersion => recommendedApiVersion;

    // No ContentFields override as we don't want to make anything more public than APackage provides
    #endregion

    #region Construction

    private Package(int requestedVersion, DbpfPackage core, bool writable, bool hasFile)
    {
        requestedApiVersion = requestedVersion;
        this.core = core;
        this.writable = writable;
        this.hasFile = hasFile;
        core.ResourceIndexInvalidated += OnCoreResourceIndexInvalidated;
    }

    private DbpfPackage Core
    {
        get
        {
            ObjectDisposedException.ThrowIf(disposed || core == null, this);
            return core!;
        }
    }

    /// <summary>
    /// Gets the TS4Tools.Core package behind this legacy package, for code that mixes the two APIs.
    /// </summary>
    /// <returns>The underlying <see cref="DbpfPackage"/>.</returns>
    public DbpfPackage GetNativePackage() => Core;

    private void OnCoreResourceIndexInvalidated(object? sender, EventArgs e) => OnResourceIndexInvalidated(this, e);

    #endregion

    #region APackage

    #region Whole package

    /// <summary>
    /// Save the package to its original file.
    /// </summary>
    /// <exception cref="InvalidOperationException">The package has no file (a new package) or was opened read-only.</exception>
    /// <remarks>Source: Package.cs lines 54-88 (in-place save, then the index is re-read)</remarks>
    [MinimumVersion(1)]
    [MaximumVersion(recommendedApiVersion)]
    public override void SavePackage()
    {
        var pkg = Core;
        if (!hasFile)
            throw new InvalidOperationException("Package has no stream to save to");
        if (!writable)
            throw new InvalidOperationException("Package is read-only");

        Run(pkg.SaveAsync());
    }

    /// <summary>
    /// Save the package to a given stream.
    /// </summary>
    /// <param name="s">Stream to save to.</param>
    /// <remarks>Source: Package.cs lines 96-147. Like legacy, the package itself is unchanged (still dirty).</remarks>
    [MinimumVersion(1)]
    [MaximumVersion(recommendedApiVersion)]
    public override void SaveAs(Stream s)
    {
        ArgumentNullException.ThrowIfNull(s);
        Run(Core.SaveToStreamAsync(s));
    }

    /// <summary>
    /// Save the package to a given file.
    /// </summary>
    /// <param name="path">File to save to - will be overwritten or created.</param>
    /// <remarks>Source: Package.cs lines 155-160</remarks>
    [MinimumVersion(1)]
    [MaximumVersion(recommendedApiVersion)]
    public override void SaveAs(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        Run(Core.SaveAsAsync(path));
    }

    /// <summary>
    /// Initialise a new, empty package and return the IPackage reference.
    /// </summary>
    /// <param name="APIversion">The requested API version.</param>
    /// <returns>IPackage reference to an empty package.</returns>
    public static new IPackage NewPackage(int APIversion) => NewPackage(APIversion, 2);

    /// <summary>
    /// Initialise a new, empty package and return the IPackage reference.
    /// </summary>
    /// <param name="APIversion">The requested API version.</param>
    /// <param name="major">Major version of the package format (only 2 is supported).</param>
    /// <returns>IPackage reference to an empty package.</returns>
    /// <exception cref="InvalidDataException"><paramref name="major"/> is not supported.</exception>
    /// <remarks>Source: Package.cs lines 169-183, 527-545</remarks>
    public static new IPackage NewPackage(int APIversion, int major)
    {
        if (!majors.Contains(major))
            throw new InvalidDataException("Expected major version(s) '" + string.Join(", ", majors) + "'.  Found '" + major.ToString(CultureInfo.InvariantCulture) + "'.");
        return new Package(APIversion, DbpfPackage.CreateNew(major), writable: true, hasFile: false);
    }

    /// <summary>
    /// Open an existing package by filename, read only.
    /// </summary>
    /// <param name="APIversion">The requested API version.</param>
    /// <param name="packagePath">The fully qualified filename of the package.</param>
    /// <returns>IPackage reference to an existing package on disk.</returns>
    /// <remarks>The package can be edited in memory and saved with <see cref="SaveAs(string)"/>, but not with <see cref="SavePackage"/>.</remarks>
    public static new IPackage OpenPackage(int APIversion, string packagePath) => OpenPackage(APIversion, packagePath, false);

    /// <summary>
    /// Open an existing package by filename, optionally readwrite.
    /// </summary>
    /// <param name="APIversion">The requested API version.</param>
    /// <param name="packagePath">The fully qualified filename of the package.</param>
    /// <param name="readwrite">True to indicate read/write access required.</param>
    /// <returns>IPackage reference to an existing package on disk.</returns>
    /// <exception cref="InvalidDataException">The file is not a valid package.</exception>
    /// <remarks>Source: Package.cs lines 237-240 (FileShare.ReadWrite), 547-554 (header check)</remarks>
    public static new IPackage OpenPackage(int APIversion, string packagePath, bool readwrite)
    {
        ArgumentNullException.ThrowIfNull(packagePath);
        try
        {
            if (readwrite)
                return new Package(APIversion, Run(DbpfPackage.OpenAsync(packagePath, readWrite: true)), writable: true, hasFile: true);

            var fs = new FileStream(packagePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var stream = new ReadOnlyPackageStream(fs);
            try
            {
                return new Package(APIversion, Run(DbpfPackage.OpenAsync(stream, leaveOpen: false)), writable: false, hasFile: true);
            }
            catch
            {
                stream.Dispose();
                throw;
            }
        }
        catch (global::TS4Tools.PackageFormatException ex)
        {
            throw new InvalidDataException(ex.Message, ex);
        }
    }

    /// <summary>
    /// Releases any internal references associated with the given package.
    /// </summary>
    /// <param name="APIversion">The requested API version.</param>
    /// <param name="pkg">IPackage reference to close.</param>
    [Obsolete("Package.ClosePackage is deprecated, please use using statement or Dispose instead")]
    public static new void ClosePackage(int APIversion, IPackage pkg)
    {
        if (pkg is Package p) p.Dispose();
    }

    #endregion

    #region Package header

    /// <summary>Package header: "DBPF" bytes.</summary>
    [ElementPriority(1)] public override byte[] Magic => Core.Magic.ToArray();
    /// <summary>Package header: 0x00000002.</summary>
    [ElementPriority(2)] public override int Major => Core.MajorVersion;
    /// <summary>Package header: 0x00000001.</summary>
    [ElementPriority(3)] public override int Minor => Core.MinorVersion;
    /// <summary>Package header: user version major.</summary>
    [ElementPriority(4)] public override int UserVersionMajor => Core.UserVersionMajor;
    /// <summary>Package header: user version minor.</summary>
    [ElementPriority(4)] public override int UserVersionMinor => Core.UserVersionMinor;
    /// <summary>Package header: unused.</summary>
    [ElementPriority(4)] public override int Unused1 => Core.Unused1;
    /// <summary>Package header: typically, not set.</summary>
    [ElementPriority(4)] public override int CreationTime => Core.CreationTime;
    /// <summary>Package header: typically, not set.</summary>
    [ElementPriority(4)] public override int UpdatedTime => Core.UpdatedTime;
    /// <summary>Package header: unused.</summary>
    [ElementPriority(4)] public override int Unused2 => Core.Unused2;
    /// <summary>Package header: number of entries in the package index.</summary>
    [ElementPriority(5)] public override int Indexcount => Core.HeaderIndexCount;
    /// <summary>Package header: index position in file (low).</summary>
    [ElementPriority(6)] public override int IndexRecordPositionLow => Core.IndexRecordPositionLow;
    /// <summary>Package header: index size on disk in bytes.</summary>
    [ElementPriority(7)] public override int Indexsize => Core.HeaderIndexSize;
    /// <summary>Package header: unused.</summary>
    [ElementPriority(8)] public override int Unused3 => Core.Unused3;
    /// <summary>Package header: unused (always 3).</summary>
    [ElementPriority(9)] public override int Unused4 => Core.Unused4;
    /// <summary>Package header: index position in file (falls back to the low position when zero).</summary>
    [ElementPriority(10)] public override int Indexposition => Core.HeaderIndexPosition;
    /// <summary>Package header: unused.</summary>
    [ElementPriority(11)] public override byte[] Unused5 => Core.Unused5.ToArray();

    /// <summary>A MemoryStream covering the package header bytes (not implemented, as in legacy).</summary>
    /// <exception cref="NotImplementedException">Always.</exception>
    [ElementPriority(12)] public override Stream HeaderStream => throw new NotImplementedException();

    #endregion

    #region Package index

    /// <summary>Package index: the index format in use.</summary>
    [ElementPriority(13)] public override uint Indextype => Core.IndexType;

    /// <summary>Package index: the index entries, including those marked deleted.</summary>
    [ElementPriority(14)] public override List<IResourceIndexEntry> GetResourceList => Index.ToList();

    private IEnumerable<IResourceIndexEntry> Index => Core.Resources.Select(View);

    private ResourceIndexEntry View(CoreEntry entry) => views.GetValue(entry, e => new ResourceIndexEntry(e, Core.IndexType));

    private static CoreEntry CoreOf(IResourceIndexEntry rie) => rie is ResourceIndexEntry { IsDetached: false } view
        ? view.Core
        : throw new ArgumentException("The index entry does not belong to this package.", nameof(rie));

    // Source: Package.cs lines 360-373
    private static bool FlagMatch(uint flags, IResourceIndexEntry values, IResourceIndexEntry target)
    {
        if (flags == 0) return true;

        for (int i = 0; i < sizeof(uint); i++)
        {
            uint j = (uint)(1 << i);
            if ((flags & j) == 0) continue;
            string f = values.ContentFields[i];
            if (!target.ContentFields.Contains(f)) return false;
            if (!values[f].Equals(target[f])) return false;
        }
        return true;
    }

    // Source: Package.cs lines 375-379
    private static bool NameMatch(string[] names, TypedValue[] values, IResourceIndexEntry target)
    {
        for (int i = 0; i < names.Length; i++)
            if (!target.ContentFields.Contains(names[i]) || !values[i].Equals(target[names[i]])) return false;
        return true;
    }

    /// <inheritdoc />
    [MinimumVersion(1)]
    [MaximumVersion(recommendedApiVersion)]
    [Obsolete("Please use Find(Predicate<IResourceIndexEntry> Match)")]
    public override IResourceIndexEntry? Find(uint flags, IResourceIndexEntry values) => Index.FirstOrDefault(x => !x.IsDeleted && FlagMatch(flags, values, x));

    /// <inheritdoc />
    [MinimumVersion(1)]
    [MaximumVersion(recommendedApiVersion)]
    [Obsolete("Please use Find(Predicate<IResourceIndexEntry> Match)")]
    public override IResourceIndexEntry? Find(string[] names, TypedValue[] values) => Index.FirstOrDefault(x => !x.IsDeleted && NameMatch(names, values, x));

    /// <inheritdoc />
    [MinimumVersion(1)]
    [MaximumVersion(recommendedApiVersion)]
    public override IResourceIndexEntry? Find(Predicate<IResourceIndexEntry> Match)
    {
        ArgumentNullException.ThrowIfNull(Match);
        return Index.FirstOrDefault(x => !x.IsDeleted && Match(x));
    }

    /// <inheritdoc />
    [MinimumVersion(1)]
    [MaximumVersion(recommendedApiVersion)]
    [Obsolete("Please use FindAll(Predicate<IResourceIndexEntry> Match)")]
    public override List<IResourceIndexEntry> FindAll(uint flags, IResourceIndexEntry values) => Index.Where(x => !x.IsDeleted && FlagMatch(flags, values, x)).ToList();

    /// <inheritdoc />
    [MinimumVersion(1)]
    [MaximumVersion(recommendedApiVersion)]
    [Obsolete("Please use FindAll(Predicate<IResourceIndexEntry> Match)")]
    public override List<IResourceIndexEntry> FindAll(string[] names, TypedValue[] values) => Index.Where(x => !x.IsDeleted && NameMatch(names, values, x)).ToList();

    /// <inheritdoc />
    [MinimumVersion(1)]
    [MaximumVersion(recommendedApiVersion)]
    public override List<IResourceIndexEntry> FindAll(Predicate<IResourceIndexEntry> Match)
    {
        ArgumentNullException.ThrowIfNull(Match);
        return Index.Where(x => !x.IsDeleted && Match(x)).ToList();
    }

    #endregion

    #region Package content

    /// <summary>
    /// Add a resource to the package.
    /// </summary>
    /// <param name="rk">The resource key.</param>
    /// <param name="stream">The stream that contains the resource data (read in full from position 0), or null for an empty resource.</param>
    /// <param name="rejectDups">If true, fail if the resource key already exists (ignoring deleted entries).</param>
    /// <returns>Null if rejectDups and the resource key exists; else the new IResourceIndexEntry.</returns>
    /// <remarks>Source: Package.cs lines 464-471. New entries are stored uncompressed unless <see cref="IResourceIndexEntry.Compressed"/> is set.</remarks>
    public override IResourceIndexEntry? AddResource(IResourceKey rk, Stream? stream, bool rejectDups)
    {
        ArgumentNullException.ThrowIfNull(rk);
        var pkg = Core;
        var key = new global::TS4Tools.ResourceKey(rk.ResourceType, rk.ResourceGroup, rk.Instance);
        if (rejectDups && pkg.Find(key) != null) return null;

        var data = stream == null ? [] : LegacyBinary.ReadAllFromStart(stream);
        var entry = pkg.AddResource(key, data, rejectDuplicates: false)!;
        var view = View(entry);
        view.HasNoData = stream == null;
        return view;
    }

    /// <summary>
    /// Replace the data for the resource indexed by <paramref name="rc"/> with the data from <paramref name="res"/>.
    /// </summary>
    /// <param name="rc">Target resource index.</param>
    /// <param name="res">Source resource (its <see cref="IResource.Stream"/> is copied).</param>
    /// <remarks>Source: Package.cs line 478</remarks>
    public override void ReplaceResource(IResourceIndexEntry rc, IResource res)
    {
        ArgumentNullException.ThrowIfNull(rc);
        ArgumentNullException.ThrowIfNull(res);
        var entry = CoreOf(rc);
        Core.ReplaceResource(entry, LegacyBinary.ReadAllFromStart(res.Stream));
        ((ResourceIndexEntry)rc).HasNoData = false;
    }

    /// <summary>
    /// Tell the package to delete the resource indexed by <paramref name="rc"/>.
    /// </summary>
    /// <param name="rc">Target resource index.</param>
    /// <remarks>Source: Package.cs lines 483-487</remarks>
    public override void DeleteResource(IResourceIndexEntry rc)
    {
        ArgumentNullException.ThrowIfNull(rc);
        if (!rc.IsDeleted)
            Core.DeleteResource(CoreOf(rc));
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Releases the package and closes its file.
    /// </summary>
    /// <param name="disposing">True when called from <see cref="Dispose()"/>.</param>
    protected void Dispose(bool disposing)
    {
        if (disposed) return;
        if (disposing && core != null)
        {
            core.ResourceIndexInvalidated -= OnCoreResourceIndexInvalidated;
            core.Dispose();
            core = null;
        }
        disposed = true;
    }

    /// <summary>
    /// Releases the package and closes its file.
    /// </summary>
    public override void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion

    #endregion

    // Required by API, not user tools

    /// <summary>
    /// Required internally by s4pi - <b>not</b> for use in user tools.
    /// Please use <c>WrapperDealer.GetResource(int, IPackage, IResourceIndexEntry)</c> instead.
    /// </summary>
    /// <param name="rie">IResourceIndexEntry of resource.</param>
    /// <returns>A new <see cref="MemoryStream"/> with the (uncompressed) resource data, or null if the entry is not from
    /// this package, was added without data, or is a deleted-record marker.</returns>
    /// <remarks>Source: Package.cs lines 649-673</remarks>
    public override Stream? GetResource(IResourceIndexEntry rie)
    {
        if (rie is not ResourceIndexEntry view) return null;
        if (view.HasNoData) return null;
        if (view.DataSource.FileSize == 1 && view.DataSource.MemorySize == 0xFFFFFFFF) return null;

        var data = Run(Core.GetResourceDataAsync(view.DataSource));
        var ms = new MemoryStream();
        ms.Write(data.Span);
        ms.Position = 0;
        return ms;
    }

    #region Synchronous bridge

    // The legacy API is synchronous. TS4Tools.Core awaits with ConfigureAwait(false) throughout, so blocking
    // here cannot deadlock on a synchronisation context.
    private static T Run<T>(ValueTask<T> task) => task.IsCompletedSuccessfully ? task.Result : task.AsTask().GetAwaiter().GetResult();

    private static T Run<T>(Task<T> task) => task.GetAwaiter().GetResult();

    private static void Run(ValueTask task)
    {
        if (task.IsCompletedSuccessfully) return;
        task.AsTask().GetAwaiter().GetResult();
    }

    #endregion
}
