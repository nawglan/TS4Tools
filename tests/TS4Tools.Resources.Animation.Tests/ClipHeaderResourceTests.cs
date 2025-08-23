using System;
using System.IO;
using System.Text;
using FluentAssertions;
using TS4Tools.Resources.Animation;
using Xunit;

namespace TS4Tools.Resources.Animation.Tests;

public class ClipHeaderResourceTests
{
    [Fact]
    public void Constructor_WithValidMockData_ParsesCorrectly()
    {
        // Arrange
        var mockData = CreateMockBC4A5044Data();
        using var stream = new MemoryStream(mockData);

        // Act
        using var resource = new ClipHeaderResource(stream);

        // Assert
        resource.Version.Should().Be(7u);
        resource.Flags.Should().Be(0x02u);
        resource.Duration.Should().Be(2.5f);
        resource.ClipName.Should().Be("MockTestAnimation");
        resource.RigName.Should().Be("MockRig");
        resource.ActorName.Should().Be("MockTestAnimation");
        resource.InitialOffsetQ.Should().Be("0,0,0,1");
        resource.InitialOffsetT.Should().Be("0,0,0");
        resource.ReferenceNamespaceHash.Should().Be(0x12345678u);
        resource.HasValidData.Should().BeTrue();
        resource.ExplicitNamespaces.Should().ContainSingle("MockNamespace");
    }

    [Fact]
    public void SetProperty_WithValidValues_UpdatesCorrectly()
    {
        // Arrange
        var mockData = CreateMockBC4A5044Data();
        using var stream = new MemoryStream(mockData);
        using var resource = new ClipHeaderResource(stream);

        // Act
        resource.SetProperty("ClipName", "NewClipName");
        resource.SetProperty("Duration", 10.0f);
        resource.SetProperty("Flags", 0x04u);

        // Assert
        resource.ClipName.Should().Be("NewClipName");
        resource.Duration.Should().Be(10.0f);
        resource.Flags.Should().Be(0x04u);
    }

    [Fact]
    public void GetProperty_WithValidPropertyNames_ReturnsCorrectValues()
    {
        // Arrange
        var mockData = CreateMockBC4A5044Data();
        using var stream = new MemoryStream(mockData);
        using var resource = new ClipHeaderResource(stream);

        // Act & Assert
        resource.GetProperty("Version").Should().Be("7");
        resource.GetProperty("ClipName").Should().Be("MockTestAnimation");
        resource.GetProperty("Duration").Should().Be("2.5");
        resource.GetProperty("HasValidData").Should().Be("True");
    }

    [Fact]
    public void ToJsonString_WithValidData_ProducesValidJson()
    {
        // Arrange
        var mockData = CreateMockBC4A5044Data();
        using var stream = new MemoryStream(mockData);
        using var resource = new ClipHeaderResource(stream);

        // Act
        var json = resource.ToJsonString();

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"Version\": 7");
        json.Should().Contain("\"ClipName\": \"MockTestAnimation\"");
        json.Should().Contain("\"Duration\": 2.5");
        json.Should().Contain("\"RigName\": \"MockRig\"");
        json.Should().Contain("\"HasValidData\": true");
    }

    [Fact]
    public void AsBytes_ReturnsValidByteArray()
    {
        // Arrange
        var mockData = CreateMockBC4A5044Data();
        using var stream = new MemoryStream(mockData);
        using var resource = new ClipHeaderResource(stream);

        // Act
        var bytes = resource.AsBytes;

        // Assert
        bytes.Should().NotBeNull();
        bytes.Length.Should().Be(mockData.Length);
        bytes.Should().BeEquivalentTo(mockData);
    }

    [Fact]
    public void Constructor_WithEmptyStream_CreatesEmptyResource()
    {
        // Arrange & Act
        using var resource = new ClipHeaderResource();

        // Assert
        resource.Version.Should().Be(0u);
        resource.HasValidData.Should().BeFalse();
        resource.ClipName.Should().BeEmpty();
        resource.RigName.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithInvalidData_HandlesGracefully()
    {
        // Arrange
        var invalidData = new byte[] { 0x01, 0x02, 0x03 }; // Too short
        using var stream = new MemoryStream(invalidData);

        // Act
        using var resource = new ClipHeaderResource(stream);

        // Assert
        resource.HasValidData.Should().BeFalse();
    }

    [Fact]
    public void SerializationRoundTrip_PreservesData()
    {
        // Arrange
        var mockData = CreateMockBC4A5044Data();
        using var stream1 = new MemoryStream(mockData);
        using var resource1 = new ClipHeaderResource(stream1);

        // Act - Serialize and deserialize
        var serializedData = resource1.AsBytes;
        using var stream2 = new MemoryStream(serializedData);
        using var resource2 = new ClipHeaderResource(stream2);

        // Assert
        resource2.Version.Should().Be(resource1.Version);
        resource2.ClipName.Should().Be(resource1.ClipName);
        resource2.Duration.Should().Be(resource1.Duration);
        resource2.RigName.Should().Be(resource1.RigName);
        resource2.HasValidData.Should().Be(resource1.HasValidData);
    }

    [Theory]
    [InlineData("Version", 14u)]
    [InlineData("Flags", 0x08u)]
    [InlineData("Duration", 3.14f)]
    [InlineData("ClipName", "TestClip")]
    [InlineData("RigName", "TestRig")]
    [InlineData("ActorName", "TestActor")]
    public void SetProperty_WithDifferentTypes_HandlesCorrectly(string propertyName, object value)
    {
        // Arrange
        var mockData = CreateMockBC4A5044Data();
        using var stream = new MemoryStream(mockData);
        using var resource = new ClipHeaderResource(stream);

        // Act
        resource.SetProperty(propertyName, value);

        // Assert
        var retrievedValue = resource.GetProperty(propertyName);
        retrievedValue.Should().Be(value.ToString());
    }

    /// <summary>
    /// Creates mock BC4A5044 binary data that matches the format expected by ClipHeaderResource
    /// </summary>
    private static byte[] CreateMockBC4A5044Data()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);
        
        // Write version (little endian)
        writer.Write(7u); // Version 7 to include clip name
        
        // Write flags
        writer.Write(0x02u);
        
        // Write duration
        writer.Write(2.5f);
        
        // Write Quaternion (4 floats for rotation)
        writer.Write(0.0f); // qx
        writer.Write(0.0f); // qy  
        writer.Write(0.0f); // qz
        writer.Write(1.0f); // qw
        
        // Write Vector3 (3 floats for translation)
        writer.Write(0.0f); // vx
        writer.Write(0.0f); // vy
        writer.Write(0.0f); // vz
        
        // Version >= 5: reference namespace hash
        writer.Write(0x12345678u);
        
        // Version >= 7: clip name
        WriteString32(writer, "MockTestAnimation");
        
        // Rig name (always present)
        WriteString32(writer, "MockRig");
        
        // Version >= 4: explicit namespaces
        writer.Write(1u); // namespace count
        WriteString32(writer, "MockNamespace");
        
        // Slot assignment count and data
        writer.Write(0u); // No slot assignments
        
        // Event count
        writer.Write(0u); // No events  
        
        // Codec data length
        writer.Write(0u); // No codec data
        
        return ms.ToArray();
    }
    
    private static void WriteString32(BinaryWriter writer, string str)
    {
        var bytes = Encoding.UTF8.GetBytes(str);
        writer.Write((uint)bytes.Length);
        writer.Write(bytes);
    }
}
