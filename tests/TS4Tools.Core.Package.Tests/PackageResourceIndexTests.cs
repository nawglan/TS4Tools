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
/// Tests for the PackageResourceIndex class
/// </summary>
public class PackageResourceIndexTests
{
    [Fact]
    public void Constructor_Empty_CreatesEmptyIndex()
    {
        // Act
        var index = new PackageResourceIndex();
        
        // Assert
        index.Count.Should().Be(0);
        index.IndexType.Should().Be(0u);
    }
    
    [Fact]
    public void Constructor_WithIndexType_SetsIndexType()
    {
        // Arrange
        const uint indexType = 0x12345678u;
        
        // Act
        var index = new PackageResourceIndex(indexType);
        
        // Assert
        index.IndexType.Should().Be(indexType);
        index.Count.Should().Be(0);
    }
    
    [Fact]
    public void Add_NewEntry_AddsToIndex()
    {
        // Arrange
        var index = new PackageResourceIndex();
        var key = new ResourceKey(0x12345678u, 0x87654321u, 0x123456789ABCDEFul);
        var entry = new ResourceIndexEntry(key, 1024, 1024, 0);
        
        // Act
        var added = index.Add(entry);
        
        // Assert
        added.Should().BeTrue();
        index.Count.Should().Be(1);
        index.Contains(key).Should().BeTrue();
        index[key].Should().Be(entry);
    }
    
    [Fact]
    public void Add_DuplicateEntry_ReturnsFalse()
    {
        // Arrange
        var index = new PackageResourceIndex();
        var key = new ResourceKey(0x12345678u, 0x87654321u, 0x123456789ABCDEFul);
        var entry1 = new ResourceIndexEntry(key, 1024, 1024, 0);
        var entry2 = new ResourceIndexEntry(key, 2048, 2048, 0);
        
        index.Add(entry1);
        
        // Act
        var added = index.Add(entry2);
        
        // Assert
        added.Should().BeFalse();
        index.Count.Should().Be(1);
        index[key].Should().Be(entry1); // Original entry should remain
    }
    
    [Fact]
    public void Remove_ExistingEntry_RemovesFromIndex()
    {
        // Arrange
        var index = new PackageResourceIndex();
        var key = new ResourceKey(0x12345678u, 0x87654321u, 0x123456789ABCDEFul);
        var entry = new ResourceIndexEntry(key, 1024, 1024, 0);
        
        index.Add(entry);
        
        // Act
        var removed = index.Remove(key);
        
        // Assert
        removed.Should().BeTrue();
        index.Count.Should().Be(0);
        index.Contains(key).Should().BeFalse();
        index[key].Should().BeNull();
    }
    
    [Fact]
    public void Remove_NonExistentEntry_ReturnsFalse()
    {
        // Arrange
        var index = new PackageResourceIndex();
        var key = new ResourceKey(0x12345678u, 0x87654321u, 0x123456789ABCDEFul);
        
        // Act
        var removed = index.Remove(key);
        
        // Assert
        removed.Should().BeFalse();
        index.Count.Should().Be(0);
    }
    
    [Fact]
    public void GetByResourceType_ExistingType_ReturnsMatchingEntries()
    {
        // Arrange
        var index = new PackageResourceIndex();
        const uint targetType = 0x12345678u;
        
        var entry1 = new ResourceIndexEntry(new ResourceKey(targetType, 0x11111111u, 0x1ul), 1024, 1024, 0);
        var entry2 = new ResourceIndexEntry(new ResourceKey(targetType, 0x22222222u, 0x2ul), 2048, 2048, 0);
        var entry3 = new ResourceIndexEntry(new ResourceKey(0x87654321u, 0x33333333u, 0x3ul), 4096, 4096, 0);
        
        index.Add(entry1);
        index.Add(entry2);
        index.Add(entry3);
        
        // Act
        var result = index.GetByResourceType(targetType).ToList();
        
        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(entry1);
        result.Should().Contain(entry2);
        result.Should().NotContain(entry3);
    }
    
    [Fact]
    public void GetByResourceGroup_ExistingGroup_ReturnsMatchingEntries()
    {
        // Arrange
        var index = new PackageResourceIndex();
        const uint targetGroup = 0x87654321u;
        
        var entry1 = new ResourceIndexEntry(new ResourceKey(0x11111111u, targetGroup, 0x1ul), 1024, 1024, 0);
        var entry2 = new ResourceIndexEntry(new ResourceKey(0x22222222u, targetGroup, 0x2ul), 2048, 2048, 0);
        var entry3 = new ResourceIndexEntry(new ResourceKey(0x33333333u, 0x12345678u, 0x3ul), 4096, 4096, 0);
        
        index.Add(entry1);
        index.Add(entry2);
        index.Add(entry3);
        
        // Act
        var result = index.GetByResourceGroup(targetGroup).ToList();
        
        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(entry1);
        result.Should().Contain(entry2);
        result.Should().NotContain(entry3);
    }
    
    [Fact]
    public void TryGetValue_ExistingKey_ReturnsTrue()
    {
        // Arrange
        var index = new PackageResourceIndex();
        var key = new ResourceKey(0x12345678u, 0x87654321u, 0x123456789ABCDEFul);
        var entry = new ResourceIndexEntry(key, 1024, 1024, 0);
        
        index.Add(entry);
        
        // Act
        var found = index.TryGetValue(key, out var result);
        
        // Assert
        found.Should().BeTrue();
        result.Should().Be(entry);
    }
    
    [Fact]
    public void TryGetValue_NonExistentKey_ReturnsFalse()
    {
        // Arrange
        var index = new PackageResourceIndex();
        var key = new ResourceKey(0x12345678u, 0x87654321u, 0x123456789ABCDEFul);
        
        // Act
        var found = index.TryGetValue(key, out var result);
        
        // Assert
        found.Should().BeFalse();
        result.Should().BeNull();
    }
    
    [Fact]
    public void GetResourceKeys_WithEntries_ReturnsAllKeys()
    {
        // Arrange
        var index = new PackageResourceIndex();
        var key1 = new ResourceKey(0x12345678u, 0x87654321u, 0x1ul);
        var key2 = new ResourceKey(0x12345678u, 0x87654321u, 0x2ul);
        var key3 = new ResourceKey(0x12345678u, 0x87654321u, 0x3ul);
        
        index.Add(new ResourceIndexEntry(key1, 1024, 1024, 0));
        index.Add(new ResourceIndexEntry(key2, 2048, 2048, 0));
        index.Add(new ResourceIndexEntry(key3, 4096, 4096, 0));
        
        // Act
        var keys = index.GetResourceKeys().ToList();
        
        // Assert
        keys.Should().HaveCount(3);
        keys.Should().Contain(key1);
        keys.Should().Contain(key2);
        keys.Should().Contain(key3);
    }
    
    [Fact]
    public void Enumeration_WithEntries_ReturnsAllEntries()
    {
        // Arrange
        var index = new PackageResourceIndex();
        var entry1 = new ResourceIndexEntry(new ResourceKey(0x1u, 0x1u, 0x1ul), 1024, 1024, 0);
        var entry2 = new ResourceIndexEntry(new ResourceKey(0x2u, 0x2u, 0x2ul), 2048, 2048, 0);
        
        index.Add(entry1);
        index.Add(entry2);
        
        // Act
        var entries = index.ToList();
        
        // Assert
        entries.Should().HaveCount(2);
        entries.Should().Contain(entry1);
        entries.Should().Contain(entry2);
    }
}
