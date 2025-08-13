using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace TS4Tools.Resources.Images;

/// <summary>
/// Represents a 4-component RGBA color entry for LRLE resources.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct LRLEColorEntry(byte R, byte G, byte B, byte A)
{
    /// <summary>
    /// Creates a ColorEntry from a byte array.
    /// </summary>
    public static LRLEColorEntry FromBytes(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 4)
            throw new ArgumentException("Color bytes must be at least 4 bytes long", nameof(bytes));

        return new LRLEColorEntry(bytes[0], bytes[1], bytes[2], bytes[3]);
    }

    /// <summary>
    /// Converts this color entry to a byte array.
    /// </summary>
    public byte[] ToByteArray() => [R, G, B, A];

    /// <summary>
    /// Gets the color as a span of bytes.
    /// </summary>
    public ReadOnlySpan<byte> AsSpan()
    {
        return MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in this), 1));
    }

    /// <summary>
    /// Normalizes the color (sets RGB to 0 if alpha is 0).
    /// </summary>
    public LRLEColorEntry Normalize()
    {
        return A == 0 ? new LRLEColorEntry(0, 0, 0, 0) : this;
    }
}

/// <summary>
/// Color palette management for LRLE resources.
/// Provides efficient color indexing and lookup for compression.
/// </summary>
public sealed class LRLEColorTable : IDisposable
{
    /// <summary>
    /// Color table entry data with usage statistics.
    /// </summary>
    private sealed class ColorData
    {
        public int Index { get; set; }
        public int Count { get; set; } = 1;

        public ColorData(int index)
        {
            Index = index;
        }
    }

    private readonly ConcurrentDictionary<LRLEColorEntry, ColorData> _colors;
    private volatile bool _disposed;
    private readonly object _sortLock = new();
    private bool _isSorted;

    /// <summary>
    /// Gets the number of unique colors in the palette.
    /// </summary>
    public int Count => _colors.Count;

    /// <summary>
    /// Gets whether the color table has been sorted by usage.
    /// </summary>
    public bool IsSorted => _isSorted;

    /// <summary>
    /// Initializes a new instance of the <see cref="LRLEColorTable"/> class.
    /// </summary>
    public LRLEColorTable()
    {
        _colors = new ConcurrentDictionary<LRLEColorEntry, ColorData>();
    }

    /// <summary>
    /// Adds a color to the palette or increments its usage count.
    /// </summary>
    /// <param name="color">The color to add.</param>
    public void AddColor(LRLEColorEntry color)
    {
        ThrowIfDisposed();

        // Normalize color (set RGB to 0 if alpha is 0)
        var normalizedColor = color.Normalize();

        _colors.AddOrUpdate(normalizedColor,
            static (_, count) => new ColorData((int)count),
            static (_, existing, _) => { existing.Count++; return existing; },
            (uint)_colors.Count);

        // Mark as unsorted when adding colors
        if (_isSorted)
        {
            lock (_sortLock)
            {
                _isSorted = false;
            }
        }
    }

    /// <summary>
    /// Adds a color from a byte array.
    /// </summary>
    /// <param name="colorBytes">RGBA color bytes.</param>
    public void AddColor(ReadOnlySpan<byte> colorBytes)
    {
        var color = LRLEColorEntry.FromBytes(colorBytes);
        AddColor(color);
    }

    /// <summary>
    /// Checks if the palette contains a specific color.
    /// </summary>
    /// <param name="color">The color to check for.</param>
    /// <returns>True if the color exists in the palette.</returns>
    public bool HasColor(LRLEColorEntry color)
    {
        ThrowIfDisposed();
        var normalizedColor = color.Normalize();
        return _colors.ContainsKey(normalizedColor);
    }

    /// <summary>
    /// Gets the index of a color in the palette.
    /// </summary>
    /// <param name="color">The color to find.</param>
    /// <returns>The color index, or -1 if not found.</returns>
    public int GetColorIndex(LRLEColorEntry color)
    {
        ThrowIfDisposed();
        var normalizedColor = color.Normalize();

        if (_colors.TryGetValue(normalizedColor, out var colorData))
        {
            return colorData.Index;
        }

        return -1;
    }

    /// <summary>
    /// Gets the index of a color from byte array.
    /// </summary>
    /// <param name="colorBytes">RGBA color bytes.</param>
    /// <returns>The color index, or -1 if not found.</returns>
    public int GetColorIndex(ReadOnlySpan<byte> colorBytes)
    {
        var color = LRLEColorEntry.FromBytes(colorBytes);
        return GetColorIndex(color);
    }

    /// <summary>
    /// Sorts colors by usage frequency (most used first).
    /// This optimizes compression by assigning lower indices to frequently used colors.
    /// </summary>
    public void SortByUsage()
    {
        ThrowIfDisposed();

        lock (_sortLock)
        {
            if (_isSorted) return;

            var sortedColors = _colors
                .OrderByDescending(kvp => kvp.Value.Count)
                .ThenBy(kvp => kvp.Key.R) // Secondary sort for deterministic results
                .ThenBy(kvp => kvp.Key.G)
                .ThenBy(kvp => kvp.Key.B)
                .ThenBy(kvp => kvp.Key.A)
                .ToList();

            // Update indices based on sorted order
            for (int i = 0; i < sortedColors.Count; i++)
            {
                sortedColors[i].Value.Index = i;
            }

            _isSorted = true;
        }
    }

    /// <summary>
    /// Gets all colors as a dictionary with their indices and usage counts.
    /// </summary>
    /// <returns>Dictionary of colors with their data.</returns>
    public IReadOnlyDictionary<LRLEColorEntry, (int Index, int Count)> GetColors()
    {
        ThrowIfDisposed();

        return _colors.ToDictionary(
            kvp => kvp.Key,
            kvp => (kvp.Value.Index, kvp.Value.Count));
    }

    /// <summary>
    /// Gets the color palette as an array of colors sorted by index.
    /// </summary>
    /// <returns>Array of colors in index order.</returns>
    public LRLEColorEntry[] GetPalette()
    {
        ThrowIfDisposed();

        var palette = new LRLEColorEntry[_colors.Count];

        foreach (var kvp in _colors)
        {
            if (kvp.Value.Index >= 0 && kvp.Value.Index < palette.Length)
            {
                palette[kvp.Value.Index] = kvp.Key;
            }
        }

        return palette;
    }

    /// <summary>
    /// Clears all colors from the palette.
    /// </summary>
    public void Clear()
    {
        ThrowIfDisposed();
        _colors.Clear();

        lock (_sortLock)
        {
            _isSorted = false;
        }
    }

    /// <summary>
    /// Gets usage statistics for the color palette.
    /// </summary>
    /// <returns>Statistics including total usage, most/least used colors.</returns>
    public (int TotalUsage, LRLEColorEntry MostUsed, LRLEColorEntry LeastUsed) GetStatistics()
    {
        ThrowIfDisposed();

        if (_colors.Count == 0)
        {
            return (0, default, default);
        }

        int totalUsage = 0;
        var mostUsed = default(KeyValuePair<LRLEColorEntry, ColorData>);
        var leastUsed = default(KeyValuePair<LRLEColorEntry, ColorData>);
        bool first = true;

        foreach (var kvp in _colors)
        {
            totalUsage += kvp.Value.Count;

            if (first || kvp.Value.Count > mostUsed.Value.Count)
            {
                mostUsed = kvp;
            }

            if (first || kvp.Value.Count < leastUsed.Value.Count)
            {
                leastUsed = kvp;
            }

            first = false;
        }

        return (totalUsage, mostUsed.Key, leastUsed.Key);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(LRLEColorTable));
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="LRLEColorTable"/>.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _colors.Clear();
            _disposed = true;
        }
    }
}
