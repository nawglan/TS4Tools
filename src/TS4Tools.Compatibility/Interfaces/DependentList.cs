// Source: legacy_references/Sims4Tools/s4pi/Interfaces/DependentList.cs lines 26-731

namespace s4pi.Interfaces;

/// <summary>
/// Abstract extension to <see cref="AHandlerList{T}"/> adding support for <see cref="System.IO.Stream"/> IO
/// and partially implementing <see cref="IGenericAdd"/>.
/// </summary>
/// <typeparam name="T"><see cref="Type"/> of list element.</typeparam>
/// <remarks>
/// The list's change handler (<c>handler</c>) is raised when the list changes; each element is given
/// <see cref="elementHandler"/>, which is raised when an element changes. Lists owned by a resource usually
/// pass the resource's <c>OnResourceChanged</c> as both, so any change marks the resource dirty.
/// </remarks>
public abstract class DependentList<T> : AHandlerList<T>, IGenericAdd
    where T : AHandlerElement, IEquatable<T>
{
    /// <summary>
    /// Holds the <see cref="EventHandler"/> delegate to invoke if an element in the <see cref="DependentList{T}"/> changes.
    /// </summary>
    /// <remarks>Work around for list event handler triggering during stream constructor and other places.</remarks>
    protected EventHandler? elementHandler;

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="DependentList{T}"/> class that is empty.
    /// </summary>
    /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
    /// <param name="maxSize">Optional; -1 for unlimited size, otherwise the maximum number of elements in the list.</param>
    protected DependentList(EventHandler? handler, long maxSize = -1) : base(handler, maxSize) { }

    /// <summary>
    /// Initializes a new instance filled with clones of the content of <paramref name="ilt"/>.
    /// </summary>
    /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
    /// <param name="ilt">The initial content of the list.</param>
    /// <param name="maxSize">Optional; -1 for unlimited size, otherwise the maximum number of elements in the list.</param>
    /// <remarks>Source: DependentList.cs lines 70-79</remarks>
    protected DependentList(EventHandler? handler, IEnumerable<T> ilt, long maxSize = -1) : base(null, maxSize)
    {
        ArgumentNullException.ThrowIfNull(ilt);
        elementHandler = handler;
        foreach (var t in ilt)
            Add((T)t.Clone(null));
        this.handler = handler;
    }

    /// <summary>
    /// Initializes a new instance filled from <see cref="System.IO.Stream"/> <paramref name="s"/>.
    /// </summary>
    /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
    /// <param name="s">The <see cref="System.IO.Stream"/> to read for the initial content of the list.</param>
    /// <param name="maxSize">Optional; -1 for unlimited size, otherwise the maximum number of elements in the list.</param>
    protected DependentList(EventHandler? handler, Stream s, long maxSize = -1) : base(null, maxSize)
    {
        elementHandler = handler;
        Parse(s);
        this.handler = handler;
    }

    #endregion

    #region Data I/O

    /// <summary>
    /// Read list entries from a stream.
    /// </summary>
    /// <param name="s">Stream containing list entries.</param>
    /// <remarks>
    /// Source: DependentList.cs lines 108-117. <see cref="CreateElement(Stream, out bool)"/> may return
    /// <c>inc = false</c> to not count the element. When the list has a maximum size, a count larger than it is
    /// rejected up front (legacy would fail on the first element past the limit).
    /// </remarks>
    protected virtual void Parse(Stream s)
    {
        Clear();
        var inc = true;
        int count = ReadCount(s);
        if (maxSize >= 0 && count > maxSize)
            throw new InvalidDataException($"List count {count} exceeds maximum size {maxSize}.");
        for (var i = count; i > 0; i -= inc ? 1 : 0)
            base.Add(CreateElement(s, out inc));
    }

    /// <summary>
    /// Return the number of elements to be created.
    /// </summary>
    /// <param name="s"><see cref="System.IO.Stream"/> being processed.</param>
    /// <returns>The number of elements to be created.</returns>
    protected virtual int ReadCount(Stream s) => LegacyBinary.ReadInt32(s);

    /// <summary>
    /// Create a new element from the <see cref="System.IO.Stream"/>.
    /// </summary>
    /// <param name="s">Stream containing element data.</param>
    /// <returns>A new element.</returns>
    protected abstract T CreateElement(Stream s);

    /// <summary>
    /// Create a new element from the <see cref="System.IO.Stream"/> and indicates whether it counts towards the number of elements to be created.
    /// </summary>
    /// <param name="s">Stream containing element data.</param>
    /// <param name="inc">Whether this call towards the number of elements to be created.</param>
    /// <returns>A new element.</returns>
    protected virtual T CreateElement(Stream s, out bool inc)
    {
        inc = true;
        return CreateElement(s);
    }

    /// <summary>
    /// Write list entries to a stream.
    /// </summary>
    /// <param name="s">Stream to receive list entries.</param>
    public virtual void UnParse(Stream s)
    {
        WriteCount(s, Count);
        foreach (var element in this)
            WriteElement(s, element);
    }

    /// <summary>
    /// Write the count of list elements to the stream.
    /// </summary>
    /// <param name="s"><see cref="System.IO.Stream"/> to write <paramref name="count"/> to.</param>
    /// <param name="count">Value to write to <see cref="System.IO.Stream"/> <paramref name="s"/>.</param>
    protected virtual void WriteCount(Stream s, int count) => LegacyBinary.Write(s, count);

    /// <summary>
    /// Write an element to the stream.
    /// </summary>
    /// <param name="s"><see cref="System.IO.Stream"/> to write <paramref name="element"/> to.</param>
    /// <param name="element">Value to write to <see cref="System.IO.Stream"/> <paramref name="s"/>.</param>
    protected abstract void WriteElement(Stream s, T element);

    #endregion

    #region IGenericAdd

    /// <summary>
    /// Add a default element to a <see cref="DependentList{T}"/>.
    /// </summary>
    /// <exception cref="NotImplementedException"><typeparamref name="T"/> is abstract.</exception>
    /// <exception cref="InvalidOperationException">Thrown when list size exceeded.</exception>
    public virtual void Add()
    {
        if (typeof(T).IsAbstract)
            throw new NotImplementedException();

        Add(typeof(T));
    }

    /// <summary>
    /// Adds an entry to a <see cref="DependentList{T}"/>, setting the element change handler.
    /// </summary>
    /// <param name="newElement">An instance of type <typeparamref name="T"/> to add to the list.</param>
    /// <exception cref="InvalidOperationException">Thrown when list size exceeded.</exception>
    /// <remarks>Source: DependentList.cs lines 285-289</remarks>
    public override void Add(T newElement)
    {
        ArgumentNullException.ThrowIfNull(newElement);
        newElement.SetHandler(elementHandler);
        base.Add(newElement);
    }

    /// <summary>
    /// Add a default element of the concrete <paramref name="elementType"/> to the list.
    /// </summary>
    /// <param name="elementType">The type of the new element (with an <c>(int, EventHandler)</c> constructor).</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="elementType"/> is abstract or not a <typeparamref name="T"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when list size exceeded.</exception>
    /// <remarks>Source: DependentList.cs lines 302-316</remarks>
    public virtual void Add(Type elementType)
    {
        ArgumentNullException.ThrowIfNull(elementType);
        if (elementType.IsAbstract)
            throw new ArgumentException("Must pass a concrete element type.", nameof(elementType));

        if (!typeof(T).IsAssignableFrom(elementType))
            throw new ArgumentException("The element type must belong to the generic type of the list.", nameof(elementType));

        var newElement = (T)Activator.CreateInstance(elementType, 0, elementHandler)!;
        base.Add(newElement);
    }

    /// <summary>
    /// Return the type to get the constructor from, for the given set of fields (deprecated).
    /// </summary>
    /// <param name="fields">Constructor parameters.</param>
    /// <returns>Class on which to invoke constructor.</returns>
    [Obsolete("Use Add(T instance) or Add(Type concrete-of-T).")]
    protected virtual Type GetElementType(params object[] fields) => throw new NotImplementedException();

    #endregion
}

/// <summary>
/// A TGIBlock list class where the count of elements is separate from the stored list.
/// </summary>
public class CountedTGIBlockList : DependentList<TGIBlock>
{
    private readonly int origCount; // count at the time the list was constructed, used to Parse() list from stream
    private readonly string order = "TGI";

    #region Constructors

    /// <summary>
    /// Initializes a new empty list of "TGI" order blocks.
    /// </summary>
    /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
    /// <param name="size">Optional; -1 for unlimited size, otherwise the maximum number of elements in the list.</param>
    public CountedTGIBlockList(EventHandler? handler, long size = -1) : this(handler, "TGI", size) { }

    /// <summary>
    /// Initializes a new list of "TGI" order blocks filled with copies of <paramref name="ilt"/>.
    /// </summary>
    /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
    /// <param name="ilt">The initial content of the list.</param>
    /// <param name="size">Optional; -1 for unlimited size, otherwise the maximum number of elements in the list.</param>
    public CountedTGIBlockList(EventHandler? handler, IEnumerable<TGIBlock> ilt, long size = -1) : this(handler, "TGI", ilt, size) { }

    /// <summary>
    /// Initializes a new list of <paramref name="count"/> "TGI" order blocks read from <paramref name="s"/>.
    /// </summary>
    /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
    /// <param name="count">The number of list elements to read.</param>
    /// <param name="s">The <see cref="System.IO.Stream"/> to read for the initial content of the list.</param>
    /// <param name="size">Optional; -1 for unlimited size, otherwise the maximum number of elements in the list.</param>
    public CountedTGIBlockList(EventHandler? handler, int count, Stream s, long size = -1) : this(handler, "TGI", count, s, size) { }

    /// <summary>
    /// Initializes a new empty list of <paramref name="order"/> blocks.
    /// </summary>
    /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
    /// <param name="order">The <see cref="TGIBlock.Order"/> of the elements.</param>
    /// <param name="size">Optional; -1 for unlimited size, otherwise the maximum number of elements in the list.</param>
    public CountedTGIBlockList(EventHandler? handler, TGIBlock.Order order, long size = -1) : this(handler, "" + order, size) { }

    /// <summary>
    /// Initializes a new list of <paramref name="order"/> blocks filled with copies of <paramref name="ilt"/>.
    /// </summary>
    /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
    /// <param name="order">The <see cref="TGIBlock.Order"/> of the elements.</param>
    /// <param name="ilt">The initial content of the list.</param>
    /// <param name="size">Optional; -1 for unlimited size, otherwise the maximum number of elements in the list.</param>
    public CountedTGIBlockList(EventHandler? handler, TGIBlock.Order order, IEnumerable<TGIBlock> ilt, long size = -1) : this(handler, "" + order, ilt, size) { }

    /// <summary>
    /// Initializes a new list of <paramref name="count"/> <paramref name="order"/> blocks read from <paramref name="s"/>.
    /// </summary>
    /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
    /// <param name="order">The <see cref="TGIBlock.Order"/> of the elements.</param>
    /// <param name="count">The number of list elements to read.</param>
    /// <param name="s">The <see cref="System.IO.Stream"/> to read for the initial content of the list.</param>
    /// <param name="size">Optional; -1 for unlimited size, otherwise the maximum number of elements in the list.</param>
    public CountedTGIBlockList(EventHandler? handler, TGIBlock.Order order, int count, Stream s, long size = -1) : this(handler, "" + order, count, s, size) { }

    /// <summary>
    /// Initializes a new empty list of <paramref name="order"/> blocks.
    /// </summary>
    /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
    /// <param name="order">The order of the elements ("TGI", "ITG", ...).</param>
    /// <param name="size">Optional; -1 for unlimited size, otherwise the maximum number of elements in the list.</param>
    public CountedTGIBlockList(EventHandler? handler, string order, long size = -1) : base(handler, size) { this.order = order; }

    /// <summary>
    /// Initializes a new list of <paramref name="order"/> blocks filled with copies of <paramref name="ilt"/>.
    /// </summary>
    /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
    /// <param name="order">The order of the elements ("TGI", "ITG", ...).</param>
    /// <param name="ilt">The initial content of the list.</param>
    /// <param name="size">Optional; -1 for unlimited size, otherwise the maximum number of elements in the list.</param>
    public CountedTGIBlockList(EventHandler? handler, string order, IEnumerable<TGIBlock> ilt, long size = -1) : base(handler, ilt, size) { this.order = order; }

    /// <summary>
    /// Initializes a new list of <paramref name="count"/> <paramref name="order"/> blocks read from <paramref name="s"/>.
    /// </summary>
    /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
    /// <param name="order">The order of the elements ("TGI", "ITG", ...).</param>
    /// <param name="count">The number of list elements to read.</param>
    /// <param name="s">The <see cref="System.IO.Stream"/> to read for the initial content of the list.</param>
    /// <param name="size">Optional; -1 for unlimited size, otherwise the maximum number of elements in the list.</param>
    /// <remarks>Source: DependentList.cs lines 465-473</remarks>
    public CountedTGIBlockList(EventHandler? handler, string order, int count, Stream s, long size = -1) : base(null, size)
    {
        origCount = count;
        this.order = order;
        elementHandler = handler;
        Parse(s);
        this.handler = handler;
    }

    #endregion

    #region Data I/O

    /// <summary>
    /// Create a new element from the <see cref="System.IO.Stream"/>.
    /// </summary>
    /// <param name="s">Stream containing element data.</param>
    /// <returns>A new element.</returns>
    protected override TGIBlock CreateElement(Stream s) => new(0, elementHandler, order, s);

    /// <summary>
    /// Write an element to the stream.
    /// </summary>
    /// <param name="s">Stream to receive the element.</param>
    /// <param name="element">The element to write.</param>
    protected override void WriteElement(Stream s, TGIBlock element)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.UnParse(s);
    }

    /// <summary>
    /// Return the number of elements to be created (the count given at construction).
    /// </summary>
    /// <param name="s">Ignored.</param>
    /// <returns>The construction-time count.</returns>
    protected override int ReadCount(Stream s) => origCount;

    /// <summary>
    /// Does nothing: the count is stored separately from the list.
    /// </summary>
    /// <param name="s">Ignored.</param>
    /// <param name="count">Ignored.</param>
    protected override void WriteCount(Stream s, int count) { }

    #endregion

    /// <summary>
    /// Add a new default element to the list.
    /// </summary>
    public override void Add() => base.Add(new TGIBlock(0, elementHandler, order));

    /// <summary>
    /// Adds a copy of <paramref name="item"/> (in this list's order) to the end of the list.
    /// </summary>
    /// <param name="item">The object to add.</param>
    public override void Add(TGIBlock item) => base.Add(new TGIBlock(0, elementHandler, order, item));

    /// <summary>
    /// Adds a new TGIBlock (in this list's order) with the values of <paramref name="rk"/> to the end of the list.
    /// </summary>
    /// <param name="rk">The key to add.</param>
    public void Add(IResourceKey rk) => base.Add(new TGIBlock(0, elementHandler, order, rk));

    /// <summary>
    /// Inserts a copy of <paramref name="item"/> (in this list's order) at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
    /// <param name="item">The object to insert.</param>
    public override void Insert(int index, TGIBlock item) => base.Insert(index, new TGIBlock(0, elementHandler, order, item));
}

/// <summary>
/// A TGIBlock list class where the count and size of the list are stored separately (but managed by this class).
/// </summary>
public class TGIBlockList : DependentList<TGIBlock>
{
    private readonly bool addEight;
    private readonly bool ignoreTgiSize;

    #region Constructors

    /// <summary>
    /// Initializes a new empty list.
    /// </summary>
    /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
    /// <param name="addEight">When true, invoke a fudge factor in parse/unparse (8 is added to the stored size).</param>
    /// <param name="ignoreTgiSize">When true, the stored size is not checked.</param>
    public TGIBlockList(EventHandler? handler, bool addEight = false, bool ignoreTgiSize = false) : base(handler)
    {
        this.addEight = addEight;
        this.ignoreTgiSize = ignoreTgiSize;
    }

    /// <summary>
    /// Initializes a new list filled with copies of <paramref name="ilt"/>.
    /// </summary>
    /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
    /// <param name="ilt">The initial content of the list.</param>
    /// <param name="addEight">When true, invoke a fudge factor in parse/unparse.</param>
    /// <param name="ignoreTgiSize">When true, the stored size is not checked.</param>
    public TGIBlockList(EventHandler? handler, IEnumerable<TGIBlock> ilt, bool addEight = false, bool ignoreTgiSize = false) : base(handler, ilt)
    {
        this.addEight = addEight;
        this.ignoreTgiSize = ignoreTgiSize;
    }

    /// <summary>
    /// Initializes a new list read from <paramref name="s"/> at <paramref name="tgiPosn"/>.
    /// </summary>
    /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
    /// <param name="s">The <see cref="System.IO.Stream"/> to read for the initial content of the list.</param>
    /// <param name="tgiPosn">Position in the <see cref="System.IO.Stream"/> where the list of TGIBlocks starts.</param>
    /// <param name="tgiSize">Size (in bytes) of the stored list.</param>
    /// <param name="addEight">When true, invoke a fudge factor in parse/unparse.</param>
    /// <param name="ignoreTgiSize">When true, the stored size is not checked.</param>
    public TGIBlockList(EventHandler? handler, Stream s, long tgiPosn, long tgiSize, bool addEight = false, bool ignoreTgiSize = false)
        : base(null)
    {
        elementHandler = handler;
        this.addEight = addEight;
        this.ignoreTgiSize = ignoreTgiSize;
        Parse(s, tgiPosn, tgiSize);
        this.handler = handler;
    }

    #endregion

    #region Data I/O

    /// <summary>
    /// Create a new element ("TGI" order) from the <see cref="System.IO.Stream"/>.
    /// </summary>
    /// <param name="s">Stream containing element data.</param>
    /// <returns>A new element.</returns>
    protected override TGIBlock CreateElement(Stream s) => new(0, elementHandler, s);

    /// <summary>
    /// Write an element to the stream.
    /// </summary>
    /// <param name="s">Stream to receive the element.</param>
    /// <param name="element">The element to write.</param>
    protected override void WriteElement(Stream s, TGIBlock element)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.UnParse(s);
    }

    /// <summary>
    /// Read list entries from a stream, checking position and size.
    /// </summary>
    /// <param name="s">Stream containing list entries.</param>
    /// <param name="tgiPosn">Position in the <see cref="System.IO.Stream"/> where the list of TGIBlocks starts.</param>
    /// <param name="tgiSize">Size (in bytes) of the stored list.</param>
    /// <exception cref="InvalidDataException">The stream position or the size read does not match.</exception>
    /// <remarks>Source: DependentList.cs lines 679-707 (legacy hard-codes checking on)</remarks>
    protected void Parse(Stream s, long tgiPosn, long tgiSize)
    {
        ArgumentNullException.ThrowIfNull(s);
        const bool checking = true;
        if (checking && tgiPosn != s.Position)
            throw new InvalidDataException(string.Format(CultureInfo.InvariantCulture,
                "Position of TGIBlock read: 0x{0:X8}, actual: 0x{1:X8}", tgiPosn, s.Position));

        if ((ignoreTgiSize && s.Position < s.Length) || tgiSize > 0)
            Parse(s);

        if (checking && !ignoreTgiSize && tgiSize != s.Position - tgiPosn + (addEight ? 8 : 0))
            throw new InvalidDataException(string.Format(CultureInfo.InvariantCulture,
                "Size of TGIBlock read: 0x{0:X8}, actual: 0x{1:X8}; at 0x{2:X8}", tgiSize, s.Position - tgiPosn, s.Position));
    }

    /// <summary>
    /// Write list entries to a stream, then back-patch the offset and size at <paramref name="ptgiO"/>.
    /// </summary>
    /// <param name="s">Stream to receive list entries.</param>
    /// <param name="ptgiO">Position in <see cref="System.IO.Stream"/> to write list position and size values.</param>
    /// <remarks>Source: DependentList.cs lines 714-727</remarks>
    public void UnParse(Stream s, long ptgiO)
    {
        ArgumentNullException.ThrowIfNull(s);
        var tgiPosn = s.Position;
        UnParse(s);
        var pos = s.Position;

        s.Position = ptgiO;
        using (var w = LegacyBinary.Writer(s))
        {
            w.Write((uint)(tgiPosn - ptgiO - sizeof(uint)));
            w.Write((uint)(pos - tgiPosn + (addEight ? 8 : 0)));
        }

        s.Position = pos;
    }

    #endregion
}
