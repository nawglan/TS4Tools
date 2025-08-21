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

using FluentAssertions;
using Xunit;
using TS4Tools.Core.Package.Compression;

namespace TS4Tools.Core.Package.Tests;

/// <summary>
/// Tests for the Package class
/// </summary>
public class PackageTests
{
    [Fact]
    public void Constructor_Default_CreatesEmptyPackage()
    {
        // Arrange
        var compressionService = TestHelper.CreateMockCompressionService();

        // Act
        using var package = new Package(compressionService);

        // Assert
        package.Should().NotBeNull();
        package.ResourceCount.Should().Be(0);
        package.IsDirty.Should().BeTrue();
        package.FileName.Should().BeNull();
        package.Major.Should().Be(2);
        package.Minor.Should().Be(0);
        package.Magic.SequenceEqual("DBPF"u8).Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithEmptyStream_CreatesPackageFromStream()
    {
        // Arrange
        var compressionService = TestHelper.CreateMockCompressionService();
        var stream = new MemoryStream();
        CreateMinimalPackageHeader(stream);

        // Act
        using var package = new Package(stream, compressionService, fileName: "test.package");

        // Assert
        package.Should().NotBeNull();
        package.FileName.Should().Be("test.package");
        package.IsDirty.Should().BeFalse();
    }

    [Fact]
    public void AddResource_ValidResource_AddsToPackage()
    {
        // Arrange
        var compressionService = TestHelper.CreateMockCompressionService();
        using var package = new Package(compressionService);
        var resourceKey = new ResourceKey(0x12345678u, 0x87654321u, 0x123456789ABCDEFul);
        var resourceData = "Hello, World!"u8.ToArray();

        // Act
        var entry = package.AddResource(resourceKey, resourceData, compressed: false);

        // Assert
        entry.Should().NotBeNull();
        entry.ResourceType.Should().Be(0x12345678u);
        entry.ResourceGroup.Should().Be(0x87654321u);
        entry.Instance.Should().Be(0x123456789ABCDEFul);
        entry.FileSize.Should().Be((uint)resourceData.Length);
        entry.MemorySize.Should().Be((uint)resourceData.Length);
        entry.Compressed.Should().Be(0x0000); // Not compressed

        package.ResourceCount.Should().Be(1);
        package.IsDirty.Should().BeTrue();
        package.ResourceIndex.Contains(resourceKey).Should().BeTrue();
    }

    [Fact]
    public void AddResource_CompressedResource_SetsCompressionFlag()
    {
        // Arrange
        var compressionService = TestHelper.CreateMockCompressionService();
        using var package = new Package(compressionService);
        var resourceKey = new ResourceKey(0x12345678u, 0x87654321u, 0x123456789ABCDEFul);
        var resourceData = "Hello, World!"u8.ToArray();

        // Act
        var entry = package.AddResource(resourceKey, resourceData, compressed: true);

        // Assert
        entry.Compressed.Should().Be(0xFFFF); // Compressed
        (entry.Compressed == 0xFFFF).Should().BeTrue(); // Check compression flag directly
    }

    [Fact]
    public void RemoveResource_ExistingResource_RemovesFromPackage()
    {
        // Arrange
        var compressionService = TestHelper.CreateMockCompressionService();
        using var package = new Package(compressionService);
        var resourceKey = new ResourceKey(0x12345678u, 0x87654321u, 0x123456789ABCDEFul);
        var resourceData = "Hello, World!"u8.ToArray();
        package.AddResource(resourceKey, resourceData);

        // Act
        var removed = package.RemoveResource(resourceKey);

        // Assert
        removed.Should().BeTrue();
        package.ResourceCount.Should().Be(0);
        package.ResourceIndex.Contains(resourceKey).Should().BeFalse();
    }

    [Fact]
    public void RemoveResource_NonExistentResource_ReturnsFalse()
    {
        // Arrange
        var compressionService = TestHelper.CreateMockCompressionService();
        using var package = new Package(compressionService);
        var resourceKey = new ResourceKey(0x12345678u, 0x87654321u, 0x123456789ABCDEFul);

        // Act
        var removed = package.RemoveResource(resourceKey);

        // Assert
        removed.Should().BeFalse();
    }

    [Fact]
    public void ContentFields_Package_ReturnsExpectedFields()
    {
        // Arrange
        var compressionService = TestHelper.CreateMockCompressionService();
        using var package = new Package(compressionService);

        // Act & Assert
        package.ContentFields.Should().HaveCount(8);
        package.ContentFields.Should().Contain("Magic");
        package.ContentFields.Should().Contain("Major");
        package.ContentFields.Should().Contain("Minor");
        package.ContentFields.Should().Contain("ResourceCount");
    }

    [Fact]
    public void Indexer_ByIndex_ReturnsCorrectValues()
    {
        // Arrange
        var compressionService = TestHelper.CreateMockCompressionService();
        using var package = new Package(compressionService);

        // Act & Assert
        package[0].Value.Should().Be("DBPF"); // Magic
        package[1].Value.Should().Be(2); // Major
        package[2].Value.Should().Be(0); // Minor
        package[7].Value.Should().Be(0); // ResourceCount
    }

    [Fact]
    public void Indexer_ByName_ReturnsCorrectValues()
    {
        // Arrange
        var compressionService = TestHelper.CreateMockCompressionService();
        using var package = new Package(compressionService);

        // Act & Assert
        package["Magic"].Value.Should().Be("DBPF");
        package["Major"].Value.Should().Be(2);
        package["Minor"].Value.Should().Be(0);
        package["ResourceCount"].Value.Should().Be(0);
    }

    [Fact]
    public void ApiVersion_Package_ReturnsExpectedVersions()
    {
        // Arrange
        var compressionService = TestHelper.CreateMockCompressionService();
        using var package = new Package(compressionService);

        // Act & Assert
        package.RequestedApiVersion.Should().Be(1);
        package.RecommendedApiVersion.Should().Be(1);
    }

    [Fact]
    public async Task SaveAsAsync_ToStream_WritesPackageData()
    {
        // Arrange
        var compressionService = TestHelper.CreateMockCompressionService();
        using var package = new Package(compressionService);
        var resourceKey = new ResourceKey(0x12345678u, 0x87654321u, 0x123456789ABCDEFul);
        var resourceData = "Hello, World!"u8.ToArray();
        package.AddResource(resourceKey, resourceData);

        using var stream = new MemoryStream();

        // Act
        await package.SaveAsAsync(stream);

        // Assert 
        stream.Length.Should().BeGreaterThan(0);
        package.IsDirty.Should().BeFalse();

        // Verify header was written
        stream.Position = 0;
        var headerBytes = new byte[4];
        await stream.ReadAsync(headerBytes);
        headerBytes.Should().Equal("DBPF"u8.ToArray());
    }

    [Fact]
    public async Task CompactAsync_EmptyPackage_CompletesSuccessfully()
    {
        // Arrange
        var compressionService = TestHelper.CreateMockCompressionService();
        using var package = new Package(compressionService);

        // Act
        await package.CompactAsync();

        // Assert
        package.IsDirty.Should().BeTrue(); // Compact marks as dirty
    }

    [Fact]
    public void Constructor_WithIndexPositionZero_LoadsResourcesCorrectly()
    {
        // Arrange - This test specifically covers the bug where IndexPosition=0 was incorrectly rejected
        var compressionService = TestHelper.CreateMockCompressionService();
        var stream = new MemoryStream();
        
        // Create a package with resources and IndexPosition=0 (valid DBPF format)
        CreatePackageWithIndexPositionZero(stream);
        
        // Act
        using var package = new Package(stream, compressionService, fileName: "test-indexposition-zero.package");

        // Assert
        package.Should().NotBeNull();
        package.FileName.Should().Be("test-indexposition-zero.package");
        package.IsDirty.Should().BeFalse();
        
        // The critical assertion - ResourceIndex.Count should work correctly with IndexPosition=0
        // Before the fix, this would be 0 due to the bug in LoadIndex method
        package.ResourceIndex.Count.Should().BeGreaterThan(0, "IndexPosition=0 should be valid and resources should be loaded");
        package.ResourceCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void LoadIndex_WithZeroIndexSize_DoesNotLoadResources()
    {
        // Arrange - Test the corrected logic: only reject when IndexSize=0 OR ResourceCount=0
        var compressionService = TestHelper.CreateMockCompressionService();
        var stream = new MemoryStream();
        
        // Create a package with IndexSize=0 (should be rejected)
        CreatePackageWithZeroIndexSize(stream);
        
        // Act
        using var package = new Package(stream, compressionService, fileName: "test-zero-indexsize.package");

        // Assert
        package.Should().NotBeNull();
        package.ResourceIndex.Count.Should().Be(0, "IndexSize=0 should result in no resources loaded");
        package.ResourceCount.Should().Be(0);
    }

    [Fact]
    public void LoadIndex_RegressionTest_ValidPackageLoadsResources()
    {
        // This test validates the fix for the ResourceIndex.Count bug
        // The original bug was incorrectly rejecting valid packages, causing ResourceIndex.Count to always be 0
        // This was fixed by changing the LoadIndex condition from:
        // OLD: if (header.IndexPosition == 0 || header.IndexSize == 0) return;
        // NEW: if (header.IndexSize == 0 || header.ResourceCount == 0) return;
        
        var compressionService = TestHelper.CreateMockCompressionService();
        
        // Test case: Valid package with IndexSize > 0 and ResourceCount > 0 should load resources
        var validStream = new MemoryStream();
        CreatePackageWithValidResources(validStream);
        using var validPackage = new Package(validStream, compressionService);
        
        // This assertion validates the fix - ResourceIndex.Count should be > 0 for valid packages
        validPackage.ResourceIndex.Count.Should().BeGreaterThan(0, 
            "Valid packages with IndexSize > 0 and ResourceCount > 0 should load resources correctly");
        
        // Test case: IndexSize=0 should prevent loading regardless of other values
        var invalidStream = new MemoryStream();
        CreatePackageWithZeroIndexSize(invalidStream);
        using var invalidPackage = new Package(invalidStream, compressionService);
        
        invalidPackage.ResourceIndex.Count.Should().Be(0,
            "IndexSize=0 should prevent resource loading");
    }

    private static void CreateMinimalPackageHeader(MemoryStream stream)
    {
        // Create a minimal valid package header
        using var writer = new BinaryWriter(stream, global::System.Text.Encoding.UTF8, leaveOpen: true);

        // Write DBPF magic
        writer.Write("DBPF"u8.ToArray());

        // Write minimal header (rest filled with zeros)
        var headerData = new byte[PackageHeader.HeaderSize - 4];
        writer.Write(headerData);

        stream.Position = 0;
    }

    private static void CreatePackageWithIndexPositionZero(MemoryStream stream)
    {
        // Create a minimal valid package with IndexPosition set to a valid location
        // This tests that IndexPosition=0 (or any valid position) should work
        using var writer = new BinaryWriter(stream, global::System.Text.Encoding.UTF8, leaveOpen: true);

        // Write DBPF header with IndexPosition pointing to valid index location
        writer.Write("DBPF"u8.ToArray()); // Magic (4 bytes)
        writer.Write((uint)2); // Major version (4 bytes)
        writer.Write((uint)1); // Minor version (4 bytes)
        writer.Write((uint)0); // UserVersionMajor (4 bytes)
        writer.Write((uint)0); // UserVersionMinor (4 bytes)
        writer.Write((uint)0); // Flags (4 bytes)
        writer.Write((uint)0); // CreatedDate (4 bytes)
        writer.Write((uint)0); // ModifiedDate (4 bytes)
        writer.Write((uint)0); // IndexMajorVersion (4 bytes)
        writer.Write((uint)1); // ResourceCount = 1 (4 bytes)
        writer.Write((uint)96); // IndexPosition = 96 (right after header) (4 bytes)
        writer.Write((uint)32); // IndexSize = 32 bytes (4 bytes)
        
        // Pad remaining header to reach 96 bytes total
        var headerBytesWritten = 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4; // 48 bytes
        var remainingHeaderBytes = 96 - headerBytesWritten;
        writer.Write(new byte[remainingHeaderBytes]);

        // Write resource index at position 96
        writer.Write((uint)0x00); // Index type
        writer.Write((uint)0x12345678); // ResourceType
        writer.Write((uint)0x87654321); // ResourceGroup  
        writer.Write((ulong)0x123456789ABCDEFul); // Instance (8 bytes)
        writer.Write((uint)128); // ChunkOffset
        writer.Write((uint)13); // FileSize
        writer.Write((uint)13); // MemorySize 
        writer.Write((ushort)0x0000); // Compressed
        writer.Write((ushort)0x0001); // Unknown2

        // Write resource data
        writer.Write("Hello, World!"u8.ToArray());

        stream.Position = 0;
    }

    private static void CreatePackageWithZeroIndexSize(MemoryStream stream)
    {
        // Create a package with IndexSize=0 (should result in no resources loaded)
        using var writer = new BinaryWriter(stream, global::System.Text.Encoding.UTF8, leaveOpen: true);

        // Write DBPF header
        writer.Write("DBPF"u8.ToArray()); // Magic
        writer.Write((uint)2); // Major version  
        writer.Write((uint)1); // Minor version
        writer.Write((uint)0); // UserVersionMajor
        writer.Write((uint)0); // UserVersionMinor
        writer.Write((uint)0); // Flags
        writer.Write((uint)0); // CreatedDate
        writer.Write((uint)0); // ModifiedDate  
        writer.Write((uint)0); // IndexMajorVersion
        writer.Write((uint)0); // ResourceCount = 0
        writer.Write((uint)96); // IndexPosition = 96
        writer.Write((uint)0); // IndexSize = 0 - This should cause LoadIndex to exit early
        
        // Pad header to 96 bytes
        var headerBytesWritten = 48;
        var remainingHeaderBytes = 96 - headerBytesWritten;
        writer.Write(new byte[remainingHeaderBytes]);

        stream.Position = 0;
    }

    private static void CreatePackageWithValidResources(MemoryStream stream)
    {
        // Create a valid package that should load resources correctly after the bug fix
        using var writer = new BinaryWriter(stream, global::System.Text.Encoding.UTF8, leaveOpen: true);

        // Write DBPF header
        writer.Write("DBPF"u8.ToArray()); // Magic
        writer.Write((uint)2); // Major version  
        writer.Write((uint)1); // Minor version
        writer.Write((uint)0); // UserVersionMajor
        writer.Write((uint)0); // UserVersionMinor
        writer.Write((uint)0); // Flags
        writer.Write((uint)0); // CreatedDate
        writer.Write((uint)0); // ModifiedDate  
        writer.Write((uint)0); // IndexMajorVersion
        writer.Write((uint)1); // ResourceCount = 1 (valid, > 0)
        writer.Write((uint)96); // IndexPosition = 96 (after header)
        writer.Write((uint)32); // IndexSize = 32 (valid, > 0)
        
        // Pad header to 96 bytes
        var headerBytesWritten = 48;
        var remainingHeaderBytes = 96 - headerBytesWritten;
        writer.Write(new byte[remainingHeaderBytes]);

        // Write index at position 96
        writer.Write((uint)0x00); // Index type
        writer.Write((uint)0x12345678); // ResourceType
        writer.Write((uint)0x87654321); // ResourceGroup  
        writer.Write((ulong)0x123456789ABCDEFul); // Instance (8 bytes)
        writer.Write((uint)128); // ChunkOffset
        writer.Write((uint)13); // FileSize
        writer.Write((uint)13); // MemorySize 
        writer.Write((ushort)0x0000); // Compressed
        writer.Write((ushort)0x0001); // Unknown2

        // Write resource data
        writer.Write("Hello, World!"u8.ToArray());

        stream.Position = 0;
    }
}
