// Source: legacy_references/Sims4Tools/s4pi/Interfaces/TypedValue.cs lines 26-800

namespace s4pi.Interfaces;

/// <summary>
/// A tuple associating a data type (or class) with a value object (of the given type).
/// </summary>
[Serializable]
public class TypedValue
    : IComparable<TypedValue>, IEqualityComparer<TypedValue>, IEquatable<TypedValue>, IConvertible, ICloneable, ISerializable
{
    /// <summary>
    /// The data type.
    /// </summary>
    public readonly Type Type;

    /// <summary>
    /// The value.
    /// </summary>
    public readonly object? Value;

    private readonly string format = "";

    /// <summary>
    /// Create a new <see cref="TypedValue"/>.
    /// </summary>
    /// <param name="t">The data type.</param>
    /// <param name="v">The value.</param>
    public TypedValue(Type t, object? v) : this(t, v, "") { }

    /// <summary>
    /// Create a new <see cref="TypedValue"/>.
    /// </summary>
    /// <param name="t">The data type.</param>
    /// <param name="v">The value.</param>
    /// <param name="f">The default format for <see cref="ToString()"/> ("X" for hexadecimal).</param>
    public TypedValue(Type t, object? v, string f)
    {
        Type = t;
        Value = v;
        format = f;
    }

    /// <summary>
    /// Return the Value as a string using the default format.
    /// </summary>
    /// <param name="tv">The <see cref="TypedValue"/> to convert.</param>
    public static implicit operator string(TypedValue tv)
    {
        ArgumentNullException.ThrowIfNull(tv);
        return tv.ToString(tv.format);
    }

    /// <summary>
    /// Return the Value as a string using the default format.
    /// </summary>
    /// <returns>String representation of Value in the default format.</returns>
    public override string ToString() => ToString(format);

    /// <summary>
    /// Return the Value as a string using the given format.
    /// </summary>
    /// <param name="format">Format to use for result ("X" formats integers as 0x-prefixed hexadecimal).</param>
    /// <returns>String representation of Value in given format.</returns>
    /// <remarks>
    /// Source: TypedValue.cs lines 95-173. A null <see cref="Value"/> (which legacy dereferences) yields "".
    /// </remarks>
    public string ToString(string format)
    {
        if (Value is null) return "";

        if (format == "X")
        {
            if (Type == typeof(long)) return "0x" + ((long)Value).ToString("X16", CultureInfo.InvariantCulture);
            if (Type == typeof(ulong)) return "0x" + ((ulong)Value).ToString("X16", CultureInfo.InvariantCulture);
            if (Type == typeof(int)) return "0x" + ((int)Value).ToString("X8", CultureInfo.InvariantCulture);
            if (Type == typeof(uint)) return "0x" + ((uint)Value).ToString("X8", CultureInfo.InvariantCulture);
            if (Type == typeof(short)) return "0x" + ((short)Value).ToString("X4", CultureInfo.InvariantCulture);
            if (Type == typeof(ushort)) return "0x" + ((ushort)Value).ToString("X4", CultureInfo.InvariantCulture);
            if (Type == typeof(sbyte)) return "0x" + ((sbyte)Value).ToString("X2", CultureInfo.InvariantCulture);
            if (Type == typeof(byte)) return "0x" + ((byte)Value).ToString("X2", CultureInfo.InvariantCulture);

            // "well, no... but..." (legacy): floating point shown with four decimals in the current culture
            if (Type == typeof(double)) return ((double)Value).ToString("F4", CultureInfo.CurrentCulture);
            if (Type == typeof(float)) return ((float)Value).ToString("F4", CultureInfo.CurrentCulture);

            if (typeof(Enum).IsAssignableFrom(Type))
            {
                var etv = new TypedValue(Enum.GetUnderlyingType(Type), Value, "X");
                return string.Format(CultureInfo.CurrentCulture, "{0} ({1})", "" + etv, "" + Value);
            }
        }

        if (typeof(string).IsAssignableFrom(Type) || typeof(char[]).IsAssignableFrom(Type))
        {
            var s = typeof(string).IsAssignableFrom(Type) ? (string)Value : new string((char[])Value);
            // A zero char is shown with control-character escapes (legacy ToDisplayString).
            if (s.Contains((char)0, StringComparison.Ordinal))
                return ToDisplayString(s.ToCharArray());
            return s.Normalize();
        }

        if (Type.HasElementType) // it's an array
        {
            var elementType = Type.GetElementType()!;
            if (typeof(AApiVersionedFields).IsAssignableFrom(elementType))
                return FromAApiVersionedFieldsArray(elementType, (Array)Value);
            return FromSimpleArray((Array)Value);
        }

        return Value.ToString() ?? "";
    }

    #region ToString helpers

    // Source: TypedValue.cs lines 187-201
    private static string FromSimpleArray(Array ary)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < ary.Length; i++)
        {
            var v = ary.GetValue(i);
            var tv = new TypedValue(v?.GetType() ?? typeof(object), v, "X");
            sb.Append(CultureInfo.CurrentCulture, $" [{i:X}:'{tv}']");
            if (i % 16 == 15)
                sb.Append('\n');
        }
        return sb.ToString().TrimStart().TrimEnd('\n');
    }

    // Source: TypedValue.cs lines 203-220
    private static string FromAApiVersionedFieldsArray(Type type, Array ary)
    {
        var sb = new StringBuilder();
        var fmt = "[{0:X}" + (type.IsAbstract ? " {1}" : "") + "]: {2}\n";
        for (var i = 0; i < ary.Length; i++)
        {
            var value = (AApiVersionedFields)ary.GetValue(i)!;
            if (value.ContentFields.Contains("Value"))
                sb.Append(string.Format(CultureInfo.CurrentCulture, fmt, i, value.GetType(), value["Value"]));
            else
                sb.Append(string.Format(CultureInfo.CurrentCulture, fmt, i, value.GetType(), value));
        }
        return sb.ToString().Trim('\n');
    }

    // Source: TypedValue.cs lines 222-255 (note: LF is deliberately absent, so 10.."VT" is legacy's table)
    private static readonly string[] LowNames =
    [
        "NUL", "SOH", "STX", "ETX", "EOT", "ENQ", "ACK", "BEL",
        "BS", "HT", /*"LF",*/ "VT", "FF", "CR", "SO", "SI",
        "DLE", "DC1", "DC2", "DC3", "DC4", "NAK", "SYN", "ETB",
        "CAN", "EM", "SUB", "ESC", "FS", "GS", "RS", "US",
    ];

    // Source: TypedValue.cs lines 257-280
    private static string ToDisplayString(char[] text)
    {
        var t = new StringBuilder();
        foreach (var c in text)
        {
            if (c < 32 && c != '\n')
            {
                // Legacy indexes LowNames (31 entries, LF omitted) directly by the character value, so
                // characters after LF show the following name and US (0x1F) would overflow; preserve the
                // mapping and guard the overflow.
                t.Append('<').Append(c < LowNames.Length ? LowNames[c] : "US").Append('>');
            }
            else if (c == 127)
                t.Append("<DEL>");
            else if (c > 127)
                t.Append(CultureInfo.InvariantCulture, $"<U+{(int)c:X4}>");
            else
                t.Append(c);
        }
        return t.ToString().Normalize();
    }

    #endregion

    #region IComparable<TypedValue> Members

    /// <summary>
    /// Compare this <see cref="TypedValue"/> to another for sort order purposes.
    /// </summary>
    /// <param name="other">Target <see cref="TypedValue"/>.</param>
    /// <returns>A 32-bit signed integer that indicates the relative order of the objects being compared.</returns>
    /// <exception cref="NotImplementedException">Either this object's Type or the target's is not comparable.</exception>
    /// <exception cref="ArgumentException">The target is not comparable with this object.</exception>
    /// <remarks>
    /// Source: TypedValue.cs lines 319-326. Legacy tests <c>this.Type is IComparable</c>, which is never true for a
    /// <see cref="System.Type"/> instance, so legacy always throws and <see cref="AApiVersionedFields.Comparer{T}"/>
    /// (used by legacy PackageIndex.Sort) could never sort. The evident intent — the value types must be
    /// comparable — is implemented instead.
    /// </remarks>
    public int CompareTo(TypedValue? other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (!Type.IsAssignableFrom(other.Type) || !typeof(IComparable).IsAssignableFrom(Type) || !typeof(IComparable).IsAssignableFrom(other.Type))
            throw new NotImplementedException();
        return ((IComparable)Value!).CompareTo(other.Value);
    }

    #endregion

    #region IEqualityComparer<TypedValue> Members

    /// <summary>
    /// Determines whether the specified <see cref="TypedValue"/> instances are equal.
    /// </summary>
    /// <param name="x">The first <see cref="TypedValue"/> to compare.</param>
    /// <param name="y">The second <see cref="TypedValue"/> to compare.</param>
    /// <returns>true if the specified <see cref="TypedValue"/> instances are equal; otherwise, false.</returns>
    public bool Equals(TypedValue? x, TypedValue? y) => x is null ? y is null : x.Equals(y);

    /// <summary>
    /// Returns a hash code for the specified <see cref="TypedValue"/>.
    /// </summary>
    /// <param name="obj">The <see cref="TypedValue"/> for which a hash code is to be returned.</param>
    /// <returns>A hash code for the specified <see cref="TypedValue"/>.</returns>
    public int GetHashCode(TypedValue obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        return obj.GetHashCode();
    }

    #endregion

    #region IEquatable<TypedValue> Members

    /// <summary>
    /// Indicates whether the current <see cref="TypedValue"/> instance is equal to another (by Value).
    /// </summary>
    /// <param name="other">A <see cref="TypedValue"/> instance to compare with this instance.</param>
    /// <returns>true if the Values are equal; otherwise, false.</returns>
    public bool Equals(TypedValue? other) => other is not null && Equals(Value, other.Value);

    /// <summary>
    /// Determines whether the specified <see cref="object"/> is equal to the current <see cref="TypedValue"/>.
    /// </summary>
    /// <param name="obj">The <see cref="object"/> to compare with the current <see cref="TypedValue"/>.</param>
    /// <returns>true if <paramref name="obj"/> is a <see cref="TypedValue"/> with an equal Value.</returns>
    public override bool Equals(object? obj) => obj is TypedValue tv && Equals(tv);

    /// <summary>
    /// Returns the hash code for this instance (that of the Value).
    /// </summary>
    /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
    public override int GetHashCode() => Value?.GetHashCode() ?? 0;

    #endregion

    #region IConvertible Members

    /// <summary>
    /// Returns the <see cref="TypeCode"/> for this instance (always <see cref="TypeCode.String"/>).
    /// </summary>
    /// <returns><see cref="TypeCode.String"/>.</returns>
    public TypeCode GetTypeCode() => TypeCode.String;

    private TOut ConvertAs<TOut>()
    {
        if (typeof(TOut).IsAssignableFrom(Type))
            return (TOut)Value!;
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public bool ToBoolean(IFormatProvider? provider) => ConvertAs<bool>();
    /// <inheritdoc />
    public byte ToByte(IFormatProvider? provider) => ConvertAs<byte>();
    /// <inheritdoc />
    public char ToChar(IFormatProvider? provider) => ConvertAs<char>();
    /// <inheritdoc />
    public DateTime ToDateTime(IFormatProvider? provider) => ConvertAs<DateTime>();
    /// <inheritdoc />
    public decimal ToDecimal(IFormatProvider? provider) => ConvertAs<decimal>();
    /// <inheritdoc />
    public double ToDouble(IFormatProvider? provider) => ConvertAs<double>();
    /// <inheritdoc />
    public short ToInt16(IFormatProvider? provider) => ConvertAs<short>();
    /// <inheritdoc />
    public int ToInt32(IFormatProvider? provider) => ConvertAs<int>();
    /// <inheritdoc />
    public long ToInt64(IFormatProvider? provider) => ConvertAs<long>();
    /// <inheritdoc />
    public sbyte ToSByte(IFormatProvider? provider) => ConvertAs<sbyte>();
    /// <inheritdoc />
    public float ToSingle(IFormatProvider? provider) => ConvertAs<float>();
    /// <inheritdoc />
    public string ToString(IFormatProvider? provider) => ConvertAs<string>();
    /// <inheritdoc />
    public ushort ToUInt16(IFormatProvider? provider) => ConvertAs<ushort>();
    /// <inheritdoc />
    public uint ToUInt32(IFormatProvider? provider) => ConvertAs<uint>();
    /// <inheritdoc />
    public ulong ToUInt64(IFormatProvider? provider) => ConvertAs<ulong>();

    /// <inheritdoc />
    public object ToType(Type conversionType, IFormatProvider? provider)
    {
        ArgumentNullException.ThrowIfNull(conversionType);
        if (conversionType.IsAssignableFrom(Type))
            return Convert.ChangeType(Value, conversionType, provider)!;
        throw new NotImplementedException();
    }

    #endregion

    #region ICloneable Members

    /// <summary>
    /// Creates a new object that is a copy of the current instance.
    /// </summary>
    /// <returns>A new object that is a copy of this instance.</returns>
    /// <exception cref="NotImplementedException">The Type is not <see cref="ICloneable"/>.</exception>
    public object Clone()
    {
        if (typeof(ICloneable).IsAssignableFrom(Type))
            return new TypedValue(Type, ((ICloneable)Value!).Clone(), format);
        throw new NotImplementedException();
    }

    #endregion

    #region ISerializable Members

    /// <summary>
    /// Populates a <see cref="SerializationInfo"/> with the data needed to serialize the target object.
    /// </summary>
    /// <param name="info">The <see cref="SerializationInfo"/> to populate with data.</param>
    /// <param name="context">The destination for this serialization.</param>
    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        ArgumentNullException.ThrowIfNull(info);
        info.AddValue("Type", Type, typeof(Type));
        info.AddValue("Value", Value, Type);
        info.AddValue("format", format);
    }

    #endregion
}
