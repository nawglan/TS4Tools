namespace TS4Tools.Resources.Materials.Tests;

public sealed class MaterialResourceTests : IDisposable
{
    private readonly MaterialResource _materialResource;

    public MaterialResourceTests()
    {
        _materialResource = new MaterialResource();
    }

    public void Dispose()
    {
        _materialResource?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var resource = new MaterialResource();

        // Assert
        using (resource)
        {
            resource.MaterialId.Should().Be(0u);
            resource.ShaderType.Should().Be("default");
            resource.HasTransparency.Should().BeFalse();
            resource.Size.Should().Be(0);
        }
    }

    [Fact]
    public async Task LoadFromDataAsync_WithValidData_ShouldParseCorrectly()
    {
        // Arrange
        var testData = CreateTestMaterialData();
        
        // Act
        await _materialResource.LoadFromDataAsync(testData);

        // Assert
        _materialResource.MaterialId.Should().Be(0x12345678u);
        _materialResource.ShaderType.Should().NotBeEmpty();
        _materialResource.Size.Should().Be(testData.Length);
    }

    [Fact]
    public async Task SaveToDataAsync_ShouldCreateValidData()
    {
        // Arrange
        await _materialResource.LoadFromDataAsync(CreateTestMaterialData());

        // Act
        var savedData = await _materialResource.SaveToDataAsync();

        // Assert
        savedData.Length.Should().BeGreaterThan(0);
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
