using System.Buffers.Binary;
using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.Wrappers.CatalogResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CatalogResource;

/// <summary>
/// Tests for <see cref="ObjectCatalogResource"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/ObjectCatalogResource.cs
/// - ObjectCatalogResource is a base class for several catalog types (RoofStyle, A8F7B517, 48C28979)
/// - Uses a different format from SimpleCatalogResource:
///   - Version + CatalogVersion + NameHash + DescHash + Price + Unknown1-3
///   - TGI list with byte count prefix (ITG order)
///   - Unknown4 + TagList (int32 count + ushort values) + SellingPointList + Unknown5-7
/// </summary>
public class ObjectCatalogResourceTests
{
    private static readonly ResourceKey TestKey = new(0x00000000, 0, 0);

    private sealed class TestObjectCatalogResource : ObjectCatalogResource
    {
        public TestObjectCatalogResource(ResourceKey key, ReadOnlyMemory<byte> data)
            : base(key, data)
        {
        }
    }

    [Fact]
    public void CreateEmpty_HasDefaultValues()
    {
        var resource = new TestObjectCatalogResource(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Version.Should().Be(ObjectCatalogResource.DefaultVersion);
        resource.CatalogVersion.Should().Be(ObjectCatalogResource.DefaultCatalogVersion);
        resource.CatalogNameHash.Should().Be(0);
        resource.CatalogDescHash.Should().Be(0);
        resource.CatalogPrice.Should().Be(0);
        resource.CatalogUnknown1.Should().Be(0);
        resource.CatalogUnknown2.Should().Be(0);
        resource.CatalogUnknown3.Should().Be(0);
        resource.CatalogStyleTgiList.Count.Should().Be(0);
        resource.CatalogUnknown4.Should().Be(0);
        resource.CatalogTagList.Should().BeEmpty();
        resource.CatalogSellingPointList.Count.Should().Be(0);
        resource.CatalogUnknown5.Should().Be(0);
        resource.CatalogUnknown6.Should().Be(0);
        resource.CatalogUnknown7.Should().Be(0);
    }

    [Fact]
    public void RoundTrip_BasicFields_PreservesData()
    {
        var original = new TestObjectCatalogResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Version = 2;
        original.CatalogVersion = 10;
        original.CatalogNameHash = 0xDEADBEEF;
        original.CatalogDescHash = 0xCAFEBABE;
        original.CatalogPrice = 500;
        original.CatalogUnknown1 = 0x11111111;
        original.CatalogUnknown2 = 0x22222222;
        original.CatalogUnknown3 = 0x33333333;

        var data = original.Data.ToArray();
        var parsed = new TestObjectCatalogResource(TestKey, data);

        parsed.Version.Should().Be(2);
        parsed.CatalogVersion.Should().Be(10);
        parsed.CatalogNameHash.Should().Be(0xDEADBEEF);
        parsed.CatalogDescHash.Should().Be(0xCAFEBABE);
        parsed.CatalogPrice.Should().Be(500);
        parsed.CatalogUnknown1.Should().Be(0x11111111);
        parsed.CatalogUnknown2.Should().Be(0x22222222);
        parsed.CatalogUnknown3.Should().Be(0x33333333);
    }

    [Fact]
    public void RoundTrip_TgiList_PreservesReferences()
    {
        var original = new TestObjectCatalogResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.CatalogStyleTgiList.Add(new TgiReference(0x1111111111111111, 0xAAAAAAAA, 0xBBBBBBBB));
        original.CatalogStyleTgiList.Add(new TgiReference(0x2222222222222222, 0xCCCCCCCC, 0xDDDDDDDD));

        var data = original.Data.ToArray();
        var parsed = new TestObjectCatalogResource(TestKey, data);

        parsed.CatalogStyleTgiList.Count.Should().Be(2);
        parsed.CatalogStyleTgiList[0].Instance.Should().Be(0x1111111111111111);
        parsed.CatalogStyleTgiList[0].Type.Should().Be(0xAAAAAAAA);
        parsed.CatalogStyleTgiList[0].Group.Should().Be(0xBBBBBBBB);
        parsed.CatalogStyleTgiList[1].Instance.Should().Be(0x2222222222222222);
        parsed.CatalogStyleTgiList[1].Type.Should().Be(0xCCCCCCCC);
        parsed.CatalogStyleTgiList[1].Group.Should().Be(0xDDDDDDDD);
    }

    [Fact]
    public void RoundTrip_TagList_PreservesTags()
    {
        var original = new TestObjectCatalogResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.CatalogTagList.Add(0x0001);
        original.CatalogTagList.Add(0x0042);
        original.CatalogTagList.Add(0xFFFF);

        var data = original.Data.ToArray();
        var parsed = new TestObjectCatalogResource(TestKey, data);

        parsed.CatalogTagList.Should().HaveCount(3);
        parsed.CatalogTagList[0].Should().Be(0x0001);
        parsed.CatalogTagList[1].Should().Be(0x0042);
        parsed.CatalogTagList[2].Should().Be(0xFFFF);
    }

    [Fact]
    public void RoundTrip_SellingPoints_PreservesData()
    {
        var original = new TestObjectCatalogResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.CatalogSellingPointList.Add(new ObjectCatalogSellingPoint(100, 500));
        original.CatalogSellingPointList.Add(new ObjectCatalogSellingPoint(200, 1000));

        var data = original.Data.ToArray();
        var parsed = new TestObjectCatalogResource(TestKey, data);

        parsed.CatalogSellingPointList.Count.Should().Be(2);
        parsed.CatalogSellingPointList.Points[0].Commodity.Should().Be(100);
        parsed.CatalogSellingPointList.Points[0].Amount.Should().Be(500);
        parsed.CatalogSellingPointList.Points[1].Commodity.Should().Be(200);
        parsed.CatalogSellingPointList.Points[1].Amount.Should().Be(1000);
    }

    [Fact]
    public void RoundTrip_TrailingUnknowns_PreservesData()
    {
        var original = new TestObjectCatalogResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.CatalogUnknown4 = 0xABCD;
        original.CatalogUnknown5 = 0x1234567890ABCDEF;
        original.CatalogUnknown6 = 0xFEDC;
        original.CatalogUnknown7 = 0xFEDCBA0987654321;

        var data = original.Data.ToArray();
        var parsed = new TestObjectCatalogResource(TestKey, data);

        parsed.CatalogUnknown4.Should().Be(0xABCD);
        parsed.CatalogUnknown5.Should().Be(0x1234567890ABCDEF);
        parsed.CatalogUnknown6.Should().Be(0xFEDC);
        parsed.CatalogUnknown7.Should().Be(0xFEDCBA0987654321);
    }

    [Fact]
    public void RoundTrip_FullResource_ByteForByte()
    {
        var original = new TestObjectCatalogResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Set all fields
        original.Version = 5;
        original.CatalogVersion = 9;
        original.CatalogNameHash = 0x12345678;
        original.CatalogDescHash = 0x87654321;
        original.CatalogPrice = 1500;
        original.CatalogUnknown1 = 0xAAAAAAAA;
        original.CatalogUnknown2 = 0xBBBBBBBB;
        original.CatalogUnknown3 = 0xCCCCCCCC;

        original.CatalogStyleTgiList.Add(new TgiReference(0x1111111111111111, 0x11111111, 0x11111111));
        original.CatalogStyleTgiList.Add(new TgiReference(0x2222222222222222, 0x22222222, 0x22222222));
        original.CatalogStyleTgiList.Add(new TgiReference(0x3333333333333333, 0x33333333, 0x33333333));

        original.CatalogUnknown4 = 0x4444;
        original.CatalogTagList.Add(0x0001);
        original.CatalogTagList.Add(0x0002);

        original.CatalogSellingPointList.Add(new ObjectCatalogSellingPoint(10, 100));

        original.CatalogUnknown5 = 0x5555555555555555;
        original.CatalogUnknown6 = 0x6666;
        original.CatalogUnknown7 = 0x7777777777777777;

        // Serialize twice
        var data1 = original.Data.ToArray();
        var parsed = new TestObjectCatalogResource(TestKey, data1);
        var data2 = parsed.Data.ToArray();

        data2.Should().BeEquivalentTo(data1, "round-trip should produce identical bytes");
    }

    [Fact]
    public void ObjectCatalogSellingPoint_Parse_ReadsCorrectly()
    {
        // Build test data: ushort (2 bytes) + uint (4 bytes) = 6 bytes
        var data = new byte[6];
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(0), 0x1234);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(2), 0xDEADBEEF);

        var point = ObjectCatalogSellingPoint.Parse(data);

        point.Commodity.Should().Be(0x1234);
        point.Amount.Should().Be(0xDEADBEEF);
    }

    [Fact]
    public void ObjectCatalogSellingPoint_WriteTo_WritesCorrectly()
    {
        var point = new ObjectCatalogSellingPoint(0xABCD, 0x12345678);
        var buffer = new byte[6];

        point.WriteTo(buffer);

        BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(0)).Should().Be(0xABCD);
        BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(2)).Should().Be(0x12345678);
    }

    [Fact]
    public void ObjectCatalogSellingPointList_Parse_ReadsCorrectly()
    {
        // Build test data: int32 count + entries
        var data = new byte[4 + 12]; // count + 2 entries
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(0), 2);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(4), 100);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(6), 500);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(10), 200);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(12), 1000);

        var list = ObjectCatalogSellingPointList.Parse(data, out int bytesRead);

        bytesRead.Should().Be(16);
        list.Count.Should().Be(2);
        list.Points[0].Commodity.Should().Be(100);
        list.Points[0].Amount.Should().Be(500);
        list.Points[1].Commodity.Should().Be(200);
        list.Points[1].Amount.Should().Be(1000);
    }

    [Fact]
    public void Factory_Create_ReturnsCorrectType()
    {
        var factory = new ObjectCatalogResourceFactory();
        var resource = factory.Create(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Should().BeAssignableTo<ObjectCatalogResource>();
    }

    [Fact]
    public void Factory_CreateEmpty_ReturnsResourceWithDefaults()
    {
        var factory = new ObjectCatalogResourceFactory();
        var resource = factory.CreateEmpty(TestKey) as ObjectCatalogResource;

        resource.Should().NotBeNull();
        resource!.Version.Should().Be(ObjectCatalogResource.DefaultVersion);
        resource.CatalogVersion.Should().Be(ObjectCatalogResource.DefaultCatalogVersion);
    }
}
