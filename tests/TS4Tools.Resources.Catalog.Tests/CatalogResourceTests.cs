using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TS4Tools.Resources.Catalog;
using Xunit;

namespace TS4Tools.Resources.Catalog.Tests;

public sealed class CatalogResourceTests
{
    private readonly NullLogger<CatalogResource> _logger = NullLogger<CatalogResource>.Instance;
    
    [Fact]
    public void Constructor_WithLogger_ShouldInitializeSuccessfully()
    {
        // Act
        var resource = new CatalogResource(_logger);
        
        // Assert
        resource.Should().NotBeNull();
        resource.Version.Should().Be(CatalogResource.CurrentVersion);
        resource.CatalogVersion.Should().Be(CatalogResource.StandardCatalogVersion);
        resource.StyleReferences.Should().BeEmpty();
        resource.Tags.Should().BeEmpty();
        resource.SellingPoints.Should().BeEmpty();
    }
    
    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new CatalogResource(null!);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("logger");
    }
    
    [Fact]
    public void ContentFields_ShouldReturnExpectedFields()
    {
        // Arrange
        var resource = new CatalogResource(_logger);
        var expectedFields = new[]
        {
            nameof(CatalogResource.Version),
            nameof(CatalogResource.CatalogVersion),
            nameof(CatalogResource.NameHash),
            nameof(CatalogResource.DescriptionHash),
            nameof(CatalogResource.Price),
            nameof(CatalogResource.Unknown1),
            nameof(CatalogResource.Unknown2),
            nameof(CatalogResource.Unknown3),
            nameof(CatalogResource.StyleReferences),
            nameof(CatalogResource.Unknown4),
            nameof(CatalogResource.Tags),
            nameof(CatalogResource.SellingPoints),
            nameof(CatalogResource.Unknown5),
            nameof(CatalogResource.Unknown6),
            nameof(CatalogResource.Unknown7)
        };
        
        // Act & Assert
        resource.ContentFields.Should().BeEquivalentTo(expectedFields);
    }
    
    [Fact]
    public void RecommendedApiVersion_ShouldReturnOne()
    {
        // Arrange
        var resource = new CatalogResource(_logger);
        
        // Act & Assert
        resource.RecommendedApiVersion.Should().Be(1);
    }
    
    [Theory]
    [InlineData(1u)]
    [InlineData(2u)]
    [InlineData(0u)]
    public void Version_SetAndGet_ShouldWorkCorrectly(uint version)
    {
        // Arrange
        var resource = new CatalogResource(_logger);
        
        // Act
        resource.Version = version;
        
        // Assert
        resource.Version.Should().Be(version);
    }
    
    [Theory]
    [InlineData(0x00000009u)]
    [InlineData(0x00000008u)]
    [InlineData(0x0000000Au)]
    public void CatalogVersion_SetAndGet_ShouldWorkCorrectly(uint catalogVersion)
    {
        // Arrange
        var resource = new CatalogResource(_logger);
        
        // Act
        resource.CatalogVersion = catalogVersion;
        
        // Assert
        resource.CatalogVersion.Should().Be(catalogVersion);
    }
    
    [Theory]
    [InlineData(0x12345678u)]
    [InlineData(0xABCDEF00u)]
    [InlineData(0u)]
    public void NameHash_SetAndGet_ShouldWorkCorrectly(uint nameHash)
    {
        // Arrange
        var resource = new CatalogResource(_logger);
        
        // Act
        resource.NameHash = nameHash;
        
        // Assert
        resource.NameHash.Should().Be(nameHash);
    }
    
    [Theory]
    [InlineData(0x87654321u)]
    [InlineData(0x11223344u)]
    [InlineData(0u)]
    public void DescriptionHash_SetAndGet_ShouldWorkCorrectly(uint descriptionHash)
    {
        // Arrange
        var resource = new CatalogResource(_logger);
        
        // Act
        resource.DescriptionHash = descriptionHash;
        
        // Assert
        resource.DescriptionHash.Should().Be(descriptionHash);
    }
    
    [Theory]
    [InlineData(0u)]
    [InlineData(100u)]
    [InlineData(9999u)]
    public void Price_SetAndGet_ShouldWorkCorrectly(uint price)
    {
        // Arrange
        var resource = new CatalogResource(_logger);
        
        // Act
        resource.Price = price;
        
        // Assert
        resource.Price.Should().Be(price);
    }
    
    [Fact]
    public void StyleReferences_ShouldBeModifiable()
    {
        // Arrange
        var resource = new CatalogResource(_logger);
        var reference = new ResourceReference(0x12345678, 0x87654321, 0x1122334455667788);
        
        // Act
        resource.StyleReferences.Add(reference);
        
        // Assert
        resource.StyleReferences.Should().ContainSingle()
            .Which.Should().Be(reference);
    }
    
    [Fact]
    public void Tags_ShouldBeModifiable()
    {
        // Arrange
        var resource = new CatalogResource(_logger);
        var tags = new ushort[] { 1, 2, 3, 100, 999 };
        
        // Act
        foreach (var tag in tags)
        {
            resource.Tags.Add(tag);
        }
        
        // Assert
        resource.Tags.Should().BeEquivalentTo(tags);
    }
    
    [Fact]
    public void SellingPoints_ShouldBeModifiable()
    {
        // Arrange
        var resource = new CatalogResource(_logger);
        var sellingPoints = new[]
        {
            new SellingPoint(1, 100),
            new SellingPoint(2, 200),
            new SellingPoint(3, 50)
        };
        
        // Act
        foreach (var sellingPoint in sellingPoints)
        {
            resource.SellingPoints.Add(sellingPoint);
        }
        
        // Assert
        resource.SellingPoints.Should().BeEquivalentTo(sellingPoints);
    }
    
    [Fact]
    public async Task SaveToStreamAsync_WithValidData_ShouldSucceed()
    {
        // Arrange
        var resource = new CatalogResource(_logger)
        {
            Version = 1,
            CatalogVersion = 9,
            NameHash = 0x12345678,
            DescriptionHash = 0x87654321,
            Price = 500
        };
        
        resource.StyleReferences.Add(new ResourceReference(0x11111111, 0x22222222, 0x3333333344444444));
        resource.Tags.Add(123);
        resource.SellingPoints.Add(new SellingPoint(1, 100));
        
        using var stream = new MemoryStream();
        
        // Act
        await resource.SaveToStreamAsync(stream);
        
        // Assert
        stream.Length.Should().BeGreaterThan(0);
        stream.Position.Should().Be(stream.Length);
    }
    
    [Fact]
    public async Task LoadFromStreamAsync_WithValidData_ShouldSucceed()
    {
        // Arrange
        var originalResource = new CatalogResource(_logger)
        {
            Version = 1,
            CatalogVersion = 9,
            NameHash = 0x12345678,
            DescriptionHash = 0x87654321,
            Price = 500,
            Unknown1 = 111,
            Unknown2 = 222,
            Unknown3 = 333,
            Unknown4 = 444,
            Unknown5 = 555,
            Unknown6 = 666,
            Unknown7 = 777
        };
        
        originalResource.StyleReferences.Add(new ResourceReference(0x11111111, 0x22222222, 0x3333333344444444));
        originalResource.Tags.Add(123);
        originalResource.SellingPoints.Add(new SellingPoint(1, 100));
        
        using var stream = new MemoryStream();
        await originalResource.SaveToStreamAsync(stream);
        
        stream.Position = 0;
        var loadedResource = new CatalogResource(_logger);
        
        // Act
        await loadedResource.LoadFromStreamAsync(stream);
        
        // Assert
        loadedResource.Should().BeEquivalentTo(originalResource, options => options
            .Excluding(x => x.Stream));
    }
    
    [Fact]
    public async Task LoadFromStreamAsync_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Arrange
        var resource = new CatalogResource(_logger);
        
        // Act & Assert
        var act = () => resource.LoadFromStreamAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("stream");
    }
    
    [Fact]
    public async Task LoadFromStreamAsync_WithEmptyStream_ShouldThrowInvalidDataException()
    {
        // Arrange
        var resource = new CatalogResource(_logger);
        using var emptyStream = new MemoryStream();
        
        // Act & Assert
        var act = () => resource.LoadFromStreamAsync(emptyStream);
        await act.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("Stream too short for catalog resource*");
    }
    
    [Fact]
    public async Task GetStreamAsync_ShouldReturnValidStream()
    {
        // Arrange
        var resource = new CatalogResource(_logger)
        {
            Version = 1,
            NameHash = 0x12345678,
            Price = 100
        };
        
        // Act
        using var stream = await resource.GetStreamAsync();
        
        // Assert
        stream.Should().NotBeNull();
        stream.Length.Should().BeGreaterThan(0);
        stream.Position.Should().Be(0);
        stream.CanRead.Should().BeTrue();
    }
    
    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var resource = new CatalogResource(_logger)
        {
            NameHash = 0x12345678,
            DescriptionHash = 0x87654321,
            Price = 500
        };
        
        resource.Tags.Add(1);
        resource.Tags.Add(2);
        resource.StyleReferences.Add(new ResourceReference(0x11111111, 0x22222222, 0x3333333344444444));
        
        // Act
        var result = resource.ToString();
        
        // Assert
        result.Should().Be("CatalogResource(Name=12345678, Desc=87654321, Price=500, Tags=2, Styles=1)");
    }
    
    [Fact]
    public void Equals_WithIdenticalResources_ShouldReturnTrue()
    {
        // Arrange
        var resource1 = new CatalogResource(_logger)
        {
            Version = 1,
            NameHash = 0x12345678,
            Price = 100
        };
        resource1.Tags.Add(123);
        
        var resource2 = new CatalogResource(_logger)
        {
            Version = 1,
            NameHash = 0x12345678,
            Price = 100
        };
        resource2.Tags.Add(123);
        
        // Act & Assert
        resource1.Equals(resource2).Should().BeTrue();
        resource1.GetHashCode().Should().Be(resource2.GetHashCode());
    }
    
    [Fact]
    public void Equals_WithDifferentResources_ShouldReturnFalse()
    {
        // Arrange
        var resource1 = new CatalogResource(_logger) { NameHash = 0x12345678 };
        var resource2 = new CatalogResource(_logger) { NameHash = 0x87654321 };
        
        // Act & Assert
        resource1.Equals(resource2).Should().BeFalse();
    }
    
    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var resource = new CatalogResource(_logger);
        
        // Act & Assert
        resource.Equals(null as CatalogResource).Should().BeFalse();
        resource.Equals(null as object).Should().BeFalse();
    }
    
    [Fact]
    public void PropertyChanged_WhenPropertyChanges_ShouldRaiseEvent()
    {
        // Arrange
        var resource = new CatalogResource(_logger);
        var eventRaised = false;
        string? changedPropertyName = null;
        
        resource.PropertyChanged += (sender, args) =>
        {
            eventRaised = true;
            changedPropertyName = args.PropertyName;
        };
        
        // Act
        resource.NameHash = 0x12345678;
        
        // Assert
        eventRaised.Should().BeTrue();
        changedPropertyName.Should().Be(nameof(CatalogResource.NameHash));
    }
    
    [Fact]
    public void PropertyChanged_WhenSameValueSet_ShouldNotRaiseEvent()
    {
        // Arrange
        var resource = new CatalogResource(_logger) { NameHash = 0x12345678 };
        var eventRaised = false;
        
        resource.PropertyChanged += (_, _) => eventRaised = true;
        
        // Act
        resource.NameHash = 0x12345678; // Same value
        
        // Assert
        eventRaised.Should().BeFalse();
    }
    
    [Fact]
    public async Task RoundTripTest_ComplexData_ShouldPreserveAllData()
    {
        // Arrange
        var original = new CatalogResource(_logger)
        {
            Version = 2,
            CatalogVersion = 10,
            NameHash = 0xAABBCCDD,
            DescriptionHash = 0x11223344,
            Price = 1500,
            Unknown1 = 0x10101010,
            Unknown2 = 0x20202020,
            Unknown3 = 0x30303030,
            Unknown4 = 12345,
            Unknown5 = 0x4040404040404040,
            Unknown6 = 54321,
            Unknown7 = 0x5050505050505050
        };
        
        // Add multiple style references
        original.StyleReferences.Add(new ResourceReference(0x11111111, 0x22222222, 0x3333333344444444));
        original.StyleReferences.Add(new ResourceReference(0x55555555, 0x66666666, 0x7777777788888888));
        original.StyleReferences.Add(new ResourceReference(0x99999999, 0xAAAAAAAA, 0xBBBBBBBBCCCCCCCC));
        
        // Add multiple tags
        original.Tags.Add(1);
        original.Tags.Add(100);
        original.Tags.Add(999);
        original.Tags.Add(12345);
        
        // Add multiple selling points
        original.SellingPoints.Add(new SellingPoint(1, 50));
        original.SellingPoints.Add(new SellingPoint(2, 75));
        original.SellingPoints.Add(new SellingPoint(10, 25));
        
        // Act: Save and reload
        using var stream = new MemoryStream();
        await original.SaveToStreamAsync(stream);
        
        stream.Position = 0;
        var reloaded = new CatalogResource(_logger);
        await reloaded.LoadFromStreamAsync(stream);
        
        // Assert
        reloaded.Should().BeEquivalentTo(original, options => options
            .Excluding(x => x.Stream));
    }
}
