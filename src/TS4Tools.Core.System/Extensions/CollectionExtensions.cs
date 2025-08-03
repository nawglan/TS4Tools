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

using System.Globalization;

namespace TS4Tools.Core.System.Extensions;

/// <summary>
/// Useful Extension Methods not provided by Linq (and without deferred execution).
/// </summary>
public static class ArrayExtensions
{
    /// <summary>
    /// Convert all elements of an <c>Array</c> to <typeparamref name="TOut"/>.
    /// </summary>
    /// <typeparam name="TOut">The output element type.</typeparam>
    /// <param name="array">The input array</param>
    /// <returns>An <c>TOut[]</c> array containing converted input elements.</returns>
    /// <exception cref="InvalidCastException">The element type of <paramref name="array"/> does not provide the <c>IConvertible</c> interface.</exception>
    public static TOut[] ConvertAll<TOut>(this Array array) where TOut : IConvertible 
        => array.ConvertAll<TOut>(0, array.Length, CultureInfo.CurrentCulture);

    /// <summary>
    /// Convert all elements of an <c>Array</c> to <typeparamref name="TOut"/>.
    /// </summary>
    /// <typeparam name="TOut">The output element type.</typeparam>
    /// <param name="array">The input array</param>
    /// <param name="provider">An <c>System.IFormatProvider</c> interface implementation that supplies culture-specific formatting information.</param>
    /// <returns>An <c>TOut[]</c> array containing converted input elements.</returns>
    /// <exception cref="InvalidCastException">The element type of <paramref name="array"/> does not provide the <c>IConvertible</c> interface.</exception>
    public static TOut[] ConvertAll<TOut>(this Array array, IFormatProvider provider) where TOut : IConvertible 
        => array.ConvertAll<TOut>(0, array.Length, provider);

    /// <summary>
    /// Convert elements of an <c>Array</c> to <typeparamref name="TOut"/>,
    /// starting at <paramref name="start"/> for <paramref name="length"/> elements.
    /// </summary>
    /// <typeparam name="TOut">The output element type.</typeparam>
    /// <param name="array">The input array</param>
    /// <param name="start">The offset into <paramref name="array"/> from which to start creating the output.</param>
    /// <param name="length">The number of elements in the output.</param>
    /// <returns>An <c>TOut[]</c> array containing converted input elements.</returns>
    /// <exception cref="InvalidCastException">The element type of <paramref name="array"/> does not provide the <c>IConvertible</c> interface.
    /// <br/>-or-<br/>
    /// this conversion is not supported.
    /// <br/>-or-<br/>
    /// an <paramref name="array"/> element is null and <typeparamref name="TOut"/> is a value type.
    /// </exception>
    /// <exception cref="IndexOutOfRangeException"><paramref name="start"/> is outside the bounds of <paramref name="array"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="length"/> has an invalid value.</exception>
    public static TOut[] ConvertAll<TOut>(this Array array, int start, int length) where TOut : IConvertible 
        => array.ConvertAll<TOut>(start, length, CultureInfo.CurrentCulture);

    /// <summary>
    /// Convert elements of an <c>Array</c> to <typeparamref name="TOut"/>,
    /// starting at <paramref name="start"/> for <paramref name="length"/> elements.
    /// </summary>
    /// <typeparam name="TOut">The output element type.</typeparam>
    /// <param name="array">The input array</param>
    /// <param name="start">The offset into <paramref name="array"/> from which to start creating the output.</param>
    /// <param name="length">The number of elements in the output.</param>
    /// <param name="provider">An <c>System.IFormatProvider</c> interface implementation that supplies culture-specific formatting information.</param>
    /// <returns>An <c>TOut[]</c> array containing converted input elements.</returns>
    /// <exception cref="InvalidCastException">The element type of <paramref name="array"/> does not provide the <c>IConvertible</c> interface.
    /// <br/>-or-<br/>
    /// this conversion is not supported.
    /// <br/>-or-<br/>
    /// an <paramref name="array"/> element is null and <typeparamref name="TOut"/> is a value type.
    /// </exception>
    /// <exception cref="IndexOutOfRangeException"><paramref name="start"/> is outside the bounds of <paramref name="array"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="length"/> has an invalid value.</exception>
    public static TOut[] ConvertAll<TOut>(this Array array, int start, int length, IFormatProvider provider) where TOut : IConvertible
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentNullException.ThrowIfNull(provider);
        
        var elementType = array.GetType().GetElementType();
        if (elementType is null || !typeof(IConvertible).IsAssignableFrom(elementType))
            throw new InvalidCastException($"{elementType?.Name ?? "Unknown"} is not IConvertible");

        ArgumentOutOfRangeException.ThrowIfGreaterThan(start, array.Length, nameof(start));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(length, array.Length - start, nameof(length));
        ArgumentOutOfRangeException.ThrowIfNegative(start, nameof(start));
        ArgumentOutOfRangeException.ThrowIfNegative(length, nameof(length));

        var result = new TOut[length];
        var list = (IList)array;

        for (int i = 0; i < result.Length; i++)
        {
            var convertedValue = Convert.ChangeType(list[i + start], typeof(TOut), provider);
            result[i] = (TOut)convertedValue!;
        }

        return result;
    }
}

/// <summary>
/// Extension methods for <see cref="IList{T}"/> collections.
/// </summary>
public static class ListExtensions
{
    /// <summary>
    /// Compares this instance to a specified list of type <typeparamref name="T"/>
    /// and returns an indication of their relative values.
    /// </summary>
    /// <typeparam name="T">A type supporting <c>IComparable{T}.</c></typeparam>
    /// <param name="value">This instance.</param>
    /// <param name="target">A list to compare.</param>
    /// <returns>An indication of the relative value of this instance and the specified list.</returns>
    public static int CompareTo<T>(this IList<T>? value, IList<T>? target) where T : IComparable<T>
    {
        if (value is null)
            return target is null ? 0 : -1;
        
        if (target is null) 
            return 1;

        var minCount = Math.Min(value.Count, target.Count);
        
        for (int i = 0; i < minCount; i++) 
        { 
            var comparison = value[i].CompareTo(target[i]); 
            if (comparison != 0) 
                return comparison; 
        }
        
        return value.Count.CompareTo(target.Count);
    }

    /// <summary>
    /// Returns a value indicating whether this instance is equal to a specified list of <typeparamref name="T"/> values.
    /// </summary>
    /// <typeparam name="T">A type supporting <see cref="IEquatable{T}"/>.</typeparam>
    /// <param name="value">This instance.</param>
    /// <param name="target">A list to compare.</param>
    /// <returns>And indication of the equality of the values of this instance and the specified list.</returns>
    public static bool Equals<T>(this IList<T>? value, IList<T>? target) where T : IEquatable<T>
    {
        if (value is null)
            return target is null;
        
        if (target is null) 
            return false;

        if (value.Count != target.Count) 
            return false;
        
        for (int i = 0; i < value.Count; i++) 
        {
            if (!value[i].Equals(target[i])) 
                return false;
        }
        
        return true;
    }
}
