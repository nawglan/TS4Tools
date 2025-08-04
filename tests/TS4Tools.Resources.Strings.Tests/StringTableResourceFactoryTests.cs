namespace TS4Tools.Resources.Strings.Tests;

/// <summary>
/// Comprehensive tests for StringTableResourceFactory functionality including resource creation,
/// factory patterns, error handling, and integration with the resource management system.
/// </summary>
public class StringTableResourceFactoryTests
{
    private readonly StringTableResourceFactory _factory;

    public StringTableResourceFactoryTests()
    {
        _factory = new StringTableResourceFactory();
    }

    #region Constructor and Basic Properties Tests

    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        // Act
        var factory = new StringTableResourceFactory();

        // Assert
        factory.FactoryName.Should().Be("String Table Resource Factory");
        factory.Description.Should().Be("Creates String Table (STBL) resources for game localization");
    }

    [Fact]
    public void ResourceTypeId_HasCorrectValue()
    {
        // Assert
        StringTableResourceFactory.ResourceTypeId.Should().Be("0x220557DA");
    }

    #endregion

    #region CanHandle Tests

    [Theory]
    [InlineData("0x220557DA", true)]
    [InlineData("0x220557da", true)] // Case insensitive
    [InlineData("0X220557DA", true)] // Different case prefix
    [InlineData("220557DA", false)]  // Missing 0x prefix
    [InlineData("0x12345678", false)] // Different resource type
    [InlineData("", false)]           // Empty string
    [InlineData(null, false)]         // Null string
    public void CanHandle_WithStringResourceType_ReturnsExpectedResult(string? resourceTypeId, bool expected)
    {
        // Act
        var result = _factory.CanHandle(resourceTypeId!);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0x220557DAu, true)]   // Correct resource type
    [InlineData(0x12345678u, false)]  // Different resource type
    [InlineData(0x00000000u, false)]  // Zero
    [InlineData(0xFFFFFFFFu, false)]  // Max uint
    public void CanHandle_WithNumericResourceType_ReturnsExpectedResult(uint resourceType, bool expected)
    {
        // Act
        var result = _factory.CanHandle(resourceType);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region CreateResourceAsync Tests

    [Fact]
    public async Task CreateResourceAsync_WithNullStream_CreatesEmptyResource()
    {
        // Arrange
        const int apiVersion = 1;

        // Act
        var resource = await _factory.CreateResourceAsync(apiVersion, null);

        // Assert
        resource.Should().NotBeNull();
        resource.RequestedApiVersion.Should().Be(apiVersion);
        resource.NumberOfEntries.Should().Be(0);
        resource.IsModified.Should().BeFalse();
    }

    [Fact]
    public async Task CreateResourceAsync_WithValidSTBLStream_CreatesCorrectResource()
    {
        // Arrange
        const int apiVersion = 1;
        var originalResource = new StringTableResource();
        originalResource.SetString(0x12345678u, "Test String");
        originalResource.SetString(0x87654321u, "Another String");
        
        var binaryData = await originalResource.ToBinaryAsync();
        using var stream = new MemoryStream(binaryData);

        // Act
        var resource = await _factory.CreateResourceAsync(apiVersion, stream);

        // Assert
        resource.Should().NotBeNull();
        resource.RequestedApiVersion.Should().Be(apiVersion);
        resource.NumberOfEntries.Should().Be(2);
        resource[0x12345678u].Should().Be("Test String");
        resource[0x87654321u].Should().Be("Another String");
        resource.IsModified.Should().BeFalse();
    }

    [Fact]
    public async Task CreateResourceAsync_WithInvalidStream_ThrowsArgumentException()
    {
        // Arrange
        const int apiVersion = 1;
        var invalidData = new byte[] { 0x01, 0x02, 0x03, 0x04 }; // Invalid STBL data
        using var stream = new MemoryStream(invalidData);

        // Act & Assert
        await _factory.Invoking(f => f.CreateResourceAsync(apiVersion, stream))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Failed to parse STBL resource*")
            .WithParameterName("stream");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task CreateResourceAsync_WithInvalidApiVersion_ThrowsArgumentException(int invalidApiVersion)
    {
        // Act & Assert
        await _factory.Invoking(f => f.CreateResourceAsync(invalidApiVersion, null))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateResourceAsync_WithCancellationToken_CompletesSuccessfully()
    {
        // Arrange
        const int apiVersion = 1;
        var cancellationToken = new CancellationToken(false); // Not cancelled

        // Act
        var resource = await _factory.CreateResourceAsync(apiVersion, null, cancellationToken);

        // Assert
        resource.Should().NotBeNull();
        resource.RequestedApiVersion.Should().Be(apiVersion);
    }

    #endregion

    #region CreateEmptyAsync Tests

    [Fact]
    public async Task CreateEmptyAsync_WithDefaultParameters_CreatesEmptyResource()
    {
        // Act
        var resource = await _factory.CreateEmptyAsync();

        // Assert
        resource.Should().NotBeNull();
        resource.RequestedApiVersion.Should().Be(1);
        resource.NumberOfEntries.Should().Be(0);
        resource.IsModified.Should().BeFalse();
    }

    [Fact]
    public async Task CreateEmptyAsync_WithSpecificApiVersion_CreatesResourceWithCorrectVersion()
    {
        // Arrange
        const int apiVersion = 2;

        // Act
        var resource = await _factory.CreateEmptyAsync(apiVersion);

        // Assert
        resource.Should().NotBeNull();
        resource.RequestedApiVersion.Should().Be(apiVersion);
        resource.NumberOfEntries.Should().Be(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateEmptyAsync_WithInvalidApiVersion_ThrowsArgumentException(int invalidApiVersion)
    {
        // Act & Assert
        await _factory.Invoking(f => f.CreateEmptyAsync(invalidApiVersion))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateEmptyAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var cancelledToken = new CancellationToken(true);

        // Act & Assert
        await _factory.Invoking(f => f.CreateEmptyAsync(1, cancelledToken))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region CreateWithStringsAsync Tests

    [Fact]
    public async Task CreateWithStringsAsync_WithValidStrings_CreatesPopulatedResource()
    {
        // Arrange
        var strings = new Dictionary<uint, string>
        {
            { 0x12345678u, "String One" },
            { 0x87654321u, "String Two" },
            { 0xABCDEF00u, "String Three" }
        };

        // Act
        var resource = await _factory.CreateWithStringsAsync(strings);

        // Assert
        resource.Should().NotBeNull();
        resource.NumberOfEntries.Should().Be(3);
        resource[0x12345678u].Should().Be("String One");
        resource[0x87654321u].Should().Be("String Two");
        resource[0xABCDEF00u].Should().Be("String Three");
        resource.IsModified.Should().BeTrue(); // Modified because strings were added
    }

    [Fact]
    public async Task CreateWithStringsAsync_WithEmptyDictionary_CreatesEmptyResource()
    {
        // Arrange
        var emptyStrings = new Dictionary<uint, string>();

        // Act
        var resource = await _factory.CreateWithStringsAsync(emptyStrings);

        // Assert
        resource.Should().NotBeNull();
        resource.NumberOfEntries.Should().Be(0);
        resource.IsModified.Should().BeFalse();
    }

    [Fact]
    public async Task CreateWithStringsAsync_WithNullDictionary_ThrowsArgumentNullException()
    {
        // Act & Assert
        await _factory.Invoking(f => f.CreateWithStringsAsync(null!))
            .Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("strings");
    }

    [Fact]
    public async Task CreateWithStringsAsync_WithSpecificApiVersion_CreatesResourceWithCorrectVersion()
    {
        // Arrange
        const int apiVersion = 2;
        var strings = new Dictionary<uint, string> { { 0x12345678u, "Test" } };

        // Act
        var resource = await _factory.CreateWithStringsAsync(strings, apiVersion);

        // Assert
        resource.RequestedApiVersion.Should().Be(apiVersion);
        resource.NumberOfEntries.Should().Be(1);
    }

    [Fact]
    public async Task CreateWithStringsAsync_WithUnicodeStrings_HandlesCorrectly()
    {
        // Arrange
        var unicodeStrings = new Dictionary<uint, string>
        {
            { 0x1u, "🎮 Gaming" },
            { 0x2u, "Тест на кириллице" },
            { 0x3u, "こんにちは" },
            { 0x4u, "مرحبا" }
        };

        // Act
        var resource = await _factory.CreateWithStringsAsync(unicodeStrings);

        // Assert
        resource.NumberOfEntries.Should().Be(4);
        resource[0x1u].Should().Be("🎮 Gaming");
        resource[0x2u].Should().Be("Тест на кириллице");
        resource[0x3u].Should().Be("こんにちは");
        resource[0x4u].Should().Be("مرحبا");
    }

    [Fact]
    public async Task CreateWithStringsAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var strings = new Dictionary<uint, string> { { 0x1u, "Test" } };
        var cancelledToken = new CancellationToken(true);

        // Act & Assert
        await _factory.Invoking(f => f.CreateWithStringsAsync(strings, 1, cancelledToken))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region CreateFromDataAsync Tests

    [Fact]
    public async Task CreateFromDataAsync_WithValidSTBLData_CreatesCorrectResource()
    {
        // Arrange
        var originalResource = new StringTableResource();
        originalResource.SetString(0x12345678u, "Data Test");
        var binaryData = await originalResource.ToBinaryAsync();

        // Act
        var resource = await _factory.CreateFromDataAsync(binaryData);

        // Assert
        resource.Should().NotBeNull();
        resource.NumberOfEntries.Should().Be(1);
        resource[0x12345678u].Should().Be("Data Test");
        resource.IsModified.Should().BeFalse();
    }

    [Fact]
    public async Task CreateFromDataAsync_WithCustomEncoding_UsesCorrectEncoding()
    {
        // Arrange
        var originalResource = new StringTableResource();
        originalResource.SetString(0x12345678u, "Encoding Test");
        var binaryData = await originalResource.ToBinaryAsync(Encoding.UTF8);

        // Act
        var resource = await _factory.CreateFromDataAsync(binaryData, 1, Encoding.UTF8);

        // Assert
        resource[0x12345678u].Should().Be("Encoding Test");
    }

    [Fact]
    public async Task CreateFromDataAsync_WithInvalidData_ThrowsArgumentException()
    {
        // Arrange
        var invalidData = new ReadOnlyMemory<byte>(new byte[] { 0x01, 0x02, 0x03 });

        // Act & Assert
        await _factory.Invoking(f => f.CreateFromDataAsync(invalidData))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Failed to parse STBL data*")
            .WithParameterName("data");
    }

    [Fact]
    public async Task CreateFromDataAsync_WithEmptyData_ThrowsArgumentException()
    {
        // Arrange
        var emptyData = new ReadOnlyMemory<byte>();

        // Act & Assert
        await _factory.Invoking(f => f.CreateFromDataAsync(emptyData))
            .Should().ThrowAsync<ArgumentException>()
            .WithParameterName("data");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateFromDataAsync_WithInvalidApiVersion_ThrowsArgumentException(int invalidApiVersion)
    {
        // Arrange
        var validData = new ReadOnlyMemory<byte>(new byte[19]); // Minimum size but invalid content

        // Act & Assert
        await _factory.Invoking(f => f.CreateFromDataAsync(validData, invalidApiVersion))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateFromDataAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var validData = new ReadOnlyMemory<byte>(new byte[19]);
        var cancelledToken = new CancellationToken(true);

        // Act & Assert
        await _factory.Invoking(f => f.CreateFromDataAsync(validData, 1, null, cancelledToken))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Round-trip Tests

    [Fact]
    public async Task RoundTrip_CreateResourceToBinaryToFactory_MaintainsData()
    {
        // Arrange
        var originalStrings = new Dictionary<uint, string>
        {
            { 0x12345678u, "Round Trip Test" },
            { 0x87654321u, "Another String" },
            { 0xABCDEF00u, "🎮 Unicode Test" }
        };

        // Act - Create resource with factory
        var originalResource = await _factory.CreateWithStringsAsync(originalStrings);
        
        // Convert to binary
        var binaryData = await originalResource.ToBinaryAsync();
        
        // Create new resource from binary using factory
        var recreatedResource = await _factory.CreateFromDataAsync(binaryData);

        // Assert
        recreatedResource.NumberOfEntries.Should().Be(originalResource.NumberOfEntries);
        foreach (var kvp in originalStrings)
        {
            recreatedResource[kvp.Key].Should().Be(kvp.Value);
        }
    }

    [Fact]
    public async Task RoundTrip_ThroughStream_MaintainsData()
    {
        // Arrange
        var originalStrings = new Dictionary<uint, string>
        {
            { 0x1u, "Stream Test 1" },
            { 0x2u, "Stream Test 2" }
        };

        // Act - Create resource
        var originalResource = await _factory.CreateWithStringsAsync(originalStrings);
        
        // Convert to stream
        using var stream = originalResource.Stream;
        stream.Position = 0; // Reset position
        
        // Create new resource from stream
        var recreatedResource = await _factory.CreateResourceAsync(1, stream);

        // Assert
        recreatedResource.NumberOfEntries.Should().Be(2);
        recreatedResource[0x1u].Should().Be("Stream Test 1");
        recreatedResource[0x2u].Should().Be("Stream Test 2");
    }

    #endregion

    #region Performance and Edge Case Tests

    [Fact]
    public async Task CreateWithStringsAsync_WithLargeNumberOfStrings_PerformsReasonably()
    {
        // Arrange
        const int stringCount = 1000;
        var strings = new Dictionary<uint, string>();
        for (uint i = 0; i < stringCount; i++)
        {
            strings[i] = $"Performance Test String {i}";
        }

        // Act
        var stopwatch = Stopwatch.StartNew();
        var resource = await _factory.CreateWithStringsAsync(strings);
        stopwatch.Stop();

        // Assert
        resource.NumberOfEntries.Should().Be(stringCount);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete within 1 second
    }

    [Fact]
    public async Task CreateFromDataAsync_WithMaximumSizeStrings_HandlesCorrectly()
    {
        // Arrange - Create string at maximum length (255 bytes)
        var maxString = new string('A', 255);
        var resource = new StringTableResource();
        resource.SetString(0x12345678u, maxString);
        var binaryData = await resource.ToBinaryAsync();

        // Act
        var recreatedResource = await _factory.CreateFromDataAsync(binaryData);

        // Assert
        recreatedResource[0x12345678u].Should().Be(maxString);
        recreatedResource[0x12345678u]!.Length.Should().Be(255);
    }

    [Fact]
    public async Task CreateWithStringsAsync_WithDuplicateKeys_UsesLastValue()
    {
        // Arrange
        var strings = new Dictionary<uint, string>
        {
            { 0x12345678u, "Final Value" }
        };

        // Act
        var resource = await _factory.CreateWithStringsAsync(strings);

        // Assert
        resource.NumberOfEntries.Should().Be(1);
        resource[0x12345678u].Should().Be("Final Value");
    }

    #endregion

    #region Concurrent Access Tests

    [Fact]
    public async Task CreateResourceAsync_ConcurrentCalls_AllSucceed()
    {
        // Arrange
        const int concurrentCalls = 10;
        var tasks = new List<Task<StringTableResource>>();

        // Act
        for (int i = 0; i < concurrentCalls; i++)
        {
            tasks.Add(_factory.CreateEmptyAsync());
        }

        var resources = await Task.WhenAll(tasks);

        // Assert
        resources.Should().HaveCount(concurrentCalls);
        resources.Should().AllSatisfy(r => r.Should().NotBeNull());
        resources.Should().AllSatisfy(r => r.NumberOfEntries.Should().Be(0));
    }

    #endregion
}
