using FluentAssertions;
using TS4Tools.Resources.Catalog;
using Xunit;

namespace TS4Tools.Resources.Catalog.Tests;

public sealed class ResourceReferenceTests
{
    [Theory]
    [InlineData(0u, 0u, 0uL)]
    [InlineData(0x12345678u, 0x87654321u, 0x1122334455667788uL)]
    [InlineData(uint.MaxValue, uint.MaxValue, ulong.MaxValue)]
    public void Constructor_WithValidValues_ShouldInitializeCorrectly(uint resourceType, uint resourceGroup, ulong instance)
    {
        // Act
        var reference = new ResourceReference(resourceType, resourceGroup, instance);

        // Assert
        reference.ResourceType.Should().Be(resourceType);
        reference.ResourceGroup.Should().Be(resourceGroup);
        reference.Instance.Should().Be(instance);
    }

    [Fact]
    public void DefaultConstructor_ShouldInitializeWithZeros()
    {
        // Act
        var reference = new ResourceReference();

        // Assert
        reference.ResourceType.Should().Be(0u);
        reference.ResourceGroup.Should().Be(0u);
        reference.Instance.Should().Be(0uL);
    }

    [Theory]
    [InlineData(0x12345678u, 0x87654321u, 0x1122334455667788uL, "TGI(12345678-87654321-1122334455667788)")]
    [InlineData(0u, 0u, 0uL, "TGI(00000000-00000000-0000000000000000)")]
    [InlineData(0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFFFFFFFFFuL, "TGI(FFFFFFFF-FFFFFFFF-FFFFFFFFFFFFFFFF)")]
    public void ToString_ShouldReturnFormattedTgiString(uint resourceType, uint resourceGroup, ulong instance, string expected)
    {
        // Arrange
        var reference = new ResourceReference(resourceType, resourceGroup, instance);

        // Act
        var result = reference.ToString();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Deconstruct_ShouldReturnCorrectValues()
    {
        // Arrange
        var originalType = 0x12345678u;
        var originalGroup = 0x87654321u;
        var originalInstance = 0x1122334455667788uL;
        var reference = new ResourceReference(originalType, originalGroup, originalInstance);

        // Act
        var (resourceType, resourceGroup, instance) = reference;

        // Assert
        resourceType.Should().Be(originalType);
        resourceGroup.Should().Be(originalGroup);
        instance.Should().Be(originalInstance);
    }

    [Fact]
    public void FromTgi_WithTuple_ShouldCreateCorrectReference()
    {
        // Arrange
        var tgi = (Type: 0x12345678u, Group: 0x87654321u, Instance: 0x1122334455667788uL);

        // Act
        var reference = ResourceReference.FromTgi(tgi);

        // Assert
        reference.ResourceType.Should().Be(tgi.Type);
        reference.ResourceGroup.Should().Be(tgi.Group);
        reference.Instance.Should().Be(tgi.Instance);
    }

    [Fact]
    public void ToTgi_ShouldReturnCorrectTuple()
    {
        // Arrange
        var reference = new ResourceReference(0x12345678u, 0x87654321u, 0x1122334455667788uL);

        // Act
        var tgi = reference.ToTgi();

        // Assert
        tgi.Type.Should().Be(reference.ResourceType);
        tgi.Group.Should().Be(reference.ResourceGroup);
        tgi.Instance.Should().Be(reference.Instance);
    }

    [Fact]
    public void Equals_WithIdenticalValues_ShouldReturnTrue()
    {
        // Arrange
        var reference1 = new ResourceReference(0x12345678u, 0x87654321u, 0x1122334455667788uL);
        var reference2 = new ResourceReference(0x12345678u, 0x87654321u, 0x1122334455667788uL);

        // Act & Assert
        reference1.Equals(reference2).Should().BeTrue();
        (reference1 == reference2).Should().BeTrue();
        (reference1 != reference2).Should().BeFalse();
        reference1.GetHashCode().Should().Be(reference2.GetHashCode());
    }

    [Theory]
    [InlineData(0x12345678u, 0x87654321u, 0x1122334455667788uL, 0x12345679u, 0x87654321u, 0x1122334455667788uL)] // Different type
    [InlineData(0x12345678u, 0x87654321u, 0x1122334455667788uL, 0x12345678u, 0x87654322u, 0x1122334455667788uL)] // Different group
    [InlineData(0x12345678u, 0x87654321u, 0x1122334455667788uL, 0x12345678u, 0x87654321u, 0x1122334455667789uL)] // Different instance
    public void Equals_WithDifferentValues_ShouldReturnFalse(
        uint type1, uint group1, ulong instance1,
        uint type2, uint group2, ulong instance2)
    {
        // Arrange
        var reference1 = new ResourceReference(type1, group1, instance1);
        var reference2 = new ResourceReference(type2, group2, instance2);

        // Act & Assert
        reference1.Equals(reference2).Should().BeFalse();
        (reference1 == reference2).Should().BeFalse();
        (reference1 != reference2).Should().BeTrue();
    }

    [Fact]
    public void PropertyChanged_ShouldNeverRaise()
    {
        // Arrange
        var reference = new ResourceReference(1, 2, 3);
        var eventRaised = false;

        // Act
        reference.PropertyChanged += (_, _) => eventRaised = true;

        // Assert
        eventRaised.Should().BeFalse("PropertyChanged should never be raised for immutable record structs");
    }

    [Fact]
    public void ImplicitConversion_ToTuple_ShouldWork()
    {
        // Arrange
        var reference = new ResourceReference(0x12345678u, 0x87654321u, 0x1122334455667788uL);

        // Act
        (uint resourceType, uint resourceGroup, ulong instance) = reference;

        // Assert
        resourceType.Should().Be(0x12345678u);
        resourceGroup.Should().Be(0x87654321u);
        instance.Should().Be(0x1122334455667788uL);
    }

    [Theory]
    [InlineData(0u, 0u, 0uL)]
    [InlineData(1u, 1u, 1uL)]
    [InlineData(uint.MaxValue, uint.MaxValue, ulong.MaxValue)]
    public void EdgeValues_ShouldBeHandledCorrectly(uint resourceType, uint resourceGroup, ulong instance)
    {
        // Act
        var reference = new ResourceReference(resourceType, resourceGroup, instance);

        // Assert
        reference.ResourceType.Should().Be(resourceType);
        reference.ResourceGroup.Should().Be(resourceGroup);
        reference.Instance.Should().Be(instance);
    }

    [Fact]
    public void RecordEquality_ShouldWorkCorrectly()
    {
        // Arrange
        var reference1 = new ResourceReference(0x11111111u, 0x22222222u, 0x3333333333333333uL);
        var reference2 = new ResourceReference(0x11111111u, 0x22222222u, 0x3333333333333333uL);
        var reference3 = new ResourceReference(0x11111111u, 0x22222222u, 0x3333333333333334uL);

        // Act & Assert
        // Test equality
        reference1.Should().Be(reference2);
        reference1.Should().NotBe(reference3);

        // Test hash codes
        reference1.GetHashCode().Should().Be(reference2.GetHashCode());
        reference1.GetHashCode().Should().NotBe(reference3.GetHashCode());
    }

    [Fact]
    public void ResourceReference_ShouldBeValueType()
    {
        // Arrange & Act
        var reference1 = new ResourceReference(1, 2, 3);
        var reference2 = reference1; // Copy

        // Assert
        // Both should have the same values (value semantics)
        reference1.Should().Be(reference2);
        reference1.Equals(reference2).Should().BeTrue("Should have value semantics");
    }

    [Fact]
    public void Collections_ShouldWorkCorrectly()
    {
        // Arrange
        var references = new[]
        {
            new ResourceReference(0x11111111u, 0x22222222u, 0x3333333333333333uL),
            new ResourceReference(0x44444444u, 0x55555555u, 0x6666666666666666uL),
            new ResourceReference(0x77777777u, 0x88888888u, 0x9999999999999999uL)
        };

        var list = new List<ResourceReference>(references);
        var set = new HashSet<ResourceReference>(references);

        // Act & Assert
        list.Should().HaveCount(3);
        set.Should().HaveCount(3);

        // Test contains
        var searchReference = new ResourceReference(0x44444444u, 0x55555555u, 0x6666666666666666uL);
        list.Should().Contain(searchReference);
        set.Should().Contain(searchReference);

        // Test not contains
        var missingReference = new ResourceReference(0x44444444u, 0x55555555u, 0x6666666666666667uL);
        list.Should().NotContain(missingReference);
        set.Should().NotContain(missingReference);
    }

    [Fact]
    public void RoundTrip_TgiConversion_ShouldPreserveValues()
    {
        // Arrange
        var originalReference = new ResourceReference(0xAABBCCDDu, 0x11223344u, 0x5566778899AABBCCuL);

        // Act
        var tgi = originalReference.ToTgi();
        var convertedReference = ResourceReference.FromTgi(tgi);

        // Assert
        convertedReference.Should().Be(originalReference);
    }

    [Fact]
    public void CommonTgiFormats_ShouldBeRepresentedCorrectly()
    {
        // Arrange & Act
        var meshReference = new ResourceReference(0x034AEECB, 0x00000000, 0x1234567890ABCDEFuL);
        var textureReference = new ResourceReference(0x00B2D882, 0x00000000, 0xFEDCBA0987654321uL);
        var stringReference = new ResourceReference(0x220557DA, 0x80000000, 0x1111111111111111uL);

        // Assert
        meshReference.ToString().Should().Be("TGI(034AEECB-00000000-1234567890ABCDEF)");
        textureReference.ToString().Should().Be("TGI(00B2D882-00000000-FEDCBA0987654321)");
        stringReference.ToString().Should().Be("TGI(220557DA-80000000-1111111111111111)");
    }
}
