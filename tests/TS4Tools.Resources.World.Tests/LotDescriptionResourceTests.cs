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
using TS4Tools.Resources.World;

namespace TS4Tools.Resources.World.Tests;

/// <summary>
/// Unit tests for LotDescriptionResource class.
/// </summary>
public sealed class LotDescriptionResourceTests : IDisposable
{
    private readonly ResourceKey _testKey = new(0x12345678, 0x9ABCDEF0, 0x1122334455667788);
    private LotDescriptionResource? _resource;

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
        _resource = new LotDescriptionResource(_testKey, 1);

        // Assert
        Assert.NotNull(_resource);
        Assert.Equal(_testKey, _resource.Key);
        Assert.Equal(1u, _resource.Version);
        Assert.True(_resource.IsDirty);
        Assert.NotNull(_resource.LotName);
        Assert.Empty(_resource.LotName);
        Assert.NotNull(_resource.LotDescription);
        Assert.Empty(_resource.LotDescription);
        Assert.NotNull(_resource.LotTraits);
        Assert.Empty(_resource.LotTraits);
        Assert.Equal(LotType.Residential, _resource.LotType);
    }

    [Fact]
    public void Constructor_NullKey_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new LotDescriptionResource(null!, 1));
    }

    #endregion

    #region IResource Implementation Tests

    [Fact]
    public void Stream_NotDisposed_ReturnsValidStream()
    {
        // Arrange
        _resource = new LotDescriptionResource(_testKey, 1);

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
        _resource = new LotDescriptionResource(_testKey, 1);

        // Act
        var bytes = _resource.AsBytes;

        // Assert
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);

        // Should contain LOTD magic bytes at start
        Assert.Equal((byte)'L', bytes[0]);
        Assert.Equal((byte)'O', bytes[1]);
        Assert.Equal((byte)'T', bytes[2]);
        Assert.Equal((byte)'D', bytes[3]);
    }

    [Fact]
    public void ContentFields_Always_ReturnsExpectedFields()
    {
        // Arrange
        _resource = new LotDescriptionResource(_testKey, 1);

        // Act
        var fields = _resource.ContentFields;

        // Assert
        Assert.NotNull(fields);
        Assert.Equal(9, fields.Count);
        Assert.Contains("LotId", fields);
        Assert.Contains("LotName", fields);
        Assert.Contains("Description", fields);
        Assert.Contains("LotType", fields);
        Assert.Contains("LotTraits", fields);
        Assert.Contains("Metadata", fields);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void IndexerByIndex_ValidIndex_ReturnsTypedValue(int index)
    {
        // Arrange
        _resource = new LotDescriptionResource(_testKey, 1);

        // Act
        var value = _resource[index];

        // Assert
        Assert.NotNull(value.Value);
    }

    [Fact]
    public void IndexerByIndex_InvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        _resource = new LotDescriptionResource(_testKey, 1);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => _resource[999]);
    }

    [Theory]
    [InlineData("LotId")]
    [InlineData("LotName")]
    [InlineData("Description")]
    [InlineData("LotType")]
    [InlineData("LotTraits")]
    [InlineData("Metadata")]
    public void IndexerByName_ValidFieldName_ReturnsTypedValue(string fieldName)
    {
        // Arrange
        _resource = new LotDescriptionResource(_testKey, 1);

        // Act
        var value = _resource[fieldName];

        // Assert
        Assert.NotNull(value.Value);
    }

    [Fact]
    public void IndexerByName_InvalidFieldName_ThrowsArgumentException()
    {
        // Arrange
        _resource = new LotDescriptionResource(_testKey, 1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _resource["InvalidField"]);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void LotName_SetValue_UpdatesAndMarksDirty()
    {
        // Arrange
        _resource = new LotDescriptionResource(_testKey, 1);
        _resource.IsDirty = false;

        // Act
        _resource.LotName = "Test Lot";

        // Assert
        Assert.Equal("Test Lot", _resource.LotName);
        Assert.True(_resource.IsDirty);
    }

    [Fact]
    public void LotDescription_SetValue_UpdatesAndMarksDirty()
    {
        // Arrange
        _resource = new LotDescriptionResource(_testKey, 1);
        _resource.IsDirty = false;

        // Act
        _resource.LotDescription = "A beautiful test lot";

        // Assert
        Assert.Equal("A beautiful test lot", _resource.LotDescription);
        Assert.True(_resource.IsDirty);
    }

    [Fact]
    public void LotType_SetValue_UpdatesAndMarksDirty()
    {
        // Arrange
        _resource = new LotDescriptionResource(_testKey, 1);
        _resource.IsDirty = false;

        // Act
        _resource.LotType = LotType.Commercial;

        // Assert
        Assert.Equal(LotType.Commercial, _resource.LotType);
        Assert.True(_resource.IsDirty);
    }

    #endregion

    #region Lot Traits Tests

    [Fact]
    public void AddLotTrait_ValidTrait_AddsToCollection()
    {
        // Arrange
        _resource = new LotDescriptionResource(_testKey, 1);
        var trait = new LotTrait("FamilyFriendly", 0x12345678UL);

        // Act
        _resource.AddLotTrait(trait);

        // Assert
        Assert.Single(_resource.LotTraits);
        Assert.Contains(trait, _resource.LotTraits);
        Assert.True(_resource.IsDirty);
    }

    [Fact]
    public void RemoveLotTrait_ExistingTrait_RemovesFromCollection()
    {
        // Arrange
        _resource = new LotDescriptionResource(_testKey, 1);
        var trait1 = new LotTrait("FamilyFriendly", 0x12345678UL);
        var trait2 = new LotTrait("Romantic", 0x87654321UL);

        _resource.AddLotTrait(trait1);
        _resource.AddLotTrait(trait2);

        // Act
        var removed = _resource.RemoveLotTrait(trait1);

        // Assert
        Assert.True(removed);
        Assert.Single(_resource.LotTraits);
        Assert.DoesNotContain(trait1, _resource.LotTraits);
        Assert.Contains(trait2, _resource.LotTraits);
        Assert.True(_resource.IsDirty);
    }

    [Fact]
    public void RemoveLotTrait_NonExistentTrait_ReturnsFalse()
    {
        // Arrange
        _resource = new LotDescriptionResource(_testKey, 1);
        var trait = new LotTrait("NonExistent", 0x12345678UL);

        // Act
        var removed = _resource.RemoveLotTrait(trait);

        // Assert
        Assert.False(removed);
    }

    #endregion

    #region Metadata Tests

    [Fact]
    public void SetMetadata_ValidValue_SetsMetadata()
    {
        // Arrange
        _resource = new LotDescriptionResource(_testKey, 1);

        // Act
        _resource.SetMetadata("customData", "test");
        _resource.SetMetadata("version", 1);

        // Assert
        Assert.Equal("test", _resource.GetMetadata<string>("customData"));
        Assert.Equal(1, _resource.GetMetadata<int>("version"));
        Assert.True(_resource.IsDirty);
    }

    [Fact]
    public void GetMetadata_NonExistentProperty_ReturnsDefault()
    {
        // Arrange
        _resource = new LotDescriptionResource(_testKey, 1);

        // Act
        var result = _resource.GetMetadata<string>("nonExistent");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region PropertyChanged Tests

    [Fact]
    public void LotName_PropertyChanged_RaisesEvent()
    {
        // Arrange
        _resource = new LotDescriptionResource(_testKey, 1);
        var eventRaised = false;
        string? changedProperty = null;

        _resource.PropertyChanged += (sender, e) =>
        {
            eventRaised = true;
            changedProperty = e.PropertyName;
        };

        // Act
        _resource.LotName = "Test Lot";

        // Assert
        Assert.True(eventRaised);
        Assert.Equal(nameof(_resource.LotName), changedProperty);
    }

    [Fact]
    public void IsDirty_PropertyChanged_RaisesEvent()
    {
        // Arrange
        _resource = new LotDescriptionResource(_testKey, 1);
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

    #endregion

    #region Stream Serialization Tests

    [Fact]
    public async Task LoadFromStreamAsync_NullStream_ThrowsArgumentNullException()
    {
        // Arrange
        _resource = new LotDescriptionResource(_testKey, 1);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _resource.LoadFromStreamAsync(null!));
    }

    #endregion

    #region Disposal Tests

    [Fact]
    public void Dispose_CalledOnce_DisposesCorrectly()
    {
        // Arrange
        _resource = new LotDescriptionResource(_testKey, 1);

        // Act
        _resource.Dispose();

        // Assert - Should not throw
        _resource.Dispose(); // Second call should be safe
    }

    [Fact]
    public void Stream_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        _resource = new LotDescriptionResource(_testKey, 1);
        _resource.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _resource.Stream);
    }

    [Fact]
    public void AsBytes_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        _resource = new LotDescriptionResource(_testKey, 1);
        _resource.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _resource.AsBytes);
    }

    #endregion
}
