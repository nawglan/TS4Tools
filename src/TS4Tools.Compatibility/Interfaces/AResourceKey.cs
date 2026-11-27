// Source: legacy_references/Sims4Tools/s4pi/Interfaces/AResourceKey.cs lines 23-233

namespace s4pi.Interfaces;

/// <summary>
/// An abstract class, descended from <see cref="AHandlerElement"/>, providing an abstract implementation of
/// <see cref="IResourceKey"/>, representing the Type, Group and Instance data of a resource key.
/// </summary>
public abstract class AResourceKey : AHandlerElement, IResourceKey
{
    #region Attributes
    /// <summary>
    /// Type of resource.
    /// </summary>
    protected uint resourceType;

    /// <summary>
    /// Group of resource.
    /// </summary>
    protected uint resourceGroup;

    /// <summary>
    /// Instance value of resource.
    /// </summary>
    protected ulong instance;
    #endregion

    /// <summary>
    /// Initialize a new instance.
    /// </summary>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the key changes.</param>
    protected AResourceKey(int apiVersion, EventHandler? handler) : base(apiVersion, handler) { }

    /// <summary>
    /// Initialize a new instance from <paramref name="basis"/>.
    /// </summary>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the key changes.</param>
    /// <param name="basis">The <see cref="IResourceKey"/> values to use to initialise the instance.</param>
    protected AResourceKey(int apiVersion, EventHandler? handler, IResourceKey basis)
        : this(apiVersion, handler, (basis ?? throw new ArgumentNullException(nameof(basis))).ResourceType, basis.ResourceGroup, basis.Instance) { }

    /// <summary>
    /// Initialize a new instance with the given values.
    /// </summary>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the key changes.</param>
    /// <param name="resourceType">The type of the resource.</param>
    /// <param name="resourceGroup">The group of the resource.</param>
    /// <param name="instance">The instance value of the resource.</param>
    protected AResourceKey(int apiVersion, EventHandler? handler, uint resourceType, uint resourceGroup, ulong instance)
        : base(apiVersion, handler)
    {
        this.resourceType = resourceType;
        this.resourceGroup = resourceGroup;
        this.instance = instance;
    }

    #region IResourceKey Members

    /// <summary>
    /// The "type" of the resource.
    /// </summary>
    [ElementPriority(1)]
    public virtual uint ResourceType
    {
        get => resourceType;
        set
        {
            if (resourceType != value)
            {
                resourceType = value;
                OnElementChanged();
            }
        }
    }

    /// <summary>
    /// The "group" the resource is part of.
    /// </summary>
    [ElementPriority(2)]
    public virtual uint ResourceGroup
    {
        get => resourceGroup;
        set
        {
            if (resourceGroup != value)
            {
                resourceGroup = value;
                OnElementChanged();
            }
        }
    }

    /// <summary>
    /// The "instance" number of the resource.
    /// </summary>
    [ElementPriority(3)]
    public virtual ulong Instance
    {
        get => instance;
        set
        {
            if (instance != value)
            {
                instance = value;
                OnElementChanged();
            }
        }
    }

    #endregion

    #region IEqualityComparer<IResourceKey> Members

    /// <summary>
    /// Determines whether the specified <see cref="IResourceKey"/> instances are equal.
    /// </summary>
    /// <param name="x">The first <see cref="IResourceKey"/> to compare.</param>
    /// <param name="y">The second <see cref="IResourceKey"/> to compare.</param>
    /// <returns>true if the specified <see cref="IResourceKey"/> instances are equal; otherwise, false.</returns>
    public bool Equals(IResourceKey? x, IResourceKey? y) => x is null ? y is null : x.Equals(y);

    /// <summary>
    /// Returns a hash code for the specified <see cref="IResourceKey"/>.
    /// </summary>
    /// <param name="obj">The <see cref="IResourceKey"/> for which a hash code is to be returned.</param>
    /// <returns>A hash code for the specified <see cref="IResourceKey"/>.</returns>
    public int GetHashCode(IResourceKey obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        return obj.GetHashCode();
    }

    /// <summary>
    /// Returns a hash code for this instance (XOR of the Type, Group and Instance hash codes).
    /// </summary>
    /// <returns>A hash code for this instance.</returns>
    public override int GetHashCode() => ResourceType.GetHashCode() ^ ResourceGroup.GetHashCode() ^ Instance.GetHashCode();

    #endregion

    #region IEquatable<IResourceKey> Members

    /// <summary>
    /// Indicates whether the current <see cref="IResourceKey"/> instance is equal to another <see cref="IResourceKey"/> instance.
    /// </summary>
    /// <param name="other">An <see cref="IResourceKey"/> instance to compare with this instance.</param>
    /// <returns>true if the Type, Group and Instance are equal; otherwise, false.</returns>
    public bool Equals(IResourceKey? other) => other is not null && CompareTo(other) == 0;

    /// <summary>
    /// Determines whether the specified <see cref="object"/> is equal to the current <see cref="AResourceKey"/>.
    /// </summary>
    /// <param name="obj">The <see cref="object"/> to compare with the current <see cref="AResourceKey"/>.</param>
    /// <returns>true if <paramref name="obj"/> is an <see cref="AResourceKey"/> with equal values.</returns>
    public override bool Equals(object? obj) => obj is AResourceKey rk && Equals(rk);

    #endregion

    #region IComparable<IResourceKey> Members

    /// <summary>
    /// Compare this <see cref="IResourceKey"/> to <paramref name="other"/>: by ResourceType, then ResourceGroup, then Instance.
    /// </summary>
    /// <param name="other">The <see cref="IResourceKey"/> to compare with this instance.</param>
    /// <returns>A signed integer that indicates the relative values of this instance and <paramref name="other"/>.</returns>
    public int CompareTo(IResourceKey? other)
    {
        ArgumentNullException.ThrowIfNull(other);
        int res = ResourceType.CompareTo(other.ResourceType);
        if (res != 0) return res;
        res = ResourceGroup.CompareTo(other.ResourceGroup);
        if (res != 0) return res;
        return Instance.CompareTo(other.Instance);
    }

    #endregion

    /// <summary>
    /// Converts the string representation of an <see cref="IResourceKey"/> to its equivalent values,
    /// storing them in <paramref name="result"/>. A return value indicates whether the conversion succeeded.
    /// </summary>
    /// <param name="value">
    /// A string containing a key to convert: <c>0xTTTTTTTT-0xGGGGGGGG-0xIIIIIIIIIIIIIIII</c> or
    /// <c>[key:]TTTTTTTT:GGGGGGGG:IIIIIIIIIIIIIIII</c> (hexadecimal, case-insensitive).
    /// </param>
    /// <param name="result">The <see cref="IResourceKey"/> to receive the values on success (unchanged on failure).</param>
    /// <returns>true if <paramref name="value"/> was converted successfully; otherwise, false.</returns>
    /// <remarks>Source: AResourceKey.cs lines 182-217</remarks>
    public static bool TryParse(string? value, IResourceKey result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (value == null) return false;

        uint t, g;
        ulong i;

        // Two formats 0x{X8}-0x{X8}-0x{X16} and (key:){X8}:{X8}:{X16}
        if (value.Contains('-', StringComparison.Ordinal))
        {
            string[] tgi = value.Trim().ToLowerInvariant().Split('-');
            if (tgi.Length != 3) return false;
            foreach (var x in tgi)
                if (!x.StartsWith("0x", StringComparison.Ordinal)) return false;

            if (!uint.TryParse(tgi[0].AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out t)) return false;
            if (!uint.TryParse(tgi[1].AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g)) return false;
            if (!ulong.TryParse(tgi[2].AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out i)) return false;
        }
        else
        {
            string[] tgi = value.Trim().ToLowerInvariant().Split(':');
            if (tgi.Length == 4 && tgi[0] == "key")
                Array.Copy(tgi, 1, tgi, 0, 3);
            else if (tgi.Length != 3) return false;

            if (!uint.TryParse(tgi[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out t)) return false;
            if (!uint.TryParse(tgi[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g)) return false;
            if (!ulong.TryParse(tgi[2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out i)) return false;
        }

        result.ResourceType = t;
        result.ResourceGroup = g;
        result.Instance = i;

        return true;
    }

    /// <summary>
    /// Converts an <see cref="AResourceKey"/> to a string representation (<c>0xTTTTTTTT-0xGGGGGGGG-0xIIIIIIIIIIIIIIII</c>).
    /// </summary>
    /// <param name="value">The <see cref="AResourceKey"/> to convert.</param>
    public static implicit operator string(AResourceKey value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return string.Format(CultureInfo.InvariantCulture, "0x{0:X8}-0x{1:X8}-0x{2:X16}", value.ResourceType, value.ResourceGroup, value.Instance);
    }

    /// <summary>
    /// Returns a string representation of this <see cref="AResourceKey"/> (<c>0xTTTTTTTT-0xGGGGGGGG-0xIIIIIIIIIIIIIIII</c>).
    /// </summary>
    /// <returns>The key as a string.</returns>
    public override string ToString() => this;

    /// <summary>
    /// Converts this key to a TS4Tools <see cref="TS4Tools.ResourceKey"/>.
    /// </summary>
    internal TS4Tools.ResourceKey ToResourceKey() => new(ResourceType, ResourceGroup, Instance);
}
