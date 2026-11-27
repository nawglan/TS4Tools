using FluentAssertions;
using TS4Tools.Package;
using TS4Tools.UI.ViewModels;
using TS4Tools.UI.Views.Dialogs;
using Xunit;

namespace TS4Tools.UI.Tests;

/// <summary>
/// Content search in the Search dialog.
/// </summary>
/// <remarks>
/// Legacy behaviour under test: legacy_references/Sims4Tools/s4pe/Tools/SearchForm.cs lines 279-291
/// (each listed IResourceIndexEntry is searched on its own).
/// </remarks>
public sealed class SearchWindowTests
{
    private static readonly ResourceKey Key = new(0x03B33DDF, 0, 0x1234);

    [Fact]
    public async Task ContentSearch_DuplicateKeys_SearchesEachRowsOwnEntry()
    {
        var package = DbpfPackage.CreateNew();
        var first = package.AddResource(Key, Encoding.ASCII.GetBytes("alpha"))!;
        var second = package.AddResource(Key, Encoding.ASCII.GetBytes("beta-needle"), rejectDuplicates: false)!;

        var firstRow = new ResourceItemViewModel(first);
        var secondRow = new ResourceItemViewModel(second);
        var pattern = Encoding.ASCII.GetBytes("needle");

        (await SearchWindow.ResourceContainsAsync(package, secondRow, pattern, CancellationToken.None))
            .Should().BeTrue("the second row's own data contains the pattern");
        (await SearchWindow.ResourceContainsAsync(package, firstRow, pattern, CancellationToken.None))
            .Should().BeFalse("the first row must not be matched through the second entry's key");
    }

    [Fact]
    public async Task ContentSearch_RowWithoutEntry_FallsBackToKeyLookup()
    {
        var package = DbpfPackage.CreateNew();
        package.AddResource(Key, Encoding.ASCII.GetBytes("xx needle xx"));
        var row = new ResourceItemViewModel(Key, 12);

        (await SearchWindow.ResourceContainsAsync(package, row, Encoding.ASCII.GetBytes("needle"), CancellationToken.None))
            .Should().BeTrue();
    }

    [Fact]
    public async Task ContentSearch_DeletedEntry_IsNotMatched()
    {
        var package = DbpfPackage.CreateNew();
        var entry = package.AddResource(Key, Encoding.ASCII.GetBytes("needle"))!;
        var row = new ResourceItemViewModel(entry);
        package.DeleteResource(entry);

        (await SearchWindow.ResourceContainsAsync(package, row, Encoding.ASCII.GetBytes("needle"), CancellationToken.None))
            .Should().BeFalse();
    }
}
