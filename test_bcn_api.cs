using BCnEncoder.Decoder;
using BCnEncoder.Encoder;
using BCnEncoder.Shared;
using System;
using System.IO;
using System.Reflection;

class Program
{
    static void Main()
    {
        Console.WriteLine("BCnEncoder.Net API Exploration");
        
        // Check BcDecoder methods
        Console.WriteLine("\nBcDecoder methods:");
        var decoderType = typeof(BcDecoder);
        foreach (var method in decoderType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            if (method.Name.Contains("Decode"))
            {
                Console.WriteLine($"  {method.Name}: {method}");
            }
        }
        
        // Check BcEncoder methods
        Console.WriteLine("\nBcEncoder methods:");
        var encoderType = typeof(BcEncoder);
        foreach (var method in encoderType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            if (method.Name.Contains("Encode"))
            {
                Console.WriteLine($"  {method.Name}: {method}");
            }
        }
    }
}
