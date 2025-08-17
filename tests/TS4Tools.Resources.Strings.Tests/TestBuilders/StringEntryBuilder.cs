using TS4Tools.Resources.Strings;

namespace TS4Tools.Resources.Strings.Tests.TestBuilders;

/// <summary>
/// Fluent builder for creating StringEntry instances in tests.
/// Follows roadmap testing guidelines by providing behavior-focused test setup.
/// </summary>
public sealed class StringEntryBuilder
{
    private uint _key = 0x12345678u;
    private string _value = "Default Test String";

    /// <summary>
    /// Sets the key for the string entry.
    /// </summary>
    /// <param name="key">The string key/hash.</param>
    /// <returns>This builder for method chaining.</returns>
    public StringEntryBuilder WithKey(uint key)
    {
        _key = key;
        return this;
    }

    /// <summary>
    /// Sets the value for the string entry.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <returns>This builder for method chaining.</returns>
    public StringEntryBuilder WithValue(string value)
    {
        _value = value;
        return this;
    }

    /// <summary>
    /// Creates a string entry with an empty value.
    /// </summary>
    /// <returns>This builder for method chaining.</returns>
    public StringEntryBuilder WithEmptyValue()
    {
        _value = string.Empty;
        return this;
    }

    /// <summary>
    /// Creates a string entry with Unicode content.
    /// </summary>
    /// <returns>This builder for method chaining.</returns>
    public StringEntryBuilder WithUnicodeValue()
    {
        _value = "Unicode: 测试 🎮 Ñoël";
        return this;
    }

    /// <summary>
    /// Creates a string entry with special characters.
    /// </summary>
    /// <returns>This builder for method chaining.</returns>
    public StringEntryBuilder WithSpecialCharacters()
    {
        _value = "Special: \n\t\"'\\";
        return this;
    }

    /// <summary>
    /// Creates a string entry with a large value for testing.
    /// </summary>
    /// <param name="size">Size of the string to generate.</param>
    /// <returns>This builder for method chaining.</returns>
    public StringEntryBuilder WithLargeValue(int size = 1000)
    {
        _value = new string('A', size);
        return this;
    }

    /// <summary>
    /// Builds the configured StringEntry.
    /// </summary>
    /// <returns>The configured StringEntry instance.</returns>
    public StringEntry Build()
    {
        return new StringEntry(_key, _value);
    }

    /// <summary>
    /// Creates a string entry builder with default test values.
    /// </summary>
    /// <returns>A new StringEntryBuilder.</returns>
    public static StringEntryBuilder Default => new();

    /// <summary>
    /// Creates a string entry builder with common test data.
    /// </summary>
    /// <returns>A new StringEntryBuilder with sample data.</returns>
    public static StringEntryBuilder WithSampleData()
    {
        return new StringEntryBuilder()
            .WithKey(0x12345678u)
            .WithValue("Hello World");
    }
}
