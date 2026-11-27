// Source: legacy_references/Sims4Tools/s4pi/Interfaces/SimpleList.cs lines 26-1041

namespace s4pi.Interfaces;

/// <summary>
/// A simple list of a value type (such as <see cref="uint"/>), with change notification and stream I/O.
/// </summary>
/// <typeparam name="T">A simple data type (such as <see cref="uint"/>).</typeparam>
/// <remarks>
/// Elements are stored as <see cref="HandlerElement{T}"/>; the <typeparamref name="T"/>-typed members wrap
/// and unwrap them. Enumerating with <c>foreach</c> yields <typeparamref name="T"/> values.
/// </remarks>
public class SimpleList<T> : DependentList<HandlerElement<T>>, IEnumerable<T>
    where T : struct, IComparable, IConvertible, IEquatable<T>, IComparable<T>
{
    #region Attributes
    private readonly CreateElementMethod? createElement;
    private readonly WriteElementMethod? writeElement;
    private readonly ReadCountMethod? readCount;
    private readonly WriteCountMethod? writeCount;
    #endregion

    #region Enumerator<U>

    /// <summary>
    /// Supports simple iteration over a <see cref="SimpleList{T}"/> as <typeparamref name="U"/> values.
    /// </summary>
    /// <typeparam name="U">The simple data type.</typeparam>
    public class Enumerator<U> : IEnumerator<U>
        where U : struct, IComparable, IConvertible, IEquatable<U>, IComparable<U>
    {
        private DependentList<HandlerElement<U>>? list;
        private List<HandlerElement<U>>.Enumerator enumerator;

        internal Enumerator(DependentList<HandlerElement<U>> list)
        {
            this.list = list;
            Reset();
        }

        /// <summary>Gets the element at the current position of the enumerator.</summary>
        public U Current => enumerator.Current;

        /// <summary>Releases the enumerator.</summary>
        public void Dispose()
        {
            enumerator.Dispose();
            list = null;
            GC.SuppressFinalize(this);
        }

        object IEnumerator.Current => enumerator.Current; // legacy yields the element, not the value

        /// <summary>Advances the enumerator to the next element.</summary>
        /// <returns>true if the enumerator advanced; false at the end of the list.</returns>
        public bool MoveNext() => enumerator.MoveNext();

        /// <summary>Sets the enumerator to its initial position.</summary>
        public void Reset() => enumerator = list!.GetEnumerator();
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new empty list.
    /// </summary>
    /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
    /// <param name="createElement">Optional; the method to create a new element from a stream (default: default(T)).</param>
    /// <param name="writeElement">Optional; the method to write an element to a stream (default: nothing written).</param>
    /// <param name="size">Optional maximum number of elements in the list.</param>
    /// <param name="readCount">Optional; default is to read an <see cref="int"/> from the stream.</param>
    /// <param name="writeCount">Optional; default is to write an <see cref="int"/> to the stream.</param>
    public SimpleList(EventHandler? handler, CreateElementMethod? createElement = null, WriteElementMethod? writeElement = null, long size = -1, ReadCountMethod? readCount = null, WriteCountMethod? writeCount = null)
        : base(handler, size)
    {
        this.createElement = createElement;
        this.writeElement = writeElement;
        this.readCount = readCount;
        this.writeCount = writeCount;
    }

    /// <summary>
    /// Initializes a new list read from <paramref name="s"/>.
    /// </summary>
    /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
    /// <param name="s">The <see cref="System.IO.Stream"/> to read for the initial content of the list.</param>
    /// <param name="createElement">Required; the method to create a new element from a stream.</param>
    /// <param name="writeElement">Required; the method to write an element to a stream.</param>
    /// <param name="size">Optional maximum number of elements in the list.</param>
    /// <param name="readCount">Optional; default is to read an <see cref="int"/> from the stream.</param>
    /// <param name="writeCount">Optional; default is to write an <see cref="int"/> to the stream.</param>
    public SimpleList(EventHandler? handler, Stream s, CreateElementMethod? createElement, WriteElementMethod? writeElement, long size = -1, ReadCountMethod? readCount = null, WriteCountMethod? writeCount = null)
        : this(null, createElement, writeElement, size, readCount, writeCount)
    {
        elementHandler = handler;
        Parse(s);
        this.handler = handler;
    }

    /// <summary>
    /// Initializes a new list filled with the values of <paramref name="collection"/>.
    /// </summary>
    /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
    /// <param name="collection">The source to use as the initial content of the list.</param>
    /// <param name="createElement">Optional; the method to create a new element from a stream.</param>
    /// <param name="writeElement">Optional; the method to write an element to a stream.</param>
    /// <param name="size">Optional maximum number of elements in the list.</param>
    /// <param name="readCount">Optional; default is to read an <see cref="int"/> from the stream.</param>
    /// <param name="writeCount">Optional; default is to write an <see cref="int"/> to the stream.</param>
    public SimpleList(EventHandler? handler, IEnumerable<T> collection, CreateElementMethod? createElement = null, WriteElementMethod? writeElement = null, long size = -1, ReadCountMethod? readCount = null, WriteCountMethod? writeCount = null)
        : this(null, createElement, writeElement, size, readCount, writeCount)
    {
        elementHandler = handler;
        AddRange(collection);
        this.handler = handler;
    }

    #endregion

    #region Data I/O

    /// <inheritdoc />
    protected override int ReadCount(Stream s) => readCount == null ? base.ReadCount(s) : readCount(s);

    /// <inheritdoc />
    protected override void WriteCount(Stream s, int count)
    {
        if (writeCount == null) base.WriteCount(s, count);
        else writeCount(s, count);
    }

    /// <inheritdoc />
    protected override HandlerElement<T> CreateElement(Stream s) => new(0, elementHandler, createElement == null ? default : createElement(s));

    /// <inheritdoc />
    protected override void WriteElement(Stream s, HandlerElement<T> element)
    {
        ArgumentNullException.ThrowIfNull(element);
        writeElement?.Invoke(s, element.Val);
    }

    #endregion

    #region Sub-classes

    /// <summary>Return a new element of type <typeparamref name="T"/> read from <paramref name="s"/>.</summary>
    /// <param name="s">Stream containing element data.</param>
    /// <returns>A new element of type <typeparamref name="T"/>.</returns>
    public delegate T CreateElementMethod(Stream s);

    /// <summary>Write an element of type <typeparamref name="T"/> to <paramref name="s"/>.</summary>
    /// <param name="s">Stream to receive the element.</param>
    /// <param name="value">The value to write.</param>
    public delegate void WriteElementMethod(Stream s, T value);

    /// <summary>Return the number of elements to be created.</summary>
    /// <param name="s">Stream being processed.</param>
    /// <returns>The number of elements to be created.</returns>
    public delegate int ReadCountMethod(Stream s);

    /// <summary>Write the count of list elements to the stream.</summary>
    /// <param name="s">Stream to write <paramref name="count"/> to.</param>
    /// <param name="count">Value to write.</param>
    public delegate void WriteCountMethod(Stream s, int count);

    #endregion

    #region DependentList<HandlerElement<T>>

    /// <summary>
    /// Add a default element to the list.
    /// </summary>
    public override void Add() => Add(default(T));

    #endregion

    #region List<T>

    /// <summary>Adds a value to the end of the list.</summary>
    /// <param name="item">The value to add.</param>
    public virtual void Add(T item) => base.Add(new HandlerElement<T>(0, elementHandler, item));

    /// <summary>Adds the values of the specified collection to the end of the list, raising the handler once.</summary>
    /// <param name="collection">The values to add.</param>
    /// <exception cref="InvalidOperationException">The list would exceed its maximum size.</exception>
    public virtual void AddRange(IEnumerable<T> collection)
    {
        int newElements = new List<T>(collection).Count;
        if (maxSize >= 0 && Count >= maxSize - newElements) throw new InvalidOperationException();

        var h = handler;
        handler = null;
        foreach (T item in collection) Add(item);
        handler = h;

        OnListChanged();
    }

    /// <summary>Searches the sorted list for a value using the default comparer.</summary>
    /// <param name="item">The value to locate.</param>
    /// <returns>The zero-based index of <paramref name="item"/>, if found; otherwise a negative number.</returns>
    public int BinarySearch(T item) => BinarySearch(item, null);

    /// <summary>Searches the sorted list for a value using the specified comparer.</summary>
    /// <param name="item">The value to locate.</param>
    /// <param name="comparer">The comparer, or null for the default.</param>
    /// <returns>The zero-based index of <paramref name="item"/>, if found; otherwise a negative number.</returns>
    public int BinarySearch(T item, IComparer<T>? comparer) => BinarySearch(0, Count, item, comparer);

    /// <summary>Searches a range of the sorted list for a value using the specified comparer.</summary>
    /// <param name="index">The zero-based starting index of the range.</param>
    /// <param name="count">The length of the range.</param>
    /// <param name="item">The value to locate.</param>
    /// <param name="comparer">The comparer, or null for the default.</param>
    /// <returns>The zero-based index of <paramref name="item"/>, if found; otherwise a negative number.</returns>
    public int BinarySearch(int index, int count, T item, IComparer<T>? comparer)
        => base.BinarySearch(index, count, new HandlerElement<T>(0, null, item), new SimpleComparer(comparer));

    /// <summary>Converts the values in the list to another type.</summary>
    /// <typeparam name="TOutput">The output type.</typeparam>
    /// <param name="converter">The converter.</param>
    /// <returns>The converted values.</returns>
    public List<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter) => base.ConvertAll(x => converter(x.Val));

    /// <summary>Copies the values to an array.</summary>
    /// <param name="array">The destination array.</param>
    public void CopyTo(T[] array) => CopyTo(0, array, 0, Count);

    /// <summary>Copies the values to an array starting at the given index.</summary>
    /// <param name="array">The destination array.</param>
    /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
    public void CopyTo(T[] array, int arrayIndex) => CopyTo(0, array, arrayIndex, Count);

    /// <summary>Copies a range of values to an array.</summary>
    /// <param name="index">The zero-based index in the list at which copying begins.</param>
    /// <param name="array">The destination array.</param>
    /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
    /// <param name="count">The number of elements to copy.</param>
    public void CopyTo(int index, T[] array, int arrayIndex, int count) => base.ConvertAll(e => e.Val).CopyTo(index, array, arrayIndex, count);

    /// <summary>Determines whether a value is in the list.</summary>
    /// <param name="item">The value to locate.</param>
    /// <returns>true if found.</returns>
    public bool Contains(T item) => base.Exists(e => e.Val.Equals(item));

    /// <summary>Determines whether the list contains a value matching the predicate.</summary>
    /// <param name="match">The predicate.</param>
    /// <returns>true if any value matches.</returns>
    public bool Exists(Predicate<T> match) => base.Exists(e => match(e.Val));

    /// <summary>Searches for a value matching the predicate and returns the first occurrence.</summary>
    /// <param name="match">The predicate.</param>
    /// <returns>The first matching value.</returns>
    /// <exception cref="NullReferenceException">No value matches (legacy behaviour).</exception>
    public T Find(Predicate<T> match) => base.Find(e => match(e.Val))!.Val;

    /// <summary>Retrieves all the values that match the predicate.</summary>
    /// <param name="match">The predicate.</param>
    /// <returns>The matching values.</returns>
    public List<T> FindAll(Predicate<T> match) => base.FindAll(e => match(e.Val)).ConvertAll(e => e.Val);

    /// <summary>Searches for a value matching the predicate and returns its index.</summary>
    /// <param name="match">The predicate.</param>
    /// <returns>The index, or -1.</returns>
    public int FindIndex(Predicate<T> match) => FindIndex(0, Count, match);

    /// <summary>Searches from <paramref name="startIndex"/> for a value matching the predicate and returns its index.</summary>
    /// <param name="startIndex">The zero-based starting index.</param>
    /// <param name="match">The predicate.</param>
    /// <returns>The index, or -1.</returns>
    public int FindIndex(int startIndex, Predicate<T> match) => FindIndex(startIndex, Count - startIndex, match);

    /// <summary>Searches a range for a value matching the predicate and returns its index.</summary>
    /// <param name="startIndex">The zero-based starting index.</param>
    /// <param name="count">The number of elements to search.</param>
    /// <param name="match">The predicate.</param>
    /// <returns>The index, or -1.</returns>
    /// <remarks>Source: SimpleList.cs line 590 (finds the first match in the whole list, then its index in the range).</remarks>
    public int FindIndex(int startIndex, int count, Predicate<T> match) => IndexOf(Find(match), startIndex, count);

    /// <summary>Searches for a value matching the predicate and returns the last occurrence.</summary>
    /// <param name="match">The predicate.</param>
    /// <returns>The last matching value.</returns>
    public T FindLast(Predicate<T> match) => base.FindLast(e => match(e.Val))!.Val;

    /// <summary>Searches for the last value matching the predicate and returns its index.</summary>
    /// <param name="match">The predicate.</param>
    /// <returns>The index, or -1.</returns>
    public int FindLastIndex(Predicate<T> match) => FindLastIndex(0, Count, match);

    /// <summary>Searches for the last value matching the predicate and returns its index.</summary>
    /// <param name="startIndex">The zero-based starting index of the backward search.</param>
    /// <param name="match">The predicate.</param>
    /// <returns>The index, or -1.</returns>
    public int FindLastIndex(int startIndex, Predicate<T> match) => FindLastIndex(startIndex, Count - startIndex, match);

    /// <summary>Searches a range for the last value matching the predicate and returns its index.</summary>
    /// <param name="startIndex">The zero-based starting index of the backward search.</param>
    /// <param name="count">The number of elements to search.</param>
    /// <param name="match">The predicate.</param>
    /// <returns>The index, or -1.</returns>
    public int FindLastIndex(int startIndex, int count, Predicate<T> match) => LastIndexOf(FindLast(match), startIndex, count);

    /// <summary>Performs the specified action on each value.</summary>
    /// <param name="action">The action.</param>
    public void ForEach(Action<T> action) => base.ForEach(e => action(e.Val));

    /// <summary>Inserts the values of a collection at the specified index, raising the handler once.</summary>
    /// <param name="index">The zero-based index.</param>
    /// <param name="collection">The values to insert.</param>
    /// <exception cref="InvalidOperationException">The list would exceed its maximum size.</exception>
    public virtual void InsertRange(int index, IEnumerable<T> collection)
    {
        int newElements = new List<T>(collection).Count;
        if (maxSize >= 0 && Count >= maxSize - newElements) throw new InvalidOperationException();

        var h = handler;
        handler = null;
        foreach (T item in collection) Insert(index++, item);
        handler = h;

        OnListChanged();
    }

    /// <summary>Returns the index of the first occurrence of a value at or after <paramref name="index"/>.</summary>
    /// <param name="item">The value to locate.</param>
    /// <param name="index">The zero-based starting index.</param>
    /// <returns>The index, or -1.</returns>
    public int IndexOf(T item, int index) => IndexOf(item, index, Count - index);

    /// <summary>Returns the index of the first occurrence of a value within a range.</summary>
    /// <param name="item">The value to locate.</param>
    /// <param name="index">The zero-based starting index.</param>
    /// <param name="count">The number of elements to search.</param>
    /// <returns>The index, or -1.</returns>
    public int IndexOf(T item, int index, int count) => base.IndexOf(base.Find(e => e.Val.Equals(item))!, index, count);

    /// <summary>Returns the index of the last occurrence of a value.</summary>
    /// <param name="item">The value to locate.</param>
    /// <returns>The index, or -1.</returns>
    public int LastIndexOf(T item) => LastIndexOf(item, Count - 1, Count);

    /// <summary>Returns the index of the last occurrence of a value at or before <paramref name="index"/>.</summary>
    /// <param name="item">The value to locate.</param>
    /// <param name="index">The zero-based starting index of the backward search.</param>
    /// <returns>The index, or -1.</returns>
    public int LastIndexOf(T item, int index) => LastIndexOf(item, index, index + 1);

    /// <summary>Returns the index of the last occurrence of a value within a range.</summary>
    /// <param name="item">The value to locate.</param>
    /// <param name="index">The zero-based starting index of the backward search.</param>
    /// <param name="count">The number of elements to search.</param>
    /// <returns>The index, or -1.</returns>
    public int LastIndexOf(T item, int index, int count) => base.LastIndexOf(base.FindLast(e => e.Val.Equals(item))!, index, count);

    /// <summary>Removes the first occurrence of a value.</summary>
    /// <param name="item">The value to remove.</param>
    /// <returns>true if removed.</returns>
    public virtual bool Remove(T item)
    {
        int index = IndexOf(item);
        if (index < 0) return false;
        RemoveAt(index);
        return true;
    }

    /// <summary>Removes all values matching the predicate.</summary>
    /// <param name="match">The predicate.</param>
    /// <returns>The number removed.</returns>
    public virtual int RemoveAll(Predicate<T> match) => base.RemoveAll(e => match(e.Val));

    /// <summary>Sorts the values using the specified comparison.</summary>
    /// <param name="comparison">The comparison.</param>
    public virtual void Sort(Comparison<T> comparison) => base.Sort((x, y) => comparison(x.Val, y.Val));

    /// <summary>Sorts the values using the specified comparer.</summary>
    /// <param name="comparer">The comparer, or null for the default.</param>
    public virtual void Sort(IComparer<T>? comparer)
    {
        if (comparer != null) Sort(comparer.Compare);
        else Sort();
    }

    /// <summary>Sorts a range of values using the specified comparer.</summary>
    /// <param name="index">The zero-based starting index.</param>
    /// <param name="count">The number of elements.</param>
    /// <param name="comparer">The comparer, or null for the default.</param>
    public virtual void Sort(int index, int count, IComparer<T>? comparer) => base.Sort(index, count, new SimpleComparer(comparer));

    private sealed class SimpleComparer(IComparer<T>? comparer) : IComparer<HandlerElement<T>>
    {
        private readonly IComparer<T> comparer = comparer ?? System.Collections.Generic.Comparer<T>.Default;

        public int Compare(HandlerElement<T>? x, HandlerElement<T>? y) => comparer.Compare(x!.Val, y!.Val);
    }

    /// <summary>Copies the values to a new array.</summary>
    /// <returns>An array of the values.</returns>
    public new T[] ToArray() => base.ConvertAll(e => e.Val).ToArray();

    /// <summary>Determines whether every value matches the predicate.</summary>
    /// <param name="match">The predicate.</param>
    /// <returns>true if all values match.</returns>
    public bool TrueForAll(Predicate<T> match) => base.TrueForAll(e => match(e.Val));

    #endregion

    #region IList<T>

    /// <summary>Gets or sets the value at the specified index.</summary>
    /// <param name="index">The zero-based index.</param>
    public new virtual T this[int index]
    {
        get => base[index].Val;
        set
        {
            if (!base[index].Val.Equals(value))
            {
                base[index].Val = value;
                OnListChanged();
            }
        }
    }

    /// <summary>Returns the index of the first occurrence of a value.</summary>
    /// <param name="item">The value to locate.</param>
    /// <returns>The index, or -1.</returns>
    public int IndexOf(T item) => IndexOf(item, 0, Count);

    /// <summary>Inserts a value at the specified index.</summary>
    /// <param name="index">The zero-based index.</param>
    /// <param name="item">The value to insert.</param>
    public virtual void Insert(int index, T item) => base.Insert(index, new HandlerElement<T>(0, elementHandler, item));

    #endregion

    #region IEnumerable<T>

    /// <summary>
    /// Returns an enumerator that iterates through the values of the list.
    /// </summary>
    /// <returns>An enumerator of <typeparamref name="T"/>.</returns>
    public new IEnumerator<T> GetEnumerator() => new Enumerator<T>(this);

    #endregion
}

/// <summary>
/// A simple list of <see cref="uint"/> values.
/// </summary>
public class UIntList : SimpleList<uint>
{
    /// <summary>Initializes a new empty list.</summary>
    /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
    /// <param name="size">Optional maximum number of elements in the list.</param>
    public UIntList(EventHandler? handler, long size = -1) : base(handler, ReadUInt32, WriteUInt32, size) { }

    /// <summary>Initializes a new list filled from <paramref name="basis"/>.</summary>
    /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
    /// <param name="basis">The initial content of the list.</param>
    /// <param name="size">Optional maximum number of elements in the list.</param>
    public UIntList(EventHandler? handler, IEnumerable<uint> basis, long size = -1) : base(handler, basis, ReadUInt32, WriteUInt32, size) { }

    /// <summary>Initializes a new list read from <paramref name="s"/>.</summary>
    /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
    /// <param name="s">The stream to read.</param>
    /// <param name="size">Optional maximum number of elements in the list.</param>
    public UIntList(EventHandler? handler, Stream s, long size = -1) : base(handler, s, ReadUInt32, WriteUInt32, size) { }

    private static uint ReadUInt32(Stream s) => LegacyBinary.ReadUInt32(s);
    private static void WriteUInt32(Stream s, uint value) => LegacyBinary.Write(s, value);
}

/// <summary>
/// A simple list of <see cref="int"/> values.
/// </summary>
public class IntList : SimpleList<int>
{
    /// <summary>Initializes a new empty list.</summary>
    /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
    /// <param name="size">Optional maximum number of elements in the list.</param>
    public IntList(EventHandler? handler, long size = -1) : base(handler, ReadInt32, WriteInt32, size) { }

    /// <summary>Initializes a new list filled from <paramref name="basis"/>.</summary>
    /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
    /// <param name="basis">The initial content of the list.</param>
    /// <param name="size">Optional maximum number of elements in the list.</param>
    public IntList(EventHandler? handler, IEnumerable<int> basis, long size = -1) : base(handler, basis, ReadInt32, WriteInt32, size) { }

    /// <summary>Initializes a new list read from <paramref name="s"/>.</summary>
    /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list or its elements.</param>
    /// <param name="s">The stream to read.</param>
    /// <param name="size">Optional maximum number of elements in the list.</param>
    public IntList(EventHandler? handler, Stream s, long size = -1) : base(handler, s, ReadInt32, WriteInt32, size) { }

    private static int ReadInt32(Stream s) => LegacyBinary.ReadInt32(s);
    private static void WriteInt32(Stream s, int value) => LegacyBinary.Write(s, value);
}
