// Source: legacy_references/Sims4Tools/s4pi/Interfaces/AApiVersionedFields.cs lines 619-1086
// (AHandlerElement, HandlerElement<T>, TGIBlockListIndex<T>)

namespace s4pi.Interfaces;

/// <summary>
/// A useful extension to <see cref="AApiVersionedFields"/> where a change handler is required.
/// </summary>
public abstract class AHandlerElement : AApiVersionedFields
{
    /// <summary>
    /// Holds the <see cref="EventHandler"/> delegate to invoke if the <see cref="AHandlerElement"/> changes.
    /// </summary>
    protected EventHandler? handler;

    /// <summary>
    /// Indicates if the <see cref="AHandlerElement"/> has been changed by OnElementChanged().
    /// </summary>
    protected bool dirty;

    /// <summary>
    /// Initialize a new instance.
    /// </summary>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the <see cref="AHandlerElement"/> changes.</param>
    protected AHandlerElement(int apiVersion, EventHandler? handler)
    {
        requestedApiVersion = apiVersion;
        this.handler = handler;
    }

    /// <summary>
    /// Get a copy of the <see cref="AHandlerElement"/> but with a new change <see cref="EventHandler"/>.
    /// </summary>
    /// <param name="handler">The replacement <see cref="EventHandler"/> delegate.</param>
    /// <returns>Return a copy of the <see cref="AHandlerElement"/> but with a new change <see cref="EventHandler"/>.</returns>
    /// <remarks>
    /// Source: AApiVersionedFields.cs lines 650-713. The copy is made with the
    /// <c>(int apiVersion, EventHandler handler, &lt;this type&gt; basis[, optional...])</c> constructor.
    /// Legacy's constructor matcher had two defects that are fixed here: a <c>null</c> handler never matched
    /// (<c>IsInstanceOfType(null)</c> is false), and padding the arguments called <c>Add</c> on a fixed-size array
    /// (NotSupportedException), so constructors with trailing optional parameters could not be used.
    /// </remarks>
    public virtual AHandlerElement Clone(EventHandler? handler)
    {
        object?[] args = [requestedApiVersion, handler, this];

        // Default values for parameters are resolved by the compiler; Activator.CreateInstance does not do that.
        foreach (var c in GetType().GetConstructors())
        {
            var padded = MatchConstructor(c, args);
            if (padded != null)
                return (AHandlerElement)c.Invoke(padded);
        }

        return (AHandlerElement)Activator.CreateInstance(GetType(), args, null)!;
    }

    // Source: AApiVersionedFields.cs lines 669-713
    private static object?[]? MatchConstructor(ConstructorInfo c, object?[] args)
    {
        var pi = c.GetParameters();

        // Our required arguments followed by one or more optional ones
        if (pi.Length <= args.Length) return null;
        if (pi[args.Length - 1].IsOptional) return null;
        if (!pi[args.Length].IsOptional) return null;

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == null)
            {
                // null matches anything except a value type
                if (pi[i].ParameterType.IsValueType) return null;
            }
            else if (!pi[i].ParameterType.IsInstanceOfType(args[i]))
            {
                return null;
            }
        }

        // Pad the args with Type.Missing so the optional parameters take their defaults
        var padded = new object?[pi.Length];
        Array.Copy(args, padded, args.Length);
        for (var i = args.Length; i < pi.Length; i++)
            padded[i] = Type.Missing;
        return padded;
    }

    /// <summary>
    /// Flag the <see cref="AHandlerElement"/> as dirty and invoke the <see cref="EventHandler"/> delegate.
    /// </summary>
    protected virtual void OnElementChanged()
    {
        dirty = true;
        handler?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Change the element change handler to <paramref name="handler"/>.
    /// </summary>
    /// <param name="handler">The new element change handler.</param>
    internal void SetHandler(EventHandler? handler)
    {
        if (this.handler != handler)
            this.handler = handler;
    }
}

/// <summary>
/// An extension to <see cref="AHandlerElement"/>, for simple data types (such as <see cref="uint"/>).
/// </summary>
/// <typeparam name="T">A simple data type (such as <see cref="uint"/>).</typeparam>
/// <remarks>For an example of use, see <see cref="SimpleList{T}"/>.</remarks>
public class HandlerElement<T> : AHandlerElement, IEquatable<HandlerElement<T>>
    where T : struct, IComparable, IConvertible, IEquatable<T>, IComparable<T>
{
    private const int recommendedApiVersion = 1;
    private T val;

    /// <summary>
    /// Initialize a new instance with a default value.
    /// </summary>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the element changes.</param>
    public HandlerElement(int apiVersion, EventHandler? handler) : this(apiVersion, handler, default(T)) { }

    /// <summary>
    /// Initialize a new instance with an initial value of <paramref name="basis"/>.
    /// </summary>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the element changes.</param>
    /// <param name="basis">Initial value for instance.</param>
    public HandlerElement(int apiVersion, EventHandler? handler, T basis) : base(apiVersion, handler) { val = basis; }

    /// <summary>
    /// Initialize a new instance with an initial value from <paramref name="basis"/>.
    /// </summary>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the element changes.</param>
    /// <param name="basis">Element containing the initial value for instance.</param>
    public HandlerElement(int apiVersion, EventHandler? handler, HandlerElement<T> basis) : base(apiVersion, handler)
    {
        ArgumentNullException.ThrowIfNull(basis);
        val = basis.val;
    }

    #region AHandlerElement

    /// <summary>
    /// Get a copy of the HandlerElement but with a new change <see cref="EventHandler"/>.
    /// </summary>
    /// <param name="handler">The replacement delegate.</param>
    /// <returns>A copy of the HandlerElement with the new change <see cref="EventHandler"/>.</returns>
    public override AHandlerElement Clone(EventHandler? handler) => new HandlerElement<T>(requestedApiVersion, handler, val);

    /// <summary>
    /// The best supported version of the API available.
    /// </summary>
    public override int RecommendedApiVersion => recommendedApiVersion;

    /// <summary>
    /// The list of available field names on this API object.
    /// </summary>
    public override List<string> ContentFields => GetContentFields(requestedApiVersion, GetType());

    #endregion

    #region IEquatable<HandlerElement<T>>

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns>true if the values are equal.</returns>
    public bool Equals(HandlerElement<T>? other) => other is not null && val.Equals(other.val);

    /// <summary>
    /// Determines whether the specified object (a <typeparamref name="T"/> or <see cref="HandlerElement{T}"/>) is equal to this value.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>true if equal.</returns>
    public override bool Equals(object? obj) => obj switch
    {
        T t => val.Equals(t),
        HandlerElement<T> h => Equals(h),
        _ => false,
    };

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>The hash code of the value.</returns>
    public override int GetHashCode() => val.GetHashCode();

    #endregion

    /// <summary>
    /// The value of the object.
    /// </summary>
    public T Val
    {
        get => val;
        set
        {
            if (!val.Equals(value))
            {
                val = value;
                OnElementChanged();
            }
        }
    }

    /// <summary>
    /// Implicit cast from <see cref="HandlerElement{T}"/> to <typeparamref name="T"/>.
    /// </summary>
    /// <param name="value">Value to cast.</param>
    public static implicit operator T(HandlerElement<T> value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.val;
    }

    /// <summary>
    /// Get displayable value.
    /// </summary>
    public string Value => new TypedValue(typeof(T), val, "X").ToString("X");
}

/// <summary>
/// An extension to <see cref="AHandlerElement"/>, for lists of TGIBlockList indices.
/// </summary>
/// <typeparam name="T">A simple data type (such as <see cref="int"/>).</typeparam>
/// <remarks>For an example of use, see <see cref="IndexList{T}"/>.</remarks>
public class TGIBlockListIndex<T> : AHandlerElement, IEquatable<TGIBlockListIndex<T>>
    where T : struct, IComparable, IConvertible, IEquatable<T>, IComparable<T>
{
    private const int recommendedApiVersion = 1;

    /// <summary>
    /// Reference to list into which this is an index.
    /// </summary>
    public DependentList<TGIBlock>? ParentTGIBlocks { get; set; }

    private T data;

    /// <summary>
    /// Initialize a new instance with a default value.
    /// </summary>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the element changes.</param>
    /// <param name="ParentTGIBlocks">Reference to list into which this is an index.</param>
    public TGIBlockListIndex(int apiVersion, EventHandler? handler, DependentList<TGIBlock>? ParentTGIBlocks = null)
        : this(apiVersion, handler, default(T), ParentTGIBlocks) { }

    /// <summary>
    /// Initialize a new instance with an initial value from <paramref name="basis"/>.
    /// </summary>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the element changes.</param>
    /// <param name="basis">Element containing the initial value for instance.</param>
    /// <param name="ParentTGIBlocks">Reference to list into which this is an index, or null to use that in <paramref name="basis"/>.</param>
    public TGIBlockListIndex(int apiVersion, EventHandler? handler, TGIBlockListIndex<T> basis, DependentList<TGIBlock>? ParentTGIBlocks = null)
        : this(apiVersion, handler, (basis ?? throw new ArgumentNullException(nameof(basis))).data, ParentTGIBlocks ?? basis.ParentTGIBlocks) { }

    /// <summary>
    /// Initialize a new instance with an initial value of <paramref name="value"/>.
    /// </summary>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the element changes.</param>
    /// <param name="value">Initial value for instance.</param>
    /// <param name="ParentTGIBlocks">Reference to list into which this is an index.</param>
    public TGIBlockListIndex(int apiVersion, EventHandler? handler, T value, DependentList<TGIBlock>? ParentTGIBlocks = null)
        : base(apiVersion, handler)
    {
        this.ParentTGIBlocks = ParentTGIBlocks;
        data = value;
    }

    #region AHandlerElement

    /// <summary>
    /// The best supported version of the API available.
    /// </summary>
    public override int RecommendedApiVersion => recommendedApiVersion;

    /// <summary>
    /// The list of available field names on this API object (excluding <see cref="ParentTGIBlocks"/>).
    /// </summary>
    public override List<string> ContentFields
    {
        get
        {
            var res = GetContentFields(requestedApiVersion, GetType());
            res.Remove("ParentTGIBlocks");
            return res;
        }
    }

    #endregion

    #region IEquatable<TGIBlockListIndex<T>>

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns>true if the values are equal.</returns>
    public bool Equals(TGIBlockListIndex<T>? other) => other is not null && data.Equals(other.data);

    /// <summary>
    /// Determines whether the specified object (a <typeparamref name="T"/> or <see cref="TGIBlockListIndex{T}"/>) is equal to this value.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>true if equal.</returns>
    public override bool Equals(object? obj) => obj switch
    {
        T t => data.Equals(t),
        TGIBlockListIndex<T> i => Equals(i),
        _ => false,
    };

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>The hash code of the value.</returns>
    public override int GetHashCode() => data.GetHashCode();

    #endregion

    /// <summary>
    /// The value of the object.
    /// </summary>
    [TGIBlockListContentField("ParentTGIBlocks")]
    public T Data
    {
        get => data;
        set
        {
            if (!data.Equals(value))
            {
                data = value;
                OnElementChanged();
            }
        }
    }

    /// <summary>
    /// Implicit cast from <see cref="TGIBlockListIndex{T}"/> to <typeparamref name="T"/>.
    /// </summary>
    /// <param name="value">Value to cast.</param>
    public static implicit operator T(TGIBlockListIndex<T> value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.data;
    }

    /// <summary>
    /// Displayable value: the index and, when in range, the referenced TGIBlock.
    /// </summary>
    public string Value => ValueBuilder.Replace("Data: ", "", StringComparison.Ordinal);
}
