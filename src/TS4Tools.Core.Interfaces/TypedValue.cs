/***************************************************************************
 *  Copyright (C) 2025 TS4Tools Project                                    *
 *                                                                         *
 *  This file is part of TS4Tools                                         *
 *                                                                         *
 *  TS4Tools is free software: you can redistribute it and/or modify      *
 *  it under the terms of the GNU General Public License as published by   *
 *  the Free Software Foundation, either version 3 of the License, or      *
 *  (at your option) any later version.                                    *
 *                                                                         *
 *  TS4Tools is distributed in the hope that it will be useful,           *
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of         *
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the          *
 *  GNU General Public License for more details.                           *
 *                                                                         *
 *  You should have received a copy of the GNU General Public License      *
 *  along with TS4Tools.  If not, see <http://www.gnu.org/licenses/>.     *
 ***************************************************************************/

namespace TS4Tools.Core.Interfaces;

/// <summary>
/// A record that associates a data type with a value object of that type
/// </summary>
/// <param name="Type">The data type</param>
/// <param name="Value">The value</param>
/// <param name="Format">The default format for string conversion</param>
public readonly record struct TypedValue(Type Type, object? Value, string Format = "")
    : IComparable<TypedValue>, IEquatable<TypedValue>
{
    /// <summary>
    /// Creates a TypedValue with a generic type parameter
    /// </summary>
    /// <typeparam name="T">The type of the value</typeparam>
    /// <param name="value">The value</param>
    /// <param name="format">The format string</param>
    /// <returns>A new TypedValue</returns>
    public static TypedValue Create<T>(T value, string format = "") =>
        new(typeof(T), value, format);

    /// <summary>
    /// Gets the value cast to the specified type
    /// </summary>
    /// <typeparam name="T">The target type</typeparam>
    /// <returns>The value cast to type T</returns>
    public T? GetValue<T>() => Value is T value ? value : default;

    /// <summary>
    /// Implicit conversion to string
    /// </summary>
    /// <param name="tv">The TypedValue to convert</param>
    public static implicit operator string(TypedValue tv) => tv.ToString();

    /// <summary>
    /// Returns a string representation of the value using the specified format
    /// </summary>
    /// <returns>Formatted string representation</returns>
    public override string ToString() => ToString(Format);

    /// <summary>
    /// Returns a string representation of the value using the specified format
    /// </summary>
    /// <param name="format">Format to use</param>
    /// <returns>Formatted string representation</returns>
    public string ToString(string format)
    {
        if (Value is null) return "null";
        
        return format switch
        {
            "X" when Value is long l => $"0x{l:X16}",
            "X" when Value is int i => $"0x{i:X8}",
            "X" when Value is short s => $"0x{s:X4}",
            "X" when Value is byte b => $"0x{b:X2}",
            "X" when Value is ulong ul => $"0x{ul:X16}",
            "X" when Value is uint ui => $"0x{ui:X8}",
            "X" when Value is ushort us => $"0x{us:X4}",
            "X" when Value is sbyte sb => $"0x{sb:X2}",
            "" => Value.ToString() ?? "null",
            _ => string.Format($"{{0:{format}}}", Value)
        };
    }

    /// <summary>
    /// Compares two TypedValue instances
    /// </summary>
    public int CompareTo(TypedValue other)
    {
        var typeComparison = string.Compare(Type.FullName, other.Type.FullName, StringComparison.Ordinal);
        if (typeComparison != 0) return typeComparison;

        return (Value, other.Value) switch
        {
            (null, null) => 0,
            (null, _) => -1,
            (_, null) => 1,
            (IComparable comparable, var otherValue) => comparable.CompareTo(otherValue),
            _ => string.Compare(Value.ToString(), other.Value?.ToString(), StringComparison.Ordinal)
        };
    }
}
