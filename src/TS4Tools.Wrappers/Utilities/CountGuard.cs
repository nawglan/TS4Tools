// Source: legacy parsers read counted lists with BinaryReader (e.g.
// legacy_references/Sims4Tools/s4pi Wrappers/StblResource/StblResource.cs lines 113-117, 311 and
// legacy_references/Sims4Tools/s4pi Wrappers/NameMapResource/NameMapResource.cs lines 59-60), which fail with
// EndOfStreamException once the data runs out. This guard makes that failure happen before a count read from the
// file is used to size a collection, so a tiny resource claiming billions of entries cannot exhaust memory.

namespace TS4Tools.Wrappers;

/// <summary>
/// Validates element counts read from resource data against the bytes that remain.
/// </summary>
internal static class CountGuard
{
    /// <summary>
    /// Checks that <paramref name="count"/> elements of at least <paramref name="minElementSize"/> bytes each fit in
    /// <paramref name="remainingBytes"/>.
    /// </summary>
    /// <param name="count">The count read from the data.</param>
    /// <param name="remainingBytes">Bytes left in the data after the count.</param>
    /// <param name="minElementSize">Smallest possible encoded size of one element (at least 1).</param>
    /// <param name="what">What is being counted, for the error message.</param>
    /// <returns><paramref name="count"/> as an <see cref="int"/>.</returns>
    /// <exception cref="ResourceFormatException">The count is negative or cannot fit in the remaining data.</exception>
    public static int Check(long count, long remainingBytes, int minElementSize, string what)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(minElementSize, 1);
        if (count < 0 || remainingBytes < 0 || count > remainingBytes / minElementSize)
            throw new ResourceFormatException(
                $"Invalid {what} count {count}: only {Math.Max(remainingBytes, 0)} bytes remain " +
                $"(each entry needs at least {minElementSize}).");
        return (int)count;
    }
}
