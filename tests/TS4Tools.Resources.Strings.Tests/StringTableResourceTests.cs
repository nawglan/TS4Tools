namespace TS4Tools.Resources.Strings.Tests;

/// <summary>
/// Comprehensive tests for StringTableResource functionality including STBL format parsing,
/// string management, localization features, and modern async patterns.
/// </summary>
public class StringTableResourceTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidApiVersion_CreatesEmptyResource()
    {
        // Arrange
        const int apiVersion = 1;

        // Act
        var resource = new StringTableResource(apiVersion);

        // Assert
        resource.RequestedApiVersion.Should().Be(apiVersion);
        resource.RecommendedApiVersion.Should().Be(1);
        resource.Version.Should().Be(StringTableResource.SupportedVersion);
        resource.IsCompressed.Should().Be(0);
        resource.NumberOfEntries.Should().Be(0);
        resource.StringDataLength.Should().Be(0);
        resource.Strings.Should().BeEmpty();
        resource.IsModified.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithDefaultApiVersion_CreatesValidResource()
    {
        // Act
        var resource = new StringTableResource();

        // Assert
        resource.RequestedApiVersion.Should().Be(1);
        resource.Version.Should().Be(StringTableResource.SupportedVersion);
        resource.NumberOfEntries.Should().Be(0);
        resource.IsModified.Should().BeFalse();
    }

    #endregion

    #region String Management Tests

    [Fact]
    public void SetString_WithValidKeyAndValue_AddsString()
    {
        // Arrange
        var resource = new StringTableResource();
        const uint key = 0x12345678u;
        const string value = "Test String";

        // Act
        resource.SetString(key, value);

        // Assert
        resource.NumberOfEntries.Should().Be(1);
        resource[key].Should().Be(value);
        resource.ContainsKey(key).Should().BeTrue();
        resource.IsModified.Should().BeTrue();
    }

    [Fact]
    public void SetString_WithExistingKey_UpdatesString()
    {
        // Arrange
        var resource = new StringTableResource();
        const uint key = 0x12345678u;
        const string originalValue = "Original Value";
        const string newValue = "Updated Value";
        resource.SetString(key, originalValue);
        resource.IsModified.Should().BeTrue(); // Reset modification tracking for test

        // Act
        resource.SetString(key, newValue);

        // Assert
        resource.NumberOfEntries.Should().Be(1);
        resource[key].Should().Be(newValue);
        resource.IsModified.Should().BeTrue();
    }

    [Fact]
    public void SetString_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        var resource = new StringTableResource();
        const uint key = 0x12345678u;

        // Act & Assert
        resource.Invoking(r => r.SetString(key, null!))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("value");
    }

    [Fact]
    public void Indexer_WithValidKey_ReturnsCorrectValue()
    {
        // Arrange
        var resource = new StringTableResource();
        const uint key = 0x12345678u;
        const string value = "Test Value";
        resource.SetString(key, value);

        // Act
        var result = resource[key];

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void Indexer_WithNonExistentKey_ReturnsNull()
    {
        // Arrange
        var resource = new StringTableResource();
        const uint nonExistentKey = 0x99999999u;

        // Act
        var result = resource[nonExistentKey];

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Indexer_SetWithNullValue_RemovesString()
    {
        // Arrange
        var resource = new StringTableResource();
        const uint key = 0x12345678u;
        const string value = "Test Value";
        resource.SetString(key, value);

        // Act
        resource[key] = null;

        // Assert
        resource.ContainsKey(key).Should().BeFalse();
        resource.NumberOfEntries.Should().Be(0);
        resource.IsModified.Should().BeTrue();
    }

    [Fact]
    public void RemoveString_WithExistingKey_RemovesAndReturnsTrue()
    {
        // Arrange
        var resource = new StringTableResource();
        const uint key = 0x12345678u;
        const string value = "Test Value";
        resource.SetString(key, value);

        // Act
        var result = resource.RemoveString(key);

        // Assert
        result.Should().BeTrue();
        resource.ContainsKey(key).Should().BeFalse();
        resource.NumberOfEntries.Should().Be(0);
        resource.IsModified.Should().BeTrue();
    }

    [Fact]
    public void RemoveString_WithNonExistentKey_ReturnsFalse()
    {
        // Arrange
        var resource = new StringTableResource();
        const uint nonExistentKey = 0x99999999u;

        // Act
        var result = resource.RemoveString(nonExistentKey);

        // Assert
        result.Should().BeFalse();
        resource.IsModified.Should().BeFalse();
    }

    [Fact]
    public void Clear_WithExistingStrings_ClearsAllStrings()
    {
        // Arrange
        var resource = new StringTableResource();
        resource.SetString(0x12345678u, "String 1");
        resource.SetString(0x87654321u, "String 2");
        resource.SetString(0xABCDEF00u, "String 3");

        // Act
        resource.Clear();

        // Assert
        resource.NumberOfEntries.Should().Be(0);
        resource.Strings.Should().BeEmpty();
        resource.IsModified.Should().BeTrue();
    }

    [Fact]
    public void Clear_WithEmptyResource_DoesNotSetModified()
    {
        // Arrange
        var resource = new StringTableResource();

        // Act
        resource.Clear();

        // Assert
        resource.IsModified.Should().BeFalse();
    }

    #endregion

    #region Unicode and Encoding Tests

    [Theory]
    [InlineData("Hello World", "Simple ASCII text")]
    [InlineData("🎮🎯🎲", "Unicode emoji characters")]
    [InlineData("Тест на кириллице", "Cyrillic text")]
    [InlineData("こんにちは世界", "Japanese text")]
    [InlineData("مرحبا بالعالم", "Arabic text")]
    [InlineData("", "Empty string")]
    public void SetString_WithUnicodeText_HandlesCorrectly(string unicodeText, string description)
    {
        // Arrange
        var resource = new StringTableResource();
        const uint key = 0x12345678u;

        // Act
        resource.SetString(key, unicodeText);

        // Assert
        resource[key].Should().Be(unicodeText, description);
        resource.NumberOfEntries.Should().Be(1);
    }

    [Fact]
    public void StringDataLength_WithUnicodeStrings_CalculatesCorrectByteLength()
    {
        // Arrange
        var resource = new StringTableResource();
        const string asciiText = "Hello"; // 5 bytes
        const string unicodeText = "🎮"; // 4 bytes in UTF-8
        const string cyrillicText = "Тест"; // 8 bytes in UTF-8

        // Act
        resource.SetString(0x1u, asciiText);
        resource.SetString(0x2u, unicodeText);
        resource.SetString(0x3u, cyrillicText);

        // Assert
        // Total: (5 + 4 + 1) + (4 + 4 + 1) + (8 + 4 + 1) = 10 + 9 + 13 = 32 bytes
        // (string bytes + 4 bytes for key + 1 byte for length per entry)
        resource.StringDataLength.Should().Be(32);
    }

    #endregion

    #region Binary Format Tests

    [Fact]
    public async Task ToBinaryAsync_WithEmptyResource_CreatesValidSTBLHeader()
    {
        // Arrange
        var resource = new StringTableResource();

        // Act
        var binaryData = await resource.ToBinaryAsync();

        // Assert
        binaryData.Should().NotBeEmpty();
        binaryData.Length.Should().BeGreaterOrEqualTo(19); // Minimum header size

        // Verify magic number
        var magic = BitConverter.ToUInt32(binaryData, 0);
        magic.Should().Be(StringTableResource.MagicNumber);

        // Verify version
        var version = BitConverter.ToUInt16(binaryData, 4);
        version.Should().Be(StringTableResource.SupportedVersion);

        // Verify entry count
        var entryCount = BitConverter.ToUInt64(binaryData, 7);
        entryCount.Should().Be(0);
    }

    [Fact]
    public async Task ToBinaryAsync_WithStrings_CreatesValidSTBLData()
    {
        // Arrange
        var resource = new StringTableResource();
        resource.SetString(0x12345678u, "Test String");
        resource.SetString(0x87654321u, "Another String");

        // Act
        var binaryData = await resource.ToBinaryAsync();

        // Assert
        binaryData.Should().NotBeEmpty();

        // Verify magic number
        var magic = BitConverter.ToUInt32(binaryData, 0);
        magic.Should().Be(StringTableResource.MagicNumber);

        // Verify entry count
        var entryCount = BitConverter.ToUInt64(binaryData, 7);
        entryCount.Should().Be(2);
    }

    [Fact]
    public async Task FromData_WithValidSTBLData_CreatesCorrectResource()
    {
        // Arrange
        var originalResource = new StringTableResource();
        originalResource.SetString(0x12345678u, "Test String");
        originalResource.SetString(0x87654321u, "Another String");
        var binaryData = await originalResource.ToBinaryAsync();

        // Act
        var parsedResource = StringTableResource.FromData(1, binaryData);

        // Assert
        parsedResource.NumberOfEntries.Should().Be(2);
        parsedResource[0x12345678u].Should().Be("Test String");
        parsedResource[0x87654321u].Should().Be("Another String");
        parsedResource.Version.Should().Be(StringTableResource.SupportedVersion);
        parsedResource.IsModified.Should().BeFalse(); // Newly parsed resources are not modified
    }

    [Fact]
    public async Task FromStreamAsync_WithValidStream_CreatesCorrectResource()
    {
        // Arrange
        var originalResource = new StringTableResource();
        originalResource.SetString(0x12345678u, "Stream Test");
        var binaryData = await originalResource.ToBinaryAsync();
        using var stream = new MemoryStream(binaryData);

        // Act
        var parsedResource = await StringTableResource.FromStreamAsync(1, stream);

        // Assert
        parsedResource.NumberOfEntries.Should().Be(1);
        parsedResource[0x12345678u].Should().Be("Stream Test");
        parsedResource.IsModified.Should().BeFalse();
    }

    [Fact]
    public void FromData_WithInvalidMagicNumber_ThrowsArgumentException()
    {
        // Arrange
        var invalidData = new byte[19];
        BitConverter.GetBytes(0xDEADBEEFu).CopyTo(invalidData, 0); // Invalid magic

        // Act & Assert
        Action act = () => StringTableResource.FromData(1, invalidData);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid magic number*");
    }

    [Fact]
    public void FromData_WithTooShortData_ThrowsArgumentException()
    {
        // Arrange
        var shortData = new byte[10]; // Too short for header

        // Act & Assert
        Action act = () => StringTableResource.FromData(1, shortData);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Data too short*");
    }

    [Fact]
    public async Task FromStreamAsync_WithNullStream_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => StringTableResource.FromStreamAsync(1, null!));
    }

    #endregion

    #region Content Fields and IResource Implementation Tests

    [Fact]
    public void ContentFields_ReturnsExpectedFields()
    {
        // Arrange
        var resource = new StringTableResource();

        // Act
        var contentFields = resource.ContentFields;

        // Assert
        contentFields.Should().Contain(nameof(StringTableResource.Version));
        contentFields.Should().Contain(nameof(StringTableResource.IsCompressed));
        contentFields.Should().Contain(nameof(StringTableResource.NumberOfEntries));
        contentFields.Should().Contain(nameof(StringTableResource.StringDataLength));
        contentFields.Should().Contain(nameof(StringTableResource.Strings));
        contentFields.Should().HaveCount(5);
    }

    [Theory]
    [InlineData(nameof(StringTableResource.Version), typeof(ushort))]
    [InlineData(nameof(StringTableResource.IsCompressed), typeof(byte))]
    [InlineData(nameof(StringTableResource.NumberOfEntries), typeof(ulong))]
    [InlineData(nameof(StringTableResource.StringDataLength), typeof(uint))]
    [InlineData(nameof(StringTableResource.Strings), typeof(IReadOnlyDictionary<uint, string>))]
    public void StringIndexer_WithValidFieldName_ReturnsCorrectTypedValue(string fieldName, Type expectedType)
    {
        // Arrange
        var resource = new StringTableResource();
        resource.SetString(0x12345678u, "Test"); // Add a string for testing

        // Act
        var typedValue = resource[fieldName];

        // Assert
        typedValue.Type.Should().Be(expectedType);
        typedValue.Value.Should().NotBeNull();
    }

    [Fact]
    public void StringIndexer_WithInvalidFieldName_ThrowsArgumentException()
    {
        // Arrange
        var resource = new StringTableResource();
        const string invalidField = "NonExistentField";

        // Act & Assert
        resource.Invoking(r => r[invalidField])
            .Should().Throw<ArgumentException>()
            .WithMessage($"*Unknown field: {invalidField}*")
            .WithParameterName("index");
    }

    [Theory]
    [InlineData(0, nameof(StringTableResource.Version))]
    [InlineData(1, nameof(StringTableResource.IsCompressed))]
    [InlineData(2, nameof(StringTableResource.NumberOfEntries))]
    [InlineData(3, nameof(StringTableResource.StringDataLength))]
    [InlineData(4, nameof(StringTableResource.Strings))]
    public void IntegerIndexer_WithValidIndex_ReturnsCorrectField(int index, string expectedField)
    {
        // Arrange
        var resource = new StringTableResource();

        // Act
        var typedValue = resource[index];

        // Assert
        typedValue.Should().Be(resource[expectedField]);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    [InlineData(10)]
    public void IntegerIndexer_WithInvalidIndex_ThrowsArgumentOutOfRangeException(int invalidIndex)
    {
        // Arrange
        var resource = new StringTableResource();

        // Act & Assert
        resource.Invoking(r => r[invalidIndex])
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("index");
    }

    [Fact]
    public void StringIndexer_SetValue_ThrowsNotSupportedException()
    {
        // Arrange
        var resource = new StringTableResource();
        var typedValue = new TypedValue(typeof(ushort), (ushort)5);

        // Act & Assert
        resource.Invoking(r => r[nameof(StringTableResource.Version)] = typedValue)
            .Should().Throw<NotSupportedException>()
            .WithMessage("*read-only*");
    }

    [Fact]
    public void IntegerIndexer_SetValue_ThrowsNotSupportedException()
    {
        // Arrange
        var resource = new StringTableResource();
        var typedValue = new TypedValue(typeof(ushort), (ushort)5);

        // Act & Assert
        resource.Invoking(r => r[0] = typedValue)
            .Should().Throw<NotSupportedException>()
            .WithMessage("*read-only*");
    }

    #endregion

    #region Resource Change Event Tests

    [Fact]
    public void SetString_WhenCalled_RaisesResourceChangedEvent()
    {
        // Arrange
        var resource = new StringTableResource();
        var eventRaised = false;
        resource.ResourceChanged += (_, _) => eventRaised = true;

        // Act
        resource.SetString(0x12345678u, "Test");

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void RemoveString_WhenSuccessful_RaisesResourceChangedEvent()
    {
        // Arrange
        var resource = new StringTableResource();
        resource.SetString(0x12345678u, "Test");
        var eventRaised = false;
        resource.ResourceChanged += (_, _) => eventRaised = true;

        // Act
        resource.RemoveString(0x12345678u);

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void RemoveString_WhenKeyNotFound_DoesNotRaiseResourceChangedEvent()
    {
        // Arrange
        var resource = new StringTableResource();
        var eventRaised = false;
        resource.ResourceChanged += (_, _) => eventRaised = true;

        // Act
        resource.RemoveString(0x12345678u);

        // Assert
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public void Clear_WithExistingData_RaisesResourceChangedEvent()
    {
        // Arrange
        var resource = new StringTableResource();
        resource.SetString(0x12345678u, "Test");
        var eventRaised = false;
        resource.ResourceChanged += (_, _) => eventRaised = true;

        // Act
        resource.Clear();

        // Assert
        eventRaised.Should().BeTrue();
    }

    #endregion

    #region GetEntries Tests

    [Fact]
    public void GetEntries_WithMultipleStrings_ReturnsCorrectStringEntries()
    {
        // Arrange
        var resource = new StringTableResource();
        var expectedEntries = new Dictionary<uint, string>
        {
            { 0x12345678u, "String One" },
            { 0x87654321u, "String Two" },
            { 0xABCDEF00u, "String Three" }
        };

        foreach (var kvp in expectedEntries)
        {
            resource.SetString(kvp.Key, kvp.Value);
        }

        // Act
        var entries = resource.GetEntries().ToList();

        // Assert
        entries.Should().HaveCount(3);
        foreach (var entry in entries)
        {
            expectedEntries.Should().ContainKey(entry.Key);
            expectedEntries[entry.Key].Should().Be(entry.Value);
        }
    }

    [Fact]
    public void GetEntries_WithEmptyResource_ReturnsEmptyCollection()
    {
        // Arrange
        var resource = new StringTableResource();

        // Act
        var entries = resource.GetEntries();

        // Assert
        entries.Should().BeEmpty();
    }

    #endregion

    #region Stream and Byte Access Tests

    [Fact]
    public void Stream_Property_ReturnsValidMemoryStream()
    {
        // Arrange
        var resource = new StringTableResource();
        resource.SetString(0x12345678u, "Test");

        // Act
        using var stream = resource.Stream;

        // Assert
        stream.Should().NotBeNull();
        stream.Should().BeOfType<MemoryStream>();
        stream.Length.Should().BeGreaterThan(0);
        stream.CanRead.Should().BeTrue();
    }

    [Fact]
    public void AsBytes_Property_ReturnsValidByteArray()
    {
        // Arrange
        var resource = new StringTableResource();
        resource.SetString(0x12345678u, "Test");

        // Act
        var bytes = resource.AsBytes;

        // Assert
        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(0);
        
        // Verify magic number in bytes
        var magic = BitConverter.ToUInt32(bytes, 0);
        magic.Should().Be(StringTableResource.MagicNumber);
    }

    #endregion

    #region Disposal and Error Handling Tests

    [Fact]
    public void Dispose_WhenCalled_MarksResourceAsDisposed()
    {
        // Arrange
        var resource = new StringTableResource();
        resource.SetString(0x12345678u, "Test");

        // Act
        resource.Dispose();

        // Assert
        resource.Invoking(r => r.SetString(0x99999999u, "New String"))
            .Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void SetString_AfterDisposal_ThrowsObjectDisposedException()
    {
        // Arrange
        var resource = new StringTableResource();
        resource.Dispose();

        // Act & Assert
        resource.Invoking(r => r.SetString(0x12345678u, "Test"))
            .Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void GetEntries_AfterDisposal_ThrowsObjectDisposedException()
    {
        // Arrange
        var resource = new StringTableResource();
        resource.Dispose();

        // Act & Assert
        resource.Invoking(r => r.GetEntries().ToList())
            .Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void ToString_WithValidResource_ReturnsDescriptiveString()
    {
        // Arrange
        var resource = new StringTableResource();
        resource.SetString(0x12345678u, "Test String");

        // Act
        var result = resource.ToString();

        // Assert
        result.Should().Contain("StringTableResource");
        result.Should().Contain("Version: 5");
        result.Should().Contain("Entries: 1");
        result.Should().Contain("Data Length:");
    }

    [Fact]
    public void ToString_AfterDisposal_ReturnsDisposedMessage()
    {
        // Arrange
        var resource = new StringTableResource();
        resource.Dispose();

        // Act
        var result = resource.ToString();

        // Assert
        result.Should().Be("StringTableResource (Disposed)");
    }

    #endregion

    #region Cancellation Token Tests

    [Fact]
    public async Task ToBinaryAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var resource = new StringTableResource();
        resource.SetString(0x12345678u, "Test");
        var cancelledToken = new CancellationToken(true);

        // Act & Assert
        await resource.Invoking(r => r.ToBinaryAsync(null, cancelledToken))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task FromStreamAsync_WithCancelledToken_ThrowsTaskCanceledException()
    {
        // Arrange
        using var stream = new MemoryStream();
        var cancelledToken = new CancellationToken(true);

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => StringTableResource.FromStreamAsync(1, stream, null, cancelledToken));
    }

    #endregion
}
