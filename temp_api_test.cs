using BCnEncoder.Decoder;
using BCnEncoder.Encoder;
using BCnEncoder.Shared;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using CommunityToolkit.HighPerformance;

// Temporary file to test BCnEncoder API
public class ApiTest
{
    public void TestDecoder()
    {
        var decoder = new BcDecoder();
        // Test what overloads exist for DecodeRaw
        // decoder.DecodeRaw - IntelliSense will show available overloads
    }
    
    public void TestEncoder()
    {
        var encoder = new BcEncoder();
        // Test what methods exist for encoding
    }
    
    public void TestImageSharp()
    {
        using var image = new Image<Rgba32>(100, 100);
        // Test what methods are available on Image<T>
        // image. - IntelliSense will show available methods
    }
    
    public void TestCommunityToolkit()
    {
        // Test what's available in CommunityToolkit.HighPerformance
        // CommunityToolkit.HighPerformance. - IntelliSense will show available namespaces
    }
}
