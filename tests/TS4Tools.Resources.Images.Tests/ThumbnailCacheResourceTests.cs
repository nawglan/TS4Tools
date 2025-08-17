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

using System.ComponentModel;
using TS4Tools.Core.Package;
using TS4Tools.Resources.Images;

namespace TS4Tools.Resources.Images.Tests;

/// <summary>
/// Unit tests for ThumbnailCacheResource class.
/// </summary>
public sealed class ThumbnailCacheResourceTests : IDisposable
{
    private readonly ResourceKey _testKey = new(0x12345678, 0x9ABCDEF0, 0x1122334455667788);
    private ThumbnailCacheResource? _resource;

    /// <summary>
    /// Disposes test resources.
    /// </summary>
    public void Dispose()
    {
        _resource?.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ValidParameters_CreatesResource()
    {
        // Arrange & Act
        _resource = new ThumbnailCacheResource(_testKey, 1);

        // Assert
        Assert.NotNull(_resource);
        Assert.Equal(_testKey, _resource.Key);
        Assert.Equal(1u, _resource.Version);
        Assert.True(_resource.IsDirty);
        Assert.Equal(1u, _resource.CacheVersion);
        Assert.Equal(10000, _resource.MaxCacheSize);
        Assert.Equal(0, _resource.CacheCount);
        Assert.Equal(0L, _resource.TotalCacheSize);
    }

    [Fact]
    public void Constructor_NullKey_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ThumbnailCacheResource(null!, 1));
    }

    #endregion

    #region IResource Implementation Tests

    [Fact]
    public void Stream_NotDisposed_ReturnsValidStream()
    {
        // Arrange
        _resource = new ThumbnailCacheResource(_testKey, 1);

        // Act
        var stream = _resource.Stream;

        // Assert
        Assert.NotNull(stream);
        Assert.True(stream.CanRead);
        Assert.True(stream.CanWrite);
        Assert.True(stream.CanSeek);
    }

    [Fact]
    public void AsBytes_EmptyResource_ReturnsValidByteArray()
    {
        // Arrange
        _resource = new ThumbnailCacheResource(_testKey, 1);

        // Act
        var bytes = _resource.AsBytes;

        // Assert
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);

        // Should contain THMC magic bytes at start
        Assert.Equal((byte)'T', bytes[0]);
        Assert.Equal((byte)'H', bytes[1]);
        Assert.Equal((byte)'M', bytes[2]);
        Assert.Equal((byte)'C', bytes[3]);
    }

    [Fact]
    public void ContentFields_Always_ReturnsExpectedFields()
    {
        // Arrange
        _resource = new ThumbnailCacheResource(_testKey, 1);

        // Act
        var fields = _resource.ContentFields;

        // Assert
        Assert.NotNull(fields);
        Assert.Equal(5, fields.Count);
        Assert.Contains("ResourceId", fields);
        Assert.Contains("CacheVersion", fields);
        Assert.Contains("MaxCacheSize", fields);
        Assert.Contains("ThumbnailCount", fields);
        Assert.Contains("TotalCacheSize", fields);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void IndexerByIndex_ValidIndex_ReturnsTypedValue(int index)
    {
        // Arrange
        _resource = new ThumbnailCacheResource(_testKey, 1);

        // Act
        var value = _resource[index];

        // Assert
        Assert.NotNull(value.Value);
    }

    [Fact]
    public void IndexerByIndex_InvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        _resource = new ThumbnailCacheResource(_testKey, 1);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => _resource[999]);
    }

    [Theory]
    [InlineData("ResourceId")]
    [InlineData("CacheVersion")]
    [InlineData("MaxCacheSize")]
    [InlineData("ThumbnailCount")]
    [InlineData("TotalCacheSize")]
    public void IndexerByName_ValidFieldName_ReturnsTypedValue(string fieldName)
    {
        // Arrange
        _resource = new ThumbnailCacheResource(_testKey, 1);

        // Act
        var value = _resource[fieldName];

        // Assert
        Assert.NotNull(value.Value);
    }

    [Fact]
    public void IndexerByName_InvalidFieldName_ThrowsArgumentException()
    {
        // Arrange
        _resource = new ThumbnailCacheResource(_testKey, 1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _resource["InvalidField"]);
    }

    #endregion

    #region Thumbnail Management Tests

    [Fact]
    public void AddThumbnail_ValidThumbnail_AddsToCache()
    {
        // Arrange
        _resource = new ThumbnailCacheResource(_testKey, 1);
        var thumbnailData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        const ulong resourceId = 0x12345678UL;

        // Act
        _resource.AddThumbnail(resourceId, thumbnailData, 128, 128, ThumbnailFormat.Png);

        // Assert
        Assert.Equal(1, _resource.CacheCount);
        Assert.True(_resource.ContainsThumbnail(resourceId));
        Assert.True(_resource.IsDirty);
        Assert.True(_resource.TotalCacheSize > 0);
    }

    [Fact]
    public void AddThumbnail_NullThumbnailData_ThrowsArgumentNullException()
    {
        // Arrange
        _resource = new ThumbnailCacheResource(_testKey, 1);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _resource.AddThumbnail(0x12345678UL, null!, 128, 128, ThumbnailFormat.Png));
    }

    [Fact]
    public void AddThumbnail_EmptyThumbnailData_ThrowsArgumentException()
    {
        // Arrange
        _resource = new ThumbnailCacheResource(_testKey, 1);
        var emptyData = Array.Empty<byte>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _resource.AddThumbnail(0x12345678UL, emptyData, 128, 128, ThumbnailFormat.Png));
    }

    [Fact]
    public void AddThumbnail_InvalidDimensions_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        _resource = new ThumbnailCacheResource(_testKey, 1);
        var thumbnailData = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _resource.AddThumbnail(0x12345678UL, thumbnailData, 0, 128, ThumbnailFormat.Png));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _resource.AddThumbnail(0x12345678UL, thumbnailData, 128, 0, ThumbnailFormat.Png));
    }

    [Fact]
    public void GetThumbnail_ExistingThumbnail_ReturnsEntry()
    {
        // Arrange
        _resource = new ThumbnailCacheResource(_testKey, 1);
        var thumbnailData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        const ulong resourceId = 0x12345678UL;

        _resource.AddThumbnail(resourceId, thumbnailData, 128, 128, ThumbnailFormat.Jpeg);

        // Act
        var thumbnail = _resource.GetThumbnail(resourceId);

        // Assert
        Assert.NotNull(thumbnail);
        Assert.Equal(resourceId, thumbnail.Value.ResourceId);
        Assert.Equal(128, thumbnail.Value.Width);
        Assert.Equal(128, thumbnail.Value.Height);
        Assert.Equal(ThumbnailFormat.Jpeg, thumbnail.Value.Format);
        Assert.Equal(thumbnailData, thumbnail.Value.Data);
    }

    [Fact]
    public void GetThumbnail_NonExistentThumbnail_ReturnsNull()
    {
        // Arrange
        _resource = new ThumbnailCacheResource(_testKey, 1);

        // Act
        var thumbnail = _resource.GetThumbnail(0x99999999UL);

        // Assert
        Assert.Null(thumbnail);
    }

    [Fact]
    public void RemoveThumbnail_ExistingThumbnail_RemovesFromCache()
    {
        // Arrange
        _resource = new ThumbnailCacheResource(_testKey, 1);
        var thumbnailData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        const ulong resourceId = 0x12345678UL;

        _resource.AddThumbnail(resourceId, thumbnailData, 128, 128, ThumbnailFormat.Png);
        Assert.Equal(1, _resource.CacheCount);

        // Act
        var removed = _resource.RemoveThumbnail(resourceId);

        // Assert
        Assert.True(removed);
        Assert.Equal(0, _resource.CacheCount);
        Assert.False(_resource.ContainsThumbnail(resourceId));
        Assert.True(_resource.IsDirty);
    }

    [Fact]
    public void RemoveThumbnail_NonExistentThumbnail_ReturnsFalse()
    {
        // Arrange
        _resource = new ThumbnailCacheResource(_testKey, 1);

        // Act
        var removed = _resource.RemoveThumbnail(0x99999999UL);

        // Assert
        Assert.False(removed);
    }

    [Fact]
    public void ContainsThumbnail_ExistingThumbnail_ReturnsTrue()
    {
        // Arrange
        _resource = new ThumbnailCacheResource(_testKey, 1);
        var thumbnailData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        const ulong resourceId = 0x12345678UL;

        _resource.AddThumbnail(resourceId, thumbnailData, 128, 128, ThumbnailFormat.Dds);

        // Act & Assert
        Assert.True(_resource.ContainsThumbnail(resourceId));
    }

    [Fact]
    public void ContainsThumbnail_NonExistentThumbnail_ReturnsFalse()
    {
        // Arrange
        _resource = new ThumbnailCacheResource(_testKey, 1);

        // Act & Assert
        Assert.False(_resource.ContainsThumbnail(0x99999999UL));
    }

    [Fact]
    public void ClearCache_WithThumbnails_RemovesAllThumbnails()
    {
        // Arrange
        _resource = new ThumbnailCacheResource(_testKey, 1);
        var thumbnailData = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        _resource.AddThumbnail(0x11111111UL, thumbnailData, 64, 64, ThumbnailFormat.Png);
        _resource.AddThumbnail(0x22222222UL, thumbnailData, 128, 128, ThumbnailFormat.Jpeg);
        _resource.AddThumbnail(0x33333333UL, thumbnailData, 256, 256, ThumbnailFormat.Tga);

        Assert.Equal(3, _resource.CacheCount);

        // Act
        _resource.ClearCache();

        // Assert
        Assert.Equal(0, _resource.CacheCount);
        Assert.Equal(0L, _resource.TotalCacheSize);
        Assert.False(_resource.ContainsThumbnail(0x11111111UL));
        Assert.False(_resource.ContainsThumbnail(0x22222222UL));
        Assert.False(_resource.ContainsThumbnail(0x33333333UL));
        Assert.True(_resource.IsDirty);
    }

    #endregion

    #region Cache Statistics Tests

    [Fact]
    public void Statistics_WithThumbnails_ReturnsCorrectStats()
    {
        // Arrange
        _resource = new ThumbnailCacheResource(_testKey, 1);
        var thumbnailData1 = new byte[] { 0x01, 0x02 };
        var thumbnailData2 = new byte[] { 0x03, 0x04, 0x05, 0x06 };

        _resource.AddThumbnail(0x11111111UL, thumbnailData1, 64, 64, ThumbnailFormat.Png);
        _resource.AddThumbnail(0x22222222UL, thumbnailData2, 128, 128, ThumbnailFormat.Jpeg);

        // Act
        var stats = _resource.Statistics;

        // Assert
        Assert.Equal(2, stats.TotalThumbnails);
        Assert.True(stats.TotalSize > 0);
        Assert.True(stats.TotalHits >= 0);
        Assert.True(stats.AverageThumbnailSize > 0);
    }

    #endregion

    #region MaxCacheSize Tests

    [Fact]
    public void MaxCacheSize_SetValue_UpdatesAndMarksDirty()
    {
        // Arrange
        _resource = new ThumbnailCacheResource(_testKey, 1);
        _resource.IsDirty = false;

        // Act
        _resource.MaxCacheSize = 5000;

        // Assert
        Assert.Equal(5000, _resource.MaxCacheSize);
        Assert.True(_resource.IsDirty);
    }

    [Fact]
    public void MaxCacheSize_SetNegativeValue_IgnoresValue()
    {
        // Arrange
        _resource = new ThumbnailCacheResource(_testKey, 1);
        var originalValue = _resource.MaxCacheSize;

        // Act
        _resource.MaxCacheSize = -1;

        // Assert - Value should remain unchanged
        Assert.Equal(originalValue, _resource.MaxCacheSize);
    }

    #endregion

    #region PropertyChanged Tests

    [Fact]
    public void IsDirty_PropertyChanged_RaisesEvent()
    {
        // Arrange
        _resource = new ThumbnailCacheResource(_testKey, 1);
        var eventRaised = false;
        string? changedProperty = null;

        _resource.PropertyChanged += (sender, e) =>
        {
            eventRaised = true;
            changedProperty = e.PropertyName;
        };

        // Act
        _resource.IsDirty = false;

        // Assert
        Assert.True(eventRaised);
        Assert.Equal(nameof(_resource.IsDirty), changedProperty);
    }

    [Fact]
    public void MaxCacheSize_PropertyChanged_RaisesEvent()
    {
        // Arrange
        _resource = new ThumbnailCacheResource(_testKey, 1);
        _resource.IsDirty = false; // Reset dirty flag
        var eventRaised = false;
        string? changedProperty = null;

        _resource.PropertyChanged += (sender, e) =>
        {
            eventRaised = true;
            changedProperty = e.PropertyName;
        };

        // Act
        _resource.MaxCacheSize = 5000;

        // Assert
        Assert.True(eventRaised);
        Assert.Equal(nameof(_resource.MaxCacheSize), changedProperty);
    }

    #endregion

    #region Stream Serialization Tests

    [Fact]
    public async Task LoadFromStreamAsync_NullStream_ThrowsArgumentNullException()
    {
        // Arrange
        _resource = new ThumbnailCacheResource(_testKey, 1);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _resource.LoadFromStreamAsync(null!));
    }

    [Fact]
    public async Task SaveToStreamAsync_ValidStream_SavesSuccessfully()
    {
        // Arrange
        _resource = new ThumbnailCacheResource(_testKey, 1);
        var stream = new MemoryStream();

        // Add some test data
        var thumbnailData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        _resource.AddThumbnail(0x12345678UL, thumbnailData, 128, 128, ThumbnailFormat.Png);

        // Act
        await _resource.SaveToStreamAsync(stream);

        // Assert
        Assert.True(stream.Length > 0);
        Assert.False(_resource.IsDirty);
    }

    [Fact]
    public async Task SaveToStreamAsync_NullStream_ThrowsArgumentNullException()
    {
        // Arrange
        _resource = new ThumbnailCacheResource(_testKey, 1);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _resource.SaveToStreamAsync(null!));
    }

    [Fact]
    public async Task AsStreamAsync_WithData_ReturnsValidStream()
    {
        // Arrange
        _resource = new ThumbnailCacheResource(_testKey, 1);
        var thumbnailData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        _resource.AddThumbnail(0x12345678UL, thumbnailData, 128, 128, ThumbnailFormat.Png);

        // Act
        var stream = await _resource.AsStreamAsync();

        // Assert
        Assert.NotNull(stream);
        Assert.True(stream.Length > 0);
        Assert.True(stream.CanRead);
    }

    #endregion

    #region Disposal Tests

    [Fact]
    public void Dispose_CalledOnce_DisposesCorrectly()
    {
        // Arrange
        _resource = new ThumbnailCacheResource(_testKey, 1);

        // Act
        _resource.Dispose();

        // Assert - Should not throw
        _resource.Dispose(); // Second call should be safe
    }

    [Fact]
    public void Stream_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        _resource = new ThumbnailCacheResource(_testKey, 1);
        _resource.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _resource.Stream);
    }

    [Fact]
    public void AsBytes_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        _resource = new ThumbnailCacheResource(_testKey, 1);
        _resource.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _resource.AsBytes);
    }

    [Fact]
    public void AddThumbnail_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        _resource = new ThumbnailCacheResource(_testKey, 1);
        _resource.Dispose();
        var thumbnailData = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() =>
            _resource.AddThumbnail(0x12345678UL, thumbnailData, 128, 128, ThumbnailFormat.Png));
    }

    #endregion
}
