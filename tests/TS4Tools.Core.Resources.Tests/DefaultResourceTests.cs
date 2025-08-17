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
        resource.Stream.ReadExactly(streamData, 0, data.Length);

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

    #region Phase 4.1.2 Enhancement Tests

    [Fact]
    public void Constructor_WithInvalidApiVersion_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        var act = () => new DefaultResource(0);
        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithMessage("*API version must be 1 or greater*");
    }

    [Fact]
    public void Constructor_WithValidData_ShouldPopulateMetadata()
    {
        // Arrange
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var stream = new MemoryStream(data);

        // Act
        var resource = new DefaultResource(1, stream);

        // Assert
        resource.Metadata.Should().NotBeEmpty();
        resource.Metadata.Should().ContainKey("CreatedAt");
        resource.Metadata.Should().ContainKey("OriginalSize");
        resource.Metadata.Should().ContainKey("ApiVersion");
        resource.Metadata.Should().ContainKey("DataLength");
        resource.Metadata["OriginalSize"].Should().Be(4L);
        resource.Metadata["ApiVersion"].Should().Be(1);
    }

    [Fact]
    public void DetectedResourceTypeHint_WithSTBLData_ShouldDetectStringTable()
    {
        // Arrange
        var stblData = new byte[] { 0x53, 0x54, 0x42, 0x4C, 0x00, 0x00, 0x00, 0x01 }; // "STBL" + version
        var stream = new MemoryStream(stblData);

        // Act
        var resource = new DefaultResource(1, stream);

        // Assert
        resource.DetectedResourceTypeHint.Should().Be("StringTableResource");
    }

    [Fact]
    public void DetectedResourceTypeHint_WithDATAResource_ShouldDetectDataResource()
    {
        // Arrange
        var dataSignature = new byte[] { 0x44, 0x41, 0x54, 0x41, 0x00, 0x00, 0x00, 0x01 }; // "DATA" + version
        var stream = new MemoryStream(dataSignature);

        // Act
        var resource = new DefaultResource(1, stream);

        // Assert
        resource.DetectedResourceTypeHint.Should().Be("DataResource");
    }

    [Fact]
    public void DetectedResourceTypeHint_WithPNGData_ShouldDetectImageResource()
    {
        // Arrange
        var pngSignature = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }; // PNG signature
        var stream = new MemoryStream(pngSignature);

        // Act
        var resource = new DefaultResource(1, stream);

        // Assert
        resource.DetectedResourceTypeHint.Should().Be("ImageResource");
    }

    [Fact]
    public void DetectedResourceTypeHint_WithXMLData_ShouldDetectXmlResource()
    {
        // Arrange
        var xmlData = Encoding.UTF8.GetBytes("<?xml version=\"1.0\"?><root></root>");
        var stream = new MemoryStream(xmlData);

        // Act
        var resource = new DefaultResource(1, stream);

        // Assert
        resource.DetectedResourceTypeHint.Should().Be("XmlResource");
    }

    [Fact]
    public void DetectedResourceTypeHint_WithUnknownData_ShouldReturnNull()
    {
        // Arrange
        var unknownData = new byte[] { 0x12, 0x34, 0x56, 0x78 };
        var stream = new MemoryStream(unknownData);

        // Act
        var resource = new DefaultResource(1, stream);

        // Assert
        resource.DetectedResourceTypeHint.Should().BeNull();
    }

    [Fact]
    public void OriginalSize_ShouldReturnCorrectSize()
    {
        // Arrange
        var data = new byte[1024];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = (byte)(i % 256);
        }
        var stream = new MemoryStream(data);

        // Act
        var resource = new DefaultResource(1, stream);

        // Assert
        resource.OriginalSize.Should().Be(1024);
    }

    [Fact]
    public void CreatedAt_ShouldBeRecentTimestamp()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var resource = new DefaultResource(1);
        var afterCreation = DateTime.UtcNow;

        // Assert
        resource.CreatedAt.Should().BeOnOrAfter(beforeCreation);
        resource.CreatedAt.Should().BeOnOrBefore(afterCreation);
    }

    [Fact]
    public void GetDiagnosticInfo_ShouldProvideDetailedInformation()
    {
        // Arrange
        var data = new byte[] { 0x44, 0x41, 0x54, 0x41, 0x00, 0x00, 0x00, 0x01 }; // "DATA" + version
        var stream = new MemoryStream(data);
        var resource = new DefaultResource(2, stream);

        // Act
        var diagnosticInfo = resource.GetDiagnosticInfo();

        // Assert
        diagnosticInfo.Should().Contain("API Version: 2");
        diagnosticInfo.Should().Contain("Original Size: 8 bytes");
        diagnosticInfo.Should().Contain("Current Size: 8 bytes");
        diagnosticInfo.Should().Contain("Detected Type Hint: DataResource");
        diagnosticInfo.Should().Contain("Metadata Count:");
    }

    [Fact]
    public void ToString_ShouldIncludeEnhancedInformation()
    {
        // Arrange
        var data = new byte[] { 0x53, 0x54, 0x42, 0x4C }; // "STBL"
        var stream = new MemoryStream(data);
        var resource = new DefaultResource(1, stream);

        // Act
        var result = resource.ToString();

        // Assert
        result.Should().Contain("DefaultResource");
        result.Should().Contain("StringTableResource");
        result.Should().Contain("4 bytes");
        result.Should().Contain("API v1");
    }

    [Fact]
    public void ToString_WithUnknownType_ShouldShowUnknown()
    {
        // Arrange
        var data = new byte[] { 0x12, 0x34 };
        var stream = new MemoryStream(data);
        var resource = new DefaultResource(1, stream);

        // Act
        var result = resource.ToString();

        // Assert
        result.Should().Contain("DefaultResource");
        result.Should().Contain("Unknown");
        result.Should().Contain("2 bytes");
    }

    [Fact]
    public void DisposedResource_PropertyAccess_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var resource = new DefaultResource(1);
        resource.Dispose();

        // Act & Assert
        var streamAct = () => resource.Stream;
        var bytesAct = () => resource.AsBytes;
        var metadataAct = () => resource.Metadata;
        var sizeAct = () => resource.OriginalSize;
        var createdAct = () => resource.CreatedAt;
        var hintAct = () => resource.DetectedResourceTypeHint;
        var diagnosticAct = () => resource.GetDiagnosticInfo();
        var indexerStringAct = () => resource["test"];
        var indexerIntAct = () => resource[0];

        streamAct.Should().Throw<ObjectDisposedException>();
        bytesAct.Should().Throw<ObjectDisposedException>();
        metadataAct.Should().Throw<ObjectDisposedException>();
        sizeAct.Should().Throw<ObjectDisposedException>();
        createdAct.Should().Throw<ObjectDisposedException>();
        hintAct.Should().Throw<ObjectDisposedException>();
        diagnosticAct.Should().Throw<ObjectDisposedException>();
        indexerStringAct.Should().Throw<ObjectDisposedException>();
        indexerIntAct.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void DisposedResource_ToString_ShouldIndicateDisposed()
    {
        // Arrange
        var resource = new DefaultResource(1);
        resource.Dispose();

        // Act
        var result = resource.ToString();

        // Assert
        result.Should().Be("DefaultResource (Disposed)");
    }

    [Fact]
    public void Metadata_WithBinaryData_ShouldDetectBinaryCharacteristics()
    {
        // Arrange
        var binaryData = new byte[] { 0x00, 0x01, 0x02, 0xFF, 0xFE, 0x80 }; // Binary data with null bytes
        var stream = new MemoryStream(binaryData);

        // Act
        var resource = new DefaultResource(1, stream);

        // Assert
        resource.Metadata.Should().ContainKey("ContainsNullBytes");
        resource.Metadata.Should().ContainKey("LikelyTextData");
        resource.Metadata["ContainsNullBytes"].Should().Be(true);
        resource.Metadata["LikelyTextData"].Should().Be(false);
    }

    [Fact]
    public void Metadata_WithTextData_ShouldDetectTextCharacteristics()
    {
        // Arrange
        var textData = Encoding.UTF8.GetBytes("Hello, World! This is text data.");
        var stream = new MemoryStream(textData);

        // Act
        var resource = new DefaultResource(1, stream);

        // Assert
        resource.Metadata.Should().ContainKey("ContainsNullBytes");
        resource.Metadata.Should().ContainKey("LikelyTextData");
        resource.Metadata["ContainsNullBytes"].Should().Be(false);
        resource.Metadata["LikelyTextData"].Should().Be(true);
    }

    [Fact]
    public void Constructor_WithLargeStream_ShouldHandlePerformanceOptimization()
    {
        // Arrange - Create a stream larger than 1MB to trigger async copying
        var largeData = new byte[2 * 1024 * 1024]; // 2MB
        for (int i = 0; i < largeData.Length; i++)
        {
            largeData[i] = (byte)(i % 256);
        }
        var stream = new MemoryStream(largeData);

        // Act
        var resource = new DefaultResource(1, stream);

        // Assert
        resource.OriginalSize.Should().Be(2 * 1024 * 1024);
        resource.AsBytes.Should().HaveCount(2 * 1024 * 1024);
        resource.AsBytes.Should().BeEquivalentTo(largeData);
    }

    [Fact]
    public void Constructor_WithCorruptedStream_ShouldThrowInvalidDataException()
    {
        // Arrange
        var mockStream = Substitute.For<Stream>();
        mockStream.Length.Returns(_ => throw new IOException("Stream error"));
        mockStream.CanRead.Returns(true);

        // Act & Assert
        var act = () => new DefaultResource(1, mockStream);
        act.Should().Throw<InvalidDataException>()
           .WithMessage("*Failed to initialize DefaultResource from stream*");
    }

    #endregion
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
