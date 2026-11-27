using FluentAssertions;
using TS4Tools.Wrappers.CatalogResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CatalogResource;

/// <summary>
/// Tests for <see cref="CatalogTagRegistry"/>, <see cref="CatalogTag"/> and <see cref="CatalogCompoundTag"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi.Resource.Commons/CatalogTags (CatalogTagRegistry.cs, Tag.cs, CompoundTag.cs)
///   and the data file S4_03B33DDF_00000000_D89CB9186B79ACB7.xml (47 categories, 1758 tags).
/// - Unknown ids return a Tag with that index and the default value "unknown".
/// - Category membership is by name prefix before the first '_'.
/// </summary>
public class CatalogTagRegistryTests
{
    [Fact]
    public void Listings_MatchLegacyDataFileCounts()
    {
        CatalogTagRegistry.Listings.Select(l => l.Name).Should().Equal("TagCategory", "Tag");
        CatalogTagRegistry.Listings[0].Elements.Should().HaveCount(47);
        CatalogTagRegistry.Listings[1].Elements.Should().HaveCount(1758);
        CatalogTagRegistry.AllCategories().Should().HaveCount(47);
        CatalogTagRegistry.AllTags().Should().HaveCount(1758);
    }

    [Theory]
    [InlineData(0x40u, "Mood_Optimism")]
    [InlineData(0x41u, "Color_Red")]
    [InlineData(0x44u, "Color_Blue")]
    [InlineData(0x45u, "Color_Green")]
    [InlineData(0x47u, "AgeAppropriate_YoungAdult")]
    [InlineData(0x49u, "Archetype_African")]
    public void FetchTag_ReturnsKnownNames(uint id, string name)
    {
        var tag = CatalogTagRegistry.FetchTag(id);

        tag.Value.Should().Be(name);
        tag.Index.Should().Be(id);
        CatalogTagRegistry.GetTagName(id).Should().Be(name);
        CatalogTagRegistry.TryGetTagName(id, out var found).Should().BeTrue();
        found.Should().Be(name);
    }

    [Theory]
    [InlineData(0x40u, "Mood")]
    [InlineData(0x41u, "Color")]
    [InlineData(0x42u, "Style")]
    [InlineData(0x43u, "Theme")]
    [InlineData(0x44u, "AgeAppropriate")]
    [InlineData(0x6Eu, "SkintoneBlend")]
    public void FetchCategory_ReturnsKnownNames(uint id, string name)
    {
        CatalogTagRegistry.FetchCategory(id).Value.Should().Be(name);
        CatalogTagRegistry.GetCategoryName(id).Should().Be(name);
    }

    [Fact]
    public void FetchTag_UnknownId_ReturnsUnknownWithIndex()
    {
        var tag = CatalogTagRegistry.FetchTag(0xFFFFFFF0);

        tag.Index.Should().Be(0xFFFFFFF0);
        tag.Value.Should().Be("unknown");
        CatalogTagRegistry.GetTagName(0xFFFFFFF0).Should().BeNull();
        CatalogTagRegistry.TryGetTagName(0xFFFFFFF0, out var name).Should().BeFalse();
        name.Should().BeNull();
        CatalogTagRegistry.FetchCategory(0xFFFFFFF0).Value.Should().Be("unknown");
        CatalogTagRegistry.GetCategoryName(0xFFFFFFF0).Should().BeNull();
    }

    [Fact]
    public void FetchTag_ZeroId_IsUnknownNotMissing()
    {
        CatalogTagRegistry.FetchTag(0).Value.Should().Be("unknown");
    }

    [Fact]
    public void AllTags_AreOrderedByIndex()
    {
        var indexes = CatalogTagRegistry.AllTags().Select(t => t.Index).ToList();

        indexes.Should().BeInAscendingOrder();
        indexes.Distinct().Should().HaveCount(indexes.Count);
        CatalogTagRegistry.AllCategories().Select(c => c.Index).Should().BeInAscendingOrder();
    }

    [Fact]
    public void FetchTagsForCategory_MatchesPrefixCaseInsensitively()
    {
        var colours = CatalogTagRegistry.FetchTagsForCategory("color").ToList();

        colours.Should().NotBeEmpty();
        colours.Should().OnlyContain(t => t.Value.StartsWith("Color_", StringComparison.Ordinal));
        colours.Select(t => t.Value).Should().Contain(["Color_Red", "Color_Blue", "Color_Green"]);
        CatalogTagRegistry.FetchTagsForCategory("NoSuchCategory").Should().BeEmpty();
    }

    [Fact]
    public void FetchTagsForCategory_PrefixMustEndWithUnderscore()
    {
        // "Col" must not match "Color_*": the legacy prefix is category + "_"
        CatalogTagRegistry.FetchTagsForCategory("Col").Should().BeEmpty();
    }

    [Fact]
    public void GetCategoryFor_UsesNamePrefix()
    {
        var category = CatalogTagRegistry.GetCategoryFor(CatalogTagRegistry.FetchTag(0x41));

        category.Value.Should().Be("Color");
        category.Index.Should().Be(0x41u);
    }

    [Fact]
    public void GetCategoryFor_NoUnderscoreOrUnknownPrefix_ReturnsDummy()
    {
        var dummy = CatalogTagRegistry.GetCategoryFor(new CatalogTag(1, "NoUnderscore"));
        dummy.Index.Should().Be(uint.MaxValue);
        dummy.Value.Should().BeEmpty();

        var unknown = CatalogTagRegistry.GetCategoryFor(new CatalogTag(2, "Zzz_Thing"));
        unknown.Index.Should().Be(uint.MaxValue);
        unknown.Value.Should().Be("Zzz");
    }

    [Fact]
    public void GetCategoryFor_NullTag_Throws()
    {
        ((Action)(() => CatalogTagRegistry.GetCategoryFor(null!))).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AllCategoriesWithDummiesForUnpairedTags_AddsDummiesOnlyForMissingPrefixes()
    {
        var all = CatalogTagRegistry.AllCategoriesWithDummiesForUnpairedTags().ToList();

        all.Take(47).Select(c => c.Index).Should().NotContain(uint.MaxValue);
        all.Skip(47).Should().OnlyContain(c => c.Index == uint.MaxValue);
        all.Skip(47).Select(c => c.Value).Should().OnlyHaveUniqueItems();
        foreach (var dummy in all.Skip(47))
            CatalogTagRegistry.AllCategories().Should().NotContain(c => c.Value.Equals(dummy.Value, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EveryTagWithAPrefix_ResolvesToACategoryOrDummy()
    {
        foreach (var tag in CatalogTagRegistry.AllTags().Where(t => t.Value.Contains('_', StringComparison.Ordinal)))
        {
            var category = CatalogTagRegistry.GetCategoryFor(tag);
            tag.Value.Should().StartWith(category.Value + "_");
        }
    }

    // ---- Tag ----

    [Fact]
    public void Tag_Defaults_AndConversions()
    {
        var tag = new CatalogTag();

        tag.Value.Should().Be("unknown");
        tag.Index.Should().Be(0u);

        var big = new CatalogTag(0x12345u, "X");
        big.ToUInt32().Should().Be(0x12345u);
        big.ToUInt16().Should().Be(0x2345);
    }

    [Fact]
    public void Tag_ToString_UsesHexIndexAndValue()
    {
        new CatalogTag(0x41, "Color_Red").ToString().Should().Be("0x0041 Color_Red");
        new CatalogTag(0x12345, "X").ToString().Should().Be("0x12345 X");
    }

    [Fact]
    public void Tag_EqualityIsByIndexOnly()
    {
        var a = new CatalogTag(5, "One");
        var b = new CatalogTag(5, "Different");
        var c = new CatalogTag(6, "One");

        (a == b).Should().BeTrue();
        a.Equals(b).Should().BeTrue();
        a.Equals((object)b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
        (a != c).Should().BeTrue();
        CatalogTag? none = null;
        CatalogTag? other = null;
        a.Equals(none).Should().BeFalse();
        a.Equals("5").Should().BeFalse();
        (a == none).Should().BeFalse();
        (none == other).Should().BeTrue();
        (none == a).Should().BeFalse();
    }

    // ---- CompoundTag ----

    [Fact]
    public void CompoundTag_UnsetParts_AreUnknownTags()
    {
        var compound = new CatalogCompoundTag();

        compound.Category.Value.Should().Be("unknown");
        compound.Value.Value.Should().Be("unknown");
        compound.ToString().Should().Be("0x0000 unknown - 0x0000 unknown");
    }

    [Fact]
    public void CompoundTag_EqualityComparesCategoryAndValue()
    {
        var a = new CatalogCompoundTag { Category = new CatalogTag(0x41, "Color"), Value = new CatalogTag(0x41, "Color_Red") };
        var b = new CatalogCompoundTag { Category = new CatalogTag(0x41, "X"), Value = new CatalogTag(0x41, "Y") };
        var c = new CatalogCompoundTag { Category = new CatalogTag(0x41, "Color"), Value = new CatalogTag(0x44, "Color_Blue") };

        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
        (a != c).Should().BeTrue();
        a.Equals((object)c).Should().BeFalse();
        CatalogCompoundTag? none = null;
        CatalogCompoundTag? other = null;
        a.Equals(none).Should().BeFalse();
        (none == other).Should().BeTrue();
        (a == none).Should().BeFalse();
        a.ToString().Should().Be("0x0041 Color - 0x0041 Color_Red");
    }
}
