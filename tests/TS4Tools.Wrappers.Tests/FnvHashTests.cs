using FluentAssertions;
using TS4Tools.Wrappers.Hashing;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

/// <summary>
/// Tests for <see cref="FnvHash"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/CS System Classes/FNVHash.cs
/// - The legacy s4pi uses FNV-1 variant (multiply then XOR), with ASCII encoding
///   and automatic lowercase conversion.
/// - Our implementation uses FNV-1a variant (XOR then multiply), which is the
///   standard algorithm used in newer Sims 4 content. This matches what the game
///   engine actually uses for most string hashing.
/// - FNV-1a constants (same as legacy):
///   - 32-bit: prime=0x01000193, offset=0x811C9DC5
///   - 64-bit: prime=0x00000100000001B3, offset=0xCBF29CE484222325
/// - Test values verified against: https://www.tools4noobs.com/online_tools/hash/
///   and the FNV reference implementation at http://www.isthe.com/chongo/tech/comp/fnv/
/// </summary>
public class FnvHashTests
{
    [Fact]
    public void Fnv32_EmptyString_ReturnsOffsetBasis()
    {
        // FNV-1a offset basis for 32-bit
        // This is the standard starting value defined by the FNV algorithm
        // Source: http://www.isthe.com/chongo/tech/comp/fnv/#FNV-param
        const uint expected = 0x811C9DC5;

        var result = FnvHash.Fnv32("");

        result.Should().Be(expected);
    }

    [Fact]
    public void Fnv32_KnownValue_ReturnsExpectedHash()
    {
        // FNV-1a 32-bit hash of "hello"
        // Verified against: https://www.tools4noobs.com/online_tools/hash/
        // Algorithm: For each byte: hash ^= byte; hash *= prime
        var result = FnvHash.Fnv32("hello");

        // FNV-1a("hello") = 0x4F9F2CAB
        result.Should().Be(0x4F9F2CAB);
    }

    [Fact]
    public void Fnv32_SameInput_ReturnsSameHash()
    {
        const string input = "test_string";

        var hash1 = FnvHash.Fnv32(input);
        var hash2 = FnvHash.Fnv32(input);

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void Fnv32_DifferentInput_ReturnsDifferentHash()
    {
        var hash1 = FnvHash.Fnv32("string1");
        var hash2 = FnvHash.Fnv32("string2");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void Fnv64_EmptyString_ReturnsOffsetBasis()
    {
        // FNV-1a offset basis for 64-bit
        // Legacy source: FNV64() constructor in legacy_references/Sims4Tools/CS System Classes/FNVHash.cs:128
        const ulong expected = 0xCBF29CE484222325;

        var result = FnvHash.Fnv64("");

        result.Should().Be(expected);
    }

    [Fact]
    public void Fnv64_KnownValue_ReturnsExpectedHash()
    {
        // FNV-1a 64-bit hash of "hello"
        // Verified against: https://www.tools4noobs.com/online_tools/hash/
        var result = FnvHash.Fnv64("hello");

        // FNV-1a 64-bit("hello") = 0xA430D84680AABD0B
        result.Should().Be(0xA430D84680AABD0B);
    }

    [Fact]
    public void Fnv32Lower_CaseInsensitive()
    {
        var hash1 = FnvHash.Fnv32Lower("HELLO");
        var hash2 = FnvHash.Fnv32Lower("hello");
        var hash3 = FnvHash.Fnv32Lower("HeLLo");

        hash1.Should().Be(hash2);
        hash2.Should().Be(hash3);
    }

    [Fact]
    public void Fnv64Lower_CaseInsensitive()
    {
        var hash1 = FnvHash.Fnv64Lower("HELLO");
        var hash2 = FnvHash.Fnv64Lower("hello");

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void Fnv32_Bytes_SameAsString()
    {
        const string input = "test";
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);

        var hashFromString = FnvHash.Fnv32(input);
        var hashFromBytes = FnvHash.Fnv32(bytes);

        hashFromString.Should().Be(hashFromBytes);
    }

    [Fact]
    public void Fnv64_Bytes_SameAsString()
    {
        const string input = "test";
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);

        var hashFromString = FnvHash.Fnv64(input);
        var hashFromBytes = FnvHash.Fnv64(bytes);

        hashFromString.Should().Be(hashFromBytes);
    }

    [Fact]
    public void Fnv32_Null_ThrowsArgumentNullException()
    {
        var act = () => FnvHash.Fnv32((string)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Fnv64_Null_ThrowsArgumentNullException()
    {
        var act = () => FnvHash.Fnv64((string)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Fnv32_UnicodeString_HashesCorrectly()
    {
        // Unicode characters should hash correctly
        var hash = FnvHash.Fnv32("日本語");

        hash.Should().NotBe(0);
    }
}
