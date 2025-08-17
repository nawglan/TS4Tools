/***************************************************************************
 *  Copyright (C) 2025 TS4Tools Project                                    *
 *                                                                         *
 *  This file is part of TS4Tools                                         *
 *                                                                         *
 *  TS4Tools is free software: you can redistribute it and/or modify      *
 *  it under the terms of the GNU General Public License as published by   *
 *  the Free Software Foundation, either version 3 of the License, or      *
 *  (at your option) any later version.                                    *
 *                                                                         *
 *  TS4Tools is distributed in the hope that it will be useful,           *
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of         *
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the          *
 *  GNU General Public License for more details.                           *
 *                                                                         *
 *  You should have received a copy of the GNU General Public License      *
 *  along with TS4Tools.  If not, see <http://www.gnu.org/licenses/>.     *
 ***************************************************************************/

namespace TS4Tools.Resources.Textures.Tests;

/// <summary>
/// Tests for the <see cref="TxtcResourceFactory"/> class.
/// </summary>
public class TxtcResourceFactoryTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TxtcResourceFactory> _logger;

    public TxtcResourceFactoryTests()
    {
        // Setup mock services
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        _serviceProvider = services.BuildServiceProvider();

        _logger = _serviceProvider.GetRequiredService<ILogger<TxtcResourceFactory>>();
    }

    [Fact]
    public void Constructor_ValidArguments_ShouldNotThrow()
    {
        // Act & Assert
        var factory = new TxtcResourceFactory(_serviceProvider, _logger);
        factory.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_NullServiceProvider_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new TxtcResourceFactory(null!, _logger);
        action.Should().Throw<ArgumentNullException>().WithParameterName("serviceProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new TxtcResourceFactory(_serviceProvider, null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void SupportedResourceTypes_ShouldContainTxtcResourceType()
    {
        // Arrange
        var factory = new TxtcResourceFactory(_serviceProvider, _logger);

        // Assert
        factory.SupportedResourceTypes.Should().Contain("0x00B2D882");
    }

    [Fact]
    public void Priority_ShouldReturn100()
    {
        // Arrange
        var factory = new TxtcResourceFactory(_serviceProvider, _logger);

        // Assert
        factory.Priority.Should().Be(100);
    }

    [Fact]
    public async Task CreateResourceAsync_EmptyStream_ShouldCreateEmptyResource()
    {
        // Arrange
        var factory = new TxtcResourceFactory(_serviceProvider, _logger);

        // Act
        var resource = await factory.CreateResourceAsync(1, null);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<TxtcResource>();

        var txtcResource = (TxtcResource)resource;
        txtcResource.Layers.Should().BeEmpty();
        txtcResource.CompositionWidth.Should().Be(0);
        txtcResource.CompositionHeight.Should().Be(0);

        // Cleanup
        resource.Dispose();
    }

    [Fact]
    public void GetDiagnosticInfo_ShouldReturnValidDictionary()
    {
        // Arrange
        var factory = new TxtcResourceFactory(_serviceProvider, _logger);

        // Act
        var diagnostics = factory.GetDiagnosticInfo();

        // Assert
        diagnostics.Should().NotBeNull();
        diagnostics.Should().ContainKey("FactoryType");
        diagnostics.Should().ContainKey("SupportedResourceTypes");
        diagnostics.Should().ContainKey("Priority");
        diagnostics.Should().ContainKey("CreatedAt");

        diagnostics["FactoryType"].Should().Be("TxtcResourceFactory");
        diagnostics["Priority"].Should().Be(100);
    }
}

/// <summary>
/// Tests for the <see cref="TxtcResource"/> class.
/// </summary>
public class TxtcResourceTests
{
    private readonly ILogger<TxtcResource> _logger;
    private readonly Fixture _fixture;
    private readonly ITestOutputHelper _output;

    public TxtcResourceTests(ITestOutputHelper output)
    {
        _output = output;
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        var serviceProvider = services.BuildServiceProvider();

        _logger = serviceProvider.GetRequiredService<ILogger<TxtcResource>>();
        _fixture = new Fixture();
    }

    [Fact]
    public void Constructor_ValidLogger_ShouldNotThrow()
    {
        // Act & Assert
        using var resource = new TxtcResource(_logger);
        resource.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new TxtcResource(null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithDimensions_ShouldSetProperties()
    {
        // Arrange
        const uint width = 512;
        const uint height = 256;
        const TextureFormat format = TextureFormat.DXT1;

        // Act
        using var resource = new TxtcResource(_logger, 1, width, height, format);

        // Assert
        resource.CompositionWidth.Should().Be(width);
        resource.CompositionHeight.Should().Be(height);
        resource.OutputFormat.Should().Be(format);
    }

    [Fact]
    public void Constructor_ZeroWidth_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        var action = () => new TxtcResource(_logger, 1, 0, 256);
        action.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("width");
    }

    [Fact]
    public void Constructor_ZeroHeight_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        var action = () => new TxtcResource(_logger, 1, 256, 0);
        action.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("height");
    }

    [Fact]
    public void Version_DefaultValue_ShouldBeOne()
    {
        // Arrange
        using var resource = new TxtcResource(_logger);

        // Assert
        resource.Version.Should().Be(1u);
    }

    [Fact]
    public void Flags_DefaultValue_ShouldBeNone()
    {
        // Arrange
        using var resource = new TxtcResource(_logger);

        // Assert
        resource.Flags.Should().Be(TxtcFlags.None);
    }

    [Fact]
    public void Layers_InitiallyEmpty_ShouldBeEmpty()
    {
        // Arrange
        using var resource = new TxtcResource(_logger);

        // Assert
        resource.Layers.Should().BeEmpty();
    }

    [Fact]
    public void AddLayer_WithExternalReference_ShouldAddLayer()
    {
        // Arrange
        using var resource = new TxtcResource(_logger);
        var tgi = new TgiReference(0x12345678, 0x87654321, 0x1234567890ABCDEF);
        var reference = new TextureReference(tgi, TextureFormat.ARGB, 512, 512, MipmapLevel.Full);

        // Act
        var index = resource.AddLayer(reference);

        // Assert
        index.Should().Be(0);
        resource.Layers.Should().HaveCount(1);
        resource.Layers[0].UsesExternalReference.Should().BeTrue();
        resource.Layers[0].Reference.Should().Be(reference);
    }

    [Fact]
    public void AddLayer_WithEmbeddedData_ShouldAddLayerAndSetFlag()
    {
        // Arrange
        using var resource = new TxtcResource(_logger);
        var data = _fixture.CreateMany<byte>(1024).ToArray();
        var embedded = new EmbeddedTextureData(TextureFormat.DXT5, 256, 256, data);

        // Act
        var index = resource.AddLayer(embedded);

        // Assert
        index.Should().Be(0);
        resource.Layers.Should().HaveCount(1);
        resource.Layers[0].HasEmbeddedData.Should().BeTrue();
        resource.Layers[0].EmbeddedData.Should().Be(embedded);
        resource.Flags.Should().HaveFlag(TxtcFlags.EmbeddedData);
    }

    [Fact]
    public async Task SerializeAsync_EmptyResource_ShouldProduceValidData()
    {
        // Arrange
        using var resource = new TxtcResource(_logger, 1, 512, 256);

        // Act
        await resource.SerializeAsync();

        // Assert
        resource.Stream.Length.Should().BeGreaterThan(0);
        var data = resource.AsBytes;
        data.Should().NotBeEmpty();

        // Check magic number (first 4 bytes should be "TXTC" = 0x54585443)
        BitConverter.ToUInt32(data, 0).Should().Be(0x54585443);
    }

    [Fact]
    public async Task DeserializeAsync_ValidData_ShouldRestoreResource()
    {
        // Arrange
        using var originalResource = new TxtcResource(_logger, 1, 512, 256);
        var tgi = new TgiReference(0x12345678, 0x87654321, 0x1234567890ABCDEF);
        var reference = new TextureReference(tgi, TextureFormat.ARGB, 512, 512);
        originalResource.AddLayer(reference);

        await originalResource.SerializeAsync();
        var data = originalResource.AsBytes;

        // Act
        using var newResource = new TxtcResource(_logger);
        using var stream = new MemoryStream(data);
        newResource.Stream.SetLength(0);
        stream.CopyTo(newResource.Stream);
        newResource.Stream.Position = 0;

        await newResource.DeserializeAsync();

        // Assert
        newResource.CompositionWidth.Should().Be(512);
        newResource.CompositionHeight.Should().Be(256);
        newResource.Layers.Should().HaveCount(1);
        newResource.Layers[0].UsesExternalReference.Should().BeTrue();
    }

    [Fact]
    public void ContentFields_ShouldContainExpectedFields()
    {
        // Arrange
        using var resource = new TxtcResource(_logger);

        // Assert
        resource.ContentFields.Should().Contain("Version");
        resource.ContentFields.Should().Contain("Flags");
        resource.ContentFields.Should().Contain("CompositionWidth");
        resource.ContentFields.Should().Contain("CompositionHeight");
        resource.ContentFields.Should().Contain("OutputFormat");
        resource.ContentFields.Should().Contain("LayerCount");
    }

    [Fact]
    public void Indexer_ByName_ShouldReturnCorrectValues()
    {
        // Arrange
        using var resource = new TxtcResource(_logger, 1, 512, 256, TextureFormat.DXT1);

        // Assert
        resource["Version"].GetValue<uint>().Should().Be(1u);
        resource["CompositionWidth"].GetValue<uint>().Should().Be(512u);
        resource["CompositionHeight"].GetValue<uint>().Should().Be(256u);
        resource["OutputFormat"].GetValue<TextureFormat>().Should().Be(TextureFormat.DXT1);
    }

    [Fact]
    public void SetCustomProperty_ValidProperty_ShouldStoreValue()
    {
        // Arrange
        using var resource = new TxtcResource(_logger);
        const string propertyName = "TestProperty";
        const string propertyValue = "TestValue";

        // Act
        resource.SetCustomProperty(propertyName, propertyValue);

        // Assert
        resource.CustomProperties.Should().ContainKey(propertyName);
        resource.CustomProperties[propertyName].Should().Be(propertyValue);
    }

    [Fact]
    public void SetCustomProperty_NullName_ShouldThrowArgumentException()
    {
        // Arrange
        using var resource = new TxtcResource(_logger);

        // Act & Assert
        var action = () => resource.SetCustomProperty(null!, "value");
        action.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void ResourceChanged_WhenLayerAdded_ShouldRaiseEvent()
    {
        // Arrange
        using var resource = new TxtcResource(_logger);
        var eventRaised = false;
        resource.ResourceChanged += (_, _) => eventRaised = true;

        var tgi = new TgiReference(0x12345678, 0x87654321, 0x1234567890ABCDEF);
        var reference = new TextureReference(tgi, TextureFormat.ARGB, 512, 512);

        // Act
        resource.AddLayer(reference);

        // Assert
        eventRaised.Should().BeTrue();
    }
}

/// <summary>
/// Tests for the texture data structures.
/// </summary>
public class TextureDataStructureTests
{
    [Fact]
    public void TextureReference_Constructor_ShouldSetProperties()
    {
        // Arrange
        var tgi = new TgiReference(0x12345678, 0x87654321, 0x1234567890ABCDEF);
        const TextureFormat format = TextureFormat.DXT5;
        const uint width = 512;
        const uint height = 256;
        const MipmapLevel mipmapLevel = MipmapLevel.Full;

        // Act
        var reference = new TextureReference(tgi, format, width, height, mipmapLevel);

        // Assert
        reference.Tgi.Should().Be(tgi);
        reference.Format.Should().Be(format);
        reference.Width.Should().Be(width);
        reference.Height.Should().Be(height);
        reference.MipmapLevel.Should().Be(mipmapLevel);
    }

    [Fact]
    public void EmbeddedTextureData_Constructor_ShouldSetProperties()
    {
        // Arrange
        const TextureFormat format = TextureFormat.ARGB;
        const uint width = 256;
        const uint height = 128;
        var data = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var embedded = new EmbeddedTextureData(format, width, height, data);

        // Assert
        embedded.Format.Should().Be(format);
        embedded.Width.Should().Be(width);
        embedded.Height.Should().Be(height);
        embedded.Data.Should().BeEquivalentTo(data);
        embedded.DataSize.Should().Be((uint)data.Length);
    }

    [Fact]
    public void EmbeddedTextureData_Constructor_NullData_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new EmbeddedTextureData(TextureFormat.ARGB, 256, 128, null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("data");
    }

    [Fact]
    public void CompositionParameters_Default_ShouldHaveCorrectValues()
    {
        // Act
        var parameters = CompositionParameters.Default;

        // Assert
        parameters.BlendMode.Should().Be(0u);
        parameters.Opacity.Should().Be(255);
        parameters.TileU.Should().Be(1.0f);
        parameters.TileV.Should().Be(1.0f);
        parameters.OffsetU.Should().Be(0.0f);
        parameters.OffsetV.Should().Be(0.0f);
        parameters.Rotation.Should().Be(0.0f);
    }

    [Theory]
    [InlineData(TextureFormat.Unknown, 0)]
    [InlineData(TextureFormat.DXT1, 1)]
    [InlineData(TextureFormat.DXT3, 2)]
    [InlineData(TextureFormat.DXT5, 3)]
    [InlineData(TextureFormat.ARGB, 4)]
    public void TextureFormat_Values_ShouldHaveCorrectIntegerValues(TextureFormat format, int expectedValue)
    {
        // Assert
        ((int)format).Should().Be(expectedValue);
    }

    [Theory]
    [InlineData(TxtcFlags.None, 0)]
    [InlineData(TxtcFlags.ExternalReference, 1)]
    [InlineData(TxtcFlags.EmbeddedData, 2)]
    [InlineData(TxtcFlags.Compressed, 4)]
    [InlineData(TxtcFlags.HasAlpha, 8)]
    public void TxtcFlags_Values_ShouldHaveCorrectIntegerValues(TxtcFlags flags, int expectedValue)
    {
        // Assert
        ((int)flags).Should().Be(expectedValue);
    }
}
