using FluentAssertions;
using TS4Tools.Package;
using TS4Tools.Resources;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.Core.Tests;

/// <summary>
/// End-to-end integration tests that verify the full pipeline:
/// Package → TypedResource → Modifications → Save → Reload → Verify
///
/// These tests ensure that the wrapper system integrates correctly with the
/// package system and that data integrity is maintained through the full lifecycle.
/// </summary>
public class WrapperIntegrationTests
{
    [Fact]
    public async Task StblResource_PackageRoundTrip_PreservesStrings()
    {
        // Create a STBL resource with strings
        var stblKey = new ResourceKey(0x220557DA, 0, 0x12345678);
        var stbl = new StblResource(stblKey, ReadOnlyMemory<byte>.Empty);
        stbl.Add(0x00000001, "Hello World");
        stbl.Add(0x00000002, "Goodbye World");
        stbl.Add(0x00000003, "Special chars: café, über, 日本語");

        // Save to package
        using var stream = new MemoryStream();
        using (var package = DbpfPackage.CreateNew())
        {
            package.AddResource(stblKey, stbl.Data.ToArray());
            await package.SaveToStreamAsync(stream);
        }

        // Reload and verify
        stream.Position = 0;
        await using var reloaded = await DbpfPackage.OpenAsync(stream, leaveOpen: true);
        var entry = reloaded.Find(stblKey);
        entry.Should().NotBeNull();

        var data = await reloaded.GetResourceDataAsync(entry!);
        var parsedStbl = new StblResource(stblKey, data);

        parsedStbl.Count.Should().Be(3);
        parsedStbl[0x00000001].Should().Be("Hello World");
        parsedStbl[0x00000002].Should().Be("Goodbye World");
        parsedStbl[0x00000003].Should().Be("Special chars: café, über, 日本語");
    }

    [Fact]
    public async Task NameMapResource_PackageRoundTrip_PreservesHashMappings()
    {
        // Create a NameMap resource
        var nmapKey = new ResourceKey(0x0166038C, 0, 0xABCDEF01);
        var nmap = new NameMapResource(nmapKey, ReadOnlyMemory<byte>.Empty);
        nmap.Add(0x12345678ABCDEF00, "S4_12345678_00000000_ABCDEF00");
        nmap.Add(0xFEDCBA9876543210, "S4_FEDCBA98_76543210_00000000");

        // Save to package
        using var stream = new MemoryStream();
        using (var package = DbpfPackage.CreateNew())
        {
            package.AddResource(nmapKey, nmap.Data.ToArray());
            await package.SaveToStreamAsync(stream);
        }

        // Reload and verify
        stream.Position = 0;
        await using var reloaded = await DbpfPackage.OpenAsync(stream, leaveOpen: true);
        var entry = reloaded.Find(nmapKey);
        entry.Should().NotBeNull();

        var data = await reloaded.GetResourceDataAsync(entry!);
        var parsedNmap = new NameMapResource(nmapKey, data);

        parsedNmap.Count.Should().Be(2);
        parsedNmap[0x12345678ABCDEF00].Should().Be("S4_12345678_00000000_ABCDEF00");
        parsedNmap[0xFEDCBA9876543210].Should().Be("S4_FEDCBA98_76543210_00000000");
    }

    [Fact]
    public async Task ScriptResource_PackageRoundTrip_PreservesAssemblyData()
    {
        // Create a ScriptResource with mock assembly data
        var scriptKey = new ResourceKey(0x073FAA07, 0, 0xDEADBEEF);
        var script = new ScriptResource(scriptKey, ReadOnlyMemory<byte>.Empty);
        script.Version = 2;
        script.GameVersion = "1.100.147.1030";

        // Simulate assembly data (random bytes representing a DLL)
        var assemblyData = new byte[2048];
        new Random(42).NextBytes(assemblyData);
        script.Assembly = assemblyData;

        // Save to package
        using var stream = new MemoryStream();
        using (var package = DbpfPackage.CreateNew())
        {
            package.AddResource(scriptKey, script.Data.ToArray());
            await package.SaveToStreamAsync(stream);
        }

        // Reload and verify
        stream.Position = 0;
        await using var reloaded = await DbpfPackage.OpenAsync(stream, leaveOpen: true);
        var entry = reloaded.Find(scriptKey);
        entry.Should().NotBeNull();

        var data = await reloaded.GetResourceDataAsync(entry!);
        var parsedScript = new ScriptResource(scriptKey, data);

        parsedScript.Version.Should().Be(2);
        parsedScript.GameVersion.Should().Be("1.100.147.1030");
        parsedScript.AssemblyData.ToArray().Should().BeEquivalentTo(assemblyData);
    }

    [Fact]
    public async Task MultipleResourceTypes_PackageRoundTrip_AllPreserved()
    {
        // Create multiple resource types
        var stblKey = new ResourceKey(0x220557DA, 0, 1);
        var nmapKey = new ResourceKey(0x0166038C, 0, 2);
        var scriptKey = new ResourceKey(0x073FAA07, 0, 3);

        var stbl = new StblResource(stblKey, ReadOnlyMemory<byte>.Empty);
        stbl.Add(0x1, "Test String");

        var nmap = new NameMapResource(nmapKey, ReadOnlyMemory<byte>.Empty);
        nmap.Add(0x123456789, "TestName");

        var script = new ScriptResource(scriptKey, ReadOnlyMemory<byte>.Empty);
        script.Assembly = [0x01, 0x02, 0x03, 0x04, 0x05];

        // Save all to package
        using var stream = new MemoryStream();
        using (var package = DbpfPackage.CreateNew())
        {
            package.AddResource(stblKey, stbl.Data.ToArray());
            package.AddResource(nmapKey, nmap.Data.ToArray());
            package.AddResource(scriptKey, script.Data.ToArray());
            await package.SaveToStreamAsync(stream);
        }

        // Reload and verify all
        stream.Position = 0;
        await using var reloaded = await DbpfPackage.OpenAsync(stream, leaveOpen: true);

        reloaded.ResourceCount.Should().Be(3);

        // Verify STBL
        var stblEntry = reloaded.Find(stblKey);
        stblEntry.Should().NotBeNull();
        var stblData = await reloaded.GetResourceDataAsync(stblEntry!);
        var parsedStbl = new StblResource(stblKey, stblData);
        parsedStbl[0x1].Should().Be("Test String");

        // Verify NMAP
        var nmapEntry = reloaded.Find(nmapKey);
        nmapEntry.Should().NotBeNull();
        var nmapData = await reloaded.GetResourceDataAsync(nmapEntry!);
        var parsedNmap = new NameMapResource(nmapKey, nmapData);
        parsedNmap[0x123456789].Should().Be("TestName");

        // Verify Script (note: data is padded to 512-byte blocks)
        var scriptEntry = reloaded.Find(scriptKey);
        scriptEntry.Should().NotBeNull();
        var scriptData = await reloaded.GetResourceDataAsync(scriptEntry!);
        var parsedScript = new ScriptResource(scriptKey, scriptData);
        parsedScript.AssemblyData.Span[..5].ToArray().Should().BeEquivalentTo(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 });
    }

    [Fact]
    public async Task ModifyAndResave_PreservesModifications()
    {
        var stblKey = new ResourceKey(0x220557DA, 0, 0x12345678);

        // Create initial package
        using var stream = new MemoryStream();
        using (var package = DbpfPackage.CreateNew())
        {
            var stbl = new StblResource(stblKey, ReadOnlyMemory<byte>.Empty);
            stbl.Add(0x1, "Original String");
            package.AddResource(stblKey, stbl.Data.ToArray());
            await package.SaveToStreamAsync(stream);
        }

        // Load, modify, and save
        stream.Position = 0;
        using var modifyStream = new MemoryStream();
        await using (var package = await DbpfPackage.OpenAsync(stream, leaveOpen: true))
        {
            var entry = package.Find(stblKey)!;
            var data = await package.GetResourceDataAsync(entry);
            var stbl = new StblResource(stblKey, data);

            // Modify the string
            stbl[0x1] = "Modified String";
            stbl.Add(0x2, "New String");

            // Create new package with modified data
            using var newPackage = DbpfPackage.CreateNew();
            newPackage.AddResource(stblKey, stbl.Data.ToArray());
            await newPackage.SaveToStreamAsync(modifyStream);
        }

        // Verify modifications
        modifyStream.Position = 0;
        await using var finalPackage = await DbpfPackage.OpenAsync(modifyStream, leaveOpen: true);
        var finalEntry = finalPackage.Find(stblKey)!;
        var finalData = await finalPackage.GetResourceDataAsync(finalEntry);
        var finalStbl = new StblResource(stblKey, finalData);

        finalStbl.Count.Should().Be(2);
        finalStbl[0x1].Should().Be("Modified String");
        finalStbl[0x2].Should().Be("New String");
    }

    [Fact]
    public async Task TextResource_PackageRoundTrip_PreservesXml()
    {
        var textKey = new ResourceKey(0x03B33DDF, 0, 0xAABBCCDD);
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <I c="Module" i="module" m="scripts.test" n="test" s="12345">
              <L n="parts">
                <U>
                  <T n="name">TestPart</T>
                </U>
              </L>
            </I>
            """;

        var text = new TextResource(textKey, ReadOnlyMemory<byte>.Empty);
        text.Text = xml;

        // Save to package
        using var stream = new MemoryStream();
        using (var package = DbpfPackage.CreateNew())
        {
            package.AddResource(textKey, text.Data.ToArray());
            await package.SaveToStreamAsync(stream);
        }

        // Reload and verify
        stream.Position = 0;
        await using var reloaded = await DbpfPackage.OpenAsync(stream, leaveOpen: true);
        var entry = reloaded.Find(textKey);
        entry.Should().NotBeNull();

        var data = await reloaded.GetResourceDataAsync(entry!);
        var parsedText = new TextResource(textKey, data);

        parsedText.Text.Should().Be(xml);
    }

    [Fact]
    public async Task LargeStbl_PackageRoundTrip_PreservesAllStrings()
    {
        // Create a large STBL with many strings
        var stblKey = new ResourceKey(0x220557DA, 0, 0x99999999);
        var stbl = new StblResource(stblKey, ReadOnlyMemory<byte>.Empty);

        const int stringCount = 500;
        var expectedStrings = new Dictionary<uint, string>();

        for (uint i = 0; i < stringCount; i++)
        {
            var value = $"String {i} with some content to make it longer";
            stbl.Add(i, value);
            expectedStrings[i] = value;
        }

        // Save to package
        using var stream = new MemoryStream();
        using (var package = DbpfPackage.CreateNew())
        {
            package.AddResource(stblKey, stbl.Data.ToArray());
            await package.SaveToStreamAsync(stream);
        }

        // Reload and verify all strings
        stream.Position = 0;
        await using var reloaded = await DbpfPackage.OpenAsync(stream, leaveOpen: true);
        var entry = reloaded.Find(stblKey);
        entry.Should().NotBeNull();

        var data = await reloaded.GetResourceDataAsync(entry!);
        var parsedStbl = new StblResource(stblKey, data);

        parsedStbl.Count.Should().Be(stringCount);
        foreach (var (key, expectedValue) in expectedStrings)
        {
            parsedStbl[key].Should().Be(expectedValue, $"String at key {key} should match");
        }
    }

    [Fact]
    public async Task EmptyResources_PackageRoundTrip_Preserved()
    {
        // Test empty resources are handled correctly
        var stblKey = new ResourceKey(0x220557DA, 0, 1);
        var nmapKey = new ResourceKey(0x0166038C, 0, 2);

        var stbl = new StblResource(stblKey, ReadOnlyMemory<byte>.Empty);
        var nmap = new NameMapResource(nmapKey, ReadOnlyMemory<byte>.Empty);

        // Save empty resources to package
        using var stream = new MemoryStream();
        using (var package = DbpfPackage.CreateNew())
        {
            package.AddResource(stblKey, stbl.Data.ToArray());
            package.AddResource(nmapKey, nmap.Data.ToArray());
            await package.SaveToStreamAsync(stream);
        }

        // Reload and verify they're still empty
        stream.Position = 0;
        await using var reloaded = await DbpfPackage.OpenAsync(stream, leaveOpen: true);

        var stblEntry = reloaded.Find(stblKey)!;
        var stblData = await reloaded.GetResourceDataAsync(stblEntry);
        var parsedStbl = new StblResource(stblKey, stblData);
        parsedStbl.Count.Should().Be(0);

        var nmapEntry = reloaded.Find(nmapKey)!;
        var nmapData = await reloaded.GetResourceDataAsync(nmapEntry);
        var parsedNmap = new NameMapResource(nmapKey, nmapData);
        parsedNmap.Count.Should().Be(0);
    }
}
