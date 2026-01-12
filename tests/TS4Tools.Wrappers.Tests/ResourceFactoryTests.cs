using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.Wrappers;
using TS4Tools.Wrappers.CatalogResource;
using Xunit;
using CasPartFactory = TS4Tools.Wrappers.CasPartResource.CasPartResourceFactory;
using CasPartRes = TS4Tools.Wrappers.CasPartResource.CasPartResource;

namespace TS4Tools.Wrappers.Tests;

/// <summary>
/// Tests for all resource factories.
/// Verifies that each factory can create resources correctly.
/// </summary>
public class ResourceFactoryTests
{
    #region StblResourceFactory Tests

    [Fact]
    public void StblResourceFactory_CreateEmpty_ReturnsValidResource()
    {
        var factory = new StblResourceFactory();
        var key = new ResourceKey(0x220557DA, 0, 0);

        var resource = factory.CreateEmpty(key);

        resource.Should().NotBeNull();
        resource.Should().BeOfType<StblResource>();
    }

    [Fact]
    public void StblResourceFactory_Create_ReturnsCorrectType()
    {
        var factory = new StblResourceFactory();
        var key = new ResourceKey(0x220557DA, 0, 0);
        var data = CreateMinimalStbl();

        var resource = factory.Create(key, data);

        resource.Should().BeOfType<StblResource>();
    }

    #endregion

    #region NameMapResourceFactory Tests

    [Fact]
    public void NameMapResourceFactory_CreateEmpty_ReturnsValidResource()
    {
        var factory = new NameMapResourceFactory();
        var key = new ResourceKey(0x0166038C, 0, 0);

        var resource = factory.CreateEmpty(key);

        resource.Should().NotBeNull();
        resource.Should().BeOfType<NameMapResource>();
    }

    [Fact]
    public void NameMapResourceFactory_Create_ReturnsCorrectType()
    {
        var factory = new NameMapResourceFactory();
        var key = new ResourceKey(0x0166038C, 0, 0);
        // Empty data is valid for NameMap
        var data = ReadOnlyMemory<byte>.Empty;

        var resource = factory.Create(key, data);

        resource.Should().BeOfType<NameMapResource>();
    }

    #endregion

    #region TextResourceFactory Tests

    [Fact]
    public void TextResourceFactory_CreateEmpty_ReturnsValidResource()
    {
        var factory = new TextResourceFactory();
        var key = new ResourceKey(0x03B33DDF, 0, 0);

        var resource = factory.CreateEmpty(key);

        resource.Should().NotBeNull();
        resource.Should().BeOfType<TextResource>();
    }

    [Fact]
    public void TextResourceFactory_Create_ReturnsCorrectType()
    {
        var factory = new TextResourceFactory();
        var key = new ResourceKey(0x03B33DDF, 0, 0);
        var data = System.Text.Encoding.UTF8.GetBytes("Test content");

        var resource = factory.Create(key, data);

        resource.Should().BeOfType<TextResource>();
    }

    #endregion

    #region ImageResourceFactory Tests

    [Fact]
    public void ImageResourceFactory_CreateEmpty_ReturnsValidResource()
    {
        var factory = new ImageResourceFactory();
        var key = new ResourceKey(0x00B00000, 0, 0); // PNG type

        var resource = factory.CreateEmpty(key);

        resource.Should().NotBeNull();
        resource.Should().BeOfType<ImageResource>();
    }

    #endregion

    #region SimDataResourceFactory Tests

    [Fact]
    public void SimDataResourceFactory_CreateEmpty_ReturnsValidResource()
    {
        var factory = new SimDataResourceFactory();
        var key = new ResourceKey(0x545AC67A, 0, 0);

        var resource = factory.CreateEmpty(key);

        resource.Should().NotBeNull();
        resource.Should().BeOfType<SimDataResource>();
    }

    #endregion

    #region RcolResourceFactory Tests

    [Fact]
    public void RcolResourceFactory_CreateEmpty_ReturnsValidResource()
    {
        var factory = new RcolResourceFactory();
        var key = new ResourceKey(RcolConstants.Modl, 0, 0);

        var resource = factory.CreateEmpty(key);

        resource.Should().NotBeNull();
        resource.Should().BeOfType<RcolResource>();
    }

    #endregion

    #region CasPartResourceFactory Tests

    [Fact]
    public void CasPartResourceFactory_CreateEmpty_ReturnsValidResource()
    {
        var factory = new CasPartFactory();
        var key = new ResourceKey(CasPartRes.TypeId, 0, 0);

        var resource = factory.CreateEmpty(key);

        resource.Should().NotBeNull();
        resource.Should().BeOfType<CasPartRes>();
    }

    #endregion

    #region ThumResourceFactory Tests

    [Fact]
    public void ThumResourceFactory_CreateEmpty_ReturnsValidResource()
    {
        var factory = new ThumResourceFactory();
        var key = new ResourceKey(0x16CA6BC4, 0, 0);

        var resource = factory.CreateEmpty(key);

        resource.Should().NotBeNull();
        resource.Should().BeOfType<ThumResource>();
    }

    #endregion

    #region ThumbnailResourceFactory Tests

    [Fact]
    public void ThumbnailResourceFactory_CreateEmpty_ReturnsValidResource()
    {
        var factory = new ThumbnailResourceFactory();
        var key = new ResourceKey(0x0D338A3A, 0, 0);

        var resource = factory.CreateEmpty(key);

        resource.Should().NotBeNull();
        resource.Should().BeOfType<ThumbnailResource>();
    }

    #endregion

    #region RigResourceFactory Tests

    [Fact]
    public void RigResourceFactory_CreateEmpty_ReturnsValidResource()
    {
        var factory = new RigResourceFactory();
        var key = new ResourceKey(RigResource.TypeId, 0, 0);

        var resource = factory.CreateEmpty(key);

        resource.Should().NotBeNull();
        resource.Should().BeOfType<RigResource>();
    }

    #endregion

    #region NgmpHashMapResourceFactory Tests

    [Fact]
    public void NgmpHashMapResourceFactory_CreateEmpty_ReturnsValidResource()
    {
        var factory = new NgmpHashMapResourceFactory();
        var key = new ResourceKey(0xF3A38370, 0, 0);

        var resource = factory.CreateEmpty(key);

        resource.Should().NotBeNull();
        resource.Should().BeOfType<NgmpHashMapResource>();
    }

    #endregion

    #region ObjKeyResourceFactory Tests

    [Fact]
    public void ObjKeyResourceFactory_CreateEmpty_ReturnsValidResource()
    {
        var factory = new ObjKeyResourceFactory();
        var key = new ResourceKey(0x02DC343F, 0, 0);

        var resource = factory.CreateEmpty(key);

        resource.Should().NotBeNull();
        resource.Should().BeOfType<ObjKeyResource>();
    }

    #endregion

    #region ScriptResourceFactory Tests

    [Fact]
    public void ScriptResourceFactory_CreateEmpty_ReturnsValidResource()
    {
        var factory = new ScriptResourceFactory();
        var key = new ResourceKey(0x073FAA07, 0, 0);

        var resource = factory.CreateEmpty(key);

        resource.Should().NotBeNull();
        resource.Should().BeOfType<ScriptResource>();
    }

    #endregion

    #region ModularResourceFactory Tests

    [Fact]
    public void ModularResourceFactory_CreateEmpty_ReturnsValidResource()
    {
        var factory = new ModularResourceFactory();
        var key = new ResourceKey(0xCF9A4ACE, 0, 0);

        var resource = factory.CreateEmpty(key);

        resource.Should().NotBeNull();
        resource.Should().BeOfType<ModularResource>();
    }

    #endregion

    #region ComplateResourceFactory Tests

    [Fact]
    public void ComplateResourceFactory_CreateEmpty_ReturnsValidResource()
    {
        var factory = new ComplateResourceFactory();
        var key = new ResourceKey(0x044AE110, 0, 0);

        var resource = factory.CreateEmpty(key);

        resource.Should().NotBeNull();
        resource.Should().BeOfType<ComplateResource>();
    }

    #endregion

    #region TxtcResourceFactory Tests

    [Fact]
    public void TxtcResourceFactory_CreateEmpty_ReturnsValidResource()
    {
        var factory = new TxtcResourceFactory();
        var key = new ResourceKey(0x033A1435, 0, 0);

        var resource = factory.CreateEmpty(key);

        resource.Should().NotBeNull();
        resource.Should().BeOfType<TxtcResource>();
    }

    #endregion

    #region UserCAStPresetResourceFactory Tests

    [Fact]
    public void UserCAStPresetResourceFactory_CreateEmpty_ReturnsValidResource()
    {
        var factory = new UserCAStPresetResourceFactory();
        var key = new ResourceKey(0x0591B1AF, 0, 0);

        var resource = factory.CreateEmpty(key);

        resource.Should().NotBeNull();
        resource.Should().BeOfType<UserCAStPresetResource>();
    }

    #endregion

    #region MiscResource Factory Tests

    [Fact]
    public void AuevResourceFactory_CreateEmpty_ReturnsValidResource()
    {
        var factory = new AuevResourceFactory();
        var key = new ResourceKey(0xBDD82221, 0, 0);

        var resource = factory.CreateEmpty(key);

        resource.Should().NotBeNull();
        resource.Should().BeOfType<AuevResource>();
    }

    [Fact]
    public void MtblResourceFactory_CreateEmpty_ReturnsValidResource()
    {
        var factory = new MtblResourceFactory();
        var key = new ResourceKey(0x81CA1A10, 0, 0);

        var resource = factory.CreateEmpty(key);

        resource.Should().NotBeNull();
        resource.Should().BeOfType<MtblResource>();
    }

    [Fact]
    public void SkyBoxTextureResourceFactory_CreateEmpty_ReturnsValidResource()
    {
        var factory = new SkyBoxTextureResourceFactory();
        var key = new ResourceKey(0x71A449C9, 0, 0);

        var resource = factory.CreateEmpty(key);

        resource.Should().NotBeNull();
        resource.Should().BeOfType<SkyBoxTextureResource>();
    }

    [Fact]
    public void TmltResourceFactory_CreateEmpty_ReturnsValidResource()
    {
        var factory = new TmltResourceFactory();
        var key = new ResourceKey(0xB0118C15, 0, 0);

        var resource = factory.CreateEmpty(key);

        resource.Should().NotBeNull();
        resource.Should().BeOfType<TmltResource>();
    }

    [Fact]
    public void TrimResourceFactory_CreateEmpty_ReturnsValidResource()
    {
        var factory = new TrimResourceFactory();
        var key = new ResourceKey(0x76BCF80C, 0, 0);

        var resource = factory.CreateEmpty(key);

        resource.Should().NotBeNull();
        resource.Should().BeOfType<TrimResource>();
    }

    #endregion

    #region ClipResourceFactory Tests

    [Fact]
    public void ClipResourceFactory_CreateEmpty_ReturnsValidResource()
    {
        var factory = new ClipResourceFactory();
        var key = new ResourceKey(ClipResource.TypeId, 0, 0);

        var resource = factory.CreateEmpty(key);

        resource.Should().NotBeNull();
        resource.Should().BeOfType<ClipResource>();
    }

    [Fact]
    public void ClipResourceFactory_Create_ReturnsCorrectType()
    {
        var factory = new ClipResourceFactory();
        var key = new ResourceKey(ClipResource.TypeId, 0, 0);

        var resource = factory.Create(key, ReadOnlyMemory<byte>.Empty);

        resource.Should().BeOfType<ClipResource>();
    }

    #endregion

    #region Catalog Resource Factory Tests

    [Theory]
    [InlineData(typeof(CobjResourceFactory), 0x319E4F1Du)]
    [InlineData(typeof(CftrResourceFactory), 0xE7ADA79Du)]
    [InlineData(typeof(CfltResourceFactory), 0x84C23219u)]
    [InlineData(typeof(CpltResourceFactory), 0xA5DFFCF3u)]
    [InlineData(typeof(CrtrResourceFactory), 0xB0311D0Fu)]
    [InlineData(typeof(CrptResourceFactory), 0xF1EDBD86u)]
    [InlineData(typeof(CtptResourceFactory), 0xEBCBB16Cu)]
    [InlineData(typeof(CflrResourceFactory), 0xB4F762C9u)]
    [InlineData(typeof(CfrzResourceFactory), 0xA057811Cu)]
    [InlineData(typeof(CfndResourceFactory), 0x2FAE983Eu)]
    [InlineData(typeof(CralResourceFactory), 0x1C1CF1F7u)]
    [InlineData(typeof(StrmResourceFactory), 0x74050B1Fu)]
    [InlineData(typeof(CblkResourceFactory), 0x07936CE0u)]
    [InlineData(typeof(CstrResourceFactory), 0x9A20CD1Cu)]
    [InlineData(typeof(CcolResourceFactory), 0x1D6DF1CFu)]
    [InlineData(typeof(CstlResourceFactory), 0x9F5CFF10u)]
    [InlineData(typeof(CspnResourceFactory), 0x3F0C529Au)]
    public void CatalogResourceFactory_CreateEmpty_ReturnsValidResource(Type factoryType, uint typeId)
    {
        // Arrange
        var factory = (IResourceFactory)Activator.CreateInstance(factoryType)!;
        var key = new ResourceKey(typeId, 0, 0);

        // Act
        var resource = factory.CreateEmpty(key);

        // Assert
        resource.Should().NotBeNull();
    }

    [Theory]
    [InlineData(typeof(CobjResourceFactory), 0x319E4F1Du)]
    [InlineData(typeof(CftrResourceFactory), 0xE7ADA79Du)]
    [InlineData(typeof(CfltResourceFactory), 0x84C23219u)]
    [InlineData(typeof(CpltResourceFactory), 0xA5DFFCF3u)]
    [InlineData(typeof(CrtrResourceFactory), 0xB0311D0Fu)]
    [InlineData(typeof(CrptResourceFactory), 0xF1EDBD86u)]
    [InlineData(typeof(CtptResourceFactory), 0xEBCBB16Cu)]
    [InlineData(typeof(CflrResourceFactory), 0xB4F762C9u)]
    [InlineData(typeof(CfrzResourceFactory), 0xA057811Cu)]
    [InlineData(typeof(CfndResourceFactory), 0x2FAE983Eu)]
    [InlineData(typeof(CralResourceFactory), 0x1C1CF1F7u)]
    [InlineData(typeof(StrmResourceFactory), 0x74050B1Fu)]
    [InlineData(typeof(CblkResourceFactory), 0x07936CE0u)]
    [InlineData(typeof(CstrResourceFactory), 0x9A20CD1Cu)]
    [InlineData(typeof(CcolResourceFactory), 0x1D6DF1CFu)]
    [InlineData(typeof(CstlResourceFactory), 0x9F5CFF10u)]
    [InlineData(typeof(CspnResourceFactory), 0x3F0C529Au)]
    public void CatalogResourceFactory_Create_WithEmptyData_DoesNotThrow(Type factoryType, uint typeId)
    {
        // Arrange
        var factory = (IResourceFactory)Activator.CreateInstance(factoryType)!;
        var key = new ResourceKey(typeId, 0, 0);

        // Act & Assert - creating with empty data should not throw
        var act = () => factory.Create(key, ReadOnlyMemory<byte>.Empty);
        act.Should().NotThrow();
    }

    #endregion

    #region Helper Methods

    private static byte[] CreateMinimalStbl()
    {
        // Minimal valid STBL with no entries
        return
        [
            // Magic "STBL"
            0x53, 0x54, 0x42, 0x4C,
            // Version (5)
            0x05, 0x00,
            // IsCompressed (0)
            0x00,
            // Entry count (0)
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            // Reserved
            0x00, 0x00,
            // String data length (0)
            0x00, 0x00, 0x00, 0x00
        ];
    }

    #endregion
}
