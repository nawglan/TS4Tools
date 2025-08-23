namespace TS4Tools.Resources.Materials.Tests;

public sealed class MaterialResourceFactoryTests
{
    private readonly MaterialResourceFactory _factory;

    public MaterialResourceFactoryTests()
    {
        _factory = new MaterialResourceFactory();
    }

    [Fact]
    public void ResourceType_ShouldReturnCorrectValue()
    {
        // Act & Assert
        MaterialResourceFactory.ResourceType.Should().Be(0x545AC67A);
    }

    [Fact]
    public void SupportedResourceTypes_ShouldContainExpectedValues()
    {
        // Act & Assert
        _factory.SupportedResourceTypes.Should().Contain("0x545AC67A");
        _factory.SupportedResourceTypes.Should().Contain("SWB");
        _factory.SupportedResourceTypes.Should().Contain("Material");
    }

    [Fact]
    public async Task CreateResourceAsync_WithValidData_ShouldReturnMaterialResource()
    {
        // Arrange
        var testData = CreateTestMaterialData();
        using var stream = new MemoryStream(testData.ToArray());

        // Act
        var resource = await _factory.CreateResourceAsync(1, stream);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<MaterialResource>();
        
        using (resource)
        {
            var materialResource = (MaterialResource)resource;
            materialResource.MaterialId.Should().Be(0x12345678u);
        }
    }

    [Fact]
    public void CanCreateResource_WithValidResourceType_ShouldReturnTrue()
    {
        // Act
        var canCreate = _factory.CanCreateResource(0x545AC67A);

        // Assert
        canCreate.Should().BeTrue();
    }

    [Fact]
    public void CanCreateResource_WithInvalidResourceType_ShouldReturnFalse()
    {
        // Act
        var canCreate = _factory.CanCreateResource(0x12345678);

        // Assert
        canCreate.Should().BeFalse();
    }

    private static ReadOnlyMemory<byte> CreateTestMaterialData()
    {
        var data = new List<byte>();
        
        // Header
        data.AddRange(BitConverter.GetBytes(0x4D544C52u)); // "MTLR" signature
        data.AddRange(BitConverter.GetBytes(1u)); // Version
        data.AddRange(BitConverter.GetBytes(0x12345678u)); // Material ID
        data.AddRange(BitConverter.GetBytes(1u)); // Flags (has transparency)
        
        // Shader type
        var shaderName = "standard"u8.ToArray();
        data.AddRange(shaderName);
        data.AddRange(new byte[32 - shaderName.Length]); // Padding
        
        return data.ToArray();
    }
}
