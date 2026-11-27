// Source: legacy_references/Sims4Tools/s4pi/Interfaces/TGIBlock.cs lines 26-268

namespace s4pi.Interfaces;

/// <summary>
/// An implementation of AResourceKey that supports storing the Type, Group and Instance in any order.
/// </summary>
/// <remarks>Stream I/O writes each field little-endian (T and G 32-bit, I 64-bit) in the block's <see cref="Order"/>.</remarks>
public class TGIBlock : AResourceKey, IEquatable<TGIBlock>
{
    #region Attributes
    private const int recommendedApiVersion = 1;
    private readonly string order = "TGI";
    #endregion

    #region Constructors

    /// <summary>
    /// Used to indicate the order of the Type, Group and Instance values when stored.
    /// </summary>
    public enum Order
    {
        /// <summary>Type, Group, Instance</summary>
        TGI,
        /// <summary>Type, Instance, Group</summary>
        TIG,
        /// <summary>Group, Type, Instance</summary>
        GTI,
        /// <summary>Group, Instance, Type</summary>
        GIT,
        /// <summary>Instance, Type, Group</summary>
        ITG,
        /// <summary>Instance, Group, Type</summary>
        IGT,
    }

    // Source: TGIBlock.cs lines 71-72
    private static string Ok(string v)
    {
        Ok(Enum.Parse<Order>(v));
        return v;
    }

    private static string Ok(Order v)
    {
        if (!Enum.IsDefined(v)) throw new ArgumentException("Invalid value " + v, nameof(v));
        return v.ToString();
    }

    /// <summary>
    /// Initialize a new instance as a copy of <paramref name="basis"/> (including its order).
    /// </summary>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the block changes.</param>
    /// <param name="basis">The TGIBlock to copy.</param>
    public TGIBlock(int apiVersion, EventHandler? handler, TGIBlock basis)
        : this(apiVersion, handler, (basis ?? throw new ArgumentNullException(nameof(basis))).order, (IResourceKey)basis) { }

    /// <summary>
    /// Initialize a new "TGI" order instance with zero values.
    /// </summary>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the block changes.</param>
    public TGIBlock(int apiVersion, EventHandler? handler) : base(apiVersion, handler, 0, 0, 0) { }

    /// <summary>
    /// Initialize a new instance with zero values and the given order.
    /// </summary>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the block changes.</param>
    /// <param name="order">A string ("TGI", "ITG", ...) giving the order.</param>
    public TGIBlock(int apiVersion, EventHandler? handler, string order) : this(apiVersion, handler) { this.order = Ok(order); }

    /// <summary>
    /// Initialize a new instance with zero values and the given order.
    /// </summary>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the block changes.</param>
    /// <param name="order">The order.</param>
    public TGIBlock(int apiVersion, EventHandler? handler, Order order) : this(apiVersion, handler) { this.order = Ok(order); }

    /// <summary>
    /// Initialize a new "TGI" order instance with the given values.
    /// </summary>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the block changes.</param>
    /// <param name="resourceType">The resource type value.</param>
    /// <param name="resourceGroup">The resource group value.</param>
    /// <param name="instance">The resource instance value.</param>
    public TGIBlock(int apiVersion, EventHandler? handler, uint resourceType, uint resourceGroup, ulong instance)
        : base(apiVersion, handler, resourceType, resourceGroup, instance) { }

    /// <summary>
    /// Initialize a new instance with the given order and values.
    /// </summary>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the block changes.</param>
    /// <param name="order">A string ("TGI", "ITG", ...) giving the order.</param>
    /// <param name="resourceType">The resource type value.</param>
    /// <param name="resourceGroup">The resource group value.</param>
    /// <param name="instance">The resource instance value.</param>
    public TGIBlock(int apiVersion, EventHandler? handler, string order, uint resourceType, uint resourceGroup, ulong instance)
        : this(apiVersion, handler, resourceType, resourceGroup, instance) { this.order = Ok(order); }

    /// <summary>
    /// Initialize a new instance with the given order and values.
    /// </summary>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the block changes.</param>
    /// <param name="order">The order.</param>
    /// <param name="resourceType">The resource type value.</param>
    /// <param name="resourceGroup">The resource group value.</param>
    /// <param name="instance">The resource instance value.</param>
    public TGIBlock(int apiVersion, EventHandler? handler, Order order, uint resourceType, uint resourceGroup, ulong instance)
        : this(apiVersion, handler, resourceType, resourceGroup, instance) { this.order = Ok(order); }

    /// <summary>
    /// Initialize a new "TGI" order instance from the values of <paramref name="rk"/>.
    /// </summary>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the block changes.</param>
    /// <param name="rk">The key to copy.</param>
    public TGIBlock(int apiVersion, EventHandler? handler, IResourceKey rk) : base(apiVersion, handler, rk) { }

    /// <summary>
    /// Initialize a new instance with the given order from the values of <paramref name="rk"/>.
    /// </summary>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the block changes.</param>
    /// <param name="order">A string ("TGI", "ITG", ...) giving the order.</param>
    /// <param name="rk">The key to copy.</param>
    public TGIBlock(int apiVersion, EventHandler? handler, string order, IResourceKey rk) : this(apiVersion, handler, rk) { this.order = Ok(order); }

    /// <summary>
    /// Initialize a new instance with the given order from the values of <paramref name="rk"/>.
    /// </summary>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the block changes.</param>
    /// <param name="order">The order.</param>
    /// <param name="rk">The key to copy.</param>
    public TGIBlock(int apiVersion, EventHandler? handler, Order order, IResourceKey rk) : this(apiVersion, handler, rk) { this.order = Ok(order); }

    /// <summary>
    /// Initialize a new "TGI" order instance read from <paramref name="s"/>.
    /// </summary>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the block changes.</param>
    /// <param name="s">The stream to read.</param>
    public TGIBlock(int apiVersion, EventHandler? handler, Stream s) : base(apiVersion, handler) { Parse(s); }

    /// <summary>
    /// Initialize a new instance with the given order read from <paramref name="s"/>.
    /// </summary>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the block changes.</param>
    /// <param name="order">A string ("TGI", "ITG", ...) giving the order.</param>
    /// <param name="s">The stream to read.</param>
    public TGIBlock(int apiVersion, EventHandler? handler, string order, Stream s) : base(apiVersion, handler)
    {
        this.order = Ok(order);
        Parse(s);
    }

    /// <summary>
    /// Initialize a new instance with the given order read from <paramref name="s"/>.
    /// </summary>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the block changes.</param>
    /// <param name="order">The order.</param>
    /// <param name="s">The stream to read.</param>
    public TGIBlock(int apiVersion, EventHandler? handler, Order order, Stream s) : base(apiVersion, handler)
    {
        this.order = Ok(order);
        Parse(s);
    }

    #endregion

    #region Data I/O

    /// <summary>
    /// Read the Type, Group and Instance values from <paramref name="s"/> in this block's order.
    /// </summary>
    /// <param name="s">The stream to read.</param>
    /// <remarks>Source: TGIBlock.cs lines 204-214</remarks>
    protected void Parse(Stream s)
    {
        using var r = LegacyBinary.Reader(s);
        foreach (char c in order)
        {
            switch (c)
            {
                case 'T': resourceType = r.ReadUInt32(); break;
                case 'G': resourceGroup = r.ReadUInt32(); break;
                case 'I': instance = r.ReadUInt64(); break;
            }
        }
    }

    /// <summary>
    /// Write the Type, Group and Instance values to <paramref name="s"/> in this block's order.
    /// </summary>
    /// <param name="s">The stream to write to.</param>
    /// <remarks>Source: TGIBlock.cs lines 220-230</remarks>
    public void UnParse(Stream s)
    {
        using var w = LegacyBinary.Writer(s);
        foreach (char c in order)
        {
            switch (c)
            {
                case 'T': w.Write(resourceType); break;
                case 'G': w.Write(resourceGroup); break;
                case 'I': w.Write(instance); break;
            }
        }
    }

    #endregion

    #region AHandlerElement

    /// <summary>
    /// The list of available field names on this API object.
    /// </summary>
    public override List<string> ContentFields => GetContentFields(requestedApiVersion, GetType());

    /// <summary>
    /// The best supported version of the API available.
    /// </summary>
    public override int RecommendedApiVersion => recommendedApiVersion;

    #endregion

    #region IEquatable<TGIBlock> Members

    /// <summary>
    /// Indicates whether the current <see cref="TGIBlock"/> instance is equal to another (by Type, Group and Instance).
    /// </summary>
    /// <param name="other">A <see cref="TGIBlock"/> instance to compare with this instance.</param>
    /// <returns>true if the keys are equal; otherwise, false.</returns>
    public bool Equals(TGIBlock? other) => base.Equals(other);

    /// <inheritdoc />
    public override bool Equals(object? obj) => base.Equals(obj);

    /// <inheritdoc />
    public override int GetHashCode() => base.GetHashCode();

    #endregion

    #region Content Fields

    /// <summary>
    /// A displayable string representing the block (<c>0xTTTTTTTT-0xGGGGGGGG-0xIIIIIIIIIIIIIIII</c>).
    /// </summary>
    public string Value => ToString();

    #endregion
}
