using FluentAssertions;
using TS4Tools.Package;
using Xunit;

namespace TS4Tools.Core.Tests;

/// <summary>
/// Tests that mutate <see cref="LibrarySettings"/> run in a non-parallel collection.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class LibrarySettingsGroup
{
    public const string Name = "LibrarySettings";
}

/// <summary>
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi/Settings/Settings.cs lines 28-52 (all three settings default true)
/// - Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs lines 553, 607-620 (CheckHeader only if checking)
/// </summary>
[Collection(LibrarySettingsGroup.Name)]
public class LibrarySettingsTests
{
    [Fact]
    public void Defaults_MatchLegacy()
    {
        LibrarySettings.Checking.Should().BeTrue();
        LibrarySettings.AsBytesWorkaround.Should().BeTrue();
        LibrarySettings.IsTS4.Should().BeTrue();
    }

    [Fact]
    public async Task Checking_GatesHeaderMagicValidation()
    {
        using var package = DbpfPackage.CreateNew();
        using var ms = new MemoryStream();
        await package.SaveToStreamAsync(ms);
        var bytes = ms.ToArray();
        bytes[0] = (byte)'X';

        var act = async () => await DbpfPackage.OpenAsync(new MemoryStream(bytes));
        await act.Should().ThrowAsync<PackageFormatException>();

        LibrarySettings.Checking = false;
        try
        {
            await using var loaded = await DbpfPackage.OpenAsync(new MemoryStream(bytes));
            loaded.Magic.Span[0].Should().Be((byte)'X');
        }
        finally
        {
            LibrarySettings.Checking = true;
        }
    }
}
