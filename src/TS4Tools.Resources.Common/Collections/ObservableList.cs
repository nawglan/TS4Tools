using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace TS4Tools.Resources.Common.Collections;

/// <summary>
/// An observable collection that provides additional functionality for bulk operations
/// and proper change notifications for data binding scenarios.
/// </summary>
/// <typeparam name="T">The type of elements in the collection.</typeparam>
public class ObservableList<T> : ObservableCollection<T>
{
    private bool _suppressNotifications;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableList{T}"/> class.
    /// </summary>
    public ObservableList()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableList{T}"/> class with the specified items.
    /// </summary>
    /// <param name="items">The items to add to the collection.</param>
    public ObservableList(IEnumerable<T> items)
        : base(items)
    {
    }

    /// <summary>
    /// Adds multiple items to the collection in a single operation.
    /// </summary>
    /// <param name="items">The items to add.</param>
    public void AddRange(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        using (SuppressNotifications())
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }

        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Removes multiple items from the collection.
    /// </summary>
    /// <param name="items">The items to remove.</param>
    /// <returns>The number of items successfully removed.</returns>
    public int RemoveRange(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var removedCount = 0;
        using (SuppressNotifications())
        {
            foreach (var item in items.ToList()) // ToList to avoid collection modification issues
            {
                if (Remove(item))
                    removedCount++;
            }
        }

        if (removedCount > 0)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        return removedCount;
    }

    /// <summary>
    /// Replaces all items in the collection with the specified items.
    /// </summary>
    /// <param name="items">The new items for the collection.</param>
    public void ReplaceAll(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        using (SuppressNotifications())
        {
            Clear();
            foreach (var item in items)
            {
                Add(item);
            }
        }

        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Temporarily suppresses collection change notifications.
    /// Use with 'using' statement for automatic restoration.
    /// </summary>
    /// <returns>A disposable object that will restore notifications when disposed.</returns>
    public IDisposable SuppressNotifications()
    {
        return new NotificationSuppressor(this);
    }

    /// <inheritdoc />
    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!_suppressNotifications)
        {
            base.OnCollectionChanged(e);
        }
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (!_suppressNotifications)
        {
            base.OnPropertyChanged(e);
        }
    }

    private sealed class NotificationSuppressor : IDisposable
    {
        private readonly ObservableList<T> _list;
        private readonly bool _previousValue;

        public NotificationSuppressor(ObservableList<T> list)
        {
            _list = list;
            _previousValue = list._suppressNotifications;
            list._suppressNotifications = true;
        }

        public void Dispose()
        {
            _list._suppressNotifications = _previousValue;
        }
    }
}
