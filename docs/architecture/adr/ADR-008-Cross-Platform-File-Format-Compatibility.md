# ADR-008: Cross-Platform File Format Compatibility

**Status:** Accepted
**Date:** August 8, 2025
**Deciders:** Architecture Team, Cross-Platform Team

## Context

The greenfield migration requires TS4Tools to work identically across Windows, Linux, and macOS while maintaining perfect compatibility with The Sims 4 package files. The DBPF (DataBase Packed File) format and associated resource types have specific binary layouts, endianness requirements, and compression algorithms that must work identically across all platforms.

Key compatibility challenges:

- Binary file format parsing must be platform-agnostic
- Compression algorithms (ZLIB, RefPack, etc.) must produce identical output
- File path handling differences between operating systems
- Native dependency requirements (DDS compression DLLs)
- Memory alignment and struct packing differences

## Decision

We will implement a **comprehensive cross-platform compatibility layer** that ensures byte-perfect file format compatibility across all supported platforms while providing platform-specific optimizations where beneficial.

## Rationale

### Compatibility Requirements Matrix

| Component | Windows | Linux | macOS | Compatibility Risk |
|-----------|---------|-------|-------|-------------------|
| **DBPF Package Format** | âœ… Reference | â“ Untested | â“ Untested | **CRITICAL** |
| **Resource Type Parsing** | âœ… Reference | â“ Untested | â“ Untested | **HIGH** |
| **Compression Algorithms** | âœ… Native DLLs | âš ï¸ Managed only | âš ï¸ Managed only | **HIGH** |
| **File I/O Operations** | âœ… Tested | âš ï¸ Path differences | âš ï¸ Path differences | **MEDIUM** |
| **Memory Layout** | âœ… x64 tested | âš ï¸ Endianness | âš ï¸ Endianness | **HIGH** |

### Critical Success Factors

1. **Byte-Perfect Compatibility**: Package files must be identical regardless of platform
1. **Performance Parity**: Cross-platform version must not significantly regress performance
1. **Format Preservation**: All compression and serialization must maintain exact formatting
1. **Path Handling**: File paths must work correctly on all target filesystems

## Implementation Strategy

### Platform Abstraction Layer

```csharp
public interface IPlatformCompatibilityService
{
    Task<byte[]> CompressDataAsync(byte[] data, CompressionType type);
    Task<byte[]> DecompressDataAsync(byte[] data, CompressionType type);
    string NormalizePath(string path);
    bool ValidateFileSystemCompatibility(string path);
    PlatformInfo GetPlatformInfo();
    Task<bool> ValidateByteOrderAsync();
}

public class CrossPlatformCompatibilityService : IPlatformCompatibilityService
{
    private readonly ILogger<CrossPlatformCompatibilityService> _logger;
    private readonly Lazy<PlatformInfo> _platformInfo;

    public CrossPlatformCompatibilityService(ILogger<CrossPlatformCompatibilityService> logger)
    {
        _logger = logger;
        _platformInfo = new Lazy<PlatformInfo>(DetectPlatformInfo);
    }

    public async Task<byte[]> CompressDataAsync(byte[] data, CompressionType type)
    {
        return type switch
        {
            CompressionType.ZLIB => await CompressZlibAsync(data),
            CompressionType.RefPack => await CompressRefPackAsync(data),
            CompressionType.LZMA => await CompressLzmaAsync(data),
            _ => throw new NotSupportedException($"Compression type {type} not supported")
        };
    }

    public async Task<byte[]> DecompressDataAsync(byte[] data, CompressionType type)
    {
        return type switch
        {
            CompressionType.ZLIB => await DecompressZlibAsync(data),
            CompressionType.RefPack => await DecompressRefPackAsync(data),
            CompressionType.LZMA => await DecompressLzmaAsync(data),
            _ => throw new NotSupportedException($"Compression type {type} not supported")
        };
    }

    public string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;

        // Handle platform-specific path separators
        var normalized = path.Replace('\\', Path.DirectorySeparatorChar)
                             .Replace('/', Path.DirectorySeparatorChar);

        // Handle case sensitivity differences
        if (_platformInfo.Value.FileSystemCaseSensitive)
        {
            return normalized; // Keep original casing
        }
        else
        {
            return normalized.ToLowerInvariant(); // Normalize case
        }
    }

    public async Task<bool> ValidateByteOrderAsync()
    {
        // Validate that our binary parsing produces identical results across platforms
        var testData = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        using var stream = new MemoryStream(testData);
        using var reader = new BinaryReader(stream);

        var int32Value = reader.ReadInt32();
        var expected = BitConverter.IsLittleEndian ? 0x04030201 : 0x01020304;

        if (int32Value != expected)
        {
            _logger.LogError("Byte order validation failed. Expected {Expected:X8}, got {Actual:X8}",
                expected, int32Value);
            return false;
        }

        return true;
    }
}

public class PlatformInfo
{
    public OperatingSystemPlatform Platform { get; set; }
    public Architecture Architecture { get; set; }
    public bool FileSystemCaseSensitive { get; set; }
    public bool HasNativeCompression { get; set; }
    public string PathSeparator { get; set; }
    public bool SupportsLongPaths { get; set; }
}
```

### Binary Format Compatibility

```csharp
public interface IBinaryCompatibilityService
{
    Task<T> ReadStructAsync<T>(Stream stream) where T : struct;
    Task WriteStructAsync<T>(Stream stream, T value) where T : struct;
    Task<bool> ValidateStructPackingAsync<T>() where T : struct;
}

public class CrossPlatformBinaryService : IBinaryCompatibilityService
{
    public async Task<T> ReadStructAsync<T>(Stream stream) where T : struct
    {
        var size = Marshal.SizeOf<T>();
        var buffer = new byte[size];
        await stream.ReadExactlyAsync(buffer, 0, size);

        // Ensure consistent byte order across platforms
        if (RequiresByteSwap<T>())
        {
            SwapBytes(buffer);
        }

        var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    public async Task WriteStructAsync<T>(Stream stream, T value) where T : struct
    {
        var size = Marshal.SizeOf<T>();
        var buffer = new byte[size];

        var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            Marshal.StructureToPtr(value, handle.AddrOfPinnedObject(), false);
        }
        finally
        {
            handle.Free();
        }

        // Ensure consistent byte order across platforms
        if (RequiresByteSwap<T>())
        {
            SwapBytes(buffer);
        }

        await stream.WriteAsync(buffer, 0, size);
    }

    private bool RequiresByteSwap<T>() where T : struct
    {
        // DBPF format is little-endian, swap if we're on big-endian system
        return !BitConverter.IsLittleEndian && HasIntegerFields<T>();
    }

    private void SwapBytes(byte[] buffer)
    {
        // Implement byte swapping for multi-byte integers
        for (int i = 0; i < buffer.Length; i += 4) // Assuming 32-bit integers
        {
            if (i + 3 < buffer.Length)
            {
                (buffer[i], buffer[i + 3]) = (buffer[i + 3], buffer[i]);
                (buffer[i + 1], buffer[i + 2]) = (buffer[i + 2], buffer[i + 1]);
            }
        }
    }
}
```

### Compression Algorithm Abstraction

```csharp
public interface ICompressionAlgorithm
{
    string Name { get; }
    CompressionType Type { get; }
    bool IsNativeAccelerated { get; }
    Task<byte[]> CompressAsync(byte[] data);
    Task<byte[]> DecompressAsync(byte[] data);
    Task<bool> ValidateCompatibilityAsync(byte[] testData);
}

// ZLIB implementation with platform-specific optimizations
public class CrossPlatformZlibCompression : ICompressionAlgorithm
{
    private readonly ILogger<CrossPlatformZlibCompression> _logger;
    private readonly IPlatformCompatibilityService _platformService;

    public string Name => "ZLIB";
    public CompressionType Type => CompressionType.ZLIB;
    public bool IsNativeAccelerated => _platformService.GetPlatformInfo().HasNativeCompression;

    public async Task<byte[]> CompressAsync(byte[] data)
    {
        if (IsNativeAccelerated && OperatingSystem.IsWindows())
        {
            // Use high-performance native implementation on Windows
            return await CompressWithNativeLibraryAsync(data);
        }
        else
        {
            // Use managed implementation for cross-platform compatibility
            return await CompressWithManagedImplementationAsync(data);
        }
    }

    private async Task<byte[]> CompressWithManagedImplementationAsync(byte[] data)
    {
        using var inputStream = new MemoryStream(data);
        using var outputStream = new MemoryStream();
        using var zlibStream = new ZLibStream(outputStream, CompressionLevel.Optimal);

        await inputStream.CopyToAsync(zlibStream);
        await zlibStream.FlushAsync();

        return outputStream.ToArray();
    }

    public async Task<bool> ValidateCompatibilityAsync(byte[] testData)
    {
        try
        {
            // Test round-trip compression to ensure consistency
            var compressed = await CompressAsync(testData);
            var decompressed = await DecompressAsync(compressed);

            return testData.SequenceEqual(decompressed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ZLIB compatibility validation failed");
            return false;
        }
    }
}

// RefPack implementation - critical for DBPF compatibility
public class CrossPlatformRefPackCompression : ICompressionAlgorithm
{
    public string Name => "RefPack";
    public CompressionType Type => CompressionType.RefPack;
    public bool IsNativeAccelerated => false; // Managed implementation only

    public async Task<byte[]> CompressAsync(byte[] data)
    {
        return await Task.Run(() => CompressRefPackData(data));
    }

    public async Task<byte[]> DecompressAsync(byte[] data)
    {
        return await Task.Run(() => DecompressRefPackData(data));
    }

    private byte[] CompressRefPackData(byte[] input)
    {
        // Port of the original RefPack algorithm - byte-perfect compatibility critical
        var output = new List<byte>();
        var position = 0;

        while (position < input.Length)
        {
            // RefPack compression algorithm implementation
            // Must match original behavior exactly
            var match = FindLongestMatch(input, position);

            if (match.Length >= 3) // Minimum match length
            {
                // Encode as back-reference
                EncodeBackReference(output, match.Distance, match.Length);
                position += match.Length;
            }
            else
            {
                // Encode as literal
                output.Add(input[position]);
                position++;
            }
        }

        return output.ToArray();
    }

    public async Task<bool> ValidateCompatibilityAsync(byte[] testData)
    {
        // Critical: RefPack must produce identical results to original implementation
        var compressed = await CompressAsync(testData);
        var decompressed = await DecompressAsync(compressed);

        var isValid = testData.SequenceEqual(decompressed);

        if (!isValid)
        {
            _logger.LogError("RefPack compatibility validation failed - decompressed data differs");
            LogDataDifferences(testData, decompressed);
        }

        return isValid;
    }
}
```

### File System Abstraction

```csharp
public interface IFileSystemService
{
    Task<bool> FileExistsAsync(string path);
    Task<Stream> OpenReadAsync(string path);
    Task<Stream> CreateAsync(string path);
    Task<string[]> GetFilesAsync(string directory, string pattern);
    string CombinePath(params string[] paths);
    string GetTempPath();
    Task<bool> ValidatePathCompatibilityAsync(string path);
}

public class CrossPlatformFileSystemService : IFileSystemService
{
    private readonly IPlatformCompatibilityService _platformService;
    private readonly ILogger<CrossPlatformFileSystemService> _logger;

    public string CombinePath(params string[] paths)
    {
        if (paths == null || paths.Length == 0) return string.Empty;

        var result = paths[0];
        for (int i = 1; i < paths.Length; i++)
        {
            result = Path.Combine(result, paths[i]);
        }

        return _platformService.NormalizePath(result);
    }

    public async Task<string[]> GetFilesAsync(string directory, string pattern)
    {
        if (!Directory.Exists(directory))
        {
            _logger.LogWarning("Directory does not exist: {Directory}", directory);
            return Array.Empty<string>();
        }

        try
        {
            var files = Directory.GetFiles(directory, pattern, SearchOption.TopDirectoryOnly);

            // Normalize paths for cross-platform consistency
            return files.Select(f => _platformService.NormalizePath(f)).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get files from {Directory} with pattern {Pattern}",
                directory, pattern);
            return Array.Empty<string>();
        }
    }

    public async Task<bool> ValidatePathCompatibilityAsync(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;

        // Check for invalid characters on current platform
        var invalidChars = Path.GetInvalidPathChars();
        if (path.Any(c => invalidChars.Contains(c)))
        {
            _logger.LogWarning("Path contains invalid characters: {Path}", path);
            return false;
        }

        // Check path length limits
        if (path.Length > GetMaxPathLength())
        {
            _logger.LogWarning("Path exceeds maximum length: {Path} ({Length} characters)",
                path, path.Length);
            return false;
        }

        // Check for reserved names (Windows specific, but validate everywhere)
        var fileName = Path.GetFileName(path);
        var reservedNames = new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4",
                                   "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2",
                                   "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };

        if (reservedNames.Any(name => string.Equals(fileName, name, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogWarning("Path uses reserved filename: {Path}", path);
            return false;
        }

        return true;
    }

    private int GetMaxPathLength()
    {
        if (OperatingSystem.IsWindows())
        {
            // Windows has 260 character limit (unless long paths enabled)
            return 260;
        }
        else
        {
            // Unix-like systems typically allow 4096 characters
            return 4096;
        }
    }
}
```

## Testing Strategy

### Cross-Platform Compatibility Tests

```csharp
[TestClass]
public class CrossPlatformCompatibilityTests
{
    private IPlatformCompatibilityService _compatibilityService;
    private IBinaryCompatibilityService _binaryService;

    [TestInitialize]
    public void Setup()
    {
        _compatibilityService = new CrossPlatformCompatibilityService(
            Mock.Of<ILogger<CrossPlatformCompatibilityService>>());
        _binaryService = new CrossPlatformBinaryService();
    }

    [TestMethod]
    public async Task ByteOrder_IsConsistentAcrossPlatforms()
    {
        // Arrange
        var testData = new uint[] { 0x12345678, 0xABCDEF00, 0x11223344 };

        // Act & Assert
        foreach (var value in testData)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(value);

            stream.Position = 0;
            using var reader = new BinaryReader(stream);
            var readValue = reader.ReadUInt32();

            Assert.AreEqual(value, readValue,
                "Binary read/write should be consistent across platforms");
        }
    }

    [TestMethod]
    public async Task CompressionAlgorithms_ProduceIdenticalResults()
    {
        // Arrange
        var testData = GenerateTestData(10000); // 10KB test data
        var algorithms = new ICompressionAlgorithm[]
        {
            new CrossPlatformZlibCompression(_compatibilityService, Mock.Of<ILogger<CrossPlatformZlibCompression>>()),
            new CrossPlatformRefPackCompression(Mock.Of<ILogger<CrossPlatformRefPackCompression>>())
        };

        // Act & Assert
        foreach (var algorithm in algorithms)
        {
            var compressed = await algorithm.CompressAsync(testData);
            var decompressed = await algorithm.DecompressAsync(compressed);

            CollectionAssert.AreEqual(testData, decompressed,
                $"{algorithm.Name} compression should produce identical round-trip results");

            // Validate against known reference outputs (if available)
            await ValidateAgainstReferenceOutput(algorithm, testData, compressed);
        }
    }

    [TestMethod]
    [DataRow("simple.package")]
    [DataRow("large_package_with_resources.package")]
    [DataRow("package_with_special_chars_Ã¼Ã±Ã­Ã§Ã¸dÃ©.package")]
    public async Task PackageProcessing_ProducesIdenticalResults(string packageFileName)
    {
        // Arrange
        var packagePath = Path.Combine(TestDataDirectory, packageFileName);
        var expectedBytes = await File.ReadAllBytesAsync(packagePath);

        // Act - Process package through our cross-platform implementation
        var package = await LoadPackageAsync(packagePath);
        var actualBytes = await package.SerializeAsync();

        // Assert - Must be byte-identical
        CollectionAssert.AreEqual(expectedBytes, actualBytes,
            $"Package {packageFileName} should serialize to identical bytes across platforms");
    }

    [TestMethod]
    public async Task PathHandling_WorksAcrossPlatforms()
    {
        // Arrange
        var testPaths = new[]
        {
            @"C:\Users\Test\Documents\Package.package",   // Windows-style
            "/home/user/documents/package.package",        // Unix-style
            @"subfolder\relative\path.package",           // Relative Windows
            "subfolder/relative/path.package",             // Relative Unix
            "package with spaces.package",                 // Spaces
            "package-with-dashes.package"                 // Special chars
        };

        var fileSystemService = new CrossPlatformFileSystemService(
            _compatibilityService, Mock.Of<ILogger<CrossPlatformFileSystemService>>());

        // Act & Assert
        foreach (var testPath in testPaths)
        {
            var normalized = _compatibilityService.NormalizePath(testPath);
            var isValid = await fileSystemService.ValidatePathCompatibilityAsync(normalized);

            Assert.IsTrue(isValid || IsExpectedToFail(testPath),
                $"Path should be valid on current platform: {testPath} -> {normalized}");
        }
    }
}

// Platform-specific test runners
[TestClass]
public class WindowsSpecificCompatibilityTests
{
    [TestMethod]
    [TestCategory("Windows")]
    public async Task NativeCompression_ProducesIdenticalResults()
    {
        // Test Windows native DLL compression against managed implementation
        if (!OperatingSystem.IsWindows()) Assert.Inconclusive("Windows-specific test");

        var testData = GenerateTestData(50000);

        var nativeCompression = new NativeWindowsDdsCompression();
        var managedCompression = new ManagedDdsCompression();

        var nativeResult = await nativeCompression.CompressAsync(testData);
        var managedResult = await managedCompression.CompressAsync(testData);

        // Results should be functionally equivalent (not necessarily byte-identical)
        var nativeDecompressed = await nativeCompression.DecompressAsync(nativeResult);
        var managedDecompressed = await managedCompression.DecompressAsync(managedResult);

        CollectionAssert.AreEqual(testData, nativeDecompressed);
        CollectionAssert.AreEqual(testData, managedDecompressed);
    }
}

[TestClass]
public class LinuxSpecificCompatibilityTests
{
    [TestMethod]
    [TestCategory("Linux")]
    public async Task FilePermissions_HandledCorrectly()
    {
        if (!OperatingSystem.IsLinux()) Assert.Inconclusive("Linux-specific test");

        // Test file permission handling on Linux
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "test data");

            // Test reading file with different permissions
            File.SetUnixFileMode(tempFile, UnixFileMode.UserRead);

            var fileSystemService = new CrossPlatformFileSystemService(
                new CrossPlatformCompatibilityService(Mock.Of<ILogger<CrossPlatformCompatibilityService>>()),
                Mock.Of<ILogger<CrossPlatformFileSystemService>>());

            var canRead = await fileSystemService.FileExistsAsync(tempFile);
            Assert.IsTrue(canRead, "Should be able to detect readable file on Linux");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
```

### Golden Master Cross-Platform Validation

```csharp
public class CrossPlatformGoldenMasterValidator
{
    private readonly Dictionary<string, byte[]> _referenceOutputs;

    public CrossPlatformGoldenMasterValidator()
    {
        // Load reference outputs generated on Windows (reference platform)
        _referenceOutputs = LoadReferenceOutputs();
    }

    public async Task<CrossPlatformValidationReport> ValidateAllPlatformsAsync(
        IEnumerable<TestPackage> packages)
    {
        var report = new CrossPlatformValidationReport();

        foreach (var package in packages)
        {
            var packageResult = await ValidatePackageAcrossPlatformsAsync(package);
            report.PackageResults.Add(packageResult);
        }

        return report;
    }

    private async Task<PackageValidationResult> ValidatePackageAcrossPlatformsAsync(TestPackage package)
    {
        var result = new PackageValidationResult { Package = package };

        // Load and process package on current platform
        var currentPlatformOutput = await ProcessPackageAsync(package.Path);

        // Compare with reference output (from Windows)
        if (_referenceOutputs.TryGetValue(package.Name, out var referenceOutput))
        {
            result.BytesMatch = currentPlatformOutput.SequenceEqual(referenceOutput);

            if (!result.BytesMatch)
            {
                result.Differences = AnalyzeDifferences(referenceOutput, currentPlatformOutput);
            }
        }
        else
        {
            result.BytesMatch = false;
            result.MissingReference = true;
        }

        return result;
    }
}
```

## Continuous Integration Strategy

### Multi-Platform CI Pipeline

```yaml

# .github/workflows/cross-platform-compatibility.yml

name: Cross-Platform Compatibility Tests

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  compatibility-tests:
    strategy:
      matrix:
        os: [ubuntu-latest, windows-latest, macos-latest]
        dotnet-version: ['9.0.x']

    runs-on: ${{ matrix.os }}

    steps:

    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: ${{ matrix.dotnet-version }}

    - name: Cache Test Data
      uses: actions/cache@v3
      with:
        path: test-data/compatibility
        key: compatibility-test-data-${{ runner.os }}-${{ hashFiles('test-data/manifest.json') }}

    - name: Run Compatibility Tests
      run: |
        dotnet test tests/TS4Tools.Tests.Compatibility/ \
          --configuration Release \
          --logger "trx;LogFileName=compatibility-results-${{ runner.os }}.trx" \
          --filter "Category!=Windows|Category!=${{ runner.os }}"

    - name: Generate Cross-Platform Report
      run: |
        dotnet run --project tools/CompatibilityAnalyzer/ \
          -- --platform ${{ runner.os }} \
                --test-results TestResults/compatibility-results-${{ runner.os }}.trx \
                --output compatibility-report-${{ runner.os }}.json

    - name: Upload Results
      uses: actions/upload-artifact@v3
      with:
        name: compatibility-results-${{ runner.os }}
        path: |
          TestResults/compatibility-results-${{ runner.os }}.trx
          compatibility-report-${{ runner.os }}.json

  cross-platform-analysis:
    needs: compatibility-tests
    runs-on: ubuntu-latest

    steps:

    - name: Download All Results
      uses: actions/download-artifact@v3

    - name: Analyze Cross-Platform Compatibility
      run: |
        dotnet run --project tools/CrossPlatformAnalyzer/ \
          -- --results-dir . --output cross-platform-analysis.html

    - name: Comment PR with Results
      if: github.event_name == 'pull_request'
      uses: actions/github-script@v6
      with:
        script: |
          const fs = require('fs');
          const analysis = fs.readFileSync('cross-platform-analysis.html', 'utf8');

          github.rest.issues.createComment({
            issue_number: context.issue.number,
            owner: context.repo.owner,
            repo: context.repo.repo,
            body: `## Cross-Platform Compatibility Analysis\n\n${analysis}`
          });
```

## Performance Monitoring

### Platform-Specific Benchmarks

```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class CrossPlatformPerformanceBenchmarks
{
    private byte[] _testData;
    private ICompressionAlgorithm[] _algorithms;

    [GlobalSetup]
    public void Setup()
    {
        _testData = GenerateTestData(1_000_000); // 1MB
        _algorithms = new ICompressionAlgorithm[]
        {
            new CrossPlatformZlibCompression(/* ... */),
            new CrossPlatformRefPackCompression(/* ... */)
        };
    }

    [Benchmark]
    [Arguments("ZLIB")]
    [Arguments("RefPack")]
    public async Task<byte[]> CompressData(string algorithmName)
    {
        var algorithm = _algorithms.First(a => a.Name == algorithmName);
        return await algorithm.CompressAsync(_testData);
    }

    [Benchmark]
    public async Task<IPackage> LoadPackage()
    {
        var packagePath = GetTestPackagePath();
        var packageService = CreatePackageService();
        return await packageService.LoadPackageAsync(packagePath);
    }

    [Benchmark]
    public async Task<byte[]> RoundTripPackage()
    {
        var packagePath = GetTestPackagePath();
        var packageService = CreatePackageService();

        var package = await packageService.LoadPackageAsync(packagePath);
        return await package.SerializeAsync();
    }
}
```

## Consequences

### Positive

- **Universal Compatibility**: TS4Tools works identically on Windows, Linux, and macOS
- **Byte-Perfect Output**: Package files are identical regardless of platform
- **Performance Options**: Native acceleration where available, managed fallbacks elsewhere
- **Path Safety**: Robust file path handling across different filesystems
- **Format Preservation**: All compression and binary formats work consistently
- **Future-Proof**: Abstractions make adding new platforms easier

### Challenges

- **Testing Complexity**: Must validate on multiple platforms continuously
- **Performance Variation**: Some platforms may be slower due to managed implementations
- **Platform-Specific Bugs**: Edge cases may only appear on specific platforms
- **Deployment Complexity**: Different packaging and distribution per platform

### Mitigation Strategies

- **Comprehensive CI/CD**: Automated testing on all target platforms
- **Performance Monitoring**: Continuous benchmarking to catch regressions
- **Platform-Specific Testing**: Dedicated test suites for platform-specific functionality
- **Community Beta Testing**: Early access for users on each platform
- **Fallback Mechanisms**: Graceful degradation when platform-specific features unavailable

## Success Metrics

| Metric | Target | Windows | Linux | macOS |
|--------|--------|---------|-------|-------|
| **Package Compatibility** | 100% | Reference | Target | Target |
| **Performance Overhead** | â‰¤ 20% | Baseline | â‰¤ 20% slower | â‰¤ 20% slower |
| **Test Pass Rate** | 99%+ | Reference | Target | Target |
| **File Format Accuracy** | Byte-perfect | Reference | Target | Target |

## Related Decisions

- ADR-004: Greenfield Migration Strategy (enables cross-platform architecture)
- ADR-006: Golden Master Testing Strategy (validates cross-platform compatibility)
- ADR-001: .NET 9 Framework (provides cross-platform runtime)
- ADR-003: Avalonia Cross-Platform UI (UI framework choice)
