using FluentAssertions;
using TS4Tools.Package;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

/// <summary>
/// Tests for <see cref="ThumWorldResource"/>.
///
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MiscellaneousResource/THMResource.cs
///
/// Format (80 bytes):
/// - version: uint32
/// - uint01: uint32
/// - uintlong02: uint64
/// - uint03: uint32
/// - selfReference: TGIBlock (ITG order: Instance, Type, Group - 16 bytes)
/// - uint04: uint32
/// - uint05: uint32
/// - magic: "THUM" (4 bytes)
/// - floats: 8 float values (32 bytes)
/// </summary>
public class ThumWorldResourceTests
{
    private static readonly ResourceKey TestKey = new(0x16CA6BC4, 0, 0);

    /// <summary>
    /// Creates valid THUM resource data.
    /// </summary>
    private static byte[] CreateValidData(
        uint version = 4,
        uint uint01 = 0,
        ulong uintlong02 = 0,
        uint uint03 = 0,
        ulong selfInstance = 0,
        uint selfType = 0,
        uint selfGroup = 0,
        uint uint04 = 0,
        uint uint05 = 0,
        float[] floats = null!)
    {
        floats ??= new float[8];

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(version);
        writer.Write(uint01);
        writer.Write(uintlong02);
        writer.Write(uint03);

        // TGI block in ITG order
        writer.Write(selfInstance);
        writer.Write(selfType);
        writer.Write(selfGroup);

        writer.Write(uint04);
        writer.Write(uint05);

        // Magic "THUM"
        writer.Write(0x4D554854u);

        // 8 floats
        for (int i = 0; i < 8; i++)
        {
            writer.Write(i < floats.Length ? floats[i] : 0f);
        }

        return ms.ToArray();
    }

    [Fact]
    public void Parse_ValidData_ExtractsFields()
    {
        // Arrange
        var floats = new float[] { 1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f };
        var data = CreateValidData(
            version: 4,
            uint01: 100,
            uintlong02: 0x123456789ABCDEF0,
            uint03: 200,
            selfInstance: 0xDEADBEEFCAFEBABE,
            selfType: 0x12345678,
            selfGroup: 0x87654321,
            uint04: 300,
            uint05: 400,
            floats: floats);

        // Act
        var resource = new ThumWorldResource(TestKey, data);

        // Assert
        resource.Version.Should().Be(4);
        resource.Uint01.Should().Be(100);
        resource.Uintlong02.Should().Be(0x123456789ABCDEF0);
        resource.Uint03.Should().Be(200);
        resource.SelfReference.Instance.Should().Be(0xDEADBEEFCAFEBABE);
        resource.SelfReference.ResourceType.Should().Be(0x12345678);
        resource.SelfReference.ResourceGroup.Should().Be(0x87654321);
        resource.Uint04.Should().Be(300);
        resource.Uint05.Should().Be(400);
        resource.Float01.Should().Be(1.0f);
        resource.Float02.Should().Be(2.0f);
        resource.Float03.Should().Be(3.0f);
        resource.Float04.Should().Be(4.0f);
        resource.Float05.Should().Be(5.0f);
        resource.Float06.Should().Be(6.0f);
        resource.Float07.Should().Be(7.0f);
        resource.Float08.Should().Be(8.0f);
    }

    [Fact]
    public void Parse_DefaultData_ExtractsZeroValues()
    {
        // Arrange
        var data = CreateValidData();

        // Act
        var resource = new ThumWorldResource(TestKey, data);

        // Assert
        resource.Version.Should().Be(4);
        resource.Uint01.Should().Be(0);
        resource.Uintlong02.Should().Be(0);
        resource.Float01.Should().Be(0f);
    }

    [Fact]
    public void Parse_InvalidMagic_ThrowsInvalidDataException()
    {
        // Arrange - create data with wrong magic
        var data = CreateValidData();
        // Overwrite magic at offset 48 (4+4+8+4+16+4+4 = 44, then magic at 44-47)
        data[44] = 0x00;
        data[45] = 0x00;
        data[46] = 0x00;
        data[47] = 0x00;

        // Act
        Action act = () => _ = new ThumWorldResource(TestKey, data);

        // Assert
        act.Should().Throw<InvalidDataException>().WithMessage("*magic*");
    }

    [Fact]
    public void Parse_TruncatedData_ThrowsInvalidDataException()
    {
        // Arrange - only partial data
        var data = new byte[40]; // Less than 80 bytes

        // Act
        Action act = () => _ = new ThumWorldResource(TestKey, data);

        // Assert
        act.Should().Throw<InvalidDataException>().WithMessage("*too short*");
    }

    [Fact]
    public void Serialize_RoundTrip_PreservesData()
    {
        // Arrange
        var floats = new float[] { 1.5f, 2.5f, 3.5f, 4.5f, 5.5f, 6.5f, 7.5f, 8.5f };
        var data = CreateValidData(
            version: 5,
            uint01: 111,
            uintlong02: 222,
            uint03: 333,
            selfInstance: 444,
            selfType: 555,
            selfGroup: 666,
            uint04: 777,
            uint05: 888,
            floats: floats);
        var resource = new ThumWorldResource(TestKey, data);

        // Act
        var serialized = resource.Data.ToArray();

        // Assert
        serialized.Should().BeEquivalentTo(data);
    }

    [Fact]
    public void Serialize_ModifiedValues_ProducesValidData()
    {
        // Arrange
        var data = CreateValidData();
        var resource = new ThumWorldResource(TestKey, data);
        resource.Version = 99;
        resource.Float01 = 42.0f;

        // Act
        var serialized = resource.Data;
        var reloaded = new ThumWorldResource(TestKey, serialized);

        // Assert
        reloaded.Version.Should().Be(99);
        reloaded.Float01.Should().Be(42.0f);
    }

    [Fact]
    public void Version_Set_MarksAsDirty()
    {
        // Arrange
        var data = CreateValidData();
        var resource = new ThumWorldResource(TestKey, data);

        // Act
        resource.Version = 10;

        // Assert
        resource.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void SelfReference_Set_MarksAsDirty()
    {
        // Arrange
        var data = CreateValidData();
        var resource = new ThumWorldResource(TestKey, data);

        // Act
        resource.SelfReference = new ResourceKey(1, 2, 3);

        // Assert
        resource.IsDirty.Should().BeTrue();
        resource.SelfReference.ResourceType.Should().Be(1);
        resource.SelfReference.ResourceGroup.Should().Be(2);
        resource.SelfReference.Instance.Should().Be(3);
    }

    [Fact]
    public void Float_Set_MarksAsDirty()
    {
        // Arrange
        var data = CreateValidData();
        var resource = new ThumWorldResource(TestKey, data);

        // Act
        resource.Float05 = 123.456f;

        // Assert
        resource.IsDirty.Should().BeTrue();
        resource.Float05.Should().Be(123.456f);
    }

    [Fact]
    public void EmptyData_InitializesDefaults()
    {
        // Arrange & Act
        var resource = new ThumWorldResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Assert
        resource.Version.Should().Be(4);
        resource.Uint01.Should().Be(0);
        resource.Float01.Should().Be(0f);
        resource.SelfReference.Should().Be(default(ResourceKey));
    }
}
