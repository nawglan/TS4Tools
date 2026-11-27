// Source: legacy_references/Sims4Tools/CS System Classes/Extensions.cs lines 25-165

namespace System;

/// <summary>
/// Useful extension methods not provided by the base class library (legacy s4pi helpers).
/// </summary>
public static class Extensions
{
    /// <summary>Convert all elements of an array to <typeparamref name="TOut"/> using the current culture.</summary>
    /// <typeparam name="TOut">The output element type.</typeparam>
    /// <param name="array">The source array (elements must be <see cref="IConvertible"/>).</param>
    /// <returns>The converted array.</returns>
    public static TOut[] ConvertAll<TOut>(this Array array) where TOut : IConvertible
        => array.ConvertAll<TOut>(0, array.Length, CultureInfo.CurrentCulture);

    /// <summary>Convert all elements of an array to <typeparamref name="TOut"/>.</summary>
    /// <typeparam name="TOut">The output element type.</typeparam>
    /// <param name="array">The source array.</param>
    /// <param name="provider">Culture-specific formatting information.</param>
    /// <returns>The converted array.</returns>
    public static TOut[] ConvertAll<TOut>(this Array array, IFormatProvider provider) where TOut : IConvertible
        => array.ConvertAll<TOut>(0, array.Length, provider);

    /// <summary>Convert the elements of an array from <paramref name="start"/> to <typeparamref name="TOut"/>.</summary>
    /// <typeparam name="TOut">The output element type.</typeparam>
    /// <param name="array">The source array.</param>
    /// <param name="start">The first element to convert.</param>
    /// <returns>The converted array.</returns>
    public static TOut[] ConvertAll<TOut>(this Array array, int start) where TOut : IConvertible
        => array.ConvertAll<TOut>(start, array.Length - start, CultureInfo.CurrentCulture);

    /// <summary>Convert the elements of an array from <paramref name="start"/> to <typeparamref name="TOut"/>.</summary>
    /// <typeparam name="TOut">The output element type.</typeparam>
    /// <param name="array">The source array.</param>
    /// <param name="start">The first element to convert.</param>
    /// <param name="provider">Culture-specific formatting information.</param>
    /// <returns>The converted array.</returns>
    public static TOut[] ConvertAll<TOut>(this Array array, int start, IFormatProvider provider) where TOut : IConvertible
        => array.ConvertAll<TOut>(start, array.Length - start, provider);

    /// <summary>Convert <paramref name="length"/> elements of an array from <paramref name="start"/> to <typeparamref name="TOut"/>.</summary>
    /// <typeparam name="TOut">The output element type.</typeparam>
    /// <param name="array">The source array.</param>
    /// <param name="start">The first element to convert.</param>
    /// <param name="length">The number of elements to convert.</param>
    /// <returns>The converted array.</returns>
    public static TOut[] ConvertAll<TOut>(this Array array, int start, int length) where TOut : IConvertible
        => array.ConvertAll<TOut>(start, length, CultureInfo.CurrentCulture);

    /// <summary>Convert <paramref name="length"/> elements of an array from <paramref name="start"/> to <typeparamref name="TOut"/>.</summary>
    /// <typeparam name="TOut">The output element type.</typeparam>
    /// <param name="array">The source array.</param>
    /// <param name="start">The first element to convert.</param>
    /// <param name="length">The number of elements to convert.</param>
    /// <param name="provider">Culture-specific formatting information.</param>
    /// <returns>The converted array.</returns>
    /// <remarks>Source: Extensions.cs lines 108-125</remarks>
    public static TOut[] ConvertAll<TOut>(this Array array, int start, int length, IFormatProvider provider) where TOut : IConvertible
    {
        ArgumentNullException.ThrowIfNull(array);
        var elementType = array.GetType().GetElementType()!;
        if (!typeof(IConvertible).IsAssignableFrom(elementType))
            throw new InvalidCastException(elementType.Name + " is not IConvertible");

        if (start > array.Length)
#pragma warning disable CA2201 // Legacy throws IndexOutOfRangeException here
            throw new IndexOutOfRangeException("'start' exceeds array length");
#pragma warning restore CA2201

        if (length > array.Length - start)
            throw new ArgumentException("'length' exceeds constrained element count");

        var res = new TOut[length];
        for (int i = 0; i < res.Length; i++)
            res[i] = (TOut)Convert.ChangeType(((IList)array)[i + start], typeof(TOut), provider)!;
        return res;
    }

    /// <summary>
    /// Compares two lists element by element, then by length.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="value">This list.</param>
    /// <param name="target">The list to compare to.</param>
    /// <returns>A signed number indicating the relative order.</returns>
    /// <remarks>Source: Extensions.cs lines 136-146</remarks>
    public static int CompareTo<T>(this IList<T>? value, IList<T>? target) where T : IComparable<T>
    {
        if (value == null)
            return target != null ? -1 : 0;
        if (target == null) return 1;

        int lim = Math.Min(value.Count, target.Count);
        for (int i = 0; i < lim; i++)
        {
            int cmp = value[i].CompareTo(target[i]);
            if (cmp != 0) return cmp;
        }
        return value.Count.CompareTo(target.Count);
    }

    /// <summary>
    /// Indicates whether two lists contain equal elements in the same order.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="value">This list.</param>
    /// <param name="target">The list to compare to.</param>
    /// <returns>True if the lists are element-wise equal.</returns>
    /// <remarks>Source: Extensions.cs lines 154-163</remarks>
    public static bool Equals<T>(this IList<T>? value, IList<T>? target) where T : IEquatable<T>
    {
        if (value == null)
            return target == null;
        if (target == null) return false;

        if (value.Count != target.Count) return false;
        for (int i = 0; i < value.Count; i++)
            if (!value[i].Equals(target[i])) return false;
        return true;
    }
}
