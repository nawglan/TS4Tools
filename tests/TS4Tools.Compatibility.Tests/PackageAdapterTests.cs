// Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs lines 54-673 (open/save/add/replace/delete, GetResource),
// legacy_references/Sims4Tools/s4pi/Package/ResourceIndexEntry.cs lines 45-256 (index entry view)

using s4pi.Interfaces;
using s4pi.Package;
using s4pi.WrapperDealer;

namespace CompatTests;

public sealed class PackageAdapterTests : IDisposable
{
    private const uint RawType = 0x0000FACE; // no legacy or TS4Tools wrapper: default wrapper
    private readonly string dir = Path.Combine(Path.GetTempPath(), "ts4tools-compat-" + Guid.NewGuid().ToString("N"));

    public PackageAdapterTests() => Directory.CreateDirectory(dir);

    public void Dispose()
    {
        try { Directory.Delete(dir, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    private string PathFor(string name) => Path.Combine(dir, name);

    private static TGIBlock Key(uint type, ulong instance) => new(0, null, type, 0, instance);

    private static byte[] Data(int length, byte seed)
    {
        var b = new byte[length];
        for (int i = 0; i < b.Length; i++) b[i] = (byte)(seed + i % 7);
        return b;
    }

    private string CreatePackage(string name, params (TGIBlock Key, byte[] Data)[] resources)
    {
        var path = PathFor(name);
        using var pkg = Package.NewPackage(0);
        foreach (var (key, data) in resources)
            pkg.AddResource(key, new MemoryStream(data), true).Should().NotBeNull();
        pkg.SaveAs(path);
        return path;
    }

    [Fact]
    public void IndexEntryClone_IsIndependentCopy()
    {
        // Source: ResourceIndexEntry.cs lines 153, 198 (Clone copies the entry bytes)
        using var pkg = Package.NewPackage(0);
        var original = pkg.AddResource(Key(RawType, 1), new MemoryStream(Data(16, 3)), true)!;

        var copy = (IResourceIndexEntry)((AHandlerElement)original).Clone(null);
        copy.Instance = 2;
        copy.Compressed = 0x5A42;
        copy.IsDeleted = true;

        original.Instance.Should().Be(1);
        original.Compressed.Should().Be(0);
        original.IsDeleted.Should().BeFalse();
        pkg.GetResourceList.Should().ContainSingle().Which.Instance.Should().Be(1);

        // A clone still reads the original data, but is not an index entry of the package.
        var read = new MemoryStream();
        ((Package)pkg).GetResource(copy)!.CopyTo(read);
        read.ToArray().Should().Equal(Data(16, 3));
        copy.IsDeleted = false;
        var delete = () => pkg.DeleteResource(copy);
        delete.Should().Throw<ArgumentException>();
        pkg.GetResourceList.Should().ContainSingle().Which.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void NewPackage_HasLegacyHeader()
    {
        using var pkg = Package.NewPackage(0);

        Encoding.ASCII.GetString(pkg.Magic).Should().Be("DBPF");
        pkg.Major.Should().Be(2);
        pkg.Minor.Should().Be(1);
        pkg.Unused4.Should().Be(3);
        pkg.GetResourceList.Should().BeEmpty();
        pkg.RecommendedApiVersion.Should().Be(1);
    }

    [Fact]
    public void NewPackage_UnsupportedMajor_Throws()
    {
        var act = () => Package.NewPackage(0, 3);
        act.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void AddSaveOpen_RoundTripsResources()
    {
        var a = Data(100, 1);
        var b = Data(10, 50);
        var path = CreatePackage("roundtrip.package", (Key(RawType, 1), a), (Key(RawType, 2), b));

        using var pkg = Package.OpenPackage(0, path);

        pkg.Indexcount.Should().Be(2);
        pkg.GetResourceList.Should().HaveCount(2);
        var rie = pkg.Find(e => e.Instance == 2);
        rie.Should().NotBeNull();
        rie!.ResourceType.Should().Be(RawType);
        rie.Memsize.Should().Be(10);
        WrapperDealer.GetResource(0, pkg, rie).AsBytes.Should().Equal(b);
        pkg.FindAll(e => e.ResourceType == RawType).Should().HaveCount(2);
    }

    [Fact]
    public void AddResource_RejectDups_ReturnsNull()
    {
        using var pkg = Package.NewPackage(0);

        pkg.AddResource(Key(RawType, 1), new MemoryStream([1]), true).Should().NotBeNull();
        pkg.AddResource(Key(RawType, 1), new MemoryStream([2]), true).Should().BeNull();
        pkg.AddResource(Key(RawType, 1), new MemoryStream([3]), false).Should().NotBeNull("duplicates are allowed when not rejected");
        pkg.GetResourceList.Should().HaveCount(2);
    }

    [Fact]
    public void AddResource_WithNullStream_HasNoData()
    {
        using var pkg = Package.NewPackage(0);

        var rie = pkg.AddResource(Key(SampleResource.TypeId, 1), null, true)!;

        ((APackage)pkg).GetResource(rie).Should().BeNull("legacy returns no stream for a resource added without one");
        var res = WrapperDealer.GetResource(0, pkg, rie);
        res.Should().BeOfType<SampleResource>().Which.Version.Should().Be(1, "the wrapper creates a new resource");
    }

    [Fact]
    public void ReadOnlyPackage_CanBeEditedAndSavedAs_ButNotSavedInPlace()
    {
        var path = CreatePackage("readonly.package", (Key(RawType, 1), Data(20, 3)));
        var copy = PathFor("readonly-copy.package");

        using (var pkg = Package.OpenPackage(0, path))
        {
            var save = () => pkg.SavePackage();
            save.Should().Throw<InvalidOperationException>().WithMessage("Package is read-only");

            pkg.AddResource(Key(RawType, 2), new MemoryStream(Data(5, 9)), true).Should().NotBeNull();
            pkg.SaveAs(copy);
        }

        using var reopened = Package.OpenPackage(0, copy);
        reopened.GetResourceList.Should().HaveCount(2);
        using var original = Package.OpenPackage(0, path);
        original.GetResourceList.Should().HaveCount(1, "the read-only file is untouched");
    }

    [Fact]
    public void NewPackage_SavePackage_Throws()
    {
        using var pkg = Package.NewPackage(0);
        var act = () => pkg.SavePackage();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ReadWritePackage_ReplaceDeleteSave_RoundTrips()
    {
        var path = CreatePackage("readwrite.package", (Key(RawType, 1), Data(30, 1)), (Key(RawType, 2), Data(30, 2)));
        var replacement = Data(12, 77);

        using (var pkg = Package.OpenPackage(0, path, readwrite: true))
        {
            int invalidated = 0;
            pkg.ResourceIndexInvalidated += (_, _) => invalidated++;

            var first = pkg.Find(e => e.Instance == 1)!;
            var res = WrapperDealer.GetResource(0, pkg, first);
            var native = (TS4Tools.Resources.DefaultResource)((TS4ToolsResource)res).GetNativeResource();
            native.SetData(replacement);
            pkg.ReplaceResource(first, res);
            first.Memsize.Should().Be(12);

            var second = pkg.Find(e => e.Instance == 2)!;
            pkg.DeleteResource(second);
            second.IsDeleted.Should().BeTrue();
            pkg.Find(e => e.Instance == 2).Should().BeNull("Find skips deleted entries");
            pkg.GetResourceList.Should().HaveCount(2, "the index still holds the deleted entry until saved");

            pkg.SavePackage();
            invalidated.Should().BeGreaterThan(0);
            pkg.GetResourceList.Should().HaveCount(1, "saving drops deleted entries");
        }

        using var reopened = Package.OpenPackage(0, path);
        reopened.GetResourceList.Should().ContainSingle();
        var rie = reopened.Find(e => e.Instance == 1)!;
        WrapperDealer.GetResource(0, reopened, rie).AsBytes.Should().Equal(replacement);
    }

    [Fact]
    public void Compressed_RequestsCompressionOnSave()
    {
        var data = new byte[4096]; // highly compressible
        var path = PathFor("compressed.package");
        using (var pkg = Package.NewPackage(0))
        {
            var rie = pkg.AddResource(Key(RawType, 1), new MemoryStream(data), true)!;
            rie.Compressed.Should().Be(0, "new resources are stored uncompressed unless requested");
            rie.Compressed = 0x5A42;
            pkg.SaveAs(path);
        }

        using var reopened = Package.OpenPackage(0, path);
        var entry = reopened.GetResourceList[0];
        entry.Compressed.Should().Be(0x5A42);
        entry.Filesize.Should().BeLessThan(entry.Memsize);
        WrapperDealer.GetResource(0, reopened, entry).AsBytes.Should().Equal(data);
    }

    [Fact]
    public void IndexEntryView_ExposesLegacyFieldsAndEvents()
    {
        using var pkg = Package.NewPackage(0);
        var rie = pkg.AddResource(Key(0x220557DA, 0x0123456789ABCDEF), new MemoryStream([1, 2, 3]), true)!;
        int changed = 0;
        ((AResourceIndexEntry)rie).ResourceIndexEntryChanged += (_, _) => changed++;

        rie.Should().BeOfType<ResourceIndexEntry>();
        rie.ContentFields.Should().Contain(new[] { "ResourceType", "ResourceGroup", "Instance", "Chunkoffset", "Filesize", "Memsize", "Compressed", "Unknown2", "IsDeleted" });
        ((string)rie["ResourceType"]).Should().Be("0x220557DA");
        rie.ToString().Should().Be("0x220557DA-0x00000000-0x0123456789ABCDEF");
        rie.Chunkoffset.Should().Be(0xFFFFFFFF, "a new resource has not been written yet");
        rie.Unknown2.Should().Be(1);

        var bytes = new byte[36];
        rie.Stream.ReadExactly(bytes);
        BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4)).Should().Be(0x220557DA);
        BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(12)).Should().Be(0x01234567);
        BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(16)).Should().Be(0x89ABCDEF);
        BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(28)).Should().Be(3);

        rie.Compressed = 0xFFFF;
        rie.Instance = 7;
        changed.Should().Be(2);
        pkg.GetResourceList[0].Should().BeSameAs(rie, "entry views are stable");
        pkg.GetResourceList[0].Instance.Should().Be(7, "changes go through to the TS4Tools entry");

        var setOffset = () => rie.Chunkoffset = 0;
        setOffset.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void OpenPackage_NotAPackage_ThrowsInvalidData()
    {
        var path = PathFor("bogus.package");
        File.WriteAllBytes(path, new byte[200]);

        var act = () => Package.OpenPackage(0, path);

        act.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void GetNativePackage_SharesState()
    {
        using var pkg = (Package)Package.NewPackage(0);
        pkg.AddResource(Key(RawType, 1), new MemoryStream([9]), true);

        pkg.GetNativePackage().ResourceCount.Should().Be(1);
    }

    [Fact]
    public void Dispose_ClosesPackage()
    {
        var path = CreatePackage("dispose.package", (Key(RawType, 1), Data(4, 1)));
        var pkg = Package.OpenPackage(0, path);

        pkg.Dispose();

        var act = () => pkg.GetResourceList;
        act.Should().Throw<ObjectDisposedException>();
        File.Delete(path); // the file is no longer held open
        File.Exists(path).Should().BeFalse();
    }
}
