using FluentAssertions;
using TS4Tools.Wrappers.Hashing;
using Xunit;

namespace TS4Tools.Wrappers.Tests.Hashing;

/// <summary>
/// Tests for the legacy FNV HashAlgorithm classes.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/CS System Classes/FNVHash.cs
/// - Legacy is FNV-1 (multiply then XOR), lower-cases and ASCII-encodes strings. FNV-1 reference vectors:
///   FNV1-32("a") = 0x050C5D7E and FNV1-64("a") = 0xAF63BD4C8601B7BE (isthe.com/chongo/tech/comp/fnv test vectors).
/// - Remaining vectors, including FNV24 folding (hash >> 24 ^ hash &amp; 0xFFFFFF) and FNV64CLIP age/species prefix masks,
///   were produced by an independent transcription of the legacy algorithm.
/// - The existing static FnvHash class implements FNV-1a and is intentionally different.
/// </summary>
public class FnvHashAlgorithmTests
{
    [Theory]
    [InlineData("", 0x811C9DC5u)]
    [InlineData("a", 0x050C5D7Eu)]
    [InlineData("Hello", 0xB6FA7167u)] // lower-cased -> "hello"
    [InlineData("hello", 0xB6FA7167u)]
    [InlineData("foobar", 0x31F0B262u)]
    public void Fnv32_MatchesLegacyVectors(string text, uint expected)
    {
        FNV32.GetHash(text).Should().Be(expected);
    }

    [Theory]
    [InlineData("", 0xCBF29CE484222325ul)]
    [InlineData("a", 0xAF63BD4C8601B7BEul)]
    [InlineData("Hello", 0x7B495389BDBDD4C7ul)]
    [InlineData("foobar", 0x340D8765A4DDA9C2ul)]
    public void Fnv64_MatchesLegacyVectors(string text, ulong expected)
    {
        FNV64.GetHash(text).Should().Be(expected);
    }

    [Theory]
    [InlineData("", 0x1C9D44u)]
    [InlineData("a", 0x0C5D7Bu)]
    [InlineData("Hello", 0xFA71D1u)]
    [InlineData("foobar", 0xF0B253u)]
    public void Fnv24_FoldsHighByteIntoLow24Bits(string text, uint expected)
    {
        FNV24.GetHash(text).Should().Be(expected);
        (FNV24.GetHash(text) & 0xFF000000).Should().Be(0u);
    }

    [Theory]
    [InlineData("b2a_stand", 0x89BD397DDD3E8FE4ul)]
    [InlineData("ad2ad_idle", 0x8583B2F49A640B72ul)]
    [InlineData("a2o_walk", 0x7E3718D84B7D8E9Dul)]
    [InlineData("o2o_run", 0x363FA4C779B70743ul)]
    [InlineData("stand_idle", 0x1E057C1A9DBCBABCul)]
    [InlineData("cd2o_x", 0xB619EBC64D478A06ul)]
    [InlineData("ab_x", 0xCADC97186BAF5687ul)]
    [InlineData("TOOLONGPREFIX_x", 0x6684E94D34F5F14Eul)]
    [InlineData("", 0x4BF29CE484222325ul)]
    public void Fnv64Clip_MatchesLegacyVectors(string text, ulong expected)
    {
        FNV64CLIP.GetHash(text).Should().Be(expected);
    }

    [Fact]
    public void Fnv64Clip_AgeMask_GoesInTopBitsAndClearsBit63()
    {
        // b2a: x=b (0x01), y=a -> only a2a_ shape remains hashed; mask 0x8000|0x01<<8|0 -> top 16 bits 0x8100 ^ hash bits
        var value = FNV64CLIP.GetHash("b2a_stand");
        var plain = FNV64.GetHash("a2a_stand") & 0x7FFFFFFFFFFFFFFF;

        (value ^ plain).Should().Be(0x8100ul << 48);
    }

    [Fact]
    public void Fnv64Clip_NoPrefixMask_ClearsOnlyTopBit()
    {
        FNV64CLIP.GetHash("stand_idle").Should().Be(FNV64.GetHash("stand_idle") & 0x7FFFFFFFFFFFFFFF);
    }

    [Theory]
    [InlineData("a2o_walk", "a2o_walk")]
    [InlineData("o2o_run", "o2o_run")]
    [InlineData("ad2cd_walk", "a2a_walk")]
    [InlineData("cd2o_x", "a2o_x")]
    [InlineData("b_idle", "a_idle")]
    [InlineData("a_idle", "a_idle")]
    [InlineData("stand_idle", "stand_idle")]
    [InlineData("TOOLONGPREFIX_x", "TOOLONGPREFIX_x")]
    [InlineData("noUnderscore", "noUnderscore")]
    public void GetGenericValue_RemovesAgeAndSpecies(string text, string expected)
    {
        FNV64CLIP.GetGenericValue(text).Should().Be(expected);
    }

    [Fact]
    public void GetHashGeneric_IgnoresAgeAndSpecies()
    {
        FNV64CLIP.GetHashGeneric("ad2cd_walk").Should().Be(FNV64CLIP.GetHashGeneric("b2c_walk"));
        FNV64CLIP.GetHashGeneric("b_idle").Should().Be(0x0862A6B95E58525Ful);
    }

    [Fact]
    public void HashAlgorithm_ComputeHash_ReturnsStateBytesLikeLegacy()
    {
        using var fnv = new FNV32();

        var bytes = fnv.ComputeHash("a");

        bytes.Should().HaveCount(8, "legacy HashFinal returns the 64 bit state");
        BitConverter.ToUInt32(bytes, 0).Should().Be(0x050C5D7Eu);
        fnv.HashSize.Should().Be(32);
        fnv.Hash.Should().Equal(BitConverter.GetBytes(0x050C5D7Eu));
    }

    [Fact]
    public void HashAlgorithm_InstanceCanBeReused()
    {
        using var fnv = new FNV64();

        var first = BitConverter.ToUInt64(fnv.ComputeHash("a"), 0);
        var second = BitConverter.ToUInt64(fnv.ComputeHash("a"), 0);

        second.Should().Be(first);
    }

    [Fact]
    public void HashAlgorithm_ByteOverloadDoesNotLowerCase()
    {
        using var fnv = new FNV32();

        BitConverter.ToUInt32(fnv.ComputeHash(new byte[] { (byte)'A' }), 0).Should().NotBe(FNV32.GetHash("A"));
        BitConverter.ToUInt32(fnv.ComputeHash(new byte[] { (byte)'a' }), 0).Should().Be(FNV32.GetHash("A"));
    }

    [Fact]
    public void HashAlgorithm_Streaming_MatchesSingleShot()
    {
        using var fnv = new FNV64();
        var data = "hello world"u8.ToArray();

        var streamed = BitConverter.ToUInt64(fnv.ComputeHash(new MemoryStream(data)), 0);

        streamed.Should().Be(FNV64.GetHash("hello world"));
    }

    [Fact]
    public void HashAlgorithm_Fnv24Instance_HashPropertyIsFolded()
    {
        using var fnv = new FNV24();
        fnv.ComputeHash("a");

        BitConverter.ToUInt32(fnv.Hash!, 0).Should().Be(0x0C5D7Bu);
        fnv.HashSize.Should().Be(32);
    }

    [Fact]
    public void NullArguments_Throw()
    {
        ((Action)(() => FNV32.GetHash(null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => FNV64CLIP.GetHash(null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => FNV64CLIP.GetGenericValue(null!))).Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("a")]
    [InlineData("Hello")]
    [InlineData("ObjectName_42")]
    public void StaticFnvHash_MatchesLegacyAlgorithms(string text)
    {
        // FnvHash delegates to the faithful legacy FNV-1 classes (CS System Classes/FNVHash.cs).
        FnvHash.Fnv32(text).Should().Be(FNV32.GetHash(text));
        FnvHash.Fnv64(text).Should().Be(FNV64.GetHash(text));
        FnvHash.Fnv24(text).Should().Be(FNV24.GetHash(text));
        FnvHash.Fnv32Lower(text).Should().Be(FNV32.GetHash(text));
    }
}
