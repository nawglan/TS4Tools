// Source: legacy_references/Sims4Tools/s4pi/Interfaces/DependentList.cs lines 124-127, 167-170
// (legacy constructs a throw-away BinaryReader/BinaryWriter over the caller's stream for each primitive)

namespace s4pi.Interfaces;

/// <summary>
/// Little-endian primitive I/O on a caller-owned stream, equivalent to legacy's
/// <c>new BinaryReader(s).ReadX()</c> / <c>new BinaryWriter(s).Write(x)</c> without taking ownership of the stream.
/// </summary>
internal static class LegacyBinary
{
    public static BinaryReader Reader(Stream s) => new(s, Encoding.UTF8, leaveOpen: true);

    public static BinaryWriter Writer(Stream s) => new(s, Encoding.UTF8, leaveOpen: true);

    public static int ReadInt32(Stream s)
    {
        using var r = Reader(s);
        return r.ReadInt32();
    }

    public static uint ReadUInt32(Stream s)
    {
        using var r = Reader(s);
        return r.ReadUInt32();
    }

    public static byte ReadByte(Stream s)
    {
        using var r = Reader(s);
        return r.ReadByte();
    }

    public static void Write(Stream s, int value)
    {
        using var w = Writer(s);
        w.Write(value);
    }

    public static void Write(Stream s, uint value)
    {
        using var w = Writer(s);
        w.Write(value);
    }

    public static void Write(Stream s, byte value)
    {
        using var w = Writer(s);
        w.Write(value);
    }

    /// <summary>Reads the rest of <paramref name="s"/> (from position 0 when seekable), as legacy resource stream copies do.</summary>
    public static byte[] ReadAllFromStart(Stream s)
    {
        if (s.CanSeek) s.Position = 0;
        using var ms = new MemoryStream();
        s.CopyTo(ms);
        return ms.ToArray();
    }
}
