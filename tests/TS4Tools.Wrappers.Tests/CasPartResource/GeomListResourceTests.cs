using FluentAssertions;
using TS4Tools.Wrappers;
using TS4Tools.Wrappers.CasPartResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CasPartResource;

public class GeomListResourceTests
{
    private static ResourceKey TestKey => new(GeomListResource.TypeId, 0, 0x12345678);

    [Fact]
    public void EmptyResource_ShouldNotBeValid()
    {
        // Arrange & Act
        var resource = new GeomListResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Assert
        resource.IsValid.Should().BeFalse();
    }

    [Fact]
    public void TruncatedHeader_ShouldNotBeValid()
    {
        // Arrange - Less than 20 bytes (5 uint32s)
        byte[] truncatedData = new byte[16];

        // Act
        var resource = new GeomListResource(TestKey, truncatedData);

        // Assert
        resource.IsValid.Should().BeFalse();
    }

    [Fact]
    public void MinimalValidResource_ShouldParse()
    {
        // Arrange - Minimum valid: header (20 bytes) + object position/length/version (12) + block count (4)
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Header
        writer.Write(1u); // ContextVersion
        writer.Write(0u); // publicKeyCount
        writer.Write(0u); // externalKeyCount
        writer.Write(0u); // delayLoadKeyCount
        writer.Write(0u); // objectCount

        // Object info
        writer.Write(0u); // objectPosition
        writer.Write(0u); // objectLength
        writer.Write(1u); // objectVersion

        // Reference block list (count only, no blocks)
        writer.Write(0); // blockCount

        byte[] data = ms.ToArray();

        // Act
        var resource = new GeomListResource(TestKey, data);

        // Assert
        resource.IsValid.Should().BeTrue();
        resource.ContextVersion.Should().Be(1);
        resource.ObjectVersion.Should().Be(1);
        resource.PublicKeys.Should().BeEmpty();
        resource.ExternalKeys.Should().BeEmpty();
        resource.DelayLoadKeys.Should().BeEmpty();
        resource.ReferenceBlocks.Should().BeEmpty();
    }

    [Fact]
    public void ResourceWithKeys_ShouldParseCorrectly()
    {
        // Arrange
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Header
        writer.Write(2u); // ContextVersion
        writer.Write(1u); // publicKeyCount
        writer.Write(1u); // externalKeyCount
        writer.Write(1u); // delayLoadKeyCount
        writer.Write(0u); // objectCount

        // Public key (ITG order: Instance, Type, Group)
        writer.Write(0x1111111111111111UL); // Instance
        writer.Write(0x22222222u); // Type
        writer.Write(0x33333333u); // Group

        // External key
        writer.Write(0x4444444444444444UL);
        writer.Write(0x55555555u);
        writer.Write(0x66666666u);

        // Delay-load key
        writer.Write(0x7777777777777777UL);
        writer.Write(0x88888888u);
        writer.Write(0x99999999u);

        // Object info
        writer.Write(0u);
        writer.Write(4u); // length
        writer.Write(3u); // version

        // Reference block list
        writer.Write(0); // count

        byte[] data = ms.ToArray();

        // Act
        var resource = new GeomListResource(TestKey, data);

        // Assert
        resource.IsValid.Should().BeTrue();
        resource.PublicKeys.Should().HaveCount(1);
        resource.PublicKeys[0].Instance.Should().Be(0x1111111111111111UL);
        resource.PublicKeys[0].ResourceType.Should().Be(0x22222222u);
        resource.PublicKeys[0].ResourceGroup.Should().Be(0x33333333u);

        resource.ExternalKeys.Should().HaveCount(1);
        resource.ExternalKeys[0].Instance.Should().Be(0x4444444444444444UL);

        resource.DelayLoadKeys.Should().HaveCount(1);
        resource.DelayLoadKeys[0].Instance.Should().Be(0x7777777777777777UL);
    }

    [Fact]
    public void ResourceWithReferenceBlocks_ShouldParseCorrectly()
    {
        // Arrange
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Header
        writer.Write(1u);
        writer.Write(0u); // no public keys
        writer.Write(0u); // no external keys
        writer.Write(0u); // no delay-load keys
        writer.Write(1u); // 1 object

        // Object info
        writer.Write(32u); // position (after this header)
        writer.Write(17u); // length (13 + 4 for count)
        writer.Write(1u);

        // Reference block list
        writer.Write(1); // count

        // Reference block: region(4) + layer(4) + isReplacement(1) + tgiCount(4) = 13 bytes min
        writer.Write((uint)CASPartRegion.Chest); // region
        writer.Write(1.5f); // layer
        writer.Write(true); // isReplacement
        writer.Write(0); // no TGIs

        byte[] data = ms.ToArray();

        // Act
        var resource = new GeomListResource(TestKey, data);

        // Assert
        resource.IsValid.Should().BeTrue();
        resource.ReferenceBlocks.Should().HaveCount(1);

        var block = resource.ReferenceBlocks[0];
        block.Region.Should().Be(CASPartRegion.Chest);
        block.Layer.Should().Be(1.5f);
        block.IsReplacement.Should().BeTrue();
        block.TgiList.Should().BeEmpty();
    }

    [Fact]
    public void ReferenceBlockWithTgiList_ShouldParseCorrectly()
    {
        // Arrange
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Header
        writer.Write(1u);
        writer.Write(0u);
        writer.Write(0u);
        writer.Write(0u);
        writer.Write(1u);

        // Object info
        writer.Write(32u);
        writer.Write(33u); // 13 + 4 + 16 (one TGI)
        writer.Write(1u);

        // Reference block list
        writer.Write(1);

        // Reference block
        writer.Write((uint)CASPartRegion.HandLeft);
        writer.Write(2.0f);
        writer.Write(false);
        writer.Write(1); // 1 TGI

        // TGI (ITG order)
        writer.Write(0xABCDEF0123456789UL);
        writer.Write(0x11111111u);
        writer.Write(0x22222222u);

        byte[] data = ms.ToArray();

        // Act
        var resource = new GeomListResource(TestKey, data);

        // Assert
        resource.IsValid.Should().BeTrue();
        resource.ReferenceBlocks.Should().HaveCount(1);

        var block = resource.ReferenceBlocks[0];
        block.TgiList.Should().HaveCount(1);
        block.TgiList[0].Instance.Should().Be(0xABCDEF0123456789UL);
        block.TgiList[0].ResourceType.Should().Be(0x11111111u);
        block.TgiList[0].ResourceGroup.Should().Be(0x22222222u);
    }

    [Fact]
    public void RoundTrip_MinimalResource_ShouldPreserveData()
    {
        // Arrange
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(5u); // ContextVersion
        writer.Write(0u);
        writer.Write(0u);
        writer.Write(0u);
        writer.Write(0u);
        writer.Write(0u);
        writer.Write(4u); // objectLength
        writer.Write(2u); // objectVersion
        writer.Write(0);

        byte[] originalData = ms.ToArray();

        // Act
        var resource = new GeomListResource(TestKey, originalData);
        var serialized = resource.Data;

        // Re-parse
        var reparsed = new GeomListResource(TestKey, serialized);

        // Assert
        reparsed.IsValid.Should().BeTrue();
        reparsed.ContextVersion.Should().Be(resource.ContextVersion);
        reparsed.ObjectVersion.Should().Be(resource.ObjectVersion);
    }

    [Fact]
    public void RoundTrip_WithKeysAndBlocks_ShouldPreserveData()
    {
        // Arrange - Create resource programmatically
        var resource = new GeomListResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Set properties and add data
        resource.ContextVersion = 3;
        resource.ObjectVersion = 2;

        resource.AddPublicKey(new ResourceKey(0x11111111, 0x22222222, 0x3333333333333333));
        resource.AddExternalKey(new ResourceKey(0x44444444, 0x55555555, 0x6666666666666666));

        resource.AddReferenceBlock(new GeomReferenceBlock
        {
            Region = CASPartRegion.Neck,
            Layer = 0.5f,
            IsReplacement = true,
            TgiList = [new ResourceKey(0xAAAAAAAA, 0xBBBBBBBB, 0xCCCCCCCCCCCCCCCC)]
        });

        // Act
        var serialized = resource.Data;
        var reparsed = new GeomListResource(TestKey, serialized);

        // Assert
        reparsed.IsValid.Should().BeTrue();
        reparsed.ContextVersion.Should().Be(3);
        reparsed.ObjectVersion.Should().Be(2);

        reparsed.PublicKeys.Should().HaveCount(1);
        reparsed.PublicKeys[0].ResourceType.Should().Be(0x11111111);

        reparsed.ExternalKeys.Should().HaveCount(1);
        reparsed.ExternalKeys[0].ResourceType.Should().Be(0x44444444);

        reparsed.ReferenceBlocks.Should().HaveCount(1);
        reparsed.ReferenceBlocks[0].Region.Should().Be(CASPartRegion.Neck);
        reparsed.ReferenceBlocks[0].Layer.Should().Be(0.5f);
        reparsed.ReferenceBlocks[0].IsReplacement.Should().BeTrue();
        reparsed.ReferenceBlocks[0].TgiList.Should().HaveCount(1);
    }

    [Fact]
    public void UnreasonableArraySize_ShouldNotBeValid()
    {
        // Arrange - publicKeyCount set absurdly high
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(1u);
        writer.Write(100001u); // Unreasonable count
        writer.Write(0u);
        writer.Write(0u);
        writer.Write(0u);

        byte[] data = ms.ToArray();

        // Act
        var resource = new GeomListResource(TestKey, data);

        // Assert
        resource.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Clear_ShouldRemoveAllData()
    {
        // Arrange
        var resource = new GeomListResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.AddPublicKey(new ResourceKey(1, 2, 3));
        resource.AddExternalKey(new ResourceKey(4, 5, 6));
        resource.AddDelayLoadKey(new ResourceKey(7, 8, 9));
        resource.AddReferenceBlock(new GeomReferenceBlock { Region = CASPartRegion.Base });

        // Act
        resource.Clear();

        // Assert
        resource.PublicKeys.Should().BeEmpty();
        resource.ExternalKeys.Should().BeEmpty();
        resource.DelayLoadKeys.Should().BeEmpty();
        resource.ReferenceBlocks.Should().BeEmpty();
    }

    [Fact]
    public void TypeId_ShouldMatchExpectedValue()
    {
        // Assert
        GeomListResource.TypeId.Should().Be(0xAC16FBEC);
    }
}
