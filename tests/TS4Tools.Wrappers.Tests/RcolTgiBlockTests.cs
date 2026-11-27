using FluentAssertions;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

/// <summary>
/// Tests for RcolTgiBlock parsing and writing.
/// </summary>
public class RcolTgiBlockTests
{
    [Fact]
    public void Read_ITGOrder_ParsesCorrectly()
    {
        // Arrange - ITG order: Instance (8 bytes), Type (4 bytes), Group (4 bytes)
        var bytes = new byte[16];
        BinaryPrimitives.WriteUInt64LittleEndian(bytes, 0x123456789ABCDEF0); // Instance
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), 0x01661233); // Type (MODL)
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12), 0x00000001); // Group

        // Act
        var tgi = RcolTgiBlock.Read(bytes);

        // Assert
        tgi.Instance.Should().Be(0x123456789ABCDEF0);
        tgi.ResourceType.Should().Be(0x01661233);
        tgi.ResourceGroup.Should().Be(0x00000001);
    }

    [Fact]
    public void Write_ToBytes_PreservesValues()
    {
        // Arrange
        var tgi = new RcolTgiBlock(0xAABBCCDDEEFF0011, 0x01D0E75D, 0x12345678);
        var bytes = new byte[16];

        // Act
        tgi.Write(bytes);

        // Assert
        BinaryPrimitives.ReadUInt64LittleEndian(bytes).Should().Be(0xAABBCCDDEEFF0011);
        BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(8)).Should().Be(0x01D0E75D);
        BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(12)).Should().Be(0x12345678);
    }

    [Fact]
    public void Read_ThenWrite_RoundTrips()
    {
        // Arrange
        var original = new byte[16];
        BinaryPrimitives.WriteUInt64LittleEndian(original, 0x1111222233334444);
        BinaryPrimitives.WriteUInt32LittleEndian(original.AsSpan(8), 0x55556666);
        BinaryPrimitives.WriteUInt32LittleEndian(original.AsSpan(12), 0x77778888);

        // Act
        var tgi = RcolTgiBlock.Read(original);
        var output = new byte[16];
        tgi.Write(output);

        // Assert
        output.Should().Equal(original);
    }

    [Fact]
    public void ToResourceKey_ConvertsCorrectly()
    {
        // Arrange
        var tgi = new RcolTgiBlock(0xDEADBEEFCAFEBABE, 0x01661233, 0x00000001);

        // Act
        var key = tgi.ToResourceKey();

        // Assert
        key.ResourceType.Should().Be(0x01661233);
        key.ResourceGroup.Should().Be(0x00000001);
        key.Instance.Should().Be(0xDEADBEEFCAFEBABE);
    }

    [Fact]
    public void FromResourceKey_ConvertsCorrectly()
    {
        // Arrange
        var key = new ResourceKey(0x01661233, 0x00000001, 0xDEADBEEFCAFEBABE);

        // Act
        var tgi = RcolTgiBlock.FromResourceKey(key);

        // Assert
        tgi.ResourceType.Should().Be(0x01661233);
        tgi.ResourceGroup.Should().Be(0x00000001);
        tgi.Instance.Should().Be(0xDEADBEEFCAFEBABE);
    }

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        // Arrange
        var tgi1 = new RcolTgiBlock(0x1234, 0x5678, 0x9ABC);
        var tgi2 = new RcolTgiBlock(0x1234, 0x5678, 0x9ABC);

        // Assert
        tgi1.Should().Be(tgi2);
        (tgi1 == tgi2).Should().BeTrue();
        (tgi1 != tgi2).Should().BeFalse();
        tgi1.GetHashCode().Should().Be(tgi2.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        // Arrange
        var tgi1 = new RcolTgiBlock(0x1234, 0x5678, 0x9ABC);
        var tgi2 = new RcolTgiBlock(0x1234, 0x5678, 0x9ABD); // Different group

        // Assert
        tgi1.Should().NotBe(tgi2);
        (tgi1 == tgi2).Should().BeFalse();
        (tgi1 != tgi2).Should().BeTrue();
    }

    [Fact]
    public void ToString_FormatsCorrectly()
    {
        // Arrange
        var tgi = new RcolTgiBlock(0x123456789ABCDEF0, 0x01661233, 0x00000001);

        // Act
        var str = tgi.ToString();

        // Assert
        str.Should().Be("0x01661233:0x00000001:0x123456789ABCDEF0");
    }

    [Fact]
    public void Read_TooShortData_ThrowsArgumentException()
    {
        // Arrange
        var bytes = new byte[10]; // Too short

        // Act & Assert
        var act = () => RcolTgiBlock.Read(bytes);
        act.Should().Throw<ArgumentException>().WithMessage("*too short*");
    }

    [Fact]
    public void Write_TooShortDestination_ThrowsArgumentException()
    {
        // Arrange
        var tgi = new RcolTgiBlock(0, 0, 0);
        var bytes = new byte[10]; // Too short

        // Act & Assert
        var act = () => tgi.Write(bytes);
        act.Should().Throw<ArgumentException>().WithMessage("*too short*");
    }

    [Fact]
    public void Read_ZeroValues_ParsesCorrectly()
    {
        // Arrange - all zeros
        var bytes = new byte[16];

        // Act
        var tgi = RcolTgiBlock.Read(bytes);

        // Assert
        tgi.Instance.Should().Be(0);
        tgi.ResourceType.Should().Be(0);
        tgi.ResourceGroup.Should().Be(0);
    }

    [Fact]
    public void ZeroTgi_RoundTrips()
    {
        // Arrange
        var tgi = new RcolTgiBlock(0, 0, 0);
        var bytes = new byte[16];

        // Act
        tgi.Write(bytes);
        var parsed = RcolTgiBlock.Read(bytes);

        // Assert
        parsed.Should().Be(tgi);
    }

    [Fact]
    public void Read_EmptySpan_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        try
        {
            _ = RcolTgiBlock.Read(ReadOnlySpan<byte>.Empty);
            throw new Xunit.Sdk.XunitException("Expected ArgumentException was not thrown");
        }
        catch (ArgumentException ex)
        {
            ex.Message.Should().Contain("too short");
        }
    }

    [Fact]
    public void Write_MaxValues_PreservesCorrectly()
    {
        // Arrange - maximum values
        var tgi = new RcolTgiBlock(ulong.MaxValue, uint.MaxValue, uint.MaxValue);
        var bytes = new byte[16];

        // Act
        tgi.Write(bytes);
        var parsed = RcolTgiBlock.Read(bytes);

        // Assert
        parsed.Instance.Should().Be(ulong.MaxValue);
        parsed.ResourceType.Should().Be(uint.MaxValue);
        parsed.ResourceGroup.Should().Be(uint.MaxValue);
    }

    [Fact]
    public void Equals_ZeroBlocks_AreEqual()
    {
        // Arrange
        var tgi1 = new RcolTgiBlock(0, 0, 0);
        var tgi2 = new RcolTgiBlock(0, 0, 0);

        // Assert
        tgi1.Should().Be(tgi2);
        tgi1.GetHashCode().Should().Be(tgi2.GetHashCode());
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        // Arrange
        var tgi = new RcolTgiBlock(0x1234, 0x5678, 0x9ABC);

        // Assert
        tgi.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void ToString_ZeroValues_FormatsCorrectly()
    {
        // Arrange
        var tgi = new RcolTgiBlock(0, 0, 0);

        // Act
        var str = tgi.ToString();

        // Assert
        str.Should().Be("0x00000000:0x00000000:0x0000000000000000");
    }

    [Fact]
    public void Read_IgnoresBytesBeyondSize()
    {
        var data = new byte[24];
        BinaryPrimitives.WriteUInt64LittleEndian(data, 0x1122334455667788);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(8), 0xAABBCCDD);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(12), 0x01020304);
        data.AsSpan(16).Fill(0xFF);

        var tgi = RcolTgiBlock.Read(data);

        tgi.Should().Be(new RcolTgiBlock(0x1122334455667788, 0xAABBCCDD, 0x01020304));
    }

    [Fact]
    public void Read_FifteenBytes_ThrowsArgumentException()
    {
        var action = () => RcolTgiBlock.Read(new byte[15]);

        action.Should().Throw<ArgumentException>().WithMessage("*expected 16 bytes, got 15*");
    }

    [Fact]
    public void Write_FifteenByteDestination_ThrowsArgumentException()
    {
        var action = () => new RcolTgiBlock(1, 2, 3).Write(new byte[15]);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Write_LargerDestination_LeavesTrailingBytesUntouched()
    {
        var buffer = new byte[20];
        buffer.AsSpan().Fill(0xEE);

        new RcolTgiBlock(1, 2, 3).Write(buffer);

        buffer[16..].Should().OnlyContain(b => b == 0xEE);
        RcolTgiBlock.Read(buffer).Should().Be(new RcolTgiBlock(1, 2, 3));
    }

    [Fact]
    public void Layout_IsInstanceThenTypeThenGroup_LittleEndian()
    {
        var buffer = new byte[16];

        new RcolTgiBlock(0x0807060504030201, 0x0C0B0A09, 0x100F0E0D).Write(buffer);

        buffer.Should().Equal(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
    }

    [Fact]
    public void Equals_DifferingOnlyInOneField_ReturnsFalse()
    {
        var baseline = new RcolTgiBlock(1, 2, 3);

        baseline.Equals(new RcolTgiBlock(9, 2, 3)).Should().BeFalse();
        baseline.Equals(new RcolTgiBlock(1, 9, 3)).Should().BeFalse();
        baseline.Equals(new RcolTgiBlock(1, 2, 9)).Should().BeFalse();
        (baseline == new RcolTgiBlock(1, 2, 3)).Should().BeTrue();
        (baseline != new RcolTgiBlock(1, 2, 4)).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_EqualBlocks_HaveEqualHashes()
    {
        new RcolTgiBlock(5, 6, 7).GetHashCode().Should().Be(new RcolTgiBlock(5, 6, 7).GetHashCode());
    }

    [Fact]
    public void Equals_BoxedOtherType_ReturnsFalse()
    {
        new RcolTgiBlock(1, 2, 3).Equals((object)"not a tgi").Should().BeFalse();
        new RcolTgiBlock(1, 2, 3).Equals((object)new RcolTgiBlock(1, 2, 3)).Should().BeTrue();
    }

    [Fact]
    public void ResourceKey_RoundTrip_PreservesAllFields()
    {
        var tgi = new RcolTgiBlock(ulong.MaxValue, uint.MaxValue, 0);

        RcolTgiBlock.FromResourceKey(tgi.ToResourceKey()).Should().Be(tgi);
        tgi.ToResourceKey().ResourceType.Should().Be(uint.MaxValue);
        tgi.ToResourceKey().ResourceGroup.Should().Be(0u);
    }

    [Fact]
    public void ToString_MaxValues_UsesFixedWidthHex()
    {
        new RcolTgiBlock(ulong.MaxValue, uint.MaxValue, uint.MaxValue).ToString()
            .Should().Be("0xFFFFFFFF:0xFFFFFFFF:0xFFFFFFFFFFFFFFFF");
    }
}
