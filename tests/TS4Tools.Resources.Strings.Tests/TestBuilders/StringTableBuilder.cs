using TS4Tools.Resources.Strings;

namespace TS4Tools.Resources.Strings.Tests.TestBuilders;

/// <summary>
/// Fluent builder for creating StringTableResource instances in tests.
/// Follows the roadmap testing guidelines by providing behavior-focused test setup
/// without exposing or duplicating implementation details.
/// </summary>
public sealed class StringTableBuilder : IDisposable
{
    private readonly StringTableResource _resource = new();
    private bool _disposed;

    /// <summary>
    /// Adds a string entry to the string table.
    /// </summary>
    /// <param name="key">The string key/hash.</param>
    /// <param name="value">The string value.</param>
    /// <returns>This builder for method chaining.</returns>
    public StringTableBuilder WithString(uint key, string value)
    {
        _resource.SetString(key, value);
        return this;
    }

    /// <summary>
    /// Adds multiple string entries from a dictionary.
    /// </summary>
    /// <param name="strings">Dictionary of key-value pairs to add.</param>
    /// <returns>This builder for method chaining.</returns>
    public StringTableBuilder WithStrings(Dictionary<uint, string> strings)
    {
        foreach (var (key, value) in strings)
        {
            _resource.SetString(key, value);
        }
        return this;
    }

    /// <summary>
    /// Creates a string table with a specific number of test strings for stress testing.
    /// </summary>
    /// <param name="count">Number of test strings to generate.</param>
    /// <param name="prefix">Prefix for generated string values.</param>
    /// <returns>This builder for method chaining.</returns>
    public StringTableBuilder WithTestStrings(int count, string prefix = "TestString")
    {
        for (uint i = 0; i < count; i++)
        {
            _resource.SetString(0x10000000u + i, $"{prefix}_{i}");
        }
        return this;
    }

    /// <summary>
    /// Creates a string table with empty strings for edge case testing.
    /// </summary>
    /// <param name="count">Number of empty string entries to add.</param>
    /// <returns>This builder for method chaining.</returns>
    public StringTableBuilder WithEmptyStrings(int count = 1)
    {
        for (uint i = 0; i < count; i++)
        {
            _resource.SetString(0x20000000u + i, string.Empty);
        }
        return this;
    }

    /// <summary>
    /// Creates a string table with Unicode/special character strings for testing.
    /// </summary>
    /// <returns>This builder for method chaining.</returns>
    public StringTableBuilder WithUnicodeStrings()
    {
        _resource.SetString(0x30000001u, "Unicode: 测试 🎮 Ñoël");
        _resource.SetString(0x30000002u, "Emoji: 🎯🚀📊✅");
        _resource.SetString(0x30000003u, "Special: \n\t\"'\\");
        return this;
    }

    /// <summary>
    /// Builds the configured StringTableResource.
    /// </summary>
    /// <returns>The configured StringTableResource instance.</returns>
    public StringTableResource Build()
    {
        return _resource;
    }

    /// <summary>
    /// Creates an empty string table for basic testing.
    /// </summary>
    /// <returns>A new StringTableBuilder with an empty string table.</returns>
    public static StringTableBuilder Empty => new();

    /// <summary>
    /// Creates a string table with common test data.
    /// </summary>
    /// <returns>A new StringTableBuilder with sample test data.</returns>
    public static StringTableBuilder WithSampleData()
    {
        return new StringTableBuilder()
            .WithString(0x12345678u, "Hello World")
            .WithString(0x87654321u, "Test String")
            .WithString(0xABCDEF00u, "Sample Value");
    }

    /// <summary>
    /// Disposes the underlying StringTableResource.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _resource?.Dispose();
            _disposed = true;
        }
    }
}
