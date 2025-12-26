using FluentAssertions;
using TS4Tools.UI.Services;
using Xunit;

namespace TS4Tools.UI.Tests;

/// <summary>
/// Tests for <see cref="HashNameService"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/NameMapResource.cs
/// - NameMap resources store FNV-64 hash to string mappings
/// - Used to provide human-readable names for instance IDs
/// - The service aggregates mappings from all NameMap resources in a package
/// </summary>
public class HashNameServiceTests
{
    [Fact]
    public void TryGetName_EmptyService_ReturnsNull()
    {
        // Arrange
        var service = new HashNameService();

        // Act
        var result = service.TryGetName(0x123456789ABCDEF0);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetDisplayName_NoMapping_ReturnsHexString()
    {
        // Arrange
        var service = new HashNameService();
        const ulong hash = 0x123456789ABCDEF0;

        // Act
        var result = service.GetDisplayName(hash);

        // Assert
        result.Should().Be("0x123456789ABCDEF0");
    }

    [Fact]
    public void GetDisplayName_ZeroHash_ReturnsFormattedHex()
    {
        // Arrange
        var service = new HashNameService();

        // Act
        var result = service.GetDisplayName(0);

        // Assert
        result.Should().Be("0x0000000000000000");
    }

    [Fact]
    public void GetDisplayNameWithHash_NoMapping_ReturnsHexString()
    {
        // Arrange
        var service = new HashNameService();
        const ulong hash = 0xABCDEF0123456789;

        // Act
        var result = service.GetDisplayNameWithHash(hash);

        // Assert
        result.Should().Be("0xABCDEF0123456789");
    }

    [Fact]
    public void Count_EmptyService_ReturnsZero()
    {
        // Arrange
        var service = new HashNameService();

        // Act & Assert
        service.Count.Should().Be(0);
    }

    [Fact]
    public void Clear_EmptyService_DoesNotThrow()
    {
        // Arrange
        var service = new HashNameService();

        // Act
        var act = () => service.Clear();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void NameContains_NoMapping_ReturnsFalse()
    {
        // Arrange
        var service = new HashNameService();
        const ulong hash = 0x123456789ABCDEF0;

        // Act
        var result = service.NameContains(hash, "test");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void NameContains_EmptySearch_ReturnsFalse()
    {
        // Arrange
        var service = new HashNameService();
        const ulong hash = 0x123456789ABCDEF0;

        // Act
        var result = service.NameContains(hash, "");

        // Assert
        result.Should().BeFalse();
    }
}
