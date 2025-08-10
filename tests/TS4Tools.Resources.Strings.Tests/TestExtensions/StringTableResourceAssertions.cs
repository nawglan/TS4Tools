using FluentAssertions;
using FluentAssertions.Collections;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using TS4Tools.Resources.Strings;

namespace TS4Tools.Resources.Strings.Tests.TestExtensions;

/// <summary>
/// Custom FluentAssertions extensions for StringTableResource testing.
/// Provides behavior-focused assertions without duplicating business logic.
/// Follows roadmap testing guidelines: "Test Behavior, Not Implementation".
/// </summary>
public static class StringTableResourceAssertionExtensions
{
    /// <summary>
    /// Asserts that the binary data represents a valid STBL format.
    /// This tests the behavior outcome without duplicating parsing logic.
    /// </summary>
    /// <param name="assertions">The byte array assertions.</param>
    /// <returns>Continuation for method chaining.</returns>
    public static AndConstraint<ObjectAssertions> BeValidSTBLFormat(this ObjectAssertions assertions)
    {
        return BeValidSTBLFormat(assertions, string.Empty);
    }

    /// <summary>
    /// Asserts that the binary data represents a valid STBL format.
    /// This tests the behavior outcome without duplicating parsing logic.
    /// </summary>
    /// <param name="assertions">The byte array assertions.</param>
    /// <param name="because">Reason for the assertion.</param>
    /// <param name="becauseArgs">Arguments for the reason.</param>
    /// <returns>Continuation for method chaining.</returns>
    public static AndConstraint<ObjectAssertions> BeValidSTBLFormat(
        this ObjectAssertions assertions,
        string because = "",
        params object[] becauseArgs)
    {
        Execute.Assertion
            .ForCondition(assertions.Subject is byte[])
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected byte array to be valid STBL format, but it was not a byte array.");

        var data = (byte[])assertions.Subject!;

        Execute.Assertion
            .ForCondition(data.Length >= 19) // Minimum STBL header size
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected byte array to have minimum STBL header size of 19 bytes, but it was {0} bytes.", data.Length);

        // Test that it can be successfully parsed by the actual implementation
        Execute.Assertion
            .ForCondition(TryParseAsSTBL(data))
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected byte array to be parseable as valid STBL format, but parsing failed.");

        return new AndConstraint<ObjectAssertions>(assertions);
    }

    /// <summary>
    /// Asserts that the binary data starts with the correct STBL magic number.
    /// This tests behavior without exposing the actual magic number value.
    /// </summary>
    /// <param name="assertions">The byte array assertions.</param>
    /// <param name="because">Reason for the assertion.</param>
    /// <param name="becauseArgs">Arguments for the reason.</param>
    /// <returns>Continuation for method chaining.</returns>
    public static AndConstraint<ObjectAssertions> StartWithSTBLMagicNumber(
        this ObjectAssertions assertions,
        string because = "",
        params object[] becauseArgs)
    {
        Execute.Assertion
            .ForCondition(assertions.Subject is byte[])
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected byte array to start with STBL magic number, but it was not a byte array.");

        var data = (byte[])assertions.Subject!;

        Execute.Assertion
            .ForCondition(data.Length >= 4)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected byte array to have at least 4 bytes for magic number, but it was {0} bytes.", data.Length);

        Execute.Assertion
            .ForCondition(HasValidSTBLMagicNumber(data))
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected byte array to start with valid STBL magic number, but it started with different bytes.");

        return new AndConstraint<ObjectAssertions>(assertions);
    }

    /// <summary>
    /// Asserts that the binary data contains the expected number of string entries.
    /// This tests behavior by parsing and counting, not by duplicating binary logic.
    /// </summary>
    /// <param name="assertions">The byte array assertions.</param>
    /// <param name="expectedCount">Expected number of string entries.</param>
    /// <param name="because">Reason for the assertion.</param>
    /// <param name="becauseArgs">Arguments for the reason.</param>
    /// <returns>Continuation for method chaining.</returns>
    public static AndConstraint<ObjectAssertions> ContainStringEntryCount(
        this ObjectAssertions assertions,
        int expectedCount,
        string because = "",
        params object[] becauseArgs)
    {
        Execute.Assertion
            .ForCondition(assertions.Subject is byte[])
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected byte array to contain {0} string entries, but it was not a byte array.", expectedCount);

        var data = (byte[])assertions.Subject!;
        var actualCount = GetStringEntryCount(data);

        Execute.Assertion
            .ForCondition(actualCount == expectedCount)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected byte array to contain {0} string entries, but it contained {1}.", expectedCount, actualCount);

        return new AndConstraint<ObjectAssertions>(assertions);
    }

    /// <summary>
    /// Attempts to parse the binary data as STBL format using the actual implementation.
    /// This verifies behavior without duplicating parsing logic.
    /// </summary>
    private static bool TryParseAsSTBL(byte[] data)
    {
        try
        {
            var resource = StringTableResource.FromData(0, data);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if the binary data has valid STBL magic number using the actual implementation.
    /// </summary>
    private static bool HasValidSTBLMagicNumber(byte[] data)
    {
        try
        {
            var resource = StringTableResource.FromData(0, data);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the string entry count by parsing with the actual implementation.
    /// This tests behavior without duplicating counting logic.
    /// </summary>
    private static int GetStringEntryCount(byte[] data)
    {
        try
        {
            var resource = StringTableResource.FromData(0, data);
            return resource.Strings.Count;
        }
        catch
        {
            return -1; // Invalid format
        }
    }
}
