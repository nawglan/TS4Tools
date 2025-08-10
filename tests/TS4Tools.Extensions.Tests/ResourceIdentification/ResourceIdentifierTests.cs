namespace TS4Tools.Extensions.Tests.ResourceIdentification;

/// <summary>
/// Tests for the <see cref="ResourceIdentifier"/> struct.
/// </summary>
public sealed class ResourceIdentifierTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        const uint type = 0x12345678;
        const uint group = 0x9ABCDEF0;
        const ulong instance = 0xFEDCBA9876543210;
        const string name = "TestResource";

        // Act
        var identifier = new ResourceIdentifier(type, group, instance, name);

        // Assert
        identifier.ResourceType.Should().Be(type);
        identifier.ResourceGroup.Should().Be(group);
        identifier.Instance.Should().Be(instance);
        identifier.Name.Should().Be(name);
    }

    [Fact]
    public void Constructor_WithoutName_CreatesInstanceWithNullName()
    {
        // Arrange
        const uint type = 0x12345678;
        const uint group = 0x9ABCDEF0;
        const ulong instance = 0xFEDCBA9876543210;

        // Act
        var identifier = new ResourceIdentifier(type, group, instance);

        // Assert
        identifier.ResourceType.Should().Be(type);
        identifier.ResourceGroup.Should().Be(group);
        identifier.Instance.Should().Be(instance);
        identifier.Name.Should().BeNull();
    }

    [Fact]
    public void FromResourceKey_WithValidResourceKey_CreatesCorrectIdentifier()
    {
        // Arrange
        var resourceKey = new ResourceKey(0x12345678, 0x9ABCDEF0, 0xFEDCBA9876543210);
        const string name = "TestResource";

        // Act
        var identifier = ResourceIdentifier.FromResourceKey(resourceKey, name);

        // Assert
        identifier.ResourceType.Should().Be(resourceKey.ResourceType);
        identifier.ResourceGroup.Should().Be(resourceKey.ResourceGroup);
        identifier.Instance.Should().Be(resourceKey.Instance);
        identifier.Name.Should().Be(name);
    }

    [Fact]
    public void FromResourceKey_WithNullResourceKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => ResourceIdentifier.FromResourceKey(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("resourceKey");
    }

    [Fact]
    public void ToResourceKey_ReturnsCorrectResourceKey()
    {
        // Arrange
        var identifier = new ResourceIdentifier(0x12345678, 0x9ABCDEF0, 0xFEDCBA9876543210, "TestResource");

        // Act
        var resourceKey = identifier.ToResourceKey();

        // Assert
        resourceKey.ResourceType.Should().Be(identifier.ResourceType);
        resourceKey.ResourceGroup.Should().Be(identifier.ResourceGroup);
        resourceKey.Instance.Should().Be(identifier.Instance);
    }

    [Fact]
    public void ToTgiString_ReturnsCorrectFormat()
    {
        // Arrange
        var identifier = new ResourceIdentifier(0x12345678, 0x9ABCDEF0, 0xFEDCBA9876543210);

        // Act
        var tgiString = identifier.ToTgiString();

        // Assert
        tgiString.Should().Be("T:12345678-G:9ABCDEF0-I:FEDCBA9876543210");
    }

    [Fact]
    public void ToTginString_WithName_ReturnsCorrectFormat()
    {
        // Arrange
        var identifier = new ResourceIdentifier(0x12345678, 0x9ABCDEF0, 0xFEDCBA9876543210, "TestResource");

        // Act
        var tginString = identifier.ToTginString();

        // Assert
        tginString.Should().Be("T:12345678-G:9ABCDEF0-I:FEDCBA9876543210-N:TestResource");
    }

    [Fact]
    public void ToTginString_WithoutName_ReturnsTgiFormat()
    {
        // Arrange
        var identifier = new ResourceIdentifier(0x12345678, 0x9ABCDEF0, 0xFEDCBA9876543210);

        // Act
        var tginString = identifier.ToTginString();

        // Assert
        tginString.Should().Be("T:12345678-G:9ABCDEF0-I:FEDCBA9876543210");
    }

    [Theory]
    [InlineData("T:12345678-G:9ABCDEF0-I:FEDCBA9876543210")]
    [InlineData("T:12345678-G:9ABCDEF0-I:FEDCBA9876543210-N:TestResource")]
    [InlineData("t:12345678-g:9abcdef0-i:fedcba9876543210")] // Case insensitive
    [InlineData("T:00000001-G:00000002-I:0000000000000003")]
    public void Parse_WithValidFormat_ReturnsCorrectIdentifier(string tginString)
    {
        // Act
        var identifier = ResourceIdentifier.Parse(tginString);

        // Assert
        identifier.Should().NotBeNull();

        if (tginString.Contains("-N:", StringComparison.OrdinalIgnoreCase))
        {
            identifier.Name.Should().NotBeNull();
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid")]
    [InlineData("T:INVALID-G:9ABCDEF0-I:FEDCBA9876543210")]
    [InlineData("T:12345678-G:INVALID-I:FEDCBA9876543210")]
    [InlineData("T:12345678-G:9ABCDEF0-I:INVALID")]
    [InlineData("T:12345678-G:9ABCDEF0")]
    [InlineData("T:12345678")]
    [InlineData("X:12345678-G:9ABCDEF0-I:FEDCBA9876543210")]
    public void Parse_WithInvalidFormat_ThrowsArgumentException(string invalidTginString)
    {
        // Act & Assert
        var act = () => ResourceIdentifier.Parse(invalidTginString);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TryParse_WithValidFormat_ReturnsTrueAndCorrectIdentifier()
    {
        // Arrange
        const string tginString = "T:12345678-G:9ABCDEF0-I:FEDCBA9876543210-N:TestResource";

        // Act
        var success = ResourceIdentifier.TryParse(tginString, out var identifier);

        // Assert
        success.Should().BeTrue();
        identifier.ResourceType.Should().Be(0x12345678u);
        identifier.ResourceGroup.Should().Be(0x9ABCDEF0u);
        identifier.Instance.Should().Be(0xFEDCBA9876543210ul);
        identifier.Name.Should().Be("TestResource");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("invalid")]
    public void TryParse_WithInvalidFormat_ReturnsFalse(string? invalidTginString)
    {
        // Act
        var success = ResourceIdentifier.TryParse(invalidTginString, out var identifier);

        // Assert
        success.Should().BeFalse();
        identifier.Should().Be(default(ResourceIdentifier));
    }

    [Fact]
    public void CompareTo_WithSameValues_ReturnsZero()
    {
        // Arrange
        var identifier1 = new ResourceIdentifier(0x12345678, 0x9ABCDEF0, 0xFEDCBA9876543210, "TestResource");
        var identifier2 = new ResourceIdentifier(0x12345678, 0x9ABCDEF0, 0xFEDCBA9876543210, "TestResource");

        // Act
        var comparison = identifier1.CompareTo(identifier2);

        // Assert
        comparison.Should().Be(0);
    }

    [Fact]
    public void CompareTo_WithDifferentType_ReturnsNonZero()
    {
        // Arrange
        var identifier1 = new ResourceIdentifier(0x12345678, 0x9ABCDEF0, 0xFEDCBA9876543210);
        var identifier2 = new ResourceIdentifier(0x12345679, 0x9ABCDEF0, 0xFEDCBA9876543210);

        // Act
        var comparison = identifier1.CompareTo(identifier2);

        // Assert
        comparison.Should().NotBe(0);
    }

    [Theory]
    [InlineData("TGI", "T:12345678-G:9ABCDEF0-I:FEDCBA9876543210")]
    [InlineData("TGIN", "T:12345678-G:9ABCDEF0-I:FEDCBA9876543210-N:TestResource")]
    [InlineData(null, "T:12345678-G:9ABCDEF0-I:FEDCBA9876543210-N:TestResource")]
    public void ToString_WithFormat_ReturnsCorrectFormat(string? format, string expected)
    {
        // Arrange
        var identifier = new ResourceIdentifier(0x12345678, 0x9ABCDEF0, 0xFEDCBA9876543210, "TestResource");

        // Act
        var result = identifier.ToString(format, null);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ToString_WithInvalidFormat_ThrowsFormatException()
    {
        // Arrange
        var identifier = new ResourceIdentifier(0x12345678, 0x9ABCDEF0, 0xFEDCBA9876543210);

        // Act & Assert
        var act = () => identifier.ToString("INVALID", null);
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void ToString_WithoutParameters_ReturnsTginFormat()
    {
        // Arrange
        var identifier = new ResourceIdentifier(0x12345678, 0x9ABCDEF0, 0xFEDCBA9876543210, "TestResource");

        // Act
        var result = identifier.ToString();

        // Assert
        result.Should().Be("T:12345678-G:9ABCDEF0-I:FEDCBA9876543210-N:TestResource");
    }

    [Fact]
    public void Equality_WithSameValues_AreEqual()
    {
        // Arrange
        var identifier1 = new ResourceIdentifier(0x12345678, 0x9ABCDEF0, 0xFEDCBA9876543210, "TestResource");
        var identifier2 = new ResourceIdentifier(0x12345678, 0x9ABCDEF0, 0xFEDCBA9876543210, "TestResource");

        // Act & Assert
        identifier1.Should().Be(identifier2);
        (identifier1 == identifier2).Should().BeTrue();
        (identifier1 != identifier2).Should().BeFalse();
    }

    [Fact]
    public void Equality_WithDifferentValues_AreNotEqual()
    {
        // Arrange
        var identifier1 = new ResourceIdentifier(0x12345678, 0x9ABCDEF0, 0xFEDCBA9876543210, "TestResource");
        var identifier2 = new ResourceIdentifier(0x12345679, 0x9ABCDEF0, 0xFEDCBA9876543210, "TestResource");

        // Act & Assert
        identifier1.Should().NotBe(identifier2);
        (identifier1 == identifier2).Should().BeFalse();
        (identifier1 != identifier2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_WithSameValues_ReturnsSameHashCode()
    {
        // Arrange
        var identifier1 = new ResourceIdentifier(0x12345678, 0x9ABCDEF0, 0xFEDCBA9876543210, "TestResource");
        var identifier2 = new ResourceIdentifier(0x12345678, 0x9ABCDEF0, 0xFEDCBA9876543210, "TestResource");

        // Act & Assert
        identifier1.GetHashCode().Should().Be(identifier2.GetHashCode());
    }
}
