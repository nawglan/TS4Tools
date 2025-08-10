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
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Resources;

namespace TS4Tools.Core.Resources.Tests;

public class ResourceFactoryBaseTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        // Arrange
        var supportedTypes = new[] { "0x12345678", "0xABCDEF12" };
        const int priority = 100;

        // Act
        var factory = new TestResourceFactory(supportedTypes, priority);

        // Assert
        factory.SupportedResourceTypes.Should().BeEquivalentTo(supportedTypes);
        factory.Priority.Should().Be(priority);
    }

    [Fact]
    public void SupportedResourceTypes_ShouldBeCaseInsensitive()
    {
        // Arrange
        var supportedTypes = new[] { "0x12345678", "0xabcdef12" };
        var factory = new TestResourceFactory(supportedTypes, 0);

        // Act & Assert
        factory.SupportedResourceTypes.Should().Contain("0x12345678");
        factory.SupportedResourceTypes.Should().Contain("0xABCDEF12");
    }

    [Fact]
    public void Constructor_WithDefaultPriority_ShouldUseZero()
    {
        // Arrange
        var supportedTypes = new[] { "0x12345678" };

        // Act
        var factory = new TestResourceFactory(supportedTypes);

        // Assert
        factory.Priority.Should().Be(0);
    }

    [Fact]
    public void ValidateApiVersion_WithValidVersion_ShouldNotThrow()
    {
        // Arrange
        var factory = new TestResourceFactory(new[] { "0x12345678" });

        // Act & Assert
        var act = () => factory.TestValidateApiVersion(1);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateApiVersion_WithInvalidVersion_ShouldThrowArgumentException()
    {
        // Arrange
        var factory = new TestResourceFactory(new[] { "0x12345678" });

        // Act & Assert
        var act = () => factory.TestValidateApiVersion(0);
        act.Should().Throw<ArgumentException>().WithParameterName("apiVersion");
    }

    [Fact]
    public async Task CreateMemoryStreamAsync_WithNullStream_ShouldReturnNull()
    {
        // Act
        var result = await TestResourceFactory.TestCreateMemoryStreamAsync(null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateMemoryStreamAsync_WithValidStream_ShouldCopyData()
    {
        // Arrange
        var originalData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var sourceStream = new MemoryStream(originalData);

        // Act
        var result = await TestResourceFactory.TestCreateMemoryStreamAsync(sourceStream);

        // Assert
        result.Should().NotBeNull();
        result!.ToArray().Should().BeEquivalentTo(originalData);
        result.Position.Should().Be(0); // Should be positioned at start
    }

    [Fact]
    public async Task CreateMemoryStreamAsync_ShouldCreateIndependentStream()
    {
        // Arrange
        var originalData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var sourceStream = new MemoryStream(originalData);
        sourceStream.Position = 2; // Position in middle

        // Act
        var result = await TestResourceFactory.TestCreateMemoryStreamAsync(sourceStream);

        // Assert
        result.Should().NotBeNull();
        result!.ToArray().Should().BeEquivalentTo(new byte[] { 0x03, 0x04 }); // Should copy from current position
        result.Position.Should().Be(0); // Result stream should be at start

        // Source stream should not be affected
        sourceStream.Position.Should().Be(4); // Should be at end after copying
    }

    // Test implementation of ResourceFactoryBase
    internal class TestResourceFactory : ResourceFactoryBase<IResource>
    {
        public TestResourceFactory(IEnumerable<string> supportedResourceTypes, int priority = 0)
            : base(supportedResourceTypes, priority)
        {
        }

        public override async Task<IResource> CreateResourceAsync(int apiVersion, Stream? stream = null, CancellationToken cancellationToken = default)
        {
            ValidateApiVersion(apiVersion);
            await Task.CompletedTask;
            return new DefaultResource(apiVersion, stream);
        }

        // Expose protected methods for testing
        public void TestValidateApiVersion(int apiVersion) => ValidateApiVersion(apiVersion);

        public static async Task<MemoryStream?> TestCreateMemoryStreamAsync(Stream? sourceStream, CancellationToken cancellationToken = default)
            => await CreateMemoryStreamAsync(sourceStream, cancellationToken);
    }
}
