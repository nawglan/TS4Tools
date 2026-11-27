// Source: legacy_references/Sims4Tools/s4pi/Interfaces/DependentList.cs lines 35-335 (element handler propagation),
// legacy_references/Sims4Tools/CS System Classes/AHandlerList.cs lines 30-289 (list change notification, max size),
// legacy_references/Sims4Tools/s4pi/Interfaces/SimpleList.cs, IndexList.cs, AResource.cs lines 67-119

using s4pi.Interfaces;

namespace CompatTests;

public class DependentListTests
{
    [Fact]
    public void ElementChange_MarksOwningResourceDirty_AndRaisesResourceChanged()
    {
        var data = SampleResource.Build(3, (1, 2, 3), (4, 5, 6));
        var res = new SampleResource(0, new MemoryStream(data));
        int changed = 0;
        res.ResourceChanged += (_, _) => changed++;

        res.IsDirty.Should().BeFalse("parsing does not raise change notifications");

        res.TGIBlocks[1].Instance = 0xFEED;

        changed.Should().Be(1);
        res.IsDirty.Should().BeTrue();
        res.AsBytes.Should().Equal(SampleResource.Build(3, (1, 2, 3), (4, 5, 0xFEED)), "the dirty resource re-serialises");
    }

    [Fact]
    public void ListChange_MarksOwningResourceDirty()
    {
        var res = new SampleResource(0, new MemoryStream(SampleResource.Build(1)));
        int changed = 0;
        res.ResourceChanged += (_, _) => changed++;

        res.TGIBlocks.Add(new TGIBlock(0, null, 7, 8, 9));

        changed.Should().Be(1);
        res.AsBytes.Should().Equal(SampleResource.Build(1, (7, 8, 9)));

        // The added element was given the list's element handler (DependentList.Add sets it)
        res.TGIBlocks[0].ResourceGroup = 0;
        changed.Should().Be(2);
    }

    [Fact]
    public void ElementHandler_IsSeparateFromListHandler()
    {
        int listChanges = 0, elementChanges = 0;
        var list = new TestList((_, _) => listChanges++, (_, _) => elementChanges++);

        list.Add(new TGIBlock(0, null, 1, 2, 3));
        list[0].Instance = 99;

        listChanges.Should().Be(1);
        elementChanges.Should().Be(1);
    }

    private sealed class TestList : DependentList<TGIBlock>
    {
        public TestList(EventHandler list, EventHandler element) : base(list) { elementHandler = element; }

        protected override TGIBlock CreateElement(Stream s) => new(0, elementHandler, s);

        protected override void WriteElement(Stream s, TGIBlock element) => element.UnParse(s);
    }

    [Fact]
    public void EnumerableConstructor_ClonesElements()
    {
        var source = new TGIBlock(0, null, 1, 2, 3);
        int changed = 0;

        var list = new TGIBlockList((_, _) => changed++, [source]);

        list[0].Should().NotBeSameAs(source);
        list[0].Should().Be(source);
        changed.Should().Be(0, "construction does not raise the handler");
        list[0].Instance = 4;
        changed.Should().Be(1);
        source.Instance.Should().Be(3);
    }

    [Fact]
    public void StreamConstructor_ReadsCountPrefixedElements_WithoutRaisingHandler()
    {
        var ms = new MemoryStream();
        using (var w = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
        {
            w.Write(3);
            w.Write(10u);
            w.Write(20u);
            w.Write(30u);
        }
        ms.Position = 0;
        int changed = 0;

        var list = new UIntList((_, _) => changed++, ms);

        list.ToArray().Should().Equal(10u, 20u, 30u); // SimpleList is IEnumerable of both T and HandlerElement<T>
        changed.Should().Be(0);

        var output = new MemoryStream();
        list.UnParse(output);
        output.ToArray().Should().Equal(ms.ToArray());
    }

    [Fact]
    public void SimpleList_ValueOperationsRaiseHandler()
    {
        int changed = 0;
        var list = new IntList((_, _) => changed++, [5, 6, 7]);

        list[1] = 6;
        changed.Should().Be(0, "setting an equal value is not a change");
        list[1] = 60;
        changed.Should().Be(2, "legacy raises the element handler (HandlerElement.Val) and then the list handler");
        list.Remove(7);
        list.Insert(0, 1);
        changed.Should().Be(4);

        list.ToArray().Should().Equal(1, 5, 60);
        list.IndexOf(60).Should().Be(2);
        list.Contains(5).Should().BeTrue();
        list.FindAll(x => x > 4).Should().Equal(5, 60);
    }

    [Fact]
    public void AddRange_RaisesHandlerOnce()
    {
        int changed = 0;
        var list = new UIntList((_, _) => changed++);

        list.AddRange([1u, 2u, 3u]);

        changed.Should().Be(1);
        list.Count.Should().Be(3);
    }

    [Fact]
    public void MaxSize_IsEnforced()
    {
        var list = new UIntList(null, size: 2) { 1u, 2u };

        var act = () => list.Add(3u);

        act.Should().Throw<InvalidOperationException>();
        list.MaxSize.Should().Be(2);
    }

    [Fact]
    public void StreamCount_LargerThanMaxSize_IsRejected()
    {
        var ms = new MemoryStream();
        using (var w = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
            w.Write(int.MaxValue);
        ms.Position = 0;

        var act = () => new UIntList(null, ms, size: 16);

        act.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void IGenericAdd_AddsDefaultElement()
    {
        int changed = 0;
        var tgis = new TGIBlockList((_, _) => changed++);
        AddThroughInterface(tgis);

        tgis.Count.Should().Be(2);
        changed.Should().Be(2);
        var abstractType = () => tgis.Add(typeof(AResourceKey));
        abstractType.Should().Throw<ArgumentException>();
    }

#pragma warning disable CA1859 // the point is to call through the legacy IGenericAdd interface
    private static void AddThroughInterface(IGenericAdd list)
    {
        list.Add();
        list.Add(typeof(TGIBlock));
    }
#pragma warning restore CA1859

    [Fact]
    public void IndexList_ParentTGIBlocks_PropagatesToElements()
    {
        var first = new TGIBlockList(null) { new TGIBlock(0, null, 1, 1, 1) };
        var second = new TGIBlockList(null) { new TGIBlock(0, null, 2, 2, 2) };
        var list = new Int32IndexList(null, [0], first);
        list.Add(0);

        list.ParentTGIBlocks = second;

        list.ToArray().Should().Equal(0, 0);
        ((List<TGIBlockListIndex<int>>)list).Should().AllSatisfy(i => i.ParentTGIBlocks.Should().BeSameAs(second));
    }

    [Fact]
    public void ByteIndexList_RoundTripsWithCustomCount()
    {
        var data = new byte[] { 2, 7, 9 };
        var list = new ByteIndexList(null, new MemoryStream(data), s => s.ReadByte(), (s, c) => s.WriteByte((byte)c));

        list.ToArray().Should().Equal((byte)7, (byte)9);
        var ms = new MemoryStream();
        list.UnParse(ms);
        ms.ToArray().Should().Equal(data);
    }

    [Fact]
    public void AHandlerList_Equals_ComparesElementwise()
    {
        var a = new UIntList(null, [1u, 2u]);
        var b = new UIntList(null, [1u, 2u]);
        var c = new UIntList(null, [1u, 3u]);

        a.Equals(b).Should().BeTrue();
        a.Equals(c).Should().BeFalse();
    }
}
