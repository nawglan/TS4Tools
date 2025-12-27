using System.Buffers.Binary;
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
}
