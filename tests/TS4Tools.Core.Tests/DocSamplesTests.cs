using FluentAssertions;
using TS4Tools.Extensions;
using TS4Tools.FileTable;
using TS4Tools.Package;
using TS4Tools.Resources;
using TS4Tools.Wrappers;
using TS4Tools.Wrappers.CasPartResource;
using TS4Tools.Wrappers.CatalogResource;
using TS4Tools.Wrappers.Hashing;
using Xunit;

namespace TS4Tools.Core.Tests;

/// <summary>
/// Every code sample in docs/agent-guide.md lives here, so the guide cannot drift from the API.
/// Keep the two in sync: when a sample changes, change both. Samples are marked "Sample N" in the guide.
/// </summary>
public class DocSamplesTests
{
    private const uint StblType = 0x220557DA;
    private const uint NameMapType = 0x0166038C;
    private const uint TuningType = 0x03B33DDF; // combined tuning (XML text)
    private const uint DdsType = 0x00B2D882;

    private static ResourceHandlerRegistry WrapperRegistry()
    {
        var registry = new ResourceHandlerRegistry();
        registry.DiscoverHandlers(typeof(StblResourceFactory).Assembly);
        return registry;
    }

    private static async Task<MemoryStream> SaveToMemoryAsync(DbpfPackage package)
    {
        var ms = new MemoryStream();
        await package.SaveToStreamAsync(ms);
        ms.Position = 0;
        return ms;
    }

    private static async Task<MemoryStream> SamplePackageAsync()
    {
        using var package = DbpfPackage.CreateNew();
        var stbl = new StblResource(new ResourceKey(StblType, 0, 0x1), ReadOnlyMemory<byte>.Empty);
        stbl.Add(FnvHash.Fnv32("Hello"), "Hello World");
        package.AddResource(new ResourceKey(StblType, 0, 0x1), stbl);
        package.AddResource(new ResourceKey(TuningType, 0, 0x2), "<I n=\"x\"/>"u8.ToArray());
        return await SaveToMemoryAsync(package);
    }

    [Fact]
    public async Task Sample_OpenFindAndReadRaw()
    {
        using var stream = await SamplePackageAsync();

        // Sample 1: open (stream), header, find, read raw bytes
        await using var package = await DbpfPackage.OpenAsync(stream, leaveOpen: true);
        Console.WriteLine($"v{package.MajorVersion}.{package.MinorVersion} resources={package.ResourceCount}");

        var key = new ResourceKey(StblType, 0, 0x1);
        IResourceIndexEntry? entry = package.Find(key);                 // by key
        entry = package.Find(StblType, 0, 0x1);                          // by t/g/i
        var allStbl = package.FindAll(e => e.Key.ResourceType == StblType).ToList();
        ReadOnlyMemory<byte> bytes = await package.GetResourceDataAsync(entry!); // decompressed
        // end sample

        package.MajorVersion.Should().Be(2);
        package.ResourceCount.Should().Be(2);
        allStbl.Should().HaveCount(1);
        bytes.Length.Should().BeGreaterThan(0);
        package.IsReadOnly.Should().BeFalse(); // a writable MemoryStream opens read-write
    }

    [Fact]
    public async Task Sample_OpenFileReadOnlyThenReadWrite()
    {
        var path = Path.Combine(Path.GetTempPath(), "ts4tools-doc-" + Guid.NewGuid().ToString("N") + ".package");
        try
        {
            using (var stream = await SamplePackageAsync())
                await File.WriteAllBytesAsync(path, stream.ToArray());

            // Sample 2: open from disk
            await using (var ro = await DbpfPackage.OpenAsync(path))                 // read-only (default)
            {
                // ro.AddResource(...) would throw InvalidOperationException
                ro.IsReadOnly.Should().BeTrue();
            }
            await using (var rw = await DbpfPackage.OpenAsync(path, readWrite: true))
            {
                rw.IsReadOnly.Should().BeFalse();
            }
            // end sample
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Sample_ResourceKeyFormats()
    {
        // Sample 3: ResourceKey formatting and parsing
        var key = new ResourceKey(0x220557DA, 0x00000000, 0x00000000DEADBEEF);
        string modern = key.ToString();                // "T:0x220557DA G:0x00000000 I:0x00000000DEADBEEF"
        ResourceKey a = ResourceKey.Parse(modern);
        ResourceKey b = ResourceKey.Parse("0x220557DA-0x00000000-0x00000000DEADBEEF");  // legacy s4pe form
        ResourceKey c = ResourceKey.Parse("220557DA:00000000:DEADBEEF");                // legacy, no 0x
        ResourceKey d = ResourceKey.Parse("key:220557DA:00000000:DEADBEEF");
        bool ok = ResourceKey.TryParse("garbage", out _);                               // false, never throws
        // end sample

        new[] { a, b, c, d }.Should().AllBeEquivalentTo(key);
        ok.Should().BeFalse();
        FluentActions.Invoking(() => ResourceKey.Parse("garbage")).Should().Throw<FormatException>();
    }

    [Fact]
    public async Task Sample_TypedReadEditStbl()
    {
        using var stream = await SamplePackageAsync();
        await using var package = await DbpfPackage.OpenAsync(stream, leaveOpen: true);

        // Sample 4: typed access with an explicit registry, edit STBL, write back
        package.ResourceHandlers = WrapperRegistry();   // or leave unset to use ResourceHandlerRegistry.Shared
        var entry = package.Find(new ResourceKey(StblType, 0, 0x1))!;

        var stbl = (StblResource)await package.GetResourceAsync(entry);
        stbl[FnvHash.Fnv32("Hello")] = "Hello, edited";             // modify (indexer adds when missing)
        stbl.Add(FnvHash.Fnv32("Goodbye"), "Goodbye");              // add (does NOT check duplicates)
        stbl.Remove(FnvHash.Fnv32("Nope"));                         // false when absent
        package.ReplaceResource(entry, stbl);                       // marks the package dirty
        // end sample

        package.IsDirty.Should().BeTrue();
        var again = (StblResource)await package.GetResourceAsync(entry);
        again[FnvHash.Fnv32("Hello")].Should().Be("Hello, edited");
        again.Count.Should().Be(2);
    }

    [Fact]
    public async Task Sample_AlwaysDefaultGivesRawWrapper()
    {
        using var stream = await SamplePackageAsync();
        await using var package = await DbpfPackage.OpenAsync(stream, leaveOpen: true);
        package.ResourceHandlers = WrapperRegistry();
        var entry = package.Find(new ResourceKey(StblType, 0, 0x1))!;

        // Sample 5: bypass wrappers
        IResource raw = await package.GetResourceAsync(entry, alwaysDefault: true);   // DefaultResource, bytes only
        var registry = ResourceHandlerRegistry.CreateDefault();                         // Wrappers found automatically
        Type wrapper = registry.GetWrapperType(StblType);                               // StblResourceFactory
        IResource fresh = registry.CreateNewResource(new ResourceKey(StblType, 0, 0x42)); // empty STBL
        // end sample

        raw.Should().BeOfType<DefaultResource>();
        wrapper.Should().Be<StblResourceFactory>();
        fresh.Should().BeOfType<StblResource>();
    }

    [Fact]
    public async Task Sample_NameMapAndText()
    {
        using var package = DbpfPackage.CreateNew();
        package.ResourceHandlers = WrapperRegistry();

        // Sample 6: create NameMap and text resources from scratch
        var nmapKey = new ResourceKey(NameMapType, 0, 0x1);
        var nmap = new NameMapResource(nmapKey, ReadOnlyMemory<byte>.Empty);
        nmap.Add(FnvHash.Fnv64("my_object"), "my_object");          // instance hash -> readable name
        package.AddResource(nmapKey, nmap);

        var textKey = new ResourceKey(TuningType, 0, FnvHash.Fnv64("my_object"));
        var text = new TextResource(textKey, ReadOnlyMemory<byte>.Empty) { Text = "<I c=\"x\" i=\"y\" m=\"z\" n=\"my_object\" s=\"1\"/>" };
        var entry = package.AddResource(textKey, text)!;
        text.Text = text.Text.Replace("\"1\"", "\"2\"", StringComparison.Ordinal);
        package.ReplaceResource(entry, text);
        // end sample

        var back = (TextResource)await package.GetResourceAsync(entry);
        back.Text.Should().Contain("s=\"2\"");
        back.IsXml.Should().BeTrue();
    }

    [Fact]
    public async Task Sample_AddCompressDeleteDuplicates()
    {
        using var package = DbpfPackage.CreateNew();
        var key = new ResourceKey(TuningType, 0, 0x10);
        var data = System.Text.Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("<I n=\"repeat\"/>\n", 200)));

        // Sample 7: add (raw by default), compress, duplicates, delete
        IResourceIndexEntry entry = package.AddResource(key, data)!;                   // stored RAW
        package.SetCompression(entry, 0x5A42);                                          // zlib; compressed on save if smaller
        IResourceIndexEntry? dup = package.AddResource(key, data);                      // null: duplicate rejected
        IResourceIndexEntry dup2 = package.AddResource(key, data, rejectDuplicates: false)!; // allowed
        int n = package.FindAll(e => e.Key == key).Count();                             // 2 (Find returns the first)
        package.DeleteResource(dup2);                                                   // reversible until saved
        bool deleted = dup2.IsDeleted;                                                  // true; Find() now skips it
        ((IMutableResourceIndexEntry)dup2).IsDeleted = false;                           // undelete
        package.DeleteResource(dup2);
        // end sample

        dup.Should().BeNull();
        n.Should().Be(2);
        deleted.Should().BeTrue();

        using var ms = await SaveToMemoryAsync(package);
        await using var reloaded = await DbpfPackage.OpenAsync(ms, leaveOpen: true);
        reloaded.ResourceCount.Should().Be(1);                       // deleted entry dropped
        var e = reloaded.Find(key)!;
        e.IsCompressed.Should().BeTrue();
        e.CompressionType.Should().Be(0x5A42);
        (await reloaded.GetResourceDataAsync(e)).ToArray().Should().Equal(data);
    }

    [Fact]
    public async Task Sample_NewResourcesAreRawUnlessCompressed()
    {
        using var package = DbpfPackage.CreateNew();
        var data = new byte[4096];
        var entry = package.AddResource(new ResourceKey(1, 0, 1), data)!;
        using var ms = await SaveToMemoryAsync(package);
        await using var reloaded = await DbpfPackage.OpenAsync(ms, leaveOpen: true);
        reloaded.Find(new ResourceKey(1, 0, 1))!.IsCompressed.Should().BeFalse();
        entry.CompressionType.Should().Be(0);
    }

    [Fact]
    public async Task Sample_SaveInPlaceAndSaveAs()
    {
        var dir = Path.Combine(Path.GetTempPath(), "ts4tools-doc-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var path = Path.Combine(dir, "a.package");
            var copy = Path.Combine(dir, "b.package");
            using (var s = await SamplePackageAsync())
                await File.WriteAllBytesAsync(path, s.ToArray());

            // Sample 8: save in place / save as / save to stream
            await using (var package = await DbpfPackage.OpenAsync(path, readWrite: true))
            {
                var entry = package.Find(new ResourceKey(TuningType, 0, 0x2))!;
                package.ReplaceResource(entry, "<I n=\"changed\"/>"u8.ToArray());
                package.IsDirty.Should().BeTrue();

                await package.SaveAsAsync(copy);        // writes a new file; package stays dirty, original untouched
                package.IsDirty.Should().BeTrue();

                await package.SaveAsync();              // atomic temp-file save back to `path`; IsDirty -> false
                package.IsDirty.Should().BeFalse();

                using var ms = new MemoryStream();
                await package.SaveToStreamAsync(ms);    // never changes IsDirty
            }
            // end sample

            await using var check = await DbpfPackage.OpenAsync(copy);
            System.Text.Encoding.UTF8.GetString((await check.GetResourceDataAsync(check.Find(new ResourceKey(TuningType, 0, 0x2))!)).Span)
                .Should().Contain("changed");
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public async Task Sample_UntouchedResourcesCopiedVerbatim()
    {
        using var package = DbpfPackage.CreateNew();
        var big = System.Text.Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("abcabcabc", 500)));
        var e1 = package.AddResource(new ResourceKey(1, 0, 1), big)!;
        package.SetCompression(e1, 0x5A42);
        package.AddResource(new ResourceKey(2, 0, 2), new byte[] { 1, 2, 3 });
        using var first = await SaveToMemoryAsync(package);

        await using var reopened = await DbpfPackage.OpenAsync(first, leaveOpen: true);
        reopened.ReplaceResource(reopened.Find(new ResourceKey(2, 0, 2))!, new byte[] { 9 });
        using var second = await SaveToMemoryAsync(reopened);

        await using var final = await DbpfPackage.OpenAsync(second, leaveOpen: true);
        var f1 = final.Find(new ResourceKey(1, 0, 1))!;
        f1.IsCompressed.Should().BeTrue();                       // still compressed
        f1.FileSize.Should().Be(reopened.Find(new ResourceKey(1, 0, 1))!.FileSize); // same stored bytes
    }

    [Fact]
    public void Sample_Hashing()
    {
        // Sample 9: hashing (FNV-1, input lower-cased)
        uint stringKey = FnvHash.Fnv32("Mod:My_String");          // STBL key hash
        ulong instance = FnvHash.Fnv64("my_object_tuning");       // tuning/CAS instance (64-bit)
        uint viaClass = FNV32.GetHash("MY_OBJECT");               // same as FNV32 of "my_object"
        ulong clip = FNV64CLIP.GetHash("a2o_idle_x");             // CLIP instance (age/species prefix folded in)
        uint h24 = FnvHash.Fnv24("abc");
        // STBL instance convention: top byte = locale (0x00 = English), low 56 bits = hash of a name
        ulong stblInstance = (0x00UL << 56) | (FnvHash.Fnv64("my_stbl") & 0x00FFFFFFFFFFFFFF);
        // end sample

        FnvHash.Fnv32("ABC").Should().Be(FnvHash.Fnv32("abc"));
        viaClass.Should().Be(FnvHash.Fnv32("my_object"));
        FnvHash.Fnv64("ABC").Should().Be(FNV64.GetHash("abc"));
        FnvHash.Fnv32("").Should().Be(0x811C9DC5);
        (stringKey ^ (uint)instance ^ (uint)clip ^ h24 ^ (uint)stblInstance).Should().NotBe(0);
        stblInstance.Should().BeLessThan(1UL << 56);
    }

    [Fact]
    public void Sample_ExtList()
    {
        // Sample 10: tags and extensions
        string tag = ExtList.GetTag(0x220557DA);          // "STBL"
        string ext = ExtList.GetExtension(0x220557DA);    // ".stbl"
        string unknown = ExtList.GetExtension(0xDEADBEEF);// ".bnry"
        bool known = ExtList.IsKnown(0xDEADBEEF);        // false
        // end sample

        tag.Should().Be("STBL");
        ext.Should().Be(".stbl");
        unknown.Should().Be(".bnry");
        known.Should().BeFalse();
    }

    [Fact]
    public void Sample_LibrarySettings()
    {
        bool before = LibrarySettings.Checking;
        try
        {
            // Sample 11: library-wide switch; defaults (true) match legacy
            LibrarySettings.Checking = true;
            // end sample
        }
        finally { LibrarySettings.Checking = before; }
    }

    [Fact]
    public async Task Sample_VersionNewerThanLegacyFallsBackToRawBytes()
    {
        // Sample 12: wrappers may refuse data they cannot round-trip; fall back to raw bytes
        static async Task<ReadOnlyMemory<byte>> SafeBytesAsync(DbpfPackage package, IResourceIndexEntry entry)
        {
            try
            {
                var resource = await package.GetResourceAsync(entry);
                return resource.Data;
            }
            catch (UnsupportedResourceVersionException)   // newer than the legacy layout (e.g. CASP)
            {
                return await package.GetResourceDataAsync(entry);   // untouched raw bytes
            }
            catch (ResourceFormatException)               // malformed or unsupported content
            {
                return await package.GetResourceDataAsync(entry);
            }
        }
        // end sample

        using var package = DbpfPackage.CreateNew();
        package.ResourceHandlers = WrapperRegistry();
        var entry = package.AddResource(new ResourceKey(StblType, 0, 1), new byte[40])!; // bad STBL magic
        (await SafeBytesAsync(package, entry)).Length.Should().Be(40);
        new UnsupportedResourceVersionException("CASP", 99, 40).Should().BeAssignableTo<ResourceFormatException>();
    }

    [Fact]
    public async Task Sample_CopyBetweenPackagesAndMergeStbl()
    {
        using var src = DbpfPackage.CreateNew();
        src.AddResource(new ResourceKey(TuningType, 0, 5), "<I/>"u8.ToArray());
        var s1 = new StblResource(new ResourceKey(StblType, 0, 1), ReadOnlyMemory<byte>.Empty);
        s1.Add(1, "one");
        var s2 = new StblResource(new ResourceKey(StblType, 0, 2), ReadOnlyMemory<byte>.Empty);
        s2.Add(1, "uno (loses)");
        s2.Add(2, "two");
        src.AddResource(s1.Key, s1);
        src.AddResource(s2.Key, s2);
        using var dst = DbpfPackage.CreateNew();

        // Sample 13: copy resources between packages (decompressed bytes in, compressed again on request)
        foreach (var entry in src.FindAll(e => e.Key.ResourceType == TuningType).ToList())
        {
            var data = await src.GetResourceDataAsync(entry);
            var added = dst.AddResource(entry.Key, data, rejectDuplicates: true);
            if (added is not null) dst.SetCompression(added, 0x5A42);
        }

        // Sample 14: merge STBLs (first definition of a key hash wins)
        var merged = new StblResource(new ResourceKey(StblType, 0, 0x99), ReadOnlyMemory<byte>.Empty);
        foreach (var entry in src.FindAll(e => e.Key.ResourceType == StblType).ToList())
        {
            var stbl = new StblResource(entry.Key, await src.GetResourceDataAsync(entry));
            foreach (var s in stbl.Entries)
                if (!merged.ContainsKey(s.KeyHash))
                    merged.Add(s.KeyHash, s.Value, s.Flags);
        }
        dst.AddResource(merged.Key, merged);
        // end sample

        dst.ResourceCount.Should().Be(2);
        merged.Count.Should().Be(2);
        merged[1].Should().Be("one");
    }

    [Fact]
    public async Task Sample_BulkRenameInstances()
    {
        using var package = DbpfPackage.CreateNew();
        package.AddResource(new ResourceKey(1, 0, 0x100), new byte[] { 1 });
        package.AddResource(new ResourceKey(1, 0, 0x101), new byte[] { 2 });

        // Sample 15: bulk-rename (re-key) resources: IMutableResourceIndexEntry.Key is settable
        foreach (var entry in package.FindAll(e => e.Key.ResourceType == 1).ToList())
        {
            var mutable = (IMutableResourceIndexEntry)entry;
            mutable.Key = entry.Key with { Instance = entry.Key.Instance + 0x1000 };
        }
        // end sample

        package.Find(new ResourceKey(1, 0, 0x1100)).Should().NotBeNull();
        package.IsDirty.Should().BeTrue();
        using var ms = await SaveToMemoryAsync(package);
        await using var reloaded = await DbpfPackage.OpenAsync(ms, leaveOpen: true);
        (await reloaded.GetResourceDataAsync(reloaded.Find(new ResourceKey(1, 0, 0x1101))!)).ToArray().Should().Equal(2);
    }

    [Fact]
    public async Task Sample_ExtractImagesAsDds()
    {
        using var package = DbpfPackage.CreateNew();
        package.ResourceHandlers = WrapperRegistry();
        package.AddResource(new ResourceKey(DdsType, 0, 1), DdsImage.CreateSolid(0xFFFF0000, 8, 8).SaveToDds());
        var outDir = Path.Combine(Path.GetTempPath(), "ts4tools-doc-" + Guid.NewGuid().ToString("N"));
        try
        {
            // Sample 16: extract every DDS/DST image as a standard .dds file
            Directory.CreateDirectory(outDir);
            foreach (var entry in package.FindAll(e => e.Key.ResourceType == 0x00B2D882).ToList())
            {
                var image = (ImageResource)await package.GetResourceAsync(entry);
                byte[]? dds = image.ToDds();                 // DST is un-shuffled into plain DDS
                if (dds is null) continue;
                await File.WriteAllBytesAsync(Path.Combine(outDir, $"{entry.Key.Instance:X16}.dds"), dds);
                // For pixels: DdsImage.Load(dds).ToRgba32() gives R,G,B,A bytes to hand to any PNG encoder.
            }
            // end sample
            Directory.GetFiles(outDir, "*.dds").Should().HaveCount(1);
            DdsImage.Load(File.ReadAllBytes(Directory.GetFiles(outDir)[0])).Width.Should().Be(8);
        }
        finally { if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true); }
    }

    [Fact]
    public async Task Sample_FindKeyInFolderViaFileTable()
    {
        var dir = Path.Combine(Path.GetTempPath(), "ts4tools-doc-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            using (var p = DbpfPackage.CreateNew())
            {
                p.AddResource(new ResourceKey(TuningType, 0, 0x77), new byte[] { 1 });
                await p.SaveAsAsync(Path.Combine(dir, "x.package"));
            }

            // Sample 17: which package contains a key? (point CustomContentPath at any folder, e.g. a Mods dir)
            await using var table = new GameFileTable(new GameFolders())
            {
                CustomContentPath = dir,
                CustomContentEnabled = true,
            };
            SpecificResource? hit = await table.FindAsync(new ResourceKey(TuningType, 0, 0x77), FileTableKind.GameContent);
            Console.WriteLine(hit?.PathPackage.Path);       // the .package that holds it
            ReadOnlyMemory<byte>? data = hit is null ? null : await hit.GetDataAsync();
            // end sample

            hit.Should().NotBeNull();
            data!.Value.ToArray().Should().Equal(1);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void Sample_CatalogObjectAndTags()
    {
        // Sample 18: catalog objects and tag names
        var cobj = new CobjResource(new ResourceKey(CobjResource.TypeId, 0, 1), ReadOnlyMemory<byte>.Empty);
        cobj.CommonBlock.Price = 250;
        cobj.CommonBlock.NameHash = FnvHash.Fnv32("my_object_name");
        ReadOnlyMemory<byte> bytes = cobj.Data;
        var reparsed = new CobjResource(cobj.Key, bytes);
        CatalogTag tag = CatalogTagRegistry.FetchTag(0x0001);   // unknown ids give Value == "unknown"
        // end sample

        reparsed.CommonBlock.Price.Should().Be(250u);
        tag.Index.Should().Be(1u);
    }

    // Samples 19-20 are compile-checked only: they need real game data to run.
    private static void CompileOnly(SimDataResource simData, CasPartResource casp, RcolResource rcol)
    {
        // Sample 19: SimData field edit (call ReplaceResource afterwards)
        var table = simData.Tables[0];
        var field = table.Schema!.Fields.First(f => f.Name == "Price");
        uint price = table.GetUInt32(0, field);
        table.SetUInt32(0, field, price + 1);

        // Sample 20: CASP is read through plain properties
        string name = casp.Name;
        _ = (name, casp.BodyType, casp.AgeGender, rcol);
    }

    [Fact]
    public void CompileOnlySamplesExist() => ((Delegate)CompileOnly).Should().NotBeNull();
}
