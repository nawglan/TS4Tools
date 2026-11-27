using FluentAssertions;
using TS4Tools.Compression;
using TS4Tools.Package;
using Xunit;

namespace TS4Tools.Core.Tests;

/// <summary>
/// Settable header fields, Unknown2 and the 0xFFE0 deletion-record label, used to rebuild a package
/// that matches an original (the CLI <c>pack</c> command).
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs lines 278-338 (header fields at 0x0C..0x5F)
///   and line 99 (SaveAs writes the in-memory header verbatim, then patches only the index fields, lines 158-166)
/// - Source: legacy_references/Sims4Tools/s4pi/Package/ResourceIndexEntry.cs lines 123-130 (Unknown2 get/set)
/// </summary>
public class PackageHeaderFieldTests
{
    private static async Task<DbpfPackage> SaveAndReopenAsync(DbpfPackage package)
    {
        var ms = new MemoryStream();
        await package.SaveToStreamAsync(ms);
        ms.Position = 0;
        return await DbpfPackage.OpenAsync(ms);
    }

    [Fact]
    public async Task HeaderSetters_SurviveSave()
    {
        using var package = DbpfPackage.CreateNew();
        var unused5 = new byte[DbpfPackage.Unused5Length];
        unused5[0] = 0xFF;
        unused5[1] = 0xFF;
        unused5[27] = 0x42;

        package.UserVersionMajor = 3;
        package.UserVersionMinor = 12000;
        package.Unused1 = 5;
        package.CreationTime = 1234;
        package.UpdatedTime = 5678;
        package.Unused2 = 9;
        package.Unused3 = 11;
        package.Unused4 = 3;
        package.Unused5 = unused5;
        package.IsDirty.Should().BeTrue();
        package.AddResource(new ResourceKey(1, 2, 3), new byte[] { 1, 2, 3 });

        await using var reopened = await SaveAndReopenAsync(package);

        reopened.UserVersionMajor.Should().Be(3);
        reopened.UserVersionMinor.Should().Be(12000);
        reopened.Unused1.Should().Be(5);
        reopened.CreationTime.Should().Be(1234);
        reopened.UpdatedTime.Should().Be(5678);
        reopened.Unused2.Should().Be(9);
        reopened.Unused3.Should().Be(11);
        reopened.Unused4.Should().Be(3);
        reopened.Unused5.ToArray().Should().Equal(unused5);
        reopened.ResourceCount.Should().Be(1);
    }

    [Fact]
    public void Unused5_WrongLength_Throws()
    {
        using var package = DbpfPackage.CreateNew();
        var act = () => package.Unused5 = new byte[27];
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task HeaderSetter_ReadOnlyPackage_Throws()
    {
        using var source = DbpfPackage.CreateNew();
        var ms = new MemoryStream();
        await source.SaveToStreamAsync(ms);
        await using var readOnly = await DbpfPackage.OpenAsync(new MemoryStream(ms.ToArray(), writable: false));

        var act = () => readOnly.UserVersionMinor = 1;
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task Unknown2_SetOnNewEntry_IsWritten()
    {
        using var package = DbpfPackage.CreateNew();
        var key = new ResourceKey(1, 2, 3);
        var entry = package.AddResource(key, new byte[] { 9 })!;
        DbpfPackage.GetUnknown2(entry).Should().Be(1);

        package.SetUnknown2(entry, 7);

        await using var reopened = await SaveAndReopenAsync(package);
        DbpfPackage.GetUnknown2(reopened.Find(key)!).Should().Be(7);
    }

    [Fact]
    public async Task EmptyEntryLabelledDeletedRecord_KeepsLabel()
    {
        using var package = DbpfPackage.CreateNew();
        var deletedKey = new ResourceKey(1, 2, 3);
        var emptyKey = new ResourceKey(1, 2, 4);
        package.SetCompression(package.AddResource(deletedKey)!, Compressor.DeletedRecordCompressionType);
        package.SetCompression(package.AddResource(emptyKey)!, Compressor.ZlibCompressionType);

        await using var reopened = await SaveAndReopenAsync(package);

        reopened.Find(deletedKey)!.CompressionType.Should().Be(Compressor.DeletedRecordCompressionType);
        reopened.Find(deletedKey)!.FileSize.Should().Be(0);
        reopened.Find(emptyKey)!.CompressionType.Should().Be(Compressor.NoCompression);
    }

    [Fact]
    public async Task NonEmptyEntryLabelledDeletedRecord_IsCompressedAsZlib()
    {
        using var package = DbpfPackage.CreateNew();
        var key = new ResourceKey(1, 2, 3);
        package.SetCompression(package.AddResource(key, new byte[1000])!, Compressor.DeletedRecordCompressionType);

        await using var reopened = await SaveAndReopenAsync(package);

        reopened.Find(key)!.CompressionType.Should().Be(Compressor.ZlibCompressionType);
        (await reopened.GetResourceDataAsync(reopened.Find(key)!)).Length.Should().Be(1000);
    }
}
