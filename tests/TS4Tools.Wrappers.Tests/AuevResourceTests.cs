using FluentAssertions;
using TS4Tools.Package;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

/// <summary>
/// Tests for <see cref="AuevResource"/>.
///
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MiscellaneousResource/AUEVResource.cs
///
/// Format:
/// - Magic: "AUEV" (4 bytes, little-endian 0x56455541)
/// - Version: uint32
/// - GroupCount: int32
/// - Content: string[GroupCount * 3]
///   - Each string: length (int32, includes null) + ASCII bytes + null terminator
/// </summary>
public class AuevResourceTests
{
    private static readonly ResourceKey TestKey = new(0xBDD82221, 0, 0);
    private static readonly string[] ExpectedXyz = ["x", "y", "z"];

    /// <summary>
    /// Creates valid AUEV resource data.
    /// </summary>
    private static byte[] CreateValidData(uint version, params string[] content)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Magic "AUEV"
        writer.Write(0x56455541u);

        // Version
        writer.Write(version);

        // GroupCount
        writer.Write(content.Length / 3);

        // Strings
        foreach (var str in content)
        {
            writer.Write(str.Length + 1); // Length includes null terminator
            writer.Write(System.Text.Encoding.ASCII.GetBytes(str));
            writer.Write((byte)0); // Null terminator
        }

        return ms.ToArray();
    }

    [Fact]
    public void Parse_ValidData_ExtractsFields()
    {
        // Arrange
        var data = CreateValidData(1, "first", "second", "third");

        // Act
        var resource = new AuevResource(TestKey, data);

        // Assert
        resource.Version.Should().Be(1);
        resource.GroupCount.Should().Be(1);
        resource.Content.Should().HaveCount(3);
        resource.Content[0].Should().Be("first");
        resource.Content[1].Should().Be("second");
        resource.Content[2].Should().Be("third");
    }

    [Fact]
    public void Parse_MultipleGroups_ExtractsAllGroups()
    {
        // Arrange
        var data = CreateValidData(2, "a1", "a2", "a3", "b1", "b2", "b3");

        // Act
        var resource = new AuevResource(TestKey, data);

        // Assert
        resource.Version.Should().Be(2);
        resource.GroupCount.Should().Be(2);
        resource.Content.Should().HaveCount(6);

        var group0 = resource.GetGroup(0);
        group0.First.Should().Be("a1");
        group0.Second.Should().Be("a2");
        group0.Third.Should().Be("a3");

        var group1 = resource.GetGroup(1);
        group1.First.Should().Be("b1");
        group1.Second.Should().Be("b2");
        group1.Third.Should().Be("b3");
    }

    [Fact]
    public void Parse_EmptyContent_ExtractsEmptyArray()
    {
        // Arrange - 0 groups
        var data = CreateValidData(1);

        // Act
        var resource = new AuevResource(TestKey, data);

        // Assert
        resource.Version.Should().Be(1);
        resource.GroupCount.Should().Be(0);
        resource.Content.Should().BeEmpty();
    }

    [Fact]
    public void Parse_InvalidMagic_ThrowsInvalidDataException()
    {
        // Arrange
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        // Act
        Action act = () => _ = new AuevResource(TestKey, data);

        // Assert
        act.Should().Throw<InvalidDataException>().WithMessage("*magic*");
    }

    [Fact]
    public void Parse_TruncatedData_ThrowsInvalidDataException()
    {
        // Arrange - only magic, no version/groupcount
        var data = new byte[] { 0x41, 0x55, 0x45, 0x56 };

        // Act
        Action act = () => _ = new AuevResource(TestKey, data);

        // Assert
        act.Should().Throw<InvalidDataException>().WithMessage("*too short*");
    }

    [Fact]
    public void Serialize_RoundTrip_PreservesData()
    {
        // Arrange
        var data = CreateValidData(5, "hello", "world", "test");
        var resource = new AuevResource(TestKey, data);

        // Act
        var serialized = resource.Data.ToArray();

        // Assert
        serialized.Should().BeEquivalentTo(data);
    }

    [Fact]
    public void Serialize_ModifiedContent_ProducesValidData()
    {
        // Arrange
        var data = CreateValidData(1, "a", "b", "c");
        var resource = new AuevResource(TestKey, data);
        resource.Content = ["x", "y", "z"];

        // Act
        var serialized = resource.Data;
        var reloaded = new AuevResource(TestKey, serialized);

        // Assert
        reloaded.Content.Should().BeEquivalentTo(ExpectedXyz);
    }

    [Fact]
    public void Content_Set_InvalidLength_ThrowsArgumentException()
    {
        // Arrange
        var data = CreateValidData(1, "a", "b", "c");
        var resource = new AuevResource(TestKey, data);

        // Act - 4 elements is not divisible by 3
        var invalidContent = new[] { "a", "b", "c", "d" };
        Action act = () => resource.Content = invalidContent;

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*multiple of 3*");
    }

    [Fact]
    public void GetGroup_ValidIndex_ReturnsGroupStrings()
    {
        // Arrange
        var data = CreateValidData(1, "first", "second", "third");
        var resource = new AuevResource(TestKey, data);

        // Act
        var (first, second, third) = resource.GetGroup(0);

        // Assert
        first.Should().Be("first");
        second.Should().Be("second");
        third.Should().Be("third");
    }

    [Fact]
    public void GetGroup_InvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var data = CreateValidData(1, "a", "b", "c");
        var resource = new AuevResource(TestKey, data);

        // Act
        Action act = () => resource.GetGroup(1);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void SetGroup_ValidIndex_UpdatesContent()
    {
        // Arrange
        var data = CreateValidData(1, "a", "b", "c");
        var resource = new AuevResource(TestKey, data);

        // Act
        resource.SetGroup(0, "x", "y", "z");

        // Assert
        resource.Content.Should().BeEquivalentTo(ExpectedXyz);
        resource.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void Version_Set_MarksAsDirty()
    {
        // Arrange
        var data = CreateValidData(1, "a", "b", "c");
        var resource = new AuevResource(TestKey, data);

        // Act
        resource.Version = 99;

        // Assert
        resource.Version.Should().Be(99);
        resource.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void EmptyData_InitializesDefaults()
    {
        // Arrange & Act
        var resource = new AuevResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Assert
        resource.Version.Should().Be(1);
        resource.GroupCount.Should().Be(0);
        resource.Content.Should().BeEmpty();
    }
}
