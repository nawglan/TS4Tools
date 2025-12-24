using FluentAssertions;
using Xunit;

namespace TS4Tools.Core.Tests;

public class ResourceKeyTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var key = new ResourceKey(0x12345678, 0x00000001, 0x00000000FFFFFFFF);

        key.ResourceType.Should().Be(0x12345678);
        key.ResourceGroup.Should().Be(0x00000001);
        key.Instance.Should().Be(0x00000000FFFFFFFF);
    }

    [Fact]
    public void Equality_SameValues_ReturnsTrue()
    {
        var key1 = new ResourceKey(1, 2, 3);
        var key2 = new ResourceKey(1, 2, 3);

        key1.Should().Be(key2);
        (key1 == key2).Should().BeTrue();
        (key1 != key2).Should().BeFalse();
    }

    [Fact]
    public void Equality_DifferentValues_ReturnsFalse()
    {
        var key1 = new ResourceKey(1, 2, 3);
        var key2 = new ResourceKey(1, 2, 4);

        key1.Should().NotBe(key2);
        (key1 == key2).Should().BeFalse();
        (key1 != key2).Should().BeTrue();
    }

    [Fact]
    public void CompareTo_SortsCorrectly()
    {
        var keys = new[]
        {
            new ResourceKey(2, 0, 0),
            new ResourceKey(1, 0, 0),
            new ResourceKey(1, 1, 0),
            new ResourceKey(1, 0, 1),
        };

        Array.Sort(keys);

        keys[0].Should().Be(new ResourceKey(1, 0, 0));
        keys[1].Should().Be(new ResourceKey(1, 0, 1));
        keys[2].Should().Be(new ResourceKey(1, 1, 0));
        keys[3].Should().Be(new ResourceKey(2, 0, 0));
    }

    [Fact]
    public void ComparisonOperators_WorkCorrectly()
    {
        var smaller = new ResourceKey(1, 0, 0);
        var larger = new ResourceKey(2, 0, 0);

        (smaller < larger).Should().BeTrue();
        (smaller <= larger).Should().BeTrue();
        (larger > smaller).Should().BeTrue();
        (larger >= smaller).Should().BeTrue();
        (smaller > larger).Should().BeFalse();
    }

    [Fact]
    public void ToString_FormatsCorrectly()
    {
        var key = new ResourceKey(0x00B2D882, 0x00000000, 0x00000000001234FF);

        var str = key.ToString();

        str.Should().Contain("00B2D882");
        str.Should().Contain("00000000");
        str.Should().Contain("00000000001234FF");
    }

    [Fact]
    public void GetHashCode_SameForEqualKeys()
    {
        var key1 = new ResourceKey(1, 2, 3);
        var key2 = new ResourceKey(1, 2, 3);

        key1.GetHashCode().Should().Be(key2.GetHashCode());
    }
}
