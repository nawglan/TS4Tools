using System.Buffers.Binary;
using FluentAssertions;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

/// <summary>
/// Tests for RcolChunkReference encoding and decoding.
/// Source: GenericRCOLResource.cs lines 385-660
/// </summary>
public class RcolChunkReferenceTests
{
    [Fact]
    public void Constructor_WithZero_IsNull()
    {
        // Arrange & Act
        var reference = new RcolChunkReference(0);

        // Assert
        reference.IsNull.Should().BeTrue();
        reference.Index.Should().Be(-1);
        reference.RawValue.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithPublicReference_DecodesCorrectly()
    {
        // Arrange - Public reference to index 0 (1-based = 1)
        // Type bits: 0x0, Index: 1 -> Raw value: 0x00000001
        var rawValue = 0x00000001u;

        // Act
        var reference = new RcolChunkReference(rawValue);

        // Assert
        reference.IsNull.Should().BeFalse();
        reference.RefType.Should().Be(RcolReferenceType.Public);
        reference.Index.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithPrivateReference_DecodesCorrectly()
    {
        // Arrange - Private reference to index 2 (1-based = 3)
        // Type bits: 0x1 << 28 = 0x10000000, Index: 3 -> Raw value: 0x10000003
        var rawValue = 0x10000003u;

        // Act
        var reference = new RcolChunkReference(rawValue);

        // Assert
        reference.IsNull.Should().BeFalse();
        reference.RefType.Should().Be(RcolReferenceType.Private);
        reference.Index.Should().Be(2);
    }

    [Fact]
    public void Constructor_WithDelayedReference_DecodesCorrectly()
    {
        // Arrange - Delayed reference to index 5 (1-based = 6)
        // Type bits: 0x3 << 28 = 0x30000000, Index: 6 -> Raw value: 0x30000006
        var rawValue = 0x30000006u;

        // Act
        var reference = new RcolChunkReference(rawValue);

        // Assert
        reference.IsNull.Should().BeFalse();
        reference.RefType.Should().Be(RcolReferenceType.Delayed);
        reference.Index.Should().Be(5);
    }

    [Fact]
    public void Constructor_FromTypeAndIndex_EncodesCorrectly()
    {
        // Arrange & Act
        var reference = new RcolChunkReference(RcolReferenceType.Private, 2);

        // Assert
        reference.RefType.Should().Be(RcolReferenceType.Private);
        reference.Index.Should().Be(2);
        reference.RawValue.Should().Be(0x10000003u);
    }

    [Fact]
    public void Constructor_FromTypeAndNegativeOneIndex_CreatesNull()
    {
        // Arrange & Act
        var reference = new RcolChunkReference(RcolReferenceType.Public, -1);

        // Assert
        reference.IsNull.Should().BeTrue();
        reference.RawValue.Should().Be(0);
    }

    [Fact]
    public void Read_FromBytes_DecodesCorrectly()
    {
        // Arrange
        var bytes = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, 0x20000005u);

        // Act
        var reference = RcolChunkReference.Read(bytes);

        // Assert
        reference.RefType.Should().Be(RcolReferenceType.External);
        reference.Index.Should().Be(4);
    }

    [Fact]
    public void Write_ToBytes_EncodesCorrectly()
    {
        // Arrange
        var reference = new RcolChunkReference(RcolReferenceType.Delayed, 10);
        var bytes = new byte[4];

        // Act
        reference.Write(bytes);

        // Assert
        var value = BinaryPrimitives.ReadUInt32LittleEndian(bytes);
        value.Should().Be(0x3000000Bu); // 0x3 << 28 | (10 + 1)
    }

    [Fact]
    public void Equals_SameValue_ReturnsTrue()
    {
        // Arrange
        var ref1 = new RcolChunkReference(0x10000003u);
        var ref2 = new RcolChunkReference(0x10000003u);

        // Assert
        ref1.Should().Be(ref2);
        (ref1 == ref2).Should().BeTrue();
        (ref1 != ref2).Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentValue_ReturnsFalse()
    {
        // Arrange
        var ref1 = new RcolChunkReference(0x10000003u);
        var ref2 = new RcolChunkReference(0x10000004u);

        // Assert
        ref1.Should().NotBe(ref2);
        (ref1 == ref2).Should().BeFalse();
        (ref1 != ref2).Should().BeTrue();
    }

    [Fact]
    public void ToString_NullReference_ReturnsNull()
    {
        // Arrange
        var reference = new RcolChunkReference(0);

        // Act
        var str = reference.ToString();

        // Assert
        str.Should().Be("(null)");
    }

    [Fact]
    public void ToString_ValidReference_ReturnsTypeAndIndex()
    {
        // Arrange
        var reference = new RcolChunkReference(RcolReferenceType.Private, 5);

        // Act
        var str = reference.ToString();

        // Assert
        str.Should().Be("Private[5]");
    }
}
