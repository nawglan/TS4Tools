// Source: legacy_references/Sims4Tools/s4pi/Interfaces/TGIBlock.cs lines 44-230 (orders, Parse/UnParse),
// legacy_references/Sims4Tools/s4pi/Interfaces/AResourceKey.cs lines 160-231 (CompareTo, TryParse, string form),
// legacy_references/Sims4Tools/s4pi/Interfaces/DependentList.cs lines 340-730 (CountedTGIBlockList, TGIBlockList)

using s4pi.Interfaces;

namespace CompatTests;

public class TGIBlockTests
{
    private const uint T = 0x11223344;
    private const uint G = 0x55667788;
    private const ulong I = 0x99AABBCCDDEEFF00;

    private static byte[] Bytes(params object[] fields)
    {
        var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        foreach (var f in fields)
        {
            switch (f)
            {
                case uint u: w.Write(u); break;
                case ulong l: w.Write(l); break;
                case int i: w.Write(i); break;
            }
        }
        w.Flush();
        return ms.ToArray();
    }

    public static TheoryData<string, byte[]> Orders => new()
    {
        { "TGI", Bytes(T, G, I) },
        { "TIG", Bytes(T, I, G) },
        { "GTI", Bytes(G, T, I) },
        { "GIT", Bytes(G, I, T) },
        { "ITG", Bytes(I, T, G) },
        { "IGT", Bytes(I, G, T) },
    };

    [Theory]
    [MemberData(nameof(Orders))]
    public void Parse_ReadsFieldsInOrder(string order, byte[] data)
    {
        var tgi = new TGIBlock(0, null, order, new MemoryStream(data));

        tgi.ResourceType.Should().Be(T);
        tgi.ResourceGroup.Should().Be(G);
        tgi.Instance.Should().Be(I);
    }

    [Theory]
    [MemberData(nameof(Orders))]
    public void UnParse_WritesFieldsInOrder(string order, byte[] expected)
    {
        var tgi = new TGIBlock(0, null, order, T, G, I);
        var ms = new MemoryStream();

        tgi.UnParse(ms);

        ms.ToArray().Should().Equal(expected);
    }

    [Fact]
    public void EnumOrder_MatchesStringOrder()
    {
        var ms = new MemoryStream();
        new TGIBlock(0, null, TGIBlock.Order.ITG, T, G, I).UnParse(ms);
        ms.ToArray().Should().Equal(Bytes(I, T, G));
    }

    [Fact]
    public void DefaultOrder_IsTGI()
    {
        var ms = new MemoryStream();
        new TGIBlock(0, null, T, G, I).UnParse(ms);
        ms.ToArray().Should().Equal(Bytes(T, G, I));
    }

    [Fact]
    public void InvalidOrder_Throws()
    {
        var act = () => new TGIBlock(0, null, "XYZ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CopyConstructor_KeepsOrder()
    {
        var original = new TGIBlock(0, null, "ITG", T, G, I);
        var copy = new TGIBlock(0, null, original);
        var ms = new MemoryStream();

        copy.UnParse(ms);

        ms.ToArray().Should().Equal(Bytes(I, T, G));
        copy.Should().Be(original);
        copy.Should().NotBeSameAs(original);
    }

    [Fact]
    public void ToString_UsesLegacyDashFormat()
    {
        new TGIBlock(0, null, 0x1, 0x2, 0x3).ToString().Should().Be("0x00000001-0x00000002-0x0000000000000003");
        ((string)new TGIBlock(0, null, T, G, I)).Should().Be("0x11223344-0x55667788-0x99AABBCCDDEEFF00");
    }

    [Theory]
    [InlineData("0x11223344-0x55667788-0x99AABBCCDDEEFF00")]
    [InlineData("0X11223344-0X55667788-0X99aabbccddeeff00")]
    [InlineData("  0x11223344-0x55667788-0x99AABBCCDDEEFF00  ")]
    [InlineData("11223344:55667788:99AABBCCDDEEFF00")]
    [InlineData("key:11223344:55667788:99AABBCCDDEEFF00")]
    [InlineData("KEY:11223344:55667788:99aabbccddeeff00")]
    public void TryParse_AcceptsLegacyFormats(string text)
    {
        var tgi = new TGIBlock(0, null);

        AResourceKey.TryParse(text, tgi).Should().BeTrue();

        tgi.ResourceType.Should().Be(T);
        tgi.ResourceGroup.Should().Be(G);
        tgi.Instance.Should().Be(I);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0x1-0x2")]
    [InlineData("0x1-2-0x3")]
    [InlineData("1:2")]
    [InlineData("tgi:1:2:3")]
    [InlineData("1:2:zz")]
    [InlineData("0x1-0x2-0x3-0x4")]
    [InlineData("T:0x00000001 G:0x00000002 I:0x0000000000000003")] // TS4Tools' own format is not a legacy format
    public void TryParse_RejectsOtherFormats_AndLeavesResultUnchanged(string? text)
    {
        var tgi = new TGIBlock(0, null, 7, 8, 9);

        AResourceKey.TryParse(text, tgi).Should().BeFalse();

        tgi.ResourceType.Should().Be(7);
        tgi.ResourceGroup.Should().Be(8);
        tgi.Instance.Should().Be(9);
    }

    [Fact]
    public void CompareTo_OrdersByTypeGroupInstance()
    {
        var a = new TGIBlock(0, null, 1, 9, 9);
        var b = new TGIBlock(0, null, 2, 0, 0);
        var c = new TGIBlock(0, null, 2, 0, 1);

        a.CompareTo(b).Should().BeNegative();
        c.CompareTo(b).Should().BePositive();
        b.CompareTo(new TGIBlock(0, null, "ITG", 2, 0, 0)).Should().Be(0);
        b.Equals((IResourceKey)new TGIBlock(0, null, "ITG", 2, 0, 0)).Should().BeTrue("equality ignores the storage order");
    }

    [Fact]
    public void SettingAField_RaisesHandlerOnlyOnChange()
    {
        int raised = 0;
        var tgi = new TGIBlock(0, (_, _) => raised++, 1, 2, 3);

        tgi.Instance = 3;
        raised.Should().Be(0);
        tgi.Instance = 4;
        tgi.ResourceGroup = 5;
        raised.Should().Be(2);
    }

    [Fact]
    public void CountedTGIBlockList_ReadsCountGivenSeparately_InItsOrder()
    {
        var data = Bytes(I, T, G, 5UL, 1u, 2u, 0xDEADBEEFu);
        var s = new MemoryStream(data);

        var list = new CountedTGIBlockList(null, "ITG", 2, s);

        list.Should().HaveCount(2);
        list[0].Instance.Should().Be(I);
        list[1].ResourceType.Should().Be(1);
        s.Position.Should().Be(32, "the trailing word is not part of the list");

        var ms = new MemoryStream();
        list.UnParse(ms);
        ms.ToArray().Should().Equal(data[..32], "the count is not written");
    }

    [Fact]
    public void CountedTGIBlockList_Add_CopiesIntoListOrder()
    {
        var list = new CountedTGIBlockList(null, TGIBlock.Order.IGT);
        var source = new TGIBlock(0, null, "TGI", T, G, I);

        list.Add(source);

        list[0].Should().NotBeSameAs(source);
        var ms = new MemoryStream();
        list.UnParse(ms);
        ms.ToArray().Should().Equal(Bytes(I, G, T));
    }

    [Fact]
    public void TGIBlockList_UnParse_BackPatchesOffsetAndSize_AndParseValidates()
    {
        var list = new TGIBlockList(null) { new TGIBlock(0, null, T, G, I), new TGIBlock(0, null, 1, 2, 3) };
        var ms = new MemoryStream();
        using (var w = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
        {
            w.Write(0u); // offset placeholder (ptgiO = 0)
            w.Write(0u); // size placeholder
            w.Write(0xCAFEu); // some other data before the list
        }

        list.UnParse(ms, 0);

        var bytes = ms.ToArray();
        uint offset = BinaryPrimitives.ReadUInt32LittleEndian(bytes);
        uint size = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4));
        offset.Should().Be(12 - 0 - 4, "the offset is relative to the end of the offset field");
        size.Should().Be(4 + 2 * 16u, "count plus two 16-byte blocks");

        var read = new MemoryStream(bytes) { Position = 12 };
        var parsed = new TGIBlockList(null, read, 12, size);
        parsed.Should().Equal(list);

        var wrongSize = () => new TGIBlockList(null, new MemoryStream(bytes) { Position = 12 }, 12, size + 1);
        wrongSize.Should().Throw<InvalidDataException>();
        var wrongPosition = () => new TGIBlockList(null, new MemoryStream(bytes) { Position = 12 }, 8, size);
        wrongPosition.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Clone_CopiesValuesAndOrder_WithNewHandler()
    {
        int raised = 0;
        var original = new TGIBlock(0, null, "GIT", T, G, I);

        var clone = (TGIBlock)original.Clone((_, _) => raised++);
        clone.Instance = 1;

        raised.Should().Be(1);
        original.Instance.Should().Be(I);
        var ms = new MemoryStream();
        new TGIBlock(0, null, clone) { Instance = I }.UnParse(ms);
        ms.ToArray().Should().Equal(Bytes(G, I, T));
    }
}
