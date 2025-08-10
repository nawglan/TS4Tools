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
}
