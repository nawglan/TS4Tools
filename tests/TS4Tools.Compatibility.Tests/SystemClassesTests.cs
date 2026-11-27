// Source: legacy_references/Sims4Tools/CS System Classes/FNVHash.cs, SevenBitString.cs, Extensions.cs;
// legacy_references/Sims4Tools/s4pi/Interfaces/BoundingBox.cs, Quaternion.cs, DataBlobHandler.cs

using s4pi.Interfaces;

namespace CompatTests;

public class SystemClassesTests
{
    [Fact]
    public void FNV_IsFnv1_OverLowerCasedAscii()
    {
        // FNV-1 (multiply then XOR) reference values for "a"
        FNV32.GetHash("a").Should().Be(0x050C5D7E);
        FNV64.GetHash("a").Should().Be(0xAF63BD4C8601B7BE);
        FNV64.GetHash("A").Should().Be(FNV64.GetHash("a"), "input is lower-cased");
        FNV24.GetHash("a").Should().Be((0x050C5D7Eu >> 24) ^ (0x050C5D7Eu & 0xFFFFFF));
        FNV64CLIP.GetHashGeneric("x").Should().Be(FNV64.GetHash("x") & 0x7FFFFFFFFFFFFFFF);
    }

    [Fact]
    public void SevenBitString_RoundTrips_WithByteLengthPrefix()
    {
        var text = new string('x', 200);
        var ms = new MemoryStream();

        SevenBitString.Write(ms, Encoding.UTF8, text);

        ms.ToArray()[..2].Should().Equal(0xC8, 0x01);
        ms.Position = 0;
        SevenBitString.Read(ms, Encoding.UTF8).Should().Be(text);

        var be = new MemoryStream();
        BigEndianUnicodeString.Write(be, "hi");
        be.ToArray().Should().Equal(4, 0, (byte)'h', 0, (byte)'i');
    }

    [Fact]
    public void ListExtensions_CompareAndEqual()
    {
        IList<int> a = [1, 2, 3];
        IList<int> b = [1, 2, 4];

        a.CompareTo(b).Should().BeNegative();
        System.Extensions.Equals(a, new List<int> { 1, 2, 3 }).Should().BeTrue();
        ((Array)new[] { 1, 2 }).ConvertAll<string>(CultureInfo.InvariantCulture).Should().Equal("1", "2");
    }

    [Fact]
    public void Vertex_And_Quaternion_RoundTrip()
    {
        var ms = new MemoryStream();
        new Vertex(0, null, 1, 2, 3).UnParse(ms);
        new Quaternion(0, null, 4, 5, 6, 7).UnParse(ms);
        ms.Length.Should().Be(28);

        ms.Position = 0;
        new Vertex(0, null, ms).Should().Be(new Vertex(0, null, 1, 2, 3));
        new Quaternion(0, null, ms).Should().Be(new Quaternion(0, null, 4, 5, 6, 7));
    }

    [Fact]
    public void BoundingBox_ChangeRaisesHandler()
    {
        int changed = 0;
        var box = new BoundingBox(0, (_, _) => changed++);

        box.Max.Z = 1; // the vertices share the box's handler
        changed.Should().Be(1);

        var ms = new MemoryStream();
        box.UnParse(ms);
        ms.Position = 0;
        new BoundingBox(0, null, ms).Should().Be(box);
    }

    [Fact]
    public void DataBlobHandler_HexDisplay()
    {
        var blob = new DataBlobHandler(0, null, new byte[] { 0x41, 0x42, 0x00 });

        blob.BuildHexDisplay(0, true).Should().Be(
            "   | 00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F" + Environment.NewLine +
            "00 | 41 42 00" + new string(' ', 39) + " | AB.");
        blob.GetRawData(1, 2).Should().Equal(0x42, 0x00);
        blob.GetRawData(2, 5).Should().BeNull();
        blob.Equals(new byte[] { 0x41, 0x42, 0x00 }).Should().BeTrue();
        new DataBlobHandler(0, null, 0).BuildHexDisplay(0, true).Should().Be("{Empty Data Blob}");
    }
}
