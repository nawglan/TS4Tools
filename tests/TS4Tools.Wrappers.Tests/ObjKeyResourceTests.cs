using FluentAssertions;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

public class ObjKeyResourceTests
{
    private static readonly ResourceKey TestKey = new(0x02DC343F, 0, 0);

    [Fact]
    public void CreateEmpty_HasDefaultValues()
    {
        var resource = new ObjKeyResource(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Format.Should().Be(ObjKeyResource.DefaultFormat);
        resource.Unknown1.Should().Be(0);
        resource.Components.Should().BeEmpty();
        resource.ComponentData.Should().BeEmpty();
        resource.TgiBlocks.Should().BeEmpty();
    }

    [Fact]
    public void AddComponent_IncreasesCount()
    {
        var resource = new ObjKeyResource(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.AddComponent(ObjKeyComponent.Model);
        resource.AddComponent(ObjKeyComponent.Script);
        resource.AddComponent(ObjKeyComponent.Animation);

        resource.Components.Count.Should().Be(3);
        resource.IsDirty.Should().BeTrue();
        resource.Components[0].Should().Be(ObjKeyComponent.Model);
        resource.Components[1].Should().Be(ObjKeyComponent.Script);
        resource.Components[2].Should().Be(ObjKeyComponent.Animation);
    }

    [Fact]
    public void HasComponent_ReturnsCorrectValue()
    {
        var resource = new ObjKeyResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.AddComponent(ObjKeyComponent.Model);
        resource.AddComponent(ObjKeyComponent.Script);

        resource.HasComponent(ObjKeyComponent.Model).Should().BeTrue();
        resource.HasComponent(ObjKeyComponent.Script).Should().BeTrue();
        resource.HasComponent(ObjKeyComponent.Animation).Should().BeFalse();
    }

    [Fact]
    public void AddComponentData_String_Works()
    {
        var resource = new ObjKeyResource(TestKey, ReadOnlyMemory<byte>.Empty);
        var data = new ComponentDataString("scriptClass", "MyScript");

        resource.AddComponentData(data);

        resource.ComponentData.Count.Should().Be(1);
        resource.ComponentData[0].Should().Be(data);
    }

    [Fact]
    public void AddComponentData_ResourceKey_Works()
    {
        var resource = new ObjKeyResource(TestKey, ReadOnlyMemory<byte>.Empty);
        var data = new ComponentDataResourceKey("modelKey", 0);

        resource.AddComponentData(data);

        resource.ComponentData.Count.Should().Be(1);
        var result = resource.ComponentData[0] as ComponentDataResourceKey;
        result.Should().NotBeNull();
        result!.Key.Should().Be("modelKey");
        result.TgiIndex.Should().Be(0);
    }

    [Fact]
    public void GetComponentData_ReturnsCorrectData()
    {
        var resource = new ObjKeyResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.AddComponentData(new ComponentDataString("key1", "value1"));
        resource.AddComponentData(new ComponentDataUInt32("key2", 42));
        resource.AddComponentData(new ComponentDataResourceKey("key3", 0));

        resource.GetComponentData("key1").Should().BeOfType<ComponentDataString>();
        resource.GetComponentData("key2").Should().BeOfType<ComponentDataUInt32>();
        resource.GetComponentData("key3").Should().BeOfType<ComponentDataResourceKey>();
        resource.GetComponentData("nonexistent").Should().BeNull();
    }

    [Fact]
    public void GetReferencedTgiBlock_ReturnsCorrectBlock()
    {
        var resource = new ObjKeyResource(TestKey, ReadOnlyMemory<byte>.Empty);
        var tgi = new ResourceKey(0x1234, 0x5678, 0x9ABC);
        resource.AddTgiBlock(tgi);

        var resKeyData = new ComponentDataResourceKey("modelKey", 0);
        resource.AddComponentData(resKeyData);

        resource.GetReferencedTgiBlock(resKeyData).Should().Be(tgi);
    }

    [Fact]
    public void GetReferencedTgiBlock_InvalidIndex_ReturnsNull()
    {
        var resource = new ObjKeyResource(TestKey, ReadOnlyMemory<byte>.Empty);
        var resKeyData = new ComponentDataResourceKey("modelKey", 99);
        resource.AddComponentData(resKeyData);

        resource.GetReferencedTgiBlock(resKeyData).Should().BeNull();
    }

    [Fact]
    public void ClearComponents_RemovesAll()
    {
        var resource = new ObjKeyResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.AddComponent(ObjKeyComponent.Model);
        resource.AddComponent(ObjKeyComponent.Script);

        resource.ClearComponents();

        resource.Components.Should().BeEmpty();
    }

    [Fact]
    public void RoundTrip_PreservesData()
    {
        var original = new ObjKeyResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Format = 7;
        original.Unknown1 = 42;
        original.AddComponent(ObjKeyComponent.Model);
        original.AddComponent(ObjKeyComponent.Script);
        original.AddTgiBlock(new ResourceKey(0x1234, 0x5678, 0x9ABCDEF0));
        original.AddComponentData(new ComponentDataString("scriptClass", "TestScript"));
        original.AddComponentData(new ComponentDataResourceKey("modelKey", 0));
        original.AddComponentData(new ComponentDataUInt32("someValue", 12345));

        // Serialize
        var serialized = original.Data;

        // Parse
        var parsed = new ObjKeyResource(TestKey, serialized);

        parsed.Format.Should().Be(original.Format);
        parsed.Unknown1.Should().Be(original.Unknown1);
        parsed.Components.Count.Should().Be(original.Components.Count);
        parsed.ComponentData.Count.Should().Be(original.ComponentData.Count);
        parsed.TgiBlocks.Count.Should().Be(original.TgiBlocks.Count);

        for (int i = 0; i < original.Components.Count; i++)
        {
            parsed.Components[i].Should().Be(original.Components[i]);
        }

        for (int i = 0; i < original.TgiBlocks.Count; i++)
        {
            parsed.TgiBlocks[i].Should().Be(original.TgiBlocks[i]);
        }

        // Check component data
        var parsedStr = parsed.GetComponentData("scriptClass") as ComponentDataString;
        parsedStr.Should().NotBeNull();
        parsedStr!.Data.Should().Be("TestScript");

        var parsedResKey = parsed.GetComponentData("modelKey") as ComponentDataResourceKey;
        parsedResKey.Should().NotBeNull();
        parsedResKey!.TgiIndex.Should().Be(0);

        var parsedUInt = parsed.GetComponentData("someValue") as ComponentDataUInt32;
        parsedUInt.Should().NotBeNull();
        parsedUInt!.Data.Should().Be(12345u);
    }

    [Fact]
    public void RoundTrip_AllComponentDataTypes()
    {
        var original = new ObjKeyResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.AddComponentData(new ComponentDataString("string", "hello"));
        original.AddComponentData(new ComponentDataSteeringInstance("steering", "steerValue"));
        original.AddComponentData(new ComponentDataResourceKey("resKey", 5));
        original.AddComponentData(new ComponentDataAssetResourceName("asset", 10));
        original.AddComponentData(new ComponentDataUInt32("uint", 0xDEADBEEF));

        var serialized = original.Data;
        var parsed = new ObjKeyResource(TestKey, serialized);

        parsed.ComponentData.Count.Should().Be(5);

        var str = parsed.GetComponentData("string") as ComponentDataString;
        str!.Data.Should().Be("hello");

        var steer = parsed.GetComponentData("steering") as ComponentDataSteeringInstance;
        steer!.Data.Should().Be("steerValue");

        var resKey = parsed.GetComponentData("resKey") as ComponentDataResourceKey;
        resKey!.TgiIndex.Should().Be(5);

        var asset = parsed.GetComponentData("asset") as ComponentDataAssetResourceName;
        asset!.TgiIndex.Should().Be(10);

        var uint32 = parsed.GetComponentData("uint") as ComponentDataUInt32;
        uint32!.Data.Should().Be(0xDEADBEEF);
    }

    [Fact]
    public void Parse_TooShortForHeader_ThrowsException()
    {
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }; // Only 5 bytes

        var act = () => new ObjKeyResource(TestKey, data);

        act.Should().Throw<ResourceFormatException>().WithMessage("*too short*");
    }

    [Fact]
    public void ObjKeyComponent_HasCorrectValues()
    {
        // Verify enum values match legacy
        ((uint)ObjKeyComponent.Animation).Should().Be(0xee17c6ad);
        ((uint)ObjKeyComponent.Model).Should().Be(0x2954e734);
        ((uint)ObjKeyComponent.Script).Should().Be(0x23177498);
        ((uint)ObjKeyComponent.Sim).Should().Be(0x22706efa);
        ((uint)ObjKeyComponent.Footprint).Should().Be(0xc807312a);
        ((uint)ObjKeyComponent.Tree).Should().Be(0xc602cd31);
    }

    [Fact]
    public void ComponentDataRecords_EqualityWorks()
    {
        var str1 = new ComponentDataString("key", "value");
        var str2 = new ComponentDataString("key", "value");
        var str3 = new ComponentDataString("key", "different");

        str1.Should().Be(str2);
        str1.Should().NotBe(str3);

        var resKey1 = new ComponentDataResourceKey("key", 5);
        var resKey2 = new ComponentDataResourceKey("key", 5);
        var resKey3 = new ComponentDataResourceKey("key", 10);

        resKey1.Should().Be(resKey2);
        resKey1.Should().NotBe(resKey3);
    }
}
