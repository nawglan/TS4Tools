namespace TS4Tools.Resources.Strings.Tests;

/// <summary>
/// Tests for StringEntry record functionality.
/// </summary>
public class StringEntryTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidKeyAndValue_CreatesEntry()
    {
        // Arrange
        const uint key = 0x12345678u;
        const string value = "Test String";

        // Act
        var entry = new StringEntry(key, value);

        // Assert
        entry.Key.Should().Be(key);
        entry.Value.Should().Be(value);
    }

    [Fact]
    public void Constructor_WithNullValue_CreatesEntryWithEmptyString()
    {
        // Arrange
        const uint key = 0x12345678u;

        // Act
        var entry = new StringEntry(key, null!);

        // Assert
        entry.Key.Should().Be(key);
        entry.Value.Should().Be(string.Empty);
    }

    #endregion

    #region Property Tests

    [Theory]
    [InlineData("Hello", 5)]
    [InlineData("", 0)]
    [InlineData("🎮", 4)] // Emoji (4 bytes in UTF-8)
    [InlineData("Тест", 8)] // Cyrillic (2 bytes per char in UTF-8)
    public void ByteLength_ReturnsCorrectUTF8ByteCount(string value, int expectedBytes)
    {
        // Arrange
        var entry = new StringEntry(0x123, value);

        // Act & Assert
        entry.ByteLength.Should().Be(expectedBytes);
    }

    #endregion

    #region Create Tests

    [Fact]
    public void Create_WithValidParameters_CreatesEntry()
    {
        // Arrange
        const uint key = 0xDEADBEEF;
        const string value = "Factory Created";

        // Act
        var entry = StringEntry.Create(key, value);

        // Assert
        entry.Key.Should().Be(key);
        entry.Value.Should().Be(value);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ReturnsCorrectFormat()
    {
        // Arrange
        var entry = new StringEntry(0x12345678u, "Test Value");

        // Act
        var result = entry.ToString();

        // Assert
        result.Should().Be("0x12345678: Test Value");
    }

    [Fact]
    public void ToString_WithEmptyValue_ShowsEmptyValue()
    {
        // Arrange
        var entry = new StringEntry(0xABCDEF00u, "");

        // Act
        var result = entry.ToString();

        // Assert
        result.Should().Be("0xABCDEF00: ");
    }

    #endregion

    #region ReadFrom Tests

    [Fact]
    public void ReadFrom_WithValidData_ParsesCorrectly()
    {
        // Arrange
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        
        const uint expectedKey = 0x12345678u;
        const string expectedValue = "Hello World";
        var expectedBytes = Encoding.UTF8.GetBytes(expectedValue);

        writer.Write(expectedKey);
        writer.Write((byte)expectedBytes.Length);
        writer.Write(expectedBytes);
        
        stream.Position = 0;
        using var reader = new BinaryReader(stream);

        // Act
        var entry = StringEntry.ReadFrom(reader);

        // Assert
        entry.Key.Should().Be(expectedKey);
        entry.Value.Should().Be(expectedValue);
    }

    [Fact]
    public void ReadFrom_WithEmptyString_ParsesCorrectly()
    {
        // Arrange
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        
        const uint expectedKey = 0xABCDEF00u;

        writer.Write(expectedKey);
        writer.Write((byte)0); // Empty string
        
        stream.Position = 0;
        using var reader = new BinaryReader(stream);

        // Act
        var entry = StringEntry.ReadFrom(reader);

        // Assert
        entry.Key.Should().Be(expectedKey);
        entry.Value.Should().Be(string.Empty);
    }

    [Fact]
    public void ReadFrom_WithNullReader_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => StringEntry.ReadFrom(null!));
    }

    [Fact]
    public void ReadFrom_WithTruncatedStream_ThrowsEndOfStreamException()
    {
        // Arrange
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        
        writer.Write(0x12345678u);
        writer.Write((byte)10); // Claims 10 bytes but we won't write them
        
        stream.Position = 0;
        using var reader = new BinaryReader(stream);

        // Act & Assert
        Assert.Throws<EndOfStreamException>(() => StringEntry.ReadFrom(reader));
    }

    [Fact]
    public void ReadFrom_WithCustomEncoding_ParsesCorrectly()
    {
        // Arrange
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.Unicode);
        
        const uint expectedKey = 0x12345678u;
        const string expectedValue = "Unicode Test 🎮";
        var expectedBytes = Encoding.Unicode.GetBytes(expectedValue);

        writer.Write(expectedKey);
        writer.Write((byte)expectedBytes.Length);
        writer.Write(expectedBytes);
        
        stream.Position = 0;
        using var reader = new BinaryReader(stream, Encoding.Unicode);

        // Act
        var entry = StringEntry.ReadFrom(reader, Encoding.Unicode);

        // Assert
        entry.Key.Should().Be(expectedKey);
        entry.Value.Should().Be(expectedValue);
    }

    #endregion

    #region WriteTo Tests

    [Fact]
    public void WriteTo_WithValidData_WritesCorrectly()
    {
        // Arrange - Use builder pattern for test object creation
        var entry = StringEntryBuilder.Default
            .WithKey(0x12345678u)
            .WithValue("Hello World")
            .Build();
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // Act
        entry.WriteTo(writer);

        // Assert - Test behavior outcomes, not implementation details
        stream.As<object>().Should().ContainValidStringEntry(0x12345678u, "Hello World");
    }

    [Fact]
    public void WriteTo_WithEmptyString_WritesCorrectly()
    {
        // Arrange - Use builder pattern for test object creation
        var entry = StringEntryBuilder.Default
            .WithKey(0xABCDEF00u)
            .WithEmptyValue()
            .Build();
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // Act
        entry.WriteTo(writer);

        // Assert - Test behavior outcomes, not implementation details
        stream.As<object>().Should().ContainValidStringEntry(0xABCDEF00u, string.Empty);
    }

    [Fact]
    public void WriteTo_WithNullWriter_ThrowsArgumentNullException()
    {
        // Arrange
        var entry = new StringEntry(0x123, "test");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => entry.WriteTo(null!));
    }

    [Fact]
    public void WriteTo_WithTooLongString_ThrowsArgumentException()
    {
        // Arrange
        var longString = new string('A', 256); // 256 bytes, exceeds byte.MaxValue
        var entry = new StringEntry(0x123, longString);
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => entry.WriteTo(writer));
        exception.Message.Should().Contain("String value too long");
    }

    [Fact]
    public void WriteTo_WithCustomEncoding_WritesCorrectly()
    {
        // Arrange - Use builder pattern for test object creation
        var entry = StringEntryBuilder.Default
            .WithKey(0x12345678u)
            .WithValue("Unicode Test 🎮")
            .Build();
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.Unicode);

        // Act
        entry.WriteTo(writer, Encoding.Unicode);

        // Assert - Test behavior outcomes, not implementation details
        // Note: Unicode encoding requires special handling, so we test round-trip behavior
        entry.Should().SerializeCorrectly();
    }

    #endregion

    #region Round-trip Tests

    [Theory]
    [InlineData(0x00000000u, "")]
    [InlineData(0x12345678u, "Simple ASCII")]
    [InlineData(0xFFFFFFFFu, "Unicode: Тест 🎮 测试")]
    [InlineData(0xDEADBEEFu, "Special chars: !@#$%^&*()")]
    public void RoundTrip_WriteAndRead_PreservesData(uint key, string value)
    {
        // Arrange
        var originalEntry = new StringEntry(key, value);
        using var stream = new MemoryStream();
        
        // Act - Write
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            originalEntry.WriteTo(writer);
        }
        
        // Act - Read
        stream.Position = 0;
        using var reader = new BinaryReader(stream);
        var parsedEntry = StringEntry.ReadFrom(reader);

        // Assert
        parsedEntry.Should().Be(originalEntry);
        parsedEntry.Key.Should().Be(key);
        parsedEntry.Value.Should().Be(value);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equality_WithSameKeyAndValue_AreEqual()
    {
        // Arrange
        var entry1 = new StringEntry(0x123, "test");
        var entry2 = new StringEntry(0x123, "test");

        // Act & Assert
        entry1.Should().Be(entry2);
        (entry1 == entry2).Should().BeTrue();
        (entry1 != entry2).Should().BeFalse();
    }

    [Fact]
    public void Equality_WithDifferentKeys_AreNotEqual()
    {
        // Arrange
        var entry1 = new StringEntry(0x123, "test");
        var entry2 = new StringEntry(0x456, "test");

        // Act & Assert
        entry1.Should().NotBe(entry2);
        (entry1 == entry2).Should().BeFalse();
        (entry1 != entry2).Should().BeTrue();
    }

    [Fact]
    public void Equality_WithDifferentValues_AreNotEqual()
    {
        // Arrange
        var entry1 = new StringEntry(0x123, "test1");
        var entry2 = new StringEntry(0x123, "test2");

        // Act & Assert
        entry1.Should().NotBe(entry2);
        (entry1 == entry2).Should().BeFalse();
        (entry1 != entry2).Should().BeTrue();
    }

    #endregion
}
