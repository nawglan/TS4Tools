using FluentAssertions;
using TS4Tools.Package;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

/// <summary>
/// Tests for <see cref="TmltResource"/>.
///
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MiscellaneousResource/TMLTResource.cs
///
/// Format (99 bytes):
/// - version: uint32
/// - uint01: uint32
/// - uintlong02: uint64
/// - uint03: uint32
/// - selfReference: TGIBlock (ITG order: Instance, Type, Group - 16 bytes)
/// - uint04: uint32
/// - uint05: uint32
/// - magic: "TMLT" (4 bytes)
/// - floats: 12 float values (48 bytes)
/// - bytes: 3 byte values
/// </summary>
public class TmltResourceTests
{
    private static readonly ResourceKey TestKey = new(0xB0118C15, 0, 0);

    /// <summary>
    /// Creates valid TMLT resource data.
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
        float[]? floats = null,
        byte byte01 = 0,
        byte byte02 = 0,
        byte byte03 = 0)
    {
        floats ??= new float[12];

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

        // Magic "TMLT"
        writer.Write(0x544C4D54u);

        // 12 floats
        for (int i = 0; i < 12; i++)
        {
            writer.Write(i < floats.Length ? floats[i] : 0f);
        }

        // 3 bytes
        writer.Write(byte01);
        writer.Write(byte02);
        writer.Write(byte03);

        return ms.ToArray();
    }

    [Fact]
    public void Parse_ValidData_ExtractsFields()
    {
        // Arrange
        var floats = new float[] { 1.0f, 2.0f, 3.0f, 0.5f, 0.7f, 0.3f, 7.0f, 8.0f, 9.0f, 10.0f, 11.0f, 12.0f };
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
            floats: floats,
            byte01: 1,
            byte02: 2,
            byte03: 3);

        // Act
        var resource = new TmltResource(TestKey, data);

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
        resource.ColorRed.Should().Be(0.5f);
        resource.ColorGreen.Should().Be(0.7f);
        resource.ColorBlue.Should().Be(0.3f);
        resource.Float07.Should().Be(7.0f);
        resource.Float08.Should().Be(8.0f);
        resource.Float09.Should().Be(9.0f);
        resource.Float10.Should().Be(10.0f);
        resource.Float11.Should().Be(11.0f);
        resource.Float12.Should().Be(12.0f);
        resource.Byte01.Should().Be(1);
        resource.Byte02.Should().Be(2);
        resource.Byte03.Should().Be(3);
    }

    [Fact]
    public void Parse_DefaultData_ExtractsZeroValues()
    {
        // Arrange
        var data = CreateValidData();

        // Act
        var resource = new TmltResource(TestKey, data);

        // Assert
        resource.Version.Should().Be(4);
        resource.Uint01.Should().Be(0);
        resource.Uintlong02.Should().Be(0);
        resource.Float01.Should().Be(0f);
        resource.ColorRed.Should().Be(0f);
        resource.Byte01.Should().Be(0);
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
        Action act = () => _ = new TmltResource(TestKey, data);

        // Assert
        act.Should().Throw<InvalidDataException>().WithMessage("*magic*");
    }

    [Fact]
    public void Parse_TruncatedData_ThrowsInvalidDataException()
    {
        // Arrange - only partial data
        var data = new byte[50]; // Less than 99 bytes

        // Act
        Action act = () => _ = new TmltResource(TestKey, data);

        // Assert
        act.Should().Throw<InvalidDataException>().WithMessage("*too short*");
    }

    [Fact]
    public void Serialize_RoundTrip_PreservesData()
    {
        // Arrange
        var floats = new float[] { 1.5f, 2.5f, 3.5f, 0.25f, 0.5f, 0.75f, 7.5f, 8.5f, 9.5f, 10.5f, 11.5f, 12.5f };
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
            floats: floats,
            byte01: 11,
            byte02: 22,
            byte03: 33);
        var resource = new TmltResource(TestKey, data);

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
        var resource = new TmltResource(TestKey, data);
        resource.Version = 99;
        resource.ColorRed = 1.0f;
        resource.ColorGreen = 0.5f;
        resource.ColorBlue = 0.0f;
        resource.Byte01 = 42;

        // Act
        var serialized = resource.Data;
        var reloaded = new TmltResource(TestKey, serialized);

        // Assert
        reloaded.Version.Should().Be(99);
        reloaded.ColorRed.Should().Be(1.0f);
        reloaded.ColorGreen.Should().Be(0.5f);
        reloaded.ColorBlue.Should().Be(0.0f);
        reloaded.Byte01.Should().Be(42);
    }

    [Fact]
    public void Version_Set_MarksAsDirty()
    {
        // Arrange
        var data = CreateValidData();
        var resource = new TmltResource(TestKey, data);

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
        var resource = new TmltResource(TestKey, data);

        // Act
        resource.SelfReference = new ResourceKey(1, 2, 3);

        // Assert
        resource.IsDirty.Should().BeTrue();
        resource.SelfReference.ResourceType.Should().Be(1);
        resource.SelfReference.ResourceGroup.Should().Be(2);
        resource.SelfReference.Instance.Should().Be(3);
    }

    [Fact]
    public void ColorFields_Set_MarksAsDirty()
    {
        // Arrange
        var data = CreateValidData();
        var resource = new TmltResource(TestKey, data);

        // Act
        resource.ColorRed = 0.9f;

        // Assert
        resource.IsDirty.Should().BeTrue();
        resource.ColorRed.Should().Be(0.9f);
    }

    [Fact]
    public void ByteFields_Set_MarksAsDirty()
    {
        // Arrange
        var data = CreateValidData();
        var resource = new TmltResource(TestKey, data);

        // Act
        resource.Byte02 = 128;

        // Assert
        resource.IsDirty.Should().BeTrue();
        resource.Byte02.Should().Be(128);
    }

    [Fact]
    public void EmptyData_InitializesDefaults()
    {
        // Arrange & Act
        var resource = new TmltResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Assert
        resource.Version.Should().Be(4);
        resource.Uint01.Should().Be(0);
        resource.Float01.Should().Be(0f);
        resource.ColorRed.Should().Be(0f);
        resource.ColorGreen.Should().Be(0f);
        resource.ColorBlue.Should().Be(0f);
        resource.Byte01.Should().Be(0);
        resource.SelfReference.Should().Be(default(ResourceKey));
    }

    [Theory]
    [InlineData(0.0f, 0.0f, 0.0f)] // Black
    [InlineData(1.0f, 1.0f, 1.0f)] // White
    [InlineData(1.0f, 0.0f, 0.0f)] // Red
    [InlineData(0.0f, 1.0f, 0.0f)] // Green
    [InlineData(0.0f, 0.0f, 1.0f)] // Blue
    [InlineData(0.5f, 0.5f, 0.5f)] // Gray
    public void Parse_ColorValues_ParsesCorrectly(float red, float green, float blue)
    {
        // Arrange
        var floats = new float[12];
        floats[3] = red;   // ColorRed
        floats[4] = green; // ColorGreen
        floats[5] = blue;  // ColorBlue
        var data = CreateValidData(floats: floats);

        // Act
        var resource = new TmltResource(TestKey, data);

        // Assert
        resource.ColorRed.Should().Be(red);
        resource.ColorGreen.Should().Be(green);
        resource.ColorBlue.Should().Be(blue);
    }

    [Fact]
    public void AllFloats_Set_SerializeCorrectly()
    {
        // Arrange
        var data = CreateValidData();
        var resource = new TmltResource(TestKey, data);

        // Act - set all float values
        resource.Float01 = 1.1f;
        resource.Float02 = 2.2f;
        resource.Float03 = 3.3f;
        resource.ColorRed = 0.4f;
        resource.ColorGreen = 0.5f;
        resource.ColorBlue = 0.6f;
        resource.Float07 = 7.7f;
        resource.Float08 = 8.8f;
        resource.Float09 = 9.9f;
        resource.Float10 = 10.1f;
        resource.Float11 = 11.1f;
        resource.Float12 = 12.1f;

        var serialized = resource.Data;
        var reloaded = new TmltResource(TestKey, serialized);

        // Assert
        reloaded.Float01.Should().BeApproximately(1.1f, 0.001f);
        reloaded.Float02.Should().BeApproximately(2.2f, 0.001f);
        reloaded.Float03.Should().BeApproximately(3.3f, 0.001f);
        reloaded.ColorRed.Should().BeApproximately(0.4f, 0.001f);
        reloaded.ColorGreen.Should().BeApproximately(0.5f, 0.001f);
        reloaded.ColorBlue.Should().BeApproximately(0.6f, 0.001f);
        reloaded.Float07.Should().BeApproximately(7.7f, 0.001f);
        reloaded.Float08.Should().BeApproximately(8.8f, 0.001f);
        reloaded.Float09.Should().BeApproximately(9.9f, 0.001f);
        reloaded.Float10.Should().BeApproximately(10.1f, 0.001f);
        reloaded.Float11.Should().BeApproximately(11.1f, 0.001f);
        reloaded.Float12.Should().BeApproximately(12.1f, 0.001f);
    }
}
