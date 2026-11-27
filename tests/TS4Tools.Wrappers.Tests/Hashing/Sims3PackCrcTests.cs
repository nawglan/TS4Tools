using FluentAssertions;
using TS4Tools.Wrappers.Hashing;
using Xunit;

namespace TS4Tools.Wrappers.Tests.Hashing;

/// <summary>
/// Tests for <see cref="Sims3PackCRC"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/CS System Classes/Sims3PackCRC.cs
/// - Seed 0x00FFFFFFFFFFFFFF, byte-swapped running value, table driven, result is the complement of the running CRC.
/// - Vectors computed by an independent transcription of the legacy algorithm using the legacy table.
/// </summary>
public class Sims3PackCrcTests
{
    [Fact]
    public void EmptyInput_IsComplementOfSeed()
    {
        Sims3PackCRC.CalculateCRC([]).Should().Be(0xFF00000000000000ul);
    }

    [Fact]
    public void CheckString_MatchesLegacyVector()
    {
        Sims3PackCRC.CalculateCRC("123456789"u8.ToArray()).Should().Be(0xF5EF783B6302902Dul);
    }

    [Fact]
    public void AllByteValues_MatchLegacyVector()
    {
        Sims3PackCRC.CalculateCRC(Enumerable.Range(0, 256).Select(i => (byte)i).ToArray()).Should().Be(0x2E2F116AD4128B9Dul);
    }

    [Fact]
    public void OffsetAndCount_SelectRegion()
    {
        var buffer = "xx123456789yy"u8.ToArray();

        Sims3PackCRC.CalculateCRC(buffer, 2, 9).Should().Be(0xF5EF783B6302902Dul);
        Sims3PackCRC.CalculateCRC(buffer, 2).Should().Be(Sims3PackCRC.CalculateCRC("123456789yy"u8.ToArray()));
    }

    [Fact]
    public void Stream_MatchesArray()
    {
        var data = Enumerable.Range(0, 5000).Select(i => (byte)(i * 7)).ToArray();

        Sims3PackCRC.CalculateCRC(new MemoryStream(data)).Should().Be(Sims3PackCRC.CalculateCRC(data));
    }

    [Fact]
    public void IncrementalHashing_MatchesSingleShot()
    {
        using var crc = new Sims3PackCRC();
        crc.TransformBlock("12345"u8.ToArray(), 0, 5, null, 0);
        crc.TransformFinalBlock("6789"u8.ToArray(), 0, 4);

        BitConverter.ToUInt64(crc.Hash!, 0).Should().Be(0xF5EF783B6302902Dul);
        crc.HashSize.Should().Be(64);
    }

    [Fact]
    public void CustomSeed_ChangesResult()
    {
        using var crc = new Sims3PackCRC(0);

        BitConverter.ToUInt64(crc.ComputeHash([]), 0).Should().Be(ulong.MaxValue);
        BitConverter.ToUInt64(crc.ComputeHash("abc"u8.ToArray()), 0).Should().NotBe(Sims3PackCRC.CalculateCRC("abc"u8.ToArray()));
    }

    [Fact]
    public void InvalidArguments_Throw()
    {
        ((Action)(() => Sims3PackCRC.CalculateCRC((byte[])null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => Sims3PackCRC.CalculateCRC(new byte[4], 5))).Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => Sims3PackCRC.CalculateCRC(new byte[4], 1, 4))).Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => Sims3PackCRC.CalculateCRC((Stream)null!))).Should().Throw<ArgumentNullException>();
    }
}
