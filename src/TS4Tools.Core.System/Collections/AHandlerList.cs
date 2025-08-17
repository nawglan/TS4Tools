/***************************************************************************
 *  Copyright (C) 2009, 2010 by Peter L Jones                              *
 *  pljones@users.sf.net                                                   *
 *                                                                         *
 *  This file is part of the Sims 3 Package Interface (s3pi)               *
 *                                                                         *
 *  s3pi is free software: you can redistribute it and/or modify           *
 *  it under the terms of the GNU General Public License as published by   *
 *  the Free Software Foundation, either version 3 of the License, or      *
 *  (at your option) any later version.                                    *
 *                                                                         *
 *  s3pi is distributed in the hope that it will be useful,                *
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of         *
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the          *
 *  GNU General Public License for more details.                           *
 *                                                                         *
 *  You should have received a copy of the GNU General Public License      *
 *  along with s3pi.  If not, see <http://www.gnu.org/licenses/>.          *
 ***************************************************************************/

using System.Diagnostics;

#pragma warning disable S2933 // Fields cannot be readonly due to handler suspension pattern  
#pragma warning disable S4035 // Abstract classes are designed for inheritance, not sealing

namespace TS4Tools.Core.System.Collections;

/// <summary>
/// Abstract extension of <see cref="List{T}"/>
/// providing feedback on list updates through the supplied <see cref="EventHandler"/>.
/// </summary>
/// <typeparam name="T"><see cref="Type"/> of list element</typeparam>
[DebuggerDisplay("Count = {Count}, MaxSize = {MaxSize}")]
public abstract class AHandlerList<T> : List<T>, IEquatable<AHandlerList<T>>, IEqualityComparer<T>
    where T : IEquatable<T>
{
    /// <summary>
    /// Holds the <see cref="EventHandler"/> delegate to invoke if the <see cref="AHandlerList{T}"/> changes.
    /// </summary>
    private EventHandler? handler;

    /// <summary>
    /// The maximum size of the list, or -1 for no limit.
    /// </summary>
    private long maxSize;

    #region Constructors
    /// <summary>
    /// Initializes a new instance of the <see cref="AHandlerList{T}"/> class
    /// that is empty
    /// and with maximum size of <paramref name="maxSize"/> (default is unlimited).
    /// </summary>
    /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list.</param>
    /// <param name="maxSize">Optional; -1 for unlimited size, otherwise the maximum number of elements in the list.</param>
    protected AHandlerList(EventHandler? handler, long maxSize = -1) : base()
    {
        this.handler = handler;
        this.maxSize = maxSize;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AHandlerList{T}"/> class,
    /// filled with the content of <paramref name="collection"/>.
    /// </summary>
    /// <param name="handler">The <see cref="EventHandler"/> to call on changes to the list.</param>
    /// <param name="collection">The <see cref="IEnumerable{T}"/> to use as the initial content of the list.</param>
    /// <param name="maxSize">Optional; -1 for unlimited size, otherwise the maximum number of elements in the list.</param>
    /// <remarks>Does not throw an exception if <paramref name="collection"/>.Count is greater than <paramref name="maxSize"/>.
    /// An exception will be thrown on any attempt to add further items unless the Count is reduced first.</remarks>
    protected AHandlerList(EventHandler? handler, IEnumerable<T> collection, long maxSize = -1) : base(collection)
    {
        this.handler = handler;
        this.maxSize = maxSize;
    }
    #endregion

    #region List<T> Members
    /// <summary>
    /// Adds the elements of the specified collection to the end of the <see cref="AHandlerList{T}"/>.
    /// </summary>
    /// <param name="collection">The collection whose elements should be added to the end of the <see cref="AHandlerList{T}"/>.
    /// The collection itself cannot be null, but it can contain elements that are null, if type <typeparamref name="T"/> is a reference type.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when list size would be exceeded.</exception>
    /// <exception cref="NotSupportedException">The <see cref="AHandlerList{T}"/> is read-only.</exception>
    /// <remarks>Calls <see cref="Add(T)"/> for each item in <paramref name="collection"/>.</remarks>
    public new virtual void AddRange(IEnumerable<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        // Calculate count efficiently
        var collectionCount = collection switch
        {
            ICollection<T> c => c.Count,
            IReadOnlyCollection<T> roc => roc.Count,
            _ => collection.Count()
        };

        if (maxSize >= 0 && Count > maxSize - collectionCount)
            throw new InvalidOperationException($"Adding {collectionCount} elements would exceed maximum size of {maxSize}");

        // Note that the following is required to allow for implementation specific processing on items added to the list:
        var originalHandler = handler;
        handler = null;
        try
        {
            foreach (var item in collection)
                Add(item);
        }
        finally
        {
            handler = originalHandler;
        }

        OnListChanged();
    }

    /// <summary>
    /// Inserts the elements of a collection into the <see cref="AHandlerList{T}"/> at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index at which the new elements should be inserted.</param>
    /// <param name="collection">The collection whose elements should be inserted into the <see cref="AHandlerList{T}"/>.
    /// The collection itself cannot be null, but it can contain elements that are null, if type <typeparamref name="T"/> is a reference type.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index"/> is less than 0.
    /// -or-
    /// <paramref name="index"/> is greater than <see cref="AHandlerList{T}"/>.Count.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when list size would be exceeded.</exception>
    /// <exception cref="NotSupportedException">The <see cref="AHandlerList{T}"/> is read-only.</exception>
    /// <remarks>Calls <see cref="Insert(int, T)"/> for each item in <paramref name="collection"/>.</remarks>
    public new virtual void InsertRange(int index, IEnumerable<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, Count);

        // Calculate count efficiently
        var collectionCount = collection switch
        {
            ICollection<T> c => c.Count,
            IReadOnlyCollection<T> roc => roc.Count,
            _ => collection.Count()
        };

        if (maxSize >= 0 && Count > maxSize - collectionCount)
            throw new InvalidOperationException($"Inserting {collectionCount} elements would exceed maximum size of {maxSize}");

        // Note that the following is required to allow for implementation specific processing on items inserted into the list:
        var originalHandler = handler;
        handler = null;
        try
        {
            var currentIndex = index;
            foreach (var item in collection)
                Insert(currentIndex++, item);
        }
        finally
        {
            handler = originalHandler;
        }

        OnListChanged();
    }

    /// <summary>
    /// Removes the all the elements that match the conditions defined by the specified predicate.
    /// </summary>
    /// <param name="match">The System.Predicate{T} delegate that defines the conditions of the elements to remove.</param>
    /// <returns>The number of elements removed from the <see cref="AHandlerList{T}"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="match"/> is null.</exception>
    /// <exception cref="NotSupportedException">The <see cref="AHandlerList{T}"/> is read-only.</exception>
    public new virtual int RemoveAll(Predicate<T> match)
    {
        var result = base.RemoveAll(match);
        if (result != 0)
            OnListChanged();
        return result;
    }

    /// <summary>
    /// Removes a range of elements from the <see cref="AHandlerList{T}"/>.
    /// </summary>
    /// <param name="index">The zero-based starting index of the range of elements to remove.</param>
    /// <param name="count">The number of elements to remove.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="index"/> and <paramref name="count"/> do not denote a valid range of elements in the <see cref="AHandlerList{T}"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index"/> is less than 0.
    /// -or-
    /// <paramref name="count"/> is less than 0.
    /// </exception>
    /// <exception cref="NotSupportedException">The <see cref="AHandlerList{T}"/> is read-only.</exception>
    public new virtual void RemoveRange(int index, int count)
    {
        if (count == 0) return;

        base.RemoveRange(index, count);
        OnListChanged();
    }

    /// <summary>
    /// Reverses the order of the elements in the entire <see cref="AHandlerList{T}"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">The <see cref="AHandlerList{T}"/> is read-only.</exception>
    public new virtual void Reverse()
    {
        if (Count <= 1) return;

        base.Reverse();
        OnListChanged();
    }

    /// <summary>
    /// Reverses the order of the elements in the specified range.
    /// </summary>
    /// <param name="index">The zero-based starting index of the range to reverse.</param>
    /// <param name="count">The number of elements in the range to reverse.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="index"/> and <paramref name="count"/> do not denote a valid range of elements in the <see cref="AHandlerList{T}"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index"/> is less than 0.
    /// -or-
    /// <paramref name="count"/> is less than 0.
    /// </exception>
    /// <exception cref="NotSupportedException">The <see cref="AHandlerList{T}"/> is read-only.</exception>
    public new virtual void Reverse(int index, int count)
    {
        if (count <= 1) return;

        base.Reverse(index, count);
        OnListChanged();
    }

    /// <summary>
    /// Sorts the elements in the entire <see cref="AHandlerList{T}"/> using the default comparer.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The default comparer <see cref="Comparer{T}"/>.Default
    /// cannot find an implementation of the <see cref="IComparable{T}"/> generic interface
    /// or the System.IComparable interface for type <typeparamref name="T"/>.
    /// </exception>
    /// <exception cref="NotSupportedException">The <see cref="AHandlerList{T}"/> is read-only.</exception>
    public new virtual void Sort()
    {
        if (Count <= 1) return;

        base.Sort();
        OnListChanged();
    }

    /// <summary>
    /// Sorts the elements in the entire <see cref="AHandlerList{T}"/> using the specified <see cref="Comparison{T}"/>.
    /// </summary>
    /// <param name="comparison">The <see cref="Comparison{T}"/> to use when comparing elements.</param>
    /// <exception cref="ArgumentException">The implementation of <paramref name="comparison"/> caused an error during the sort.
    /// For example, <paramref name="comparison"/> might not return 0 when comparing an item with itself.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="comparison"/> is null.</exception>
    /// <exception cref="NotSupportedException">The <see cref="AHandlerList{T}"/> is read-only.</exception>
    public new virtual void Sort(Comparison<T> comparison)
    {
        if (Count <= 1) return;

        base.Sort(comparison);
        OnListChanged();
    }

    /// <summary>
    /// Sorts the elements in the entire <see cref="AHandlerList{T}"/> using the specified comparer.
    /// </summary>
    /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements,
    /// or null to use the default comparer <see cref="Comparer{T}"/>.Default.</param>
    /// <exception cref="ArgumentException">
    /// The implementation of <paramref name="comparer"/> caused an error during the sort.
    /// For example, <paramref name="comparer"/> might not return 0 when comparing an item with itself.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="comparer"/> is null, and the default comparer <see cref="Comparer{T}"/>.Default
    /// cannot find implementation of the <see cref="IComparable{T}"/> generic interface
    /// or the System.IComparable interface for type <typeparamref name="T"/>.
    /// </exception>
    /// <exception cref="NotSupportedException">The <see cref="AHandlerList{T}"/> is read-only.</exception>
    public new virtual void Sort(IComparer<T>? comparer)
    {
        if (Count <= 1) return;

        base.Sort(comparer);
        OnListChanged();
    }

    /// <summary>
    /// Sorts the elements in a range of elements in <see cref="AHandlerList{T}"/> using the specified comparer.
    /// </summary>
    /// <param name="index">The zero-based starting index of the range to sort.</param>
    /// <param name="count">The number of elements in the range to sort.</param>
    /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements,
    /// or null to use the default comparer <see cref="Comparer{T}"/>.Default.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="index"/> and <paramref name="count"/> do not denote a valid range of elements in the <see cref="AHandlerList{T}"/>.
    /// -or-
    /// The implementation of <paramref name="comparer"/> caused an error during the sort.
    /// For example, <paramref name="comparer"/> might not return 0 when comparing an item with itself.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index"/> is less than 0.
    /// -or-
    /// <paramref name="count"/> is less than 0.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="comparer"/> is null, and the default comparer <see cref="Comparer{T}"/>.Default
    /// cannot find implementation of the <see cref="IComparable{T}"/> generic interface
    /// or the System.IComparable interface for type <typeparamref name="T"/>.
    /// </exception>
    /// <exception cref="NotSupportedException">The <see cref="AHandlerList{T}"/> is read-only.</exception>
    public new virtual void Sort(int index, int count, IComparer<T>? comparer)
    {
        if (count <= 1) return;

        base.Sort(index, count, comparer);
        OnListChanged();
    }
    #endregion

    #region IList<T> Members
    /// <summary>
    /// Inserts an item to the <see cref="AHandlerList{T}"/> at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index at which item should be inserted.</param>
    /// <param name="item">The object to insert into the <see cref="AHandlerList{T}"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="AHandlerList{T}"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when list size exceeded.</exception>
    /// <exception cref="NotSupportedException">The <see cref="AHandlerList{T}"/> is read-only.</exception>
    public new virtual void Insert(int index, T item)
    {
        if (maxSize >= 0 && Count == maxSize)
            throw new InvalidOperationException($"Cannot insert item, maximum size of {maxSize} reached");

        base.Insert(index, item);
        OnListChanged();
    }

    /// <summary>
    /// Removes the <see cref="AHandlerList{T}"/> item at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the item to remove.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="AHandlerList{T}"/>.</exception>
    /// <exception cref="NotSupportedException">The <see cref="AHandlerList{T}"/> is read-only.</exception>
    public new virtual void RemoveAt(int index)
    {
        base.RemoveAt(index);
        OnListChanged();
    }

    /// <summary>
    /// Gets or sets the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the element to get or set.</param>
    /// <returns>The element at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="AHandlerList{T}"/>.</exception>
    /// <exception cref="NotSupportedException">The <see cref="AHandlerList{T}"/> is read-only.</exception>
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
    /// <summary>
    /// Adds an object to the end of the <see cref="AHandlerList{T}"/>.
    /// </summary>
    /// <param name="item">The object to add to the <see cref="AHandlerList{T}"/>.</param>
    /// <exception cref="InvalidOperationException">Thrown when list size exceeded.</exception>
    /// <exception cref="NotSupportedException">The <see cref="AHandlerList{T}"/> is read-only.</exception>
    public new virtual void Add(T item)
    {
        if (maxSize >= 0 && Count == maxSize)
            throw new InvalidOperationException($"Cannot add item, maximum size of {maxSize} reached");

        base.Add(item);
        OnListChanged();
    }

    /// <summary>
    /// Removes all items from the <see cref="AHandlerList{T}"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">The <see cref="AHandlerList{T}"/> is read-only.</exception>
    public new virtual void Clear()
    {
        if (Count == 0) return;

        base.Clear();
        OnListChanged();
    }

    /// <summary>
    /// Removes the first occurrence of a specific object from the <see cref="AHandlerList{T}"/>.
    /// </summary>
    /// <param name="item">The object to remove from the <see cref="AHandlerList{T}"/>.</param>
    /// <returns>
    /// true if item was successfully removed from the <see cref="AHandlerList{T}"/>
    /// otherwise, false. This method also returns false if item is not found in
    /// the original <see cref="AHandlerList{T}"/>.
    /// </returns>
    /// <exception cref="NotSupportedException">The <see cref="AHandlerList{T}"/> is read-only.</exception>
    public new virtual bool Remove(T item)
    {
        var result = base.Remove(item);
        if (result)
            OnListChanged();
        return result;
    }
    #endregion

    /// <summary>
    /// The maximum size of the list, or -1 for no limit (read-only).
    /// </summary>
    public long MaxSize => maxSize;

    /// <summary>
    /// Invokes the list change event handler.
    /// </summary>
    protected void OnListChanged()
    {
        handler?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Determine whether this list is equal to <paramref name="target"/>.
    /// </summary>
    /// <param name="target">A <see cref="AHandlerList{T}"/> against which to test this list for equality.</param>
    /// <returns>True if this list is equal to <paramref name="target"/>; otherwise false.</returns>
    public bool Equals(AHandlerList<T>? target)
    {
        if (target is null) return false;
        if (ReferenceEquals(this, target)) return true;
        if (Count != target.Count) return false;

        for (int i = 0; i < Count; i++)
        {
            if (!this[i].Equals(target[i]))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Determines whether the specified objects are equal.
    /// </summary>
    /// <param name="x">The first object to compare.</param>
    /// <param name="y">The second object to compare.</param>
    /// <returns>true if the specified objects are equal; otherwise, false.</returns>
    public bool Equals(T? x, T? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        return x.Equals(y);
    }

    /// <summary>
    /// Returns a hash code for the specified object.
    /// </summary>
    /// <param name="obj">The object for which a hash code is to be returned.</param>
    /// <returns>A hash code for the specified object.</returns>
    public int GetHashCode(T obj)
    {
        return obj?.GetHashCode() ?? 0;
    }
}
