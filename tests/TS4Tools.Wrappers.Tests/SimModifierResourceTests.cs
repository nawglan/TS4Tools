using FluentAssertions;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

/// <summary>
/// Tests for <see cref="SimModifierResource"/>.
///
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MiscellaneousResource/SimModifierResource.cs
///
/// Format:
/// - ContexData header (version, key lists)
/// - Version: uint32
/// - Gender: uint32
/// - Region: uint32
/// - LinkTag: uint32
/// - BonePoseKey: TGIBlock (ITG order, 16 bytes)
/// - DeformerMapShapeKey: TGIBlock (ITG order, 16 bytes)
/// - DeformerMapNormalKey: TGIBlock (ITG order, 16 bytes)
/// - BoneEntryCount: uint32
/// - BoneEntries: BoneEntry[] (8 bytes each)
/// </summary>
public class SimModifierResourceTests
{
    private static readonly ResourceKey TestKey = new(0xC5F6763E, 0, 0);

    private const int TgiBlockSize = 16; // Instance (8) + Type (4) + Group (4)
    private const int ObjectDataSize = 8; // Position (4) + Length (4)
    private const int BoneEntrySize = 8; // BoneHash (4) + Multiplier (4)

    /// <summary>
    /// Creates valid SMOD resource data.
    /// </summary>
    private static byte[] CreateValidData(
        uint contextVersion = 1,
        ResourceKey[]? publicKeys = null,
        ResourceKey[]? externalKeys = null,
        ResourceKey[]? delayLoadKeys = null,
        SimModifierObjectData[]? objectData = null,
        uint version = 1,
        uint gender = 0,
        uint region = 0,
        uint linkTag = 0,
        ResourceKey? bonePoseKey = null,
        ResourceKey? deformerMapShapeKey = null,
        ResourceKey? deformerMapNormalKey = null,
        SimModifierBoneEntry[]? boneEntries = null)
    {
        publicKeys ??= [];
        externalKeys ??= [];
        delayLoadKeys ??= [];
        objectData ??= [];
        boneEntries ??= [];
        bonePoseKey ??= default;
        deformerMapShapeKey ??= default;
        deformerMapNormalKey ??= default;

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // ContexData header
        writer.Write(contextVersion);
        writer.Write(publicKeys.Length);
        writer.Write(externalKeys.Length);
        writer.Write(delayLoadKeys.Length);
        writer.Write(objectData.Length);

        // Public keys
        foreach (var key in publicKeys)
        {
            WriteTgiBlock(writer, key);
        }

        // External keys
        foreach (var key in externalKeys)
        {
            WriteTgiBlock(writer, key);
        }

        // Delay load keys
        foreach (var key in delayLoadKeys)
        {
            WriteTgiBlock(writer, key);
        }

        // Object data
        foreach (var obj in objectData)
        {
            writer.Write(obj.Position);
            writer.Write(obj.Length);
        }

        // Main resource fields
        writer.Write(version);
        writer.Write(gender);
        writer.Write(region);
        writer.Write(linkTag);

        WriteTgiBlock(writer, bonePoseKey.Value);
        WriteTgiBlock(writer, deformerMapShapeKey.Value);
        WriteTgiBlock(writer, deformerMapNormalKey.Value);

        // Bone entries
        writer.Write(boneEntries.Length);
        foreach (var entry in boneEntries)
        {
            writer.Write(entry.BoneHash);
            writer.Write(entry.Multiplier);
        }

        return ms.ToArray();
    }

    private static void WriteTgiBlock(BinaryWriter writer, ResourceKey key)
    {
        // ITG order: Instance (8 bytes), Type (4 bytes), Group (4 bytes)
        writer.Write(key.Instance);
        writer.Write(key.ResourceType);
        writer.Write(key.ResourceGroup);
    }

    [Fact]
    public void Parse_ValidData_ExtractsFields()
    {
        // Arrange
        var publicKeys = new ResourceKey[]
        {
            new(0x12345678, 0x00000001, 0xABCDEF0123456789),
        };
        var boneEntries = new SimModifierBoneEntry[]
        {
            new(0x12345678, 1.5f),
            new(0xDEADBEEF, -0.5f),
        };
        var bonePoseKey = new ResourceKey(0xAAAAAAAA, 0xBBBBBBBB, 0xCCCCCCCCDDDDDDDD);

        var data = CreateValidData(
            contextVersion: 2,
            publicKeys: publicKeys,
            version: 3,
            gender: 1,
            region: 5,
            linkTag: 100,
            bonePoseKey: bonePoseKey,
            boneEntries: boneEntries);

        // Act
        var resource = new SimModifierResource(TestKey, data);

        // Assert
        resource.ContextData.Version.Should().Be(2);
        resource.ContextData.PublicKeys.Should().HaveCount(1);
        resource.ContextData.PublicKeys[0].Should().Be(publicKeys[0]);

        resource.Version.Should().Be(3);
        resource.Gender.Should().Be(1);
        resource.Region.Should().Be(5);
        resource.LinkTag.Should().Be(100);
        resource.BonePoseKey.Should().Be(bonePoseKey);

        resource.BoneEntries.Should().HaveCount(2);
        resource.BoneEntries[0].BoneHash.Should().Be(0x12345678);
        resource.BoneEntries[0].Multiplier.Should().Be(1.5f);
        resource.BoneEntries[1].BoneHash.Should().Be(0xDEADBEEF);
        resource.BoneEntries[1].Multiplier.Should().Be(-0.5f);
    }

    [Fact]
    public void Parse_AllKeyTypes_ParsesCorrectly()
    {
        // Arrange
        var publicKeys = new ResourceKey[] { new(0x11111111, 0x22222222, 0x3333333344444444) };
        var externalKeys = new ResourceKey[] { new(0x55555555, 0x66666666, 0x7777777788888888) };
        var delayLoadKeys = new ResourceKey[] { new(0x99999999, 0xAAAAAAAA, 0xBBBBBBBBCCCCCCCC) };
        var objectData = new SimModifierObjectData[] { new(100, 200), new(300, 400) };

        var data = CreateValidData(
            publicKeys: publicKeys,
            externalKeys: externalKeys,
            delayLoadKeys: delayLoadKeys,
            objectData: objectData);

        // Act
        var resource = new SimModifierResource(TestKey, data);

        // Assert
        resource.ContextData.PublicKeys.Should().HaveCount(1);
        resource.ContextData.PublicKeys[0].Should().Be(publicKeys[0]);

        resource.ContextData.ExternalKeys.Should().HaveCount(1);
        resource.ContextData.ExternalKeys[0].Should().Be(externalKeys[0]);

        resource.ContextData.DelayLoadKeys.Should().HaveCount(1);
        resource.ContextData.DelayLoadKeys[0].Should().Be(delayLoadKeys[0]);

        resource.ContextData.ObjectData.Should().HaveCount(2);
        resource.ContextData.ObjectData[0].Position.Should().Be(100);
        resource.ContextData.ObjectData[0].Length.Should().Be(200);
        resource.ContextData.ObjectData[1].Position.Should().Be(300);
        resource.ContextData.ObjectData[1].Length.Should().Be(400);
    }

    [Fact]
    public void Parse_EmptyLists_ParsesCorrectly()
    {
        // Arrange
        var data = CreateValidData();

        // Act
        var resource = new SimModifierResource(TestKey, data);

        // Assert
        resource.ContextData.PublicKeys.Should().BeEmpty();
        resource.ContextData.ExternalKeys.Should().BeEmpty();
        resource.ContextData.DelayLoadKeys.Should().BeEmpty();
        resource.ContextData.ObjectData.Should().BeEmpty();
        resource.BoneEntries.Should().BeEmpty();
    }

    [Fact]
    public void Parse_AllTgiReferences_ParsesCorrectly()
    {
        // Arrange
        var bonePoseKey = new ResourceKey(0x11111111, 0x22222222, 0x3333333344444444);
        var deformerMapShapeKey = new ResourceKey(0x55555555, 0x66666666, 0x7777777788888888);
        var deformerMapNormalKey = new ResourceKey(0x99999999, 0xAAAAAAAA, 0xBBBBBBBBCCCCCCCC);

        var data = CreateValidData(
            bonePoseKey: bonePoseKey,
            deformerMapShapeKey: deformerMapShapeKey,
            deformerMapNormalKey: deformerMapNormalKey);

        // Act
        var resource = new SimModifierResource(TestKey, data);

        // Assert
        resource.BonePoseKey.Should().Be(bonePoseKey);
        resource.DeformerMapShapeKey.Should().Be(deformerMapShapeKey);
        resource.DeformerMapNormalKey.Should().Be(deformerMapNormalKey);
    }

    [Fact]
    public void Parse_TruncatedData_ThrowsInvalidDataException()
    {
        // Arrange - data too short for ContexData header
        var data = new byte[10];

        // Act
        Action act = () => _ = new SimModifierResource(TestKey, data);

        // Assert
        act.Should().Throw<InvalidDataException>().WithMessage("*too short*");
    }

    [Fact]
    public void Parse_TruncatedBoneEntries_ThrowsInvalidDataException()
    {
        // Arrange
        var data = CreateValidData(boneEntries: [new SimModifierBoneEntry(1, 1.0f)]);
        // Truncate the bone entry data
        var truncated = data[..^4];

        // Act
        Action act = () => _ = new SimModifierResource(TestKey, truncated);

        // Assert
        act.Should().Throw<InvalidDataException>().WithMessage("*too short*");
    }

    [Fact]
    public void Serialize_RoundTrip_PreservesData()
    {
        // Arrange
        var publicKeys = new ResourceKey[] { new(0x11111111, 0x22222222, 0x3333333344444444) };
        var externalKeys = new ResourceKey[] { new(0x55555555, 0x66666666, 0x7777777788888888) };
        var objectData = new SimModifierObjectData[] { new(100, 200) };
        var boneEntries = new SimModifierBoneEntry[] { new(0xAAAA, 2.0f), new(0xBBBB, 3.0f) };
        var bonePoseKey = new ResourceKey(0xDDDDDDDD, 0xEEEEEEEE, 0xFFFFFFFFFFFFFFFF);

        var data = CreateValidData(
            contextVersion: 5,
            publicKeys: publicKeys,
            externalKeys: externalKeys,
            objectData: objectData,
            version: 7,
            gender: 2,
            region: 3,
            linkTag: 999,
            bonePoseKey: bonePoseKey,
            boneEntries: boneEntries);

        var resource = new SimModifierResource(TestKey, data);

        // Act
        var serialized = resource.Data.ToArray();

        // Assert
        serialized.Should().BeEquivalentTo(data);
    }

    [Fact]
    public void AddBoneEntry_AddsToList()
    {
        // Arrange
        var data = CreateValidData();
        var resource = new SimModifierResource(TestKey, data);
        var newEntry = new SimModifierBoneEntry(0x12345678, 1.5f);

        // Act
        resource.AddBoneEntry(newEntry);

        // Assert
        resource.BoneEntries.Should().HaveCount(1);
        resource.BoneEntries[0].BoneHash.Should().Be(0x12345678);
        resource.BoneEntries[0].Multiplier.Should().Be(1.5f);
        resource.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void RemoveBoneEntry_RemovesFromList()
    {
        // Arrange
        var entry1 = new SimModifierBoneEntry(1, 1.0f);
        var entry2 = new SimModifierBoneEntry(2, 2.0f);
        var data = CreateValidData(boneEntries: [entry1, entry2]);
        var resource = new SimModifierResource(TestKey, data);

        // Act
        var result = resource.RemoveBoneEntry(entry1);

        // Assert
        result.Should().BeTrue();
        resource.BoneEntries.Should().HaveCount(1);
        resource.BoneEntries[0].BoneHash.Should().Be(2);
        resource.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void RemoveBoneEntry_NotFound_ReturnsFalse()
    {
        // Arrange
        var entry = new SimModifierBoneEntry(1, 1.0f);
        var data = CreateValidData(boneEntries: [entry]);
        var resource = new SimModifierResource(TestKey, data);
        var notFoundEntry = new SimModifierBoneEntry(99, 99.0f);

        // Act
        var result = resource.RemoveBoneEntry(notFoundEntry);

        // Assert
        result.Should().BeFalse();
        resource.BoneEntries.Should().HaveCount(1);
    }

    [Fact]
    public void ClearBoneEntries_RemovesAllEntries()
    {
        // Arrange
        var entries = new SimModifierBoneEntry[]
        {
            new(1, 1.0f),
            new(2, 2.0f),
            new(3, 3.0f),
        };
        var data = CreateValidData(boneEntries: entries);
        var resource = new SimModifierResource(TestKey, data);

        // Act
        resource.ClearBoneEntries();

        // Assert
        resource.BoneEntries.Should().BeEmpty();
        resource.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void SetBoneEntry_UpdatesEntry()
    {
        // Arrange
        var entry = new SimModifierBoneEntry(1, 1.0f);
        var data = CreateValidData(boneEntries: [entry]);
        var resource = new SimModifierResource(TestKey, data);
        var newEntry = new SimModifierBoneEntry(99, 99.99f);

        // Act
        resource.SetBoneEntry(0, newEntry);

        // Assert
        resource.BoneEntries[0].BoneHash.Should().Be(99);
        resource.BoneEntries[0].Multiplier.Should().Be(99.99f);
        resource.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void SetBoneEntry_InvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var data = CreateValidData();
        var resource = new SimModifierResource(TestKey, data);

        // Act
        Action act = () => resource.SetBoneEntry(0, new SimModifierBoneEntry(1, 1.0f));

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void SetBoneEntry_NegativeIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var data = CreateValidData(boneEntries: [new SimModifierBoneEntry(1, 1.0f)]);
        var resource = new SimModifierResource(TestKey, data);

        // Act
        Action act = () => resource.SetBoneEntry(-1, new SimModifierBoneEntry(1, 1.0f));

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Properties_Set_MarksAsDirty()
    {
        // Arrange
        var data = CreateValidData();
        var resource = new SimModifierResource(TestKey, data);

        // Act
        resource.Version = 5;

        // Assert
        resource.IsDirty.Should().BeTrue();
        resource.Version.Should().Be(5);
    }

    [Fact]
    public void EmptyData_InitializesDefaults()
    {
        // Arrange & Act
        var resource = new SimModifierResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Assert
        resource.Version.Should().Be(0);
        resource.Gender.Should().Be(0);
        resource.Region.Should().Be(0);
        resource.LinkTag.Should().Be(0);
        resource.BonePoseKey.Should().Be(default(ResourceKey));
        resource.DeformerMapShapeKey.Should().Be(default(ResourceKey));
        resource.DeformerMapNormalKey.Should().Be(default(ResourceKey));
        resource.BoneEntries.Should().BeEmpty();
        resource.ContextData.PublicKeys.Should().BeEmpty();
        resource.ContextData.ExternalKeys.Should().BeEmpty();
        resource.ContextData.DelayLoadKeys.Should().BeEmpty();
        resource.ContextData.ObjectData.Should().BeEmpty();
    }

    [Fact]
    public void Serialize_ModifiedValues_ProducesValidData()
    {
        // Arrange
        var data = CreateValidData();
        var resource = new SimModifierResource(TestKey, data);
        resource.Version = 10;
        resource.Gender = 1;
        resource.Region = 5;
        resource.LinkTag = 999;
        resource.BonePoseKey = new ResourceKey(0x11111111, 0x22222222, 0x3333333344444444);
        resource.AddBoneEntry(new SimModifierBoneEntry(0xAAAA, 2.5f));

        // Act
        var serialized = resource.Data;
        var reloaded = new SimModifierResource(TestKey, serialized);

        // Assert
        reloaded.Version.Should().Be(10);
        reloaded.Gender.Should().Be(1);
        reloaded.Region.Should().Be(5);
        reloaded.LinkTag.Should().Be(999);
        reloaded.BonePoseKey.Should().Be(new ResourceKey(0x11111111, 0x22222222, 0x3333333344444444));
        reloaded.BoneEntries.Should().HaveCount(1);
        reloaded.BoneEntries[0].BoneHash.Should().Be(0xAAAA);
        reloaded.BoneEntries[0].Multiplier.Should().Be(2.5f);
    }

    [Fact]
    public void SimModifierBoneEntry_Equality_WorksCorrectly()
    {
        // Arrange
        var entry1 = new SimModifierBoneEntry(0x12345678, 1.5f);
        var entry2 = new SimModifierBoneEntry(0x12345678, 1.5f);
        var entry3 = new SimModifierBoneEntry(0x12345678, 2.0f);

        // Assert
        entry1.Should().Be(entry2);
        entry1.Should().NotBe(entry3);
    }

    [Fact]
    public void SimModifierObjectData_Equality_WorksCorrectly()
    {
        // Arrange
        var data1 = new SimModifierObjectData(100, 200);
        var data2 = new SimModifierObjectData(100, 200);
        var data3 = new SimModifierObjectData(100, 300);

        // Assert
        data1.Should().Be(data2);
        data1.Should().NotBe(data3);
    }

    [Fact]
    public void Parse_ManyBoneEntries_ParsesCorrectly()
    {
        // Arrange
        var entries = Enumerable.Range(0, 100)
            .Select(i => new SimModifierBoneEntry((uint)i, i * 0.1f))
            .ToArray();
        var data = CreateValidData(boneEntries: entries);

        // Act
        var resource = new SimModifierResource(TestKey, data);

        // Assert
        resource.BoneEntries.Should().HaveCount(100);
        resource.BoneEntries[50].BoneHash.Should().Be(50);
        resource.BoneEntries[50].Multiplier.Should().BeApproximately(5.0f, 0.01f);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Parse_GenderValues_PreservesValue(uint gender)
    {
        // Arrange
        var data = CreateValidData(gender: gender);

        // Act
        var resource = new SimModifierResource(TestKey, data);

        // Assert
        resource.Gender.Should().Be(gender);
    }

    [Fact]
    public void Serialize_AllFieldTypes_RoundTripsCorrectly()
    {
        // Arrange - test with extreme values
        var publicKeys = new ResourceKey[]
        {
            new(uint.MaxValue, uint.MaxValue, ulong.MaxValue),
            new(0, 0, 0),
        };
        var objectData = new SimModifierObjectData[]
        {
            new(uint.MaxValue, uint.MaxValue),
            new(0, 0),
        };
        var boneEntries = new SimModifierBoneEntry[]
        {
            new(uint.MaxValue, float.MaxValue),
            new(0, float.MinValue),
            new(0x12345678, float.Epsilon),
        };

        var data = CreateValidData(
            contextVersion: uint.MaxValue,
            publicKeys: publicKeys,
            objectData: objectData,
            version: uint.MaxValue,
            gender: uint.MaxValue,
            region: uint.MaxValue,
            linkTag: uint.MaxValue,
            bonePoseKey: new ResourceKey(uint.MaxValue, uint.MaxValue, ulong.MaxValue),
            deformerMapShapeKey: new ResourceKey(0, 0, 0),
            deformerMapNormalKey: new ResourceKey(0x12345678, 0x9ABCDEF0, 0xFEDCBA9876543210),
            boneEntries: boneEntries);

        // Act
        var resource = new SimModifierResource(TestKey, data);
        var serialized = resource.Data.ToArray();
        var reloaded = new SimModifierResource(TestKey, serialized);

        // Assert
        reloaded.ContextData.Version.Should().Be(uint.MaxValue);
        reloaded.Version.Should().Be(uint.MaxValue);
        reloaded.Gender.Should().Be(uint.MaxValue);
        reloaded.Region.Should().Be(uint.MaxValue);
        reloaded.LinkTag.Should().Be(uint.MaxValue);
        reloaded.BonePoseKey.Should().Be(new ResourceKey(uint.MaxValue, uint.MaxValue, ulong.MaxValue));
        reloaded.BoneEntries.Should().HaveCount(3);
        reloaded.BoneEntries[0].Multiplier.Should().Be(float.MaxValue);
        reloaded.BoneEntries[1].Multiplier.Should().Be(float.MinValue);
    }

    [Fact]
    public void Factory_Create_ReturnsCorrectType()
    {
        // Arrange
        var factory = new SimModifierResourceFactory();
        var data = CreateValidData(version: 5);

        // Act
        var resource = factory.Create(TestKey, data);

        // Assert
        resource.Should().BeOfType<SimModifierResource>();
        ((SimModifierResource)resource).Version.Should().Be(5);
    }

    [Fact]
    public void Factory_CreateEmpty_ReturnsDefaultResource()
    {
        // Arrange
        var factory = new SimModifierResourceFactory();

        // Act
        var resource = factory.CreateEmpty(TestKey);

        // Assert
        resource.Should().BeOfType<SimModifierResource>();
        var smod = (SimModifierResource)resource;
        smod.Version.Should().Be(0);
        smod.BoneEntries.Should().BeEmpty();
    }

    [Fact]
    public void ContextData_ModifyLists_WorksCorrectly()
    {
        // Arrange
        var data = CreateValidData();
        var resource = new SimModifierResource(TestKey, data);

        // Act
        resource.ContextData.PublicKeys.Add(new ResourceKey(1, 2, 3));
        resource.ContextData.ExternalKeys.Add(new ResourceKey(4, 5, 6));
        resource.ContextData.DelayLoadKeys.Add(new ResourceKey(7, 8, 9));
        resource.ContextData.ObjectData.Add(new SimModifierObjectData(100, 200));

        // Assert
        resource.ContextData.PublicKeys.Should().HaveCount(1);
        resource.ContextData.ExternalKeys.Should().HaveCount(1);
        resource.ContextData.DelayLoadKeys.Should().HaveCount(1);
        resource.ContextData.ObjectData.Should().HaveCount(1);
    }

    [Fact]
    public void Serialize_AfterContextDataModification_PreservesChanges()
    {
        // Arrange
        var data = CreateValidData();
        var resource = new SimModifierResource(TestKey, data);
        var newKey = new ResourceKey(0x11111111, 0x22222222, 0x3333333344444444);
        resource.ContextData.PublicKeys.Add(newKey);

        // Act
        var serialized = resource.Data;
        var reloaded = new SimModifierResource(TestKey, serialized);

        // Assert
        reloaded.ContextData.PublicKeys.Should().HaveCount(1);
        reloaded.ContextData.PublicKeys[0].Should().Be(newKey);
    }
}
