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
/// Tests for the PackageHeader struct
/// </summary>
public class PackageHeaderTests
{
    [Fact]
    public void CreateDefault_ReturnsValidHeader()
    {
        // Act
        var header = PackageHeader.CreateDefault();
        
        // Assert
        header.IsValid.Should().BeTrue();
        header.Magic.SequenceEqual("DBPF"u8).Should().BeTrue();
        header.Major.Should().Be(2);
        header.Minor.Should().Be(0);
        header.IndexMajor.Should().Be(7);
        header.IndexMinor.Should().Be(1);
        header.CreatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        header.ModifiedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }
    
    [Fact]
    public void Constructor_WithValidMagic_CreatesValidHeader()
    {
        // Act
        var header = new PackageHeader("DBPF"u8);
        
        // Assert
        header.IsValid.Should().BeTrue();
        header.Magic.SequenceEqual("DBPF"u8).Should().BeTrue();
        header.Major.Should().Be(2);
        header.Minor.Should().Be(0);
    }
    
    [Fact]
    public void Constructor_WithInvalidMagic_CreatesInvalidHeader()
    {
        // Act
        var header = new PackageHeader("INVALID"u8);
        
        // Assert
        header.IsValid.Should().BeFalse();
        header.Magic.SequenceEqual("INVALID"u8).Should().BeTrue();
    }
    
    [Fact]
    public void WriteAndRead_RoundTrip_PreservesData()
    {
        // Arrange
        var originalHeader = new PackageHeader(
            "DBPF"u8,
            major: 2,
            minor: 1,
            userVersionMajor: 3,
            userVersionMinor: 4,
            createdDate: 1000000000u,
            modifiedDate: 2000000000u,
            resourceCount: 42,
            indexPosition: 1024,
            indexSize: 512);
        
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        
        // Act - Write
        originalHeader.Write(writer);
        
        // Act - Read
        stream.Position = 0;
        using var reader = new BinaryReader(stream);
        var readHeader = PackageHeader.Read(reader);
        
        // Assert
        readHeader.Magic.SequenceEqual(originalHeader.Magic).Should().BeTrue();
        readHeader.Major.Should().Be(originalHeader.Major);
        readHeader.Minor.Should().Be(originalHeader.Minor);
        readHeader.UserVersionMajor.Should().Be(originalHeader.UserVersionMajor);
        readHeader.UserVersionMinor.Should().Be(originalHeader.UserVersionMinor);
        readHeader.CreatedDateRaw.Should().Be(originalHeader.CreatedDateRaw);
        readHeader.ModifiedDateRaw.Should().Be(originalHeader.ModifiedDateRaw);
        readHeader.ResourceCount.Should().Be(originalHeader.ResourceCount);
        readHeader.IndexPosition.Should().Be(originalHeader.IndexPosition);
        readHeader.IndexSize.Should().Be(originalHeader.IndexSize);
    }
    
    [Fact]
    public void HeaderSize_IsCorrect()
    {
        // Assert
        PackageHeader.HeaderSize.Should().Be(96);
    }
    
    [Fact]
    public void Write_WritesCorrectSize()
    {
        // Arrange
        var header = PackageHeader.CreateDefault();
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        
        // Act
        header.Write(writer);
        
        // Assert
        stream.Length.Should().Be(PackageHeader.HeaderSize);
    }
    
    [Fact]
    public void CreatedDate_ConvertsFromUnixTime()
    {
        // Arrange
        var unixTime = 1609459200u; // January 1, 2021 00:00:00 UTC
        var header = new PackageHeader("DBPF"u8, createdDate: unixTime);
        
        // Act
        var dateTime = header.CreatedDate;
        
        // Assert
        dateTime.Year.Should().Be(2021);
        dateTime.Month.Should().Be(1);
        dateTime.Day.Should().Be(1);
    }
    
    [Fact]
    public void ModifiedDate_ConvertsFromUnixTime()
    {
        // Arrange
        var unixTime = 1609459200u; // January 1, 2021 00:00:00 UTC
        var header = new PackageHeader("DBPF"u8, modifiedDate: unixTime);
        
        // Act
        var dateTime = header.ModifiedDate;
        
        // Assert
        dateTime.Year.Should().Be(2021);
        dateTime.Month.Should().Be(1);
        dateTime.Day.Should().Be(1);
    }
    
    [Fact]
    public void ExpectedMagic_ReturnsDBPF()
    {
        // Act
        var magic = PackageHeader.ExpectedMagic;
        
        // Assert
        magic.SequenceEqual("DBPF"u8).Should().BeTrue();
    }
}
