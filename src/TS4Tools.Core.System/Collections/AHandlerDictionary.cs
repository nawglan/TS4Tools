/***************************************************************************
 *  Copyright (C) 2017 by Peter L Jones                                    *
 *  pljones@users.sf.net                                                   *
 *                                                                         *
 *  This file is part of the Sims 4 Package Interface (s4pi)               *
 *                                                                         *
 *  s4pi is free software: you can redistribute it and/or modify           *
 *  it under the terms of the GNU General Public License as published by   *
 *  the Free Software Foundation, either version 3 of the License, or      *
 *  (at your option) any later version.                                    *
 *                                                                         *
 *  s4pi is distributed in the hope that it will be useful,                *
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of         *
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the          *
 *  GNU General Public License for more details.                           *
 *                                                                         *
 *  You should have received a copy of the GNU General Public License      *
 *  along with s4pi.  If not, see <http://www.gnu.org/licenses/>.          *
 ***************************************************************************/

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TS4Tools.Core.System.Collections;

/// <summary>
/// Abstract extension of <see cref="Dictionary{TKey, TValue}"/>
/// providing feedback on dictionary updates through the supplied <see cref="EventHandler"/>.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
[ComVisible(false)]
[DebuggerDisplay("Count = {Count}")]
public abstract class AHandlerDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    where TKey : notnull
    where TValue : IEquatable<TValue>
{
    /// <summary>
    /// Holds the <see cref="EventHandler"/> delegate to invoke if the <see cref="AHandlerDictionary{TKey, TValue}"/> changes.
    /// </summary>
    protected EventHandler? handler;

    #region Constructors
    /// <summary>
    /// Initializes a new instance of the <see cref="AHandlerDictionary{TKey,TValue}"/> class that
    /// has the supplied change handler,
    /// is empty,
    /// has the default initial capacity,
    /// and uses the default equality comparer for the key type.
    /// </summary>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the <see cref="AHandlerDictionary{TKey, TValue}"/> changes.</param>
    protected AHandlerDictionary(EventHandler? handler) : base() 
    { 
        this.handler = handler; 
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AHandlerDictionary{TKey, TValue}"/> class that
    /// has the supplied change handler,
    /// contains elements copied from the specified <see cref="IDictionary{TKey,TValue}"/>,
    /// and uses the default equality comparer for the key type.
    /// </summary>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the <see cref="AHandlerDictionary{TKey, TValue}"/> changes.</param>
    /// <param name="dictionary">The <see cref="IDictionary{TKey,TValue}"/> whose elements are copied to the new <see cref="AHandlerDictionary{TKey, TValue}"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="dictionary"/> contains one or more duplicate keys.</exception>
    protected AHandlerDictionary(EventHandler? handler, IDictionary<TKey, TValue> dictionary) : base(dictionary) 
    { 
        this.handler = handler; 
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AHandlerDictionary{TKey, TValue}"/> class that
    /// has the supplied change handler,
    /// is empty,
    /// has the default initial capacity,
    /// and uses the specified <see cref="IEqualityComparer{T}"/>.
    /// </summary>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the <see cref="AHandlerDictionary{TKey, TValue}"/> changes.</param>
    /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use when comparing keys, or null to use the default <see cref="EqualityComparer{T}"/> for the type of the key.</param>
    protected AHandlerDictionary(EventHandler? handler, IEqualityComparer<TKey>? comparer) : base(comparer) 
    { 
        this.handler = handler; 
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AHandlerDictionary{TKey, TValue}"/> class that
    /// has the supplied change handler,
    /// is empty,
    /// has the specified initial capacity,
    /// and uses the default equality comparer for the key type.
    /// </summary>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the <see cref="AHandlerDictionary{TKey, TValue}"/> changes.</param>
    /// <param name="capacity">The initial number of elements that the <see cref="AHandlerDictionary{TKey, TValue}"/> can contain.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than 0.</exception>
    protected AHandlerDictionary(EventHandler? handler, int capacity) : base(capacity) 
    { 
        this.handler = handler; 
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AHandlerDictionary{TKey, TValue}"/> class that
    /// has the supplied change handler,
    /// contains elements copied from the specified <see cref="IDictionary{TKey,TValue}"/>,
    /// and uses the specified <see cref="IEqualityComparer{T}"/>.
    /// </summary>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the <see cref="AHandlerDictionary{TKey, TValue}"/> changes.</param>
    /// <param name="dictionary">The <see cref="IDictionary{TKey,TValue}"/> whose elements are copied to the new <see cref="AHandlerDictionary{TKey, TValue}"/>.</param>
    /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use when comparing keys, or null to use the default <see cref="EqualityComparer{T}"/> for the type of the key.</param>
    /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="dictionary"/> contains one or more duplicate keys.</exception>
    protected AHandlerDictionary(EventHandler? handler, IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey>? comparer) : base(dictionary, comparer) 
    { 
        this.handler = handler; 
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AHandlerDictionary{TKey, TValue}"/> class that
    /// has the supplied change handler,
    /// is empty,
    /// has the specified initial capacity,
    /// and uses the specified <see cref="IEqualityComparer{T}"/>.
    /// </summary>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the <see cref="AHandlerDictionary{TKey, TValue}"/> changes.</param>
    /// <param name="capacity">The initial number of elements that the <see cref="AHandlerDictionary{TKey, TValue}"/> can contain.</param>
    /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use when comparing keys, or null to use the default <see cref="EqualityComparer{T}"/> for the type of the key.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than 0.</exception>
    protected AHandlerDictionary(EventHandler? handler, int capacity, IEqualityComparer<TKey>? comparer) : base(capacity, comparer) 
    { 
        this.handler = handler; 
    }
    #endregion

    /// <summary>
    /// Gets or sets the value associated with the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key of the value to get or set.</param>
    /// <returns>
    /// The value associated with the specified <paramref name="key"/>.
    /// If the specified <paramref name="key"/> is not found, a get operation throws a <see cref="KeyNotFoundException"/>,
    /// and a set operation creates a new element with the specified <paramref name="key"/>.
    /// Any change to the dictionary invokes the supplied dictionary changed event handler.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
    /// <exception cref="KeyNotFoundException">The property is retrieved and <paramref name="key"/> does not exist in the collection.</exception>
    public new virtual TValue this[TKey key] 
    { 
        get => base[key]; 
        set 
        { 
            if (TryGetValue(key, out var currentValue) && currentValue.Equals(value))
                return;
                
            base[key] = value; 
            OnDictionaryChanged(); 
        } 
    }

    /// <summary>
    /// Adds the specified <paramref name="key"/> and <paramref name="value"/> to the dictionary.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value of the element to add. The value can be null for reference types.</param>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
    /// <exception cref="ArgumentException">An element with the same key already exists in the <see cref="AHandlerDictionary{TKey,TValue}"/>.</exception>
    public new virtual void Add(TKey key, TValue value) 
    { 
        base.Add(key, value); 
        OnDictionaryChanged(); 
    }

    /// <summary>
    /// Removes all keys and values from the <see cref="AHandlerDictionary{TKey,TValue}"/>.
    /// </summary>
    public new virtual void Clear() 
    { 
        if (Count == 0) return;
        
        base.Clear(); 
        OnDictionaryChanged(); 
    }

    /// <summary>
    /// Removes the value with the specified <paramref name="key"/> from the <see cref="AHandlerDictionary{TKey,TValue}"/>.
    /// </summary>
    /// <param name="key">The key of the element to remove.</param>
    /// <returns><c>true</c> if the element is successfully found and removed; otherwise, <c>false</c>.
    /// This method returns <c>false</c> if <paramref name="key"/> is not found in the <see cref="AHandlerDictionary{TKey,TValue}"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
    public new virtual bool Remove(TKey key) 
    { 
        if (!base.Remove(key)) 
            return false;
            
        OnDictionaryChanged(); 
        return true;
    }

    /// <summary>
    /// Invokes the dictionary change event handler.
    /// </summary>
    protected void OnDictionaryChanged() 
    { 
        handler?.Invoke(this, EventArgs.Empty); 
    }
}
