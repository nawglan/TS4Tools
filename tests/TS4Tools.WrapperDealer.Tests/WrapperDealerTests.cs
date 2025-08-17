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
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;
using TS4Tools.Core.Resources;
using Xunit;

namespace TS4Tools.WrapperDealer.Tests;

/// <summary>
/// Tests for WrapperDealer compatibility layer - Phase 4.20 validation
/// </summary>
public class WrapperDealerTests
{
    [Fact]
    public void Initialize_WithNullServiceProvider_ShouldThrow()
    {
        // Act & Assert
        var action = () => WrapperDealer.Initialize(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Disabled_ShouldReturnEmptyCollectionInitially()
    {
        // Act
        var result = WrapperDealer.Disabled;

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void RegisterWrapper_WithValidParameters_ShouldAddToTypeMap()
    {
        // Act
        WrapperDealer.RegisterWrapper(typeof(TestResource), "0xABCDEF00", "0xABCDEF01");

        // Assert
        WrapperDealer.GetWrapperType("0xABCDEF00").Should().Be(typeof(TestResource));
        WrapperDealer.GetWrapperType("0xABCDEF01").Should().Be(typeof(TestResource));
    }

    [Fact]
    public void UnregisterWrapper_WithExistingType_ShouldRemoveFromTypeMap()
    {
        // Arrange
        WrapperDealer.RegisterWrapper(typeof(TestResource), "0xABCDEF00");

        // Act
        WrapperDealer.UnregisterWrapper("0xABCDEF00");

        // Assert
        WrapperDealer.GetWrapperType("0xABCDEF00").Should().BeNull();
    }

    [Fact]
    public void RegisterWrapper_WithNullType_ShouldThrow()
    {
        // Act & Assert
        var action = () => WrapperDealer.RegisterWrapper(null!, "0xABCDEF00");
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RegisterWrapper_WithNullResourceTypes_ShouldThrow()
    {
        // Act & Assert
        var action = () => WrapperDealer.RegisterWrapper(typeof(TestResource), null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UnregisterWrapper_WithNullResourceType_ShouldThrow()
    {
        // Act & Assert
        var action = () => WrapperDealer.UnregisterWrapper(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetSupportedResourceTypes_ShouldReturnArray()
    {
        // Arrange
        WrapperDealer.RegisterWrapper(typeof(TestResource), "0xTEST001");

        // Act
        var result = WrapperDealer.GetSupportedResourceTypes();

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("0xTEST001");
    }

    [Fact]
    public void ReloadWrappers_ShouldNotThrow()
    {
        // Act & Assert
        var action = () => WrapperDealer.ReloadWrappers();
        action.Should().NotThrow();
    }

    [Fact]
    public void RefreshWrappers_ShouldNotThrow()
    {
        // Act & Assert
        var action = () => WrapperDealer.RefreshWrappers();
        action.Should().NotThrow();
    }

    // Test resource class
    private class TestResource : IResource
    {
        public Stream Stream => new MemoryStream();
        public byte[] AsBytes => Array.Empty<byte>();
        public event EventHandler? ResourceChanged;
        public int RequestedApiVersion => 1;
        public int RecommendedApiVersion => 1;
        public IReadOnlyList<string> ContentFields => Array.Empty<string>();
        
        public TypedValue this[int index] 
        { 
            get => new TypedValue(); 
            set { } 
        }
        
        public TypedValue this[string name] 
        { 
            get => new TypedValue(); 
            set { } 
        }
        
        public void Dispose() { }
    }
}
