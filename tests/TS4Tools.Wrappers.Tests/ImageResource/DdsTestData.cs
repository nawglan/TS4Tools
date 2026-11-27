namespace TS4Tools.Wrappers.Tests;

/// <summary>Helpers to build DDS files for tests.</summary>
internal static class DdsTestData
{
    public static byte[] Build(DdsFormat format, int width, int height, byte[] body, int mips = 1)
    {
        var header = DdsHeader.Create(format, width, height, mips);
        var bytes = new List<byte>();
        header.Write(bytes);
        bytes.AddRange(body);
        return [.. bytes];
    }

    /// <summary>Deterministic pseudo-random bytes.</summary>
    public static byte[] Noise(int length, int seed)
    {
        var rng = new Random(seed);
        var data = new byte[length];
        rng.NextBytes(data);
        return data;
    }

    /// <summary>RGBA32 pixels for a width x height gradient with a fixed alpha.</summary>
    public static byte[] Gradient(int width, int height, byte alpha = 255)
    {
        var rgba = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var o = ((y * width) + x) * 4;
                rgba[o] = (byte)(x * 255 / Math.Max(1, width - 1));
                rgba[o + 1] = (byte)(y * 255 / Math.Max(1, height - 1));
                rgba[o + 2] = (byte)(128 + (x - y) / 2);
                rgba[o + 3] = alpha;
            }
        }

        return rgba;
    }
}
