// Source: legacy_references/Sims4Tools/CS System Classes/AHandlerList.cs lines 23-289

namespace System.Collections.Generic;

/// <summary>
/// Abstract extension of <see cref="List{T}"/> providing feedback on list updates through the supplied
/// <see cref="EventHandler"/>.
/// </summary>
/// <typeparam name="T"><see cref="Type"/> of list element.</typeparam>
/// <remarks>
/// Members of <see cref="List{T}"/> that change the list are hidden with <c>new virtual</c> versions that raise
/// the change handler, exactly as legacy does. Calls made through a <see cref="List{T}"/> (or interface)
/// reference bypass the handler, as in legacy.
/// </remarks>
public abstract class AHandlerList<T> : List<T>
    where T : IEquatable<T>
{
    /// <summary>
    /// Holds the <see cref="EventHandler"/> delegate to invoke if the <see cref="AHandlerList{T}"/> changes.
    /// </summary>
    protected EventHandler? handler;

    /// <summary>
    /// The maximum size of the list, or -1 for no limit.
    /// </summary>
    protected long maxSize = -1;

    /// <summary>
    /// Initialize a new instance.
    /// </summary>
    /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list.</param>
    /// <param name="maxSize">Optional; -1 for unlimited size, otherwise the maximum number of elements in the list.</param>
    protected AHandlerList(EventHandler? handler, long maxSize = -1)
    {
        this.handler = handler;
        this.maxSize = maxSize;
    }

    /// <summary>
    /// Initialize a new instance filled from <paramref name="ilt"/>.
    /// </summary>
    /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list.</param>
    /// <param name="ilt">The initial content of the list.</param>
    /// <param name="maxSize">Optional; -1 for unlimited size, otherwise the maximum number of elements in the list.</param>
    protected AHandlerList(EventHandler? handler, IEnumerable<T> ilt, long maxSize = -1) : base(ilt)
    {
        this.handler = handler;
        this.maxSize = maxSize;
    }

    #region List<T> Members

    /// <summary>
    /// Adds the elements of the specified collection to the end of the list, raising the handler once.
    /// </summary>
    /// <param name="collection">The elements to add.</param>
    /// <exception cref="InvalidOperationException">The list would exceed its maximum size.</exception>
    /// <remarks>Source: AHandlerList.cs lines 73-85</remarks>
    public new virtual void AddRange(IEnumerable<T> collection)
    {
        int newElements = new List<T>(collection).Count;
        if (maxSize >= 0 && Count >= maxSize - newElements) throw new InvalidOperationException();

        // Implementation specific processing happens in Add, so suppress the handler and raise it once.
        var h = handler;
        handler = null;
        foreach (T item in collection) Add(item);
        handler = h;

        OnListChanged();
    }

    /// <summary>
    /// Inserts the elements of a collection at the specified index, raising the handler once.
    /// </summary>
    /// <param name="index">The zero-based index at which the new elements should be inserted.</param>
    /// <param name="collection">The elements to insert.</param>
    /// <exception cref="InvalidOperationException">The list would exceed its maximum size.</exception>
    /// <remarks>Source: AHandlerList.cs lines 101-113</remarks>
    public new virtual void InsertRange(int index, IEnumerable<T> collection)
    {
        int newElements = new List<T>(collection).Count;
        if (maxSize >= 0 && Count >= maxSize - newElements) throw new InvalidOperationException();

        var h = handler;
        handler = null;
        foreach (T item in collection) Insert(index++, item);
        handler = h;

        OnListChanged();
    }

    /// <summary>Removes all elements matching the predicate.</summary>
    /// <param name="match">The predicate.</param>
    /// <returns>The number of elements removed.</returns>
    public new virtual int RemoveAll(Predicate<T> match)
    {
        int res = base.RemoveAll(match);
        if (res != 0) OnListChanged();
        return res;
    }

    /// <summary>Removes a range of elements.</summary>
    /// <param name="index">The zero-based starting index.</param>
    /// <param name="count">The number of elements to remove.</param>
    public new virtual void RemoveRange(int index, int count) { base.RemoveRange(index, count); OnListChanged(); }

    /// <summary>Reverses the order of the elements.</summary>
    public new virtual void Reverse() { base.Reverse(); OnListChanged(); }

    /// <summary>Reverses the order of the elements in the specified range.</summary>
    /// <param name="index">The zero-based starting index.</param>
    /// <param name="count">The number of elements.</param>
    public new virtual void Reverse(int index, int count) { base.Reverse(index, count); OnListChanged(); }

    /// <summary>Sorts the elements using the default comparer.</summary>
    public new virtual void Sort() { base.Sort(); OnListChanged(); }

    /// <summary>Sorts the elements using the specified comparison.</summary>
    /// <param name="comparison">The comparison.</param>
    public new virtual void Sort(Comparison<T> comparison) { base.Sort(comparison); OnListChanged(); }

    /// <summary>Sorts the elements using the specified comparer.</summary>
    /// <param name="comparer">The comparer, or null for the default.</param>
    public new virtual void Sort(IComparer<T>? comparer) { base.Sort(comparer); OnListChanged(); }

    /// <summary>Sorts a range of elements using the specified comparer.</summary>
    /// <param name="index">The zero-based starting index.</param>
    /// <param name="count">The number of elements.</param>
    /// <param name="comparer">The comparer, or null for the default.</param>
    public new virtual void Sort(int index, int count, IComparer<T>? comparer) { base.Sort(index, count, comparer); OnListChanged(); }

    #endregion

    #region IList<T> Members

    /// <summary>Inserts an element at the specified index.</summary>
    /// <param name="index">The zero-based index.</param>
    /// <param name="item">The element.</param>
    /// <exception cref="InvalidOperationException">The list is at its maximum size.</exception>
    public new virtual void Insert(int index, T item)
    {
        if (maxSize >= 0 && Count == maxSize) throw new InvalidOperationException();
        base.Insert(index, item);
        OnListChanged();
    }

    /// <summary>Removes the element at the specified index.</summary>
    /// <param name="index">The zero-based index.</param>
    public new virtual void RemoveAt(int index) { base.RemoveAt(index); OnListChanged(); }

    /// <summary>Gets or sets the element at the specified index; setting a different value raises the handler.</summary>
    /// <param name="index">The zero-based index.</param>
    public new virtual T this[int index]
    {
        get => base[index];
        set
        {
            if (!base[index].Equals(value))
            {
                base[index] = value;
                OnListChanged();
            }
        }
    }

    #endregion

    #region ICollection<T> Members

    /// <summary>Adds an element to the end of the list.</summary>
    /// <param name="item">The element.</param>
    /// <exception cref="InvalidOperationException">The list is at its maximum size.</exception>
    public new virtual void Add(T item)
    {
        if (maxSize >= 0 && Count == maxSize) throw new InvalidOperationException();
        base.Add(item);
        OnListChanged();
    }

    /// <summary>Removes all elements.</summary>
    public new virtual void Clear() { base.Clear(); OnListChanged(); }

    /// <summary>Removes the first occurrence of an element.</summary>
    /// <param name="item">The element.</param>
    /// <returns>True if removed.</returns>
    public new virtual bool Remove(T item)
    {
        bool res = base.Remove(item);
        if (res) OnListChanged();
        return res;
    }

    #endregion

    /// <summary>
    /// The maximum size of the list, or -1 for no limit.
    /// </summary>
    public long MaxSize => maxSize;

    /// <summary>
    /// Invokes the list change event handler.
    /// </summary>
    protected void OnListChanged() => handler?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Indicates whether this list is element-wise equal to <paramref name="target"/>.
    /// </summary>
    /// <param name="target">The list to compare with.</param>
    /// <returns>True if both lists contain equal elements in the same order.</returns>
    /// <remarks>Source: AHandlerList.cs line 287</remarks>
    public bool Equals(AHandlerList<T>? target) => System.Extensions.Equals<T>(this, target);
}
