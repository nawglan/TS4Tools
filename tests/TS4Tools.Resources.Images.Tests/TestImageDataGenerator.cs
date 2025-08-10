namespace TS4Tools.Resources.Images.Tests;

/// <summary>
/// Helper class for generating test image data in various formats.
/// </summary>
public static class TestImageDataGenerator
{
    /// <summary>
    /// Creates a simple 4x4 PNG image with known pixel data.
    /// </summary>
    /// <returns>PNG image data as byte array.</returns>
    public static byte[] CreateTestPng()
    {
        using var image = new Image<Rgba32>(4, 4);

        // Create a simple pattern: red, green, blue, white corners
        image[0, 0] = Color.Red;
        image[3, 0] = Color.Green;
        image[0, 3] = Color.Blue;
        image[3, 3] = Color.White;

        using var stream = new MemoryStream();
        image.SaveAsPng(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Creates a simple 4x4 JPEG image.
    /// </summary>
    /// <returns>JPEG image data as byte array.</returns>
    public static byte[] CreateTestJpeg()
    {
        using var image = new Image<Rgba32>(4, 4);

        // Create a gradient pattern
        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                byte intensity = (byte)((x + y) * 32);
                image[x, y] = new Rgba32(intensity, intensity, intensity, 255);
            }
        }

        using var stream = new MemoryStream();
        image.SaveAsJpeg(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Creates a simple 4x4 BMP image.
    /// </summary>
    /// <returns>BMP image data as byte array.</returns>
    public static byte[] CreateTestBmp()
    {
        using var image = new Image<Rgba32>(4, 4);

        // Create a checkerboard pattern
        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                bool isBlack = (x + y) % 2 == 0;
                image[x, y] = isBlack ? Color.Black : Color.White;
            }
        }

        using var stream = new MemoryStream();
        image.SaveAsBmp(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Creates a simple DDS header for testing.
    /// </summary>
    /// <param name="width">Image width.</param>
    /// <param name="height">Image height.</param>
    /// <param name="fourCC">Compression format.</param>
    /// <returns>DDS header as byte array.</returns>
    public static byte[] CreateTestDdsHeader(uint width = 4, uint height = 4, DdsFourCC fourCC = DdsFourCC.DXT5)
    {
        var header = DdsHeader.CreateForCompressed(width, height, fourCC, width * height);

        using var stream = new MemoryStream();
        stream.WriteDdsHeader(header);
        return stream.ToArray();
    }

    /// <summary>
    /// Creates test data that should not be recognized as any image format.
    /// </summary>
    /// <returns>Random binary data.</returns>
    public static byte[] CreateInvalidImageData()
    {
        var random = new Random(42); // Fixed seed for consistent tests
        var data = new byte[100];
        random.NextBytes(data);
        return data;
    }

    /// <summary>
    /// Creates an empty data array.
    /// </summary>
    /// <returns>Empty byte array.</returns>
    public static byte[] CreateEmptyData() => Array.Empty<byte>();

    /// <summary>
    /// Creates minimal data that's too small to be any valid image format.
    /// </summary>
    /// <returns>Very small byte array.</returns>
    public static byte[] CreateTooSmallData() => new byte[] { 0x01, 0x02 };
}
