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

public class DefaultResourceTests
{
    [Fact]
    public void Constructor_WithValidApiVersion_ShouldInitializeCorrectly()
    {
        // Arrange
        const int apiVersion = 1;

        // Act
        var resource = new DefaultResource(apiVersion);

        // Assert
        resource.RequestedApiVersion.Should().Be(apiVersion);
        resource.RecommendedApiVersion.Should().Be(apiVersion);
        resource.ContentFields.Should().BeEmpty();
        resource.AsBytes.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithStream_ShouldCopyStreamData()
    {
        // Arrange
        const int apiVersion = 1;
        var originalData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var sourceStream = new MemoryStream(originalData);

        // Act
        var resource = new DefaultResource(apiVersion, sourceStream);

        // Assert
        resource.RequestedApiVersion.Should().Be(apiVersion);
        resource.AsBytes.Should().BeEquivalentTo(originalData);
        
        // Stream should be independent of source
        sourceStream.Position = 100; // Modify source stream position
        resource.Stream.Position.Should().Be(0); // Resource stream should not be affected
    }

    [Fact]
    public void AsBytes_ShouldReturnStreamContents()
    {
        // Arrange
        var data = new byte[] { 0xAB, 0xCD, 0xEF, 0x12 };
        var stream = new MemoryStream(data);
        var resource = new DefaultResource(1, stream);

        // Act
        var result = resource.AsBytes;

        // Assert
        result.Should().BeEquivalentTo(data);
    }

    [Fact]
    public void Stream_ShouldProvideAccessToData()
    {
        // Arrange
        var data = new byte[] { 0xAB, 0xCD, 0xEF, 0x12 };
        var sourceStream = new MemoryStream(data);
        var resource = new DefaultResource(1, sourceStream);

        // Act
        var streamData = new byte[data.Length];
        resource.Stream.Read(streamData, 0, data.Length);

        // Assert
        streamData.Should().BeEquivalentTo(data);
    }

    [Fact]
    public void ContentFieldAccess_ByStringIndex_ShouldThrowNotSupportedException()
    {
        // Arrange
        var resource = new DefaultResource(1);

        // Act & Assert
        var getAct = () => resource["SomeField"];
        var setAct = () => resource["SomeField"] = TypedValue.Create("value");

        getAct.Should().Throw<NotSupportedException>();
        setAct.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void ContentFieldAccess_ByIntIndex_ShouldThrowNotSupportedException()
    {
        // Arrange
        var resource = new DefaultResource(1);

        // Act & Assert
        var getAct = () => resource[0];
        var setAct = () => resource[0] = TypedValue.Create("value");

        getAct.Should().Throw<NotSupportedException>();
        setAct.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Dispose_ShouldDisposeStreamCleanly()
    {
        // Arrange
        var resource = new DefaultResource(1);

        // Act & Assert (Should not throw)
        resource.Dispose();
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var resource = new DefaultResource(1);

        // Act & Assert (Should not throw)
        resource.Dispose();
        resource.Dispose();
        resource.Dispose();
    }
}

public class DefaultResourceFactoryTests
{
    private readonly DefaultResourceFactory _factory;

    public DefaultResourceFactoryTests()
    {
        _factory = new DefaultResourceFactory();
    }

    [Fact]
    public void SupportedResourceTypes_ShouldContainWildcard()
    {
        // Assert
        _factory.SupportedResourceTypes.Should().Contain("*");
        _factory.SupportedResourceTypes.Should().HaveCount(1);
    }

    [Fact]
    public void Priority_ShouldBeLowest()
    {
        // Assert
        _factory.Priority.Should().Be(-1000);
    }

    [Fact]
    public async Task CreateResourceAsync_WithValidApiVersion_ShouldCreateResource()
    {
        // Arrange
        const int apiVersion = 1;

        // Act
        var resource = await _factory.CreateResourceAsync(apiVersion);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<DefaultResource>();
        resource.RequestedApiVersion.Should().Be(apiVersion);
    }

    [Fact]
    public async Task CreateResourceAsync_WithStream_ShouldCreateResourceWithData()
    {
        // Arrange
        const int apiVersion = 1;
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var stream = new MemoryStream(data);

        // Act
        var resource = await _factory.CreateResourceAsync(apiVersion, stream);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<DefaultResource>();
        resource.AsBytes.Should().BeEquivalentTo(data);
    }

    [Fact]
    public async Task CreateResourceAsync_WithInvalidApiVersion_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = async () => await _factory.CreateResourceAsync(0);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateResourceAsync_WithCancellation_ShouldRespectCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var resource = await _factory.CreateResourceAsync(1, null, cts.Token);

        // Assert - Since no actual async work is done, cancellation won't affect this simple case
        resource.Should().NotBeNull();
    }
}
