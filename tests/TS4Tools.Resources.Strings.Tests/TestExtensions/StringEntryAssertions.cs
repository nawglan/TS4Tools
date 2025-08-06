using FluentAssertions;
using FluentAssertions.Collections;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using TS4Tools.Resources.Strings;

namespace TS4Tools.Resources.Strings.Tests.TestExtensions;

/// <summary>
/// Custom FluentAssertions extensions for StringEntry testing.
/// Provides behavior-focused assertions without duplicating serialization logic.
/// Follows roadmap testing guidelines: "Test Behavior, Not Implementation".
/// </summary>
public static class StringEntryAssertionExtensions
{
    /// <summary>
    /// Asserts that a stream contains valid StringEntry binary data.
    /// This tests the behavior outcome without duplicating binary parsing logic.
    /// </summary>
    /// <param name="assertions">The stream assertions.</param>
    /// <param name="expectedKey">Expected string key.</param>
    /// <param name="expectedValue">Expected string value.</param>
    /// <param name="because">Reason for the assertion.</param>
    /// <param name="becauseArgs">Arguments for the reason.</param>
    /// <returns>Continuation for method chaining.</returns>
    public static AndConstraint<ObjectAssertions> ContainValidStringEntry(
        this ObjectAssertions assertions,
        uint expectedKey,
        string expectedValue,
        string because = "",
        params object[] becauseArgs)
    {
        // Handle both Stream and byte[] types
        Stream? stream = null;
        bool shouldDisposeStream = false;

        if (assertions.Subject is Stream s)
        {
            stream = s;
            stream.Position = 0;
        }
        else if (assertions.Subject is byte[] bytes)
        {
            stream = new MemoryStream(bytes);
            shouldDisposeStream = true;
        }
        else
        {
            Execute.Assertion
                .ForCondition(false)
                .BecauseOf(because, becauseArgs)
                .FailWith("Expected stream or byte array to contain valid StringEntry data, but it was {0}.", assertions.Subject?.GetType());
        }

        try
        {
            // Test by parsing with the actual implementation rather than duplicating logic
            var parsedEntry = ParseStringEntryFromStream(stream!);
            
            Execute.Assertion
                .ForCondition(parsedEntry is not null)
                .BecauseOf(because, becauseArgs)
                .FailWith("Expected stream to contain parseable StringEntry data, but parsing failed.");

            Execute.Assertion
                .ForCondition(parsedEntry.HasValue && parsedEntry.Value.Key == expectedKey)
                .BecauseOf(because, becauseArgs)
                .FailWith("Expected StringEntry key to be {0:X8}, but it was {1:X8}.", expectedKey, parsedEntry?.Key);

            Execute.Assertion
                .ForCondition(parsedEntry.HasValue && parsedEntry.Value.Value == expectedValue)
                .BecauseOf(because, becauseArgs)
                .FailWith("Expected StringEntry value to be '{0}', but it was '{1}'.", expectedValue, parsedEntry?.Value);
        }
        finally
        {
            if (shouldDisposeStream)
            {
                stream?.Dispose();
            }
        }

        return new AndConstraint<ObjectAssertions>(assertions);
    }
    
    /// <summary>
    /// Asserts that serialized StringEntry data can be round-trip parsed correctly.
    /// This tests serialization behavior without duplicating implementation logic.
    /// </summary>
    /// <param name="assertions">The StringEntry assertions.</param>
    /// <param name="because">Reason for the assertion.</param>
    /// <param name="becauseArgs">Arguments for the reason.</param>
    /// <returns>Continuation for method chaining.</returns>
    public static AndConstraint<ObjectAssertions> SerializeCorrectly(
        this ObjectAssertions assertions,
        string because = "",
        params object[] becauseArgs)
    {
        Execute.Assertion
            .ForCondition(assertions.Subject is StringEntry)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected object to be a StringEntry, but it was {0}.", assertions.Subject?.GetType());

        var originalEntry = (StringEntry)assertions.Subject!;
        
        // Test round-trip serialization behavior
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        
        originalEntry.WriteTo(writer);
        stream.Position = 0;
        
        var parsedEntry = ParseStringEntryFromStream(stream);
        
        Execute.Assertion
            .ForCondition(parsedEntry is not null)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected StringEntry to serialize and parse correctly, but parsing failed.");

        Execute.Assertion
            .ForCondition(parsedEntry.HasValue && parsedEntry.Value.Key == originalEntry.Key)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected round-trip key to be {0:X8}, but it was {1:X8}.", originalEntry.Key, parsedEntry?.Key);

        Execute.Assertion
            .ForCondition(parsedEntry.HasValue && parsedEntry.Value.Value == originalEntry.Value)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected round-trip value to be '{0}', but it was '{1}'.", originalEntry.Value, parsedEntry?.Value);

        return new AndConstraint<ObjectAssertions>(assertions);
    }
    
    /// <summary>
    /// Parses a StringEntry from a stream using the actual implementation.
    /// This delegates to real parsing logic rather than duplicating it.
    /// </summary>
    private static StringEntry? ParseStringEntryFromStream(Stream stream)
    {
        try
        {
            using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
            return StringEntry.ReadFrom(reader);
        }
        catch
        {
            return null;
        }
    }
}
