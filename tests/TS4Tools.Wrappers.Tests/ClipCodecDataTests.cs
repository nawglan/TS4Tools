// Source: legacy_references/Sims4Tools/s4pi Wrappers/AnimationResources/S3CLIP.cs lines 28-906 (layout and channel/frame encodings)

using FluentAssertions;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

public class ClipCodecDataTests
{
    private sealed record Chan(ClipChannelType Type, byte Pad, (ushort Tick, ushort Flags, ushort[] Idx)[] Frames);

    private static byte[] BuildCodec(string clipName, string sourceName, float[] palette, Chan[] channels, bool legacyPadding = false)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write("_pilC3S_"u8.ToArray());
        w.Write(2u);
        w.Write(0u);
        w.Write(0.0333f);
        w.Write((ushort)16);
        w.Write((ushort)0);
        w.Write((uint)channels.Length);
        w.Write((uint)palette.Length);
        long offsets = ms.Position;
        for (int i = 0; i < 4; i++) w.Write(0u);
        long chanPos = ms.Position;
        ms.Position += channels.Length * 20;
        long namePos = ms.Position;
        w.Write(Encoding.UTF8.GetBytes(clipName));
        w.Write((byte)0);
        long srcPos = ms.Position;
        w.Write(Encoding.UTF8.GetBytes(sourceName));
        w.Write((byte)0);
        long palPos = ms.Position;
        foreach (var p in palette) w.Write(p);
        var dataOffsets = new List<long>();
        foreach (var c in channels)
        {
            dataOffsets.Add(ms.Position);
            foreach (var f in c.Frames)
            {
                w.Write(f.Tick);
                w.Write(f.Flags);
                switch (c.Type)
                {
                    case ClipChannelType.F1Normalized:
                        w.Write((byte)f.Idx[0]);
                        if (legacyPadding)
                            w.Write(new byte[3]);
                        else
                            w.Write(c.Pad);
                        break;
                    case ClipChannelType.F3:
                        foreach (var i in f.Idx) w.Write(i);
                        break;
                    case ClipChannelType.F3HighPrecisionNormalized:
                        w.Write(((uint)f.Idx[2] << 20) + ((uint)f.Idx[1] << 10) + f.Idx[0]);
                        break;
                }
            }
        }

        long end = ms.Position;
        ms.Position = chanPos;
        for (int i = 0; i < channels.Length; i++)
        {
            w.Write((uint)dataOffsets[i]);
            w.Write(0xAABBCCDDu);
            w.Write(0.5f);
            w.Write(2f);
            w.Write((ushort)channels[i].Frames.Length);
            w.Write((byte)channels[i].Type);
            w.Write((byte)ClipSubTargetType.Translation);
        }

        ms.Position = offsets;
        w.Write((uint)chanPos);
        w.Write((uint)palPos);
        w.Write((uint)namePos);
        w.Write((uint)srcPos);
        return ms.ToArray().AsSpan(0, (int)end).ToArray();
    }

    private static Chan[] SampleChannels(byte pad) =>
    [
        new(ClipChannelType.F1Normalized, pad, [(0, 0x20, [255]), (4, 0x80, [7])]),
        new(ClipChannelType.F3, pad, [(0, 0, [1, 2, 3])]),
        new(ClipChannelType.F3HighPrecisionNormalized, pad, [(2, 1, [1, 2, 3]), (9, 0, [1023, 0, 512])]),
    ];

    private static byte[] Write(ClipCodecData codec)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        codec.Write(w);
        return ms.ToArray();
    }

    [Theory]
    [InlineData(false, (byte)0xFF)]
    [InlineData(false, (byte)0x00)]
    [InlineData(true, (byte)0x00)]
    public void Parse_Channels_AreStructuredAndRoundTripExactly(bool legacyPadding, byte pad)
    {
        var data = BuildCodec("clip", "src", [1.5f, -2f], SampleChannels(pad), legacyPadding);

        var codec = ClipCodecData.Parse(data, data.Length);

        codec.HasStructuredData.Should().BeTrue();
        codec.Channels.Should().HaveCount(3);
        codec.F1Palette.Should().Equal(1.5f, -2f);
        codec.ClipName.Should().Be("clip");
        codec.Channels[0].Frames.Should().HaveCount(2);
        codec.Channels[0].Frames[0].Indices.Should().Equal([(ushort)255]);
        codec.Channels[1].Frames[0].Indices.Should().Equal([(ushort)1, (ushort)2, (ushort)3]);
        codec.Channels[2].Frames[1].Indices.Should().Equal([(ushort)1023, (ushort)0, (ushort)512]);
        codec.Channels[0].Target.Should().Be(0xAABBCCDDu);
        Write(codec).Should().Equal(data);
    }

    [Fact]
    public void Parse_EditedChannel_IsWrittenFromStructure()
    {
        var data = BuildCodec("clip", "src", [], SampleChannels(0xFF));
        var codec = ClipCodecData.Parse(data, data.Length);
        codec.Channels[1].Frames[0].Indices = [9, 8, 7];

        var written = Write(codec);
        var again = ClipCodecData.Parse(written, written.Length);

        again.HasStructuredData.Should().BeTrue();
        again.Channels[1].Frames[0].Indices.Should().Equal([(ushort)9, (ushort)8, (ushort)7]);
    }

    [Fact]
    public void Parse_ChannelCountBeyondData_FallsBackToRawBytes()
    {
        var data = BuildCodec("clip", "src", [], SampleChannels(0xFF));
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(24), 0x00FFFFFF);

        var codec = ClipCodecData.Parse(data, data.Length);

        codec.HasStructuredData.Should().BeFalse();
        codec.Channels.Should().BeEmpty();
        Write(codec).Should().Equal(data);
    }

    [Fact]
    public void Parse_NonCanonicalBytes_FallBackToRawBytes()
    {
        var data = BuildCodec("clip", "src", [], SampleChannels(0xFF)).Concat(new byte[] { 1, 2, 3 }).ToArray();

        var codec = ClipCodecData.Parse(data, data.Length);

        codec.HasStructuredData.Should().BeFalse();
        Write(codec).Should().Equal(data);
    }
}
