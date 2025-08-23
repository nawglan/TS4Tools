using TS4Tools.Resources.Animation;

Console.WriteLine("Testing ClipHeaderResource with minimal data...");

// Create a minimal binary clip header structure
using var stream = new MemoryStream();
using var writer = new BinaryWriter(stream);

// Write basic header
writer.Write((uint)7);  // Version 7
writer.Write((uint)0);  // Flags  
writer.Write(5.5f);     // Duration

// Quaternion (identity)
writer.Write(0.0f); // x
writer.Write(0.0f); // y  
writer.Write(0.0f); // z
writer.Write(1.0f); // w

// Vector3 (zero)
writer.Write(0.0f); // x
writer.Write(0.0f); // y
writer.Write(0.0f); // z

// Reference namespace hash (version >= 5)
writer.Write((uint)0x12345678);

// Surface hashes (version >= 10) - not included for version 7

// Clip name (version >= 7) 
var clipNameBytes = System.Text.Encoding.UTF8.GetBytes("test_clip_name\0");
writer.Write((uint)clipNameBytes.Length);
writer.Write(clipNameBytes);

// Rig name
var rigNameBytes = System.Text.Encoding.UTF8.GetBytes("test_rig\0");
writer.Write((uint)rigNameBytes.Length);
writer.Write(rigNameBytes);

// Explicit namespaces (version >= 4)
writer.Write((uint)2); // namespace count
var ns1Bytes = System.Text.Encoding.UTF8.GetBytes("namespace1\0");
writer.Write((uint)ns1Bytes.Length);
writer.Write(ns1Bytes);
var ns2Bytes = System.Text.Encoding.UTF8.GetBytes("namespace2\0");
writer.Write((uint)ns2Bytes.Length);
writer.Write(ns2Bytes);

// IK count
writer.Write((uint)0);

// Event count  
writer.Write((uint)0);

// Codec data length
writer.Write((uint)0);

stream.Position = 0;

Console.WriteLine($"Created test stream with {stream.Length} bytes");

try
{
    var resource = new ClipHeaderResource(stream);
    
    Console.WriteLine($"✅ Successfully created resource!");
    Console.WriteLine($"  Version: {resource.Version}");
    Console.WriteLine($"  ClipName: '{resource.ClipName}'");
    Console.WriteLine($"  RigName: '{resource.RigName}'");
    Console.WriteLine($"  Duration: {resource.Duration}");
    Console.WriteLine($"  HasValidData: {resource.HasValidData}");
    Console.WriteLine($"  Explicit Namespaces: {resource.ExplicitNamespaces.Count}");
    
    foreach (var ns in resource.ExplicitNamespaces)
    {
        Console.WriteLine($"    - {ns}");
    }
    
    var json = resource.JsonData;
    if (json != null)
    {
        Console.WriteLine("\n📄 Generated JSON:");
        Console.WriteLine(json);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}
