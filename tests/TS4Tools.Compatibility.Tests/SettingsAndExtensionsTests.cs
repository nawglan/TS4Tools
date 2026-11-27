// Source: legacy_references/Sims4Tools/s4pi/Settings/Settings.cs lines 28-52,
// legacy_references/Sims4Tools/s4pi/Interfaces/AResource.cs lines 67-80 (AsBytesWorkaround),
// legacy_references/Sims4Tools/s4pi Extras/Extensions/ExtList.cs, TGIN.cs, FileNameConverter.cs

using s4pi.Extensions;

namespace CompatTests;

public class SettingsAndExtensionsTests
{
    [Fact]
    public void Settings_ForwardToLibrarySettings()
    {
        var saved = (TS4Tools.LibrarySettings.Checking, TS4Tools.LibrarySettings.AsBytesWorkaround, TS4Tools.LibrarySettings.IsTS4);
        try
        {
            s4pi.Settings.Settings.Checking.Should().BeTrue("legacy default");
            s4pi.Settings.Settings.AsBytesWorkaround.Should().BeTrue("legacy default");
            s4pi.Settings.Settings.IsTS4.Should().BeTrue("legacy default");

            TS4Tools.LibrarySettings.Checking = false;
            TS4Tools.LibrarySettings.IsTS4 = false;
            s4pi.Settings.Settings.Checking.Should().BeFalse();
            s4pi.Settings.Settings.IsTS4.Should().BeFalse();
        }
        finally
        {
            (TS4Tools.LibrarySettings.Checking, TS4Tools.LibrarySettings.AsBytesWorkaround, TS4Tools.LibrarySettings.IsTS4) = saved;
        }
    }

    [Fact]
    public void AsBytesWorkaroundOff_ReturnsParsedStreamUntilDirty()
    {
        var saved = TS4Tools.LibrarySettings.AsBytesWorkaround;
        try
        {
            TS4Tools.LibrarySettings.AsBytesWorkaround = false;
            var original = new MemoryStream(SampleResource.Build(2));
            var res = new SampleResource(0, original);

            res.Stream.Should().BeSameAs(original, "a clean resource is not re-serialised");
            res.Version = 3;
            res.Stream.Should().NotBeSameAs(original);
            res.Version.Should().Be(3);
        }
        finally
        {
            TS4Tools.LibrarySettings.AsBytesWorkaround = saved;
        }
    }

    [Fact]
    public void ExtList_UsesTS4ToolsData_WithStarFallback()
    {
        ExtList.Ext[0x220557DA].Should().Equal("STBL", ".stbl");
        ExtList.Ext[0x220557DA][0].Should().Be(TS4Tools.Extensions.ExtList.GetTag(0x220557DA));
        ExtList.Ext[0x220557DA][1].Should().Be(TS4Tools.Extensions.ExtList.GetExtension(0x220557DA));
        ExtList.Ext["0xFFFFFFFF"].Should().Equal("UNKN", ".bnry");
        ExtList.Ext.Should().ContainKey("*");

        var set = () => ExtList.Ext["0x00000000"] = [];
        set.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void TGIN_RoundTripsExportFileName()
    {
        var tgin = new TGIN { ResType = 0x220557DA, ResGroup = 0x80000000, ResInstance = 0x0123456789ABCDEF, ResName = "My-Name" };

        string name = tgin;

        name.Should().Be("S4_220557DA_80000000_0123456789ABCDEF_My%2dName%%+STBL.stbl");
        TGIN back = name;
        back.ResType.Should().Be(0x220557DA);
        back.ResGroup.Should().Be(0x80000000);
        back.ResInstance.Should().Be(0x0123456789ABCDEF);
        back.ResName.Should().Be("My-Name");
    }

    [Fact]
    public void TGIN_WithoutName_AndUnknownType()
    {
        string name = new TGIN { ResType = 0xFFFFFFFF, ResGroup = 1, ResInstance = 2 };

        name.Should().Be("S4_FFFFFFFF_00000001_0000000000000002%%+UNKN.bnry");
    }

    [Fact]
    public void TGIN_ConvertsTS4FileNames()
    {
        FileNameConverter.IsValidFileName("0x00000001!0x0000000000000002.trayitem").Should().BeTrue();
        FileNameConverter.IsValidFileName("readme.txt").Should().BeFalse();

        TGIN tgin = "0x00000001!0x0000000000000002.trayitem";

        tgin.ResType.Should().Be(0x2A8A5E22);
        tgin.ResGroup.Should().Be(1);
        tgin.ResInstance.Should().Be(2);
    }

    [Fact]
    public void TGIN_ConvertsToAndFromResourceKey()
    {
        var tgi = new s4pi.Interfaces.TGIBlock(0, null, 1, 2, 3);

        TGIN tgin = tgi;
        s4pi.Interfaces.AResourceKey key = tgin;

        key.Should().Be(tgi);
    }
}
