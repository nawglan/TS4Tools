// Source: legacy_references/Sims4Tools/s4pi/Interfaces/AApiVersionedFields.cs lines 69-209 (indexer, GetContentFields),
// lines 290-436 (ValueBuilder), lines 528-561 (Comparer); legacy_references/Sims4Tools/s4pi/Interfaces/TypedValue.cs lines 95-280

using s4pi.Interfaces;

namespace CompatTests;

public class ContentFieldsTests
{
    public enum Colour : ushort { Red = 1, Green = 2 }

    /// <summary>An element with versioned and prioritised fields.</summary>
    public sealed class Versioned : AHandlerElement, IEquatable<Versioned>
    {
        private const int recommendedApiVersion = 2;

        public Versioned(int apiVersion, EventHandler? handler) : base(apiVersion, handler) { }

        public override int RecommendedApiVersion => recommendedApiVersion;
        public override List<string> ContentFields => GetContentFields(requestedApiVersion, GetType());

        [ElementPriority(2)] public int Second { get; set; } = 2;
        [ElementPriority(1)] public int First { get; set; } = 1;
        public int Zebra { get; set; }
        public int Alpha { get; set; }
        [MinimumVersion(3)] public int FromV3 { get; set; }
        [MaximumVersion(1)] public int UpToV1 { get; set; }

        public bool Equals(Versioned? other) => ReferenceEquals(this, other);
        public override bool Equals(object? obj) => Equals(obj as Versioned);
        public override int GetHashCode() => 0;
    }

    [Fact]
    public void GetContentFields_SortsByPriorityThenOrdinalName_AndExcludesApiMembers()
    {
        var fields = AApiVersionedFields.GetContentFields(0, typeof(TGIBlock));

        fields.Should().Equal("ResourceType", "ResourceGroup", "Instance", "Value");
    }

    [Fact]
    public void GetContentFields_FiltersByVersion_UsingRecommendedVersionForZero()
    {
        // API 0 => recommended (2): FromV3 (min 3) and UpToV1 (max 1) are excluded
        AApiVersionedFields.GetContentFields(0, typeof(Versioned)).Should().Equal("First", "Second", "Alpha", "Zebra");
        AApiVersionedFields.GetContentFields(1, typeof(Versioned)).Should().Equal("First", "Second", "Alpha", "UpToV1", "Zebra");
        AApiVersionedFields.GetContentFields(3, typeof(Versioned)).Should().Equal("First", "Second", "Alpha", "FromV3", "Zebra");
    }

    [Fact]
    public void Indexer_ReturnsTypedValue_FormattedAsHex()
    {
        var tgi = new TGIBlock(0, null, 0x220557DA, 0x80000000, 0x0123456789ABCDEF);

        var tv = tgi["ResourceType"];
        tv.Type.Should().Be<uint>();
        tv.Value.Should().Be(0x220557DAu);
        ((string)tv).Should().Be("0x220557DA");
        tgi["Instance"].ToString().Should().Be("0x0123456789ABCDEF");
        tgi["Value"].ToString().Should().Be("0x220557DA-0x80000000-0x0123456789ABCDEF");
    }

    [Fact]
    public void Indexer_Set_AssignsProperty()
    {
        var tgi = new TGIBlock(0, null);

        tgi["Instance"] = new TypedValue(typeof(ulong), 42UL);

        tgi.Instance.Should().Be(42);
    }

    [Fact]
    public void Indexer_FollowsDottedPaths()
    {
        var box = new BoundingBox(0, null, new Vertex(0, null, 1, 2, 3), new Vertex(0, null, 4, 5, 6));

        box["Max.Y"].Value.Should().Be(5f);
        box["Min.X"] = new TypedValue(typeof(float), 9f);
        box.Min.X.Should().Be(9f);
    }

    [Fact]
    public void Indexer_UnknownField_Throws()
    {
        var tgi = new TGIBlock(0, null);
        var act = () => tgi["NoSuchField"];
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(typeof(int), -1, "0xFFFFFFFF")]
    [InlineData(typeof(uint), 10u, "0x0000000A")]
    [InlineData(typeof(short), (short)-2, "0xFFFE")]
    [InlineData(typeof(ushort), (ushort)0xAB, "0x00AB")]
    [InlineData(typeof(byte), (byte)10, "0x0A")]
    [InlineData(typeof(sbyte), (sbyte)-1, "0xFF")]
    [InlineData(typeof(long), 1L, "0x0000000000000001")]
    [InlineData(typeof(ulong), ulong.MaxValue, "0xFFFFFFFFFFFFFFFF")]
    [InlineData(typeof(bool), true, "True")]
    public void TypedValue_HexFormat(Type type, object value, string expected)
    {
        new TypedValue(type, value, "X").ToString().Should().Be(expected);
    }

    [Fact]
    public void TypedValue_OtherFormats()
    {
        new TypedValue(typeof(int), 10).ToString().Should().Be("10", "without \"X\" integers are decimal");
        new TypedValue(typeof(Colour), Colour.Green, "X").ToString().Should().Be("0x0002 (Green)");
        new TypedValue(typeof(string), "a\0bé", "X").ToString().Should().Be("a<NUL>b<U+00E9>");
        new TypedValue(typeof(string), "plain", "X").ToString().Should().Be("plain");
        new TypedValue(typeof(char[]), "chars".ToCharArray(), "X").ToString().Should().Be("chars");
        new TypedValue(typeof(byte[]), new byte[] { 1, 0xFF }, "X").ToString().Should().Be("[0:'0x01'] [1:'0xFF']");
    }

    [Fact]
    public void TypedValue_FloatsUseFourDecimals()
    {
        var saved = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            new TypedValue(typeof(float), 1.5f, "X").ToString().Should().Be("1.5000");
            new TypedValue(typeof(double), 2.0, "X").ToString().Should().Be("2.0000");
        }
        finally
        {
            CultureInfo.CurrentCulture = saved;
        }
    }

    [Fact]
    public void TypedValue_EqualityAndComparison_UseTheValue()
    {
        var a = new TypedValue(typeof(uint), 5u, "X");
        var b = new TypedValue(typeof(uint), 5u, "");
        var c = new TypedValue(typeof(uint), 7u);

        a.Equals(b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
        a.CompareTo(c).Should().BeNegative();
        var incomparable = () => a.CompareTo(new TypedValue(typeof(string), "x"));
        incomparable.Should().Throw<NotImplementedException>();
    }

    [Fact]
    public void ValueBuilder_DumpsFieldsOnePerLine()
    {
        var tgi = new TGIBlock(0, null, 1, 2, 3);

        tgi.ToString().Should().Be("0x00000001-0x00000002-0x0000000000000003", "AResourceKey overrides ToString");

        var box = new BoundingBox(0, null, new Vertex(0, null, 1, 2, 3), new Vertex(0, null, 4, 5, 6));
        var saved = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            box.ToString().Should().Be(
                "Min: { 1.0000; 2.0000; 3.0000 }\nMax: { 4.0000; 5.0000; 6.0000 }",
                "element fields with a string Value are shown by that Value");
            box.Value.Should().Be("[ Min: { 1.0000; 2.0000; 3.0000 } | Max: { 4.0000; 5.0000; 6.0000 } ]");
        }
        finally
        {
            CultureInfo.CurrentCulture = saved;
        }
    }

    [Fact]
    public void ValueBuilder_ListsSimpleListsAndTGIBlockLists()
    {
        var holder = new ListHolder();

        holder.ToString().Should().Be(
            "--- UIntList: Numbers (0x2) ---\n   [0]: 0x00000001\n   [1]: 0x0000000A\n---" +
            "\n--- TGIBlockList: Tgis (0x1) ---\n   [0]: 0x00000001-0x00000002-0x0000000000000003\n---" +
            "\nPick: 0x00000000 (0x00000001-0x00000002-0x0000000000000003)",
            "the leading newline is trimmed and an index field shows the TGIBlock it refers to");
    }

    public sealed class ListHolder : AHandlerElement, IEquatable<ListHolder>
    {
        public ListHolder() : base(0, null)
        {
            Numbers = new UIntList(null, [1u, 10u]);
            Tgis = new TGIBlockList(null) { new TGIBlock(0, null, 1, 2, 3) };
        }

        public override int RecommendedApiVersion => 1;
        public override List<string> ContentFields => GetContentFields(requestedApiVersion, GetType());

        [ElementPriority(1)] public UIntList Numbers { get; }
        [ElementPriority(2)] public TGIBlockList Tgis { get; }
        [ElementPriority(3), TGIBlockListContentField("Tgis")] public int Pick { get; set; }

        public bool Equals(ListHolder? other) => ReferenceEquals(this, other);
        public override bool Equals(object? obj) => Equals(obj as ListHolder);
        public override int GetHashCode() => 0;
    }

    [Fact]
    public void TGIBlockListIndex_Value_ShowsReferencedBlock()
    {
        var tgis = new TGIBlockList(null) { new TGIBlock(0, null, 1, 2, 3), new TGIBlock(0, null, 4, 5, 6) };
        var index = new TGIBlockListIndex<int>(0, null, 1, tgis);

        index.ContentFields.Should().Equal("Data", "Value");
        index.Value.Should().Be("0x00000001 (0x00000004-0x00000005-0x0000000000000006)");
        new TGIBlockListIndex<int>(0, null, 7, tgis).Value.Should().Be("0x00000007", "out of range indices have no suffix");
    }

    [Fact]
    public void Comparer_SortsByContentField()
    {
        var list = new List<TGIBlock> { new(0, null, 3, 0, 0), new(0, null, 1, 0, 0), new(0, null, 2, 0, 0) };

        list.Sort(new AApiVersionedFields.Comparer<TGIBlock>("ResourceType"));

        list.Select(x => x.ResourceType).Should().Equal(1u, 2u, 3u);
    }

    [Fact]
    public void CompareByPriority_UsesElementPriority()
    {
        var v = new Versioned(0, null);
        v.CompareByPriority("First", "Second").Should().BeNegative();
        v.CompareByPriority("Alpha", "First").Should().BePositive("unprioritised fields sort after prioritised ones");
    }

    [Fact]
    public void HandlerElement_ValAndCloneAndEquality()
    {
        int raised = 0;
        var e = new HandlerElement<uint>(0, (_, _) => raised++, 5);

        e.Value.Should().Be("0x00000005");
        e.ContentFields.Should().Equal("Val", "Value");
        e.Equals((object)5u).Should().BeTrue();
        e.Val = 5;
        raised.Should().Be(0);
        e.Val = 6;
        raised.Should().Be(1);

        var clone = (HandlerElement<uint>)e.Clone(null);
        clone.Should().Be(e);
        clone.Val = 7;
        raised.Should().Be(1, "the clone has its own handler");
    }

    [Fact]
    public void Clone_UsesConstructorWithTrailingOptionalParameters()
    {
        var tgis = new TGIBlockList(null);
        var index = new TGIBlockListIndex<int>(0, (_, _) => { }, 3, tgis);

        var clone = (TGIBlockListIndex<int>)index.Clone(null);

        clone.Data.Should().Be(3);
        clone.ParentTGIBlocks.Should().BeSameAs(tgis);
    }

    [Fact]
    public void FOURCC_RoundTrips()
    {
        AApiVersionedFields.FOURCC("DBPF").Should().Be(0x46504244);
        AApiVersionedFields.FOURCC(0x46504244).Should().Be("DBPF");
        var tooLong = () => AApiVersionedFields.FOURCC("123456789");
        tooLong.Should().Throw<ArgumentLengthException>();
    }
}
