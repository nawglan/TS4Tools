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

using System;
using System.IO;
using FluentAssertions;
using TS4Tools.Core.Package;
using TS4Tools.Resources.Geometry;
using Xunit;

namespace TS4Tools.Resources.Geometry.Tests;

public sealed class ModularResourceTests : IDisposable
{
    private ModularResource? _resource;
    private readonly ResourceKey _testKey;

    public ModularResourceTests()
    {
        _testKey = new ResourceKey(0xCF9A4ACE, 0x12345678U, 0xFEDCBA9876543210UL);
    }

    public void Dispose()
    {
        _resource?.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ValidParameters_CreatesResource()
    {
        // Act
        _resource = new ModularResource(_testKey, 1);

        // Assert
        Assert.NotNull(_resource);
        Assert.Equal(_testKey, _resource.Key);
        Assert.Equal(1U, _resource.Version);
    }

    [Fact]
    public void Constructor_NullKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ModularResource(null!, 1));
    }

    #endregion

    #region IResource Implementation Tests

    [Fact]
    public void Stream_NotDisposed_ReturnsValidStream()
    {
        // Arrange
        _resource = new ModularResource(_testKey, 1);

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
        _resource = new ModularResource(_testKey, 1);

        // Act
        var bytes = _resource.AsBytes;

        // Assert
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);

        // Should contain MODR magic bytes at start
        Assert.Equal((byte)'M', bytes[0]);
        Assert.Equal((byte)'O', bytes[1]);
        Assert.Equal((byte)'D', bytes[2]);
        Assert.Equal((byte)'R', bytes[3]);
    }

    [Fact]
    public void ContentFields_Always_ReturnsExpectedFields()
    {
        // Arrange
        _resource = new ModularResource(_testKey, 1);

        // Act
        var fields = _resource.ContentFields;

        // Assert
        Assert.NotNull(fields);
        Assert.Equal(3, fields.Count);
        Assert.Contains("ComponentCount", fields);
        Assert.Contains("ConnectionCount", fields);
        Assert.Contains("ConstraintCount", fields);
    }

    [Theory]
    [InlineData(0)] // ComponentCount
    [InlineData(1)] // ConnectionCount
    [InlineData(2)] // ConstraintCount
    public void IndexerByIndex_ValidIndex_ReturnsTypedValue(int index)
    {
        // Arrange
        _resource = new ModularResource(_testKey, 1);

        // Act
        var value = _resource[index];

        // Assert
        Assert.NotNull(value.Value);
        Assert.IsType<int>(value.Value);
    }

    [Fact]
    public void IndexerByIndex_InvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        _resource = new ModularResource(_testKey, 1);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => _resource[999]);
    }

    [Theory]
    [InlineData("ComponentCount")]
    [InlineData("ConnectionCount")]
    [InlineData("ConstraintCount")]
    public void IndexerByName_ValidFieldName_ReturnsTypedValue(string fieldName)
    {
        // Arrange
        _resource = new ModularResource(_testKey, 1);

        // Act
        var value = _resource[fieldName];

        // Assert
        Assert.NotNull(value.Value);
        Assert.IsType<int>(value.Value);
    }

    [Fact]
    public void IndexerByName_InvalidFieldName_ThrowsArgumentException()
    {
        // Arrange
        _resource = new ModularResource(_testKey, 1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _resource["InvalidField"]);
    }

    #endregion
}
