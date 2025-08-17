/***************************************************************************
 *  Copyright (C) 2025 TS4Tools Project                                    *
 *                                                                         *
 *  This file is part of TS4Tools                                         *
 *                                                                         *
 *  TS4Tools is free software: you can redistribute it and/or modify      *
 *  it under the terms of the GNU General Public License as published by   *
 *  the Free Software Foundation, either version 3 of the License, or      *
 *  (at your option) any later version.                                    *
 *                                                                         *
 *  TS4Tools is distributed in the hope that it will be useful,           *
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of         *
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the          *
 *  GNU General Public License for more details.                           *
 *                                                                         *
 *  You should have received a copy of the GNU General Public License      *
 *  along with TS4Tools.  If not, see <http://www.gnu.org/licenses/>.     *
 ***************************************************************************/

namespace TS4Tools.Core.Package.Tests;

/// <summary>
/// Tests for the ResourceKey class
/// </summary>
public class ResourceKeyTests
{
    [Fact]
    public void Constructor_WithValidParameters_SetsProperties()
    {
        // Arrange
        const uint resourceType = 0x12345678u;
        const uint resourceGroup = 0x87654321u;
        const ulong instance = 0x123456789ABCDEFul;

        // Act
        var key = new ResourceKey(resourceType, resourceGroup, instance);

        // Assert
        key.ResourceType.Should().Be(resourceType);
        key.ResourceGroup.Should().Be(resourceGroup);
        key.Instance.Should().Be(instance);
    }

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        // Arrange
        var key1 = new ResourceKey(0x12345678u, 0x87654321u, 0x123456789ABCDEFul);
        var key2 = new ResourceKey(0x12345678u, 0x87654321u, 0x123456789ABCDEFul);

        // Act & Assert
        key1.Equals(key2).Should().BeTrue();
        key1.Equals((IResourceKey)key2).Should().BeTrue();
        (key1 == key2).Should().BeTrue();
        (key1 != key2).Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        // Arrange
        var key1 = new ResourceKey(0x12345678u, 0x87654321u, 0x123456789ABCDEFul);
        var key2 = new ResourceKey(0x12345679u, 0x87654321u, 0x123456789ABCDEFul);

        // Act & Assert
        key1.Equals(key2).Should().BeFalse();
        key1.Equals((IResourceKey)key2).Should().BeFalse();
        (key1 == key2).Should().BeFalse();
        (key1 != key2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_SameValues_ReturnsSameHash()
    {
        // Arrange
        var key1 = new ResourceKey(0x12345678u, 0x87654321u, 0x123456789ABCDEFul);
        var key2 = new ResourceKey(0x12345678u, 0x87654321u, 0x123456789ABCDEFul);

        // Act
        var hash1 = key1.GetHashCode();
        var hash2 = key2.GetHashCode();

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void CompareTo_DifferentTypes_ReturnsCorrectOrder()
    {
        // Arrange
        var key1 = new ResourceKey(0x12345678u, 0x87654321u, 0x123456789ABCDEFul);
        var key2 = new ResourceKey(0x12345679u, 0x87654321u, 0x123456789ABCDEFul);

        // Act
        var comparison = key1.CompareTo(key2);

        // Assert
        comparison.Should().BeLessThan(0);
        (key1 < key2).Should().BeTrue();
        (key1 <= key2).Should().BeTrue();
        (key1 > key2).Should().BeFalse();
        (key1 >= key2).Should().BeFalse();
    }

    [Fact]
    public void CompareTo_SameValues_ReturnsZero()
    {
        // Arrange
        var key1 = new ResourceKey(0x12345678u, 0x87654321u, 0x123456789ABCDEFul);
        var key2 = new ResourceKey(0x12345678u, 0x87654321u, 0x123456789ABCDEFul);

        // Act
        var comparison = key1.CompareTo(key2);

        // Assert
        comparison.Should().Be(0);
        (key1 <= key2).Should().BeTrue();
        (key1 >= key2).Should().BeTrue();
    }

    [Fact]
    public void CompareTo_Null_ReturnsPositive()
    {
        // Arrange
        var key = new ResourceKey(0x12345678u, 0x87654321u, 0x123456789ABCDEFul);

        // Act
        var comparison = key.CompareTo(null);

        // Assert
        comparison.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var key = new ResourceKey(0x12345678u, 0x87654321u, 0x123456789ABCDEFul);

        // Act
        var result = key.ToString();

        // Assert
        result.Should().Contain("12345678");
        result.Should().Contain("87654321");
        result.Should().Contain("123456789ABCDEF");
    }

    [Theory]
    [InlineData(0x12345678u, 0x87654321u, 0x123456789ABCDEFul)]
    [InlineData(0x00000000u, 0x00000000u, 0x0000000000000000ul)]
    [InlineData(0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFFFFFFFFFul)]
    public void Constructor_VariousValues_WorksCorrectly(uint type, uint group, ulong instance)
    {
        // Act
        var key = new ResourceKey(type, group, instance);

        // Assert
        key.ResourceType.Should().Be(type);
        key.ResourceGroup.Should().Be(group);
        key.Instance.Should().Be(instance);
    }
}
