#pragma warning disable CA1848 // Use the LoggerMessage delegates for better performance
#pragma warning disable CA2254 // Template should be a static expression

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;
using TS4Tools.Core.Settings;

namespace TS4Tools.Tests.Common;

/// <summary>
/// Test data provider that can access real game files from appsettings.json configuration
/// or fall back to mock data if real files are not available.
/// This utility class can be used across all test projects to access consistent test data.
/// </summary>
public sealed class TestDataProvider : IDisposable
{
    private readonly ILogger _logger;
    private readonly ApplicationSettings _settings;
    private readonly string _testDataCachePath;
    private bool _disposed;

    public TestDataProvider(ILogger? logger = null)
    {
        _logger = logger ?? NullLogger.Instance;
        _settings = LoadApplicationSettings();
        _testDataCachePath = Path.Combine(Path.GetTempPath(), "TS4Tools", "TestData", "Mock");

        // Only create directory if we need to create mock packages
        if (!IsRealGameDataAvailable())
        {
            Directory.CreateDirectory(_testDataCachePath);
        }
    }

    /// <summary>
    /// Gets a collection of real package files for testing.
    /// First attempts to use real game packages from configured directories,
    /// then falls back to mock packages if real ones are unavailable.
    /// </summary>
    /// <param name="maxCount">Maximum number of packages to return (default: 5)</param>
    /// <returns>Collection of package file paths</returns>
    public async Task<IEnumerable<string>> GetTestPackageFilesAsync(int maxCount = 5)
    {
        var packages = new List<string>();

        try
        {
            // First try to get real game packages
            var realPackages = await GetRealGamePackagesAsync();
            packages.AddRange(realPackages.Take(maxCount));

            if (packages.Count > 0)
            {
                _logger.LogInformation($"Using {packages.Count} real game packages for testing");
                return packages;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load real game packages, falling back to test data");
        }

        // Fallback to configured client data directory
        var clientDataPackages = GetClientDataPackages();
        packages.AddRange(clientDataPackages.Take(maxCount));

        if (packages.Count > 0)
        {
            _logger.LogInformation($"Using {packages.Count} packages from client data directory");
            return packages;
        }

        // Final fallback to mock packages
        var mockPackages = await CreateMockPackagesAsync(Math.Min(maxCount, 3));
        packages.AddRange(mockPackages);

        _logger.LogInformation($"Using {packages.Count} mock packages for testing");
        return packages;
    }

    /// <summary>
    /// Gets a single test package file for simple testing scenarios.
    /// </summary>
    /// <returns>Path to a test package file</returns>
    public async Task<string> GetSingleTestPackageAsync()
    {
        var packages = await GetTestPackageFilesAsync(1);
        return packages.FirstOrDefault() ?? throw new InvalidOperationException("No test packages available");
    }

    /// <summary>
    /// Gets test packages specifically from the configured client data directory.
    /// This is useful when you want to use the smaller client packages that are
    /// already copied from the game installation.
    /// </summary>
    /// <param name="maxCount">Maximum number of packages to return</param>
    /// <returns>Collection of client data package paths</returns>
    public IEnumerable<string> GetClientDataPackages(int maxCount = 10)
    {
        var packages = new List<string>();

        try
        {
            var clientDataDirectory = _settings?.Game?.ClientDataDirectory;

            if (!string.IsNullOrEmpty(clientDataDirectory) && Directory.Exists(clientDataDirectory))
            {
                packages.AddRange(Directory.GetFiles(clientDataDirectory, "*.package", SearchOption.TopDirectoryOnly)
                    .Take(maxCount));

                _logger.LogDebug($"Found {packages.Count} packages in client data directory: {clientDataDirectory}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error accessing client data packages: {ex.Message}");
        }

        return packages;
    }

    /// <summary>
    /// Checks if real game data is available (i.e., if the configured directories exist).
    /// </summary>
    /// <returns>True if real game data is available, false otherwise</returns>
    public bool IsRealGameDataAvailable()
    {
        var dataDirectory = _settings?.Game?.DataDirectory;
        var clientDataDirectory = _settings?.Game?.ClientDataDirectory;

        return (!string.IsNullOrEmpty(dataDirectory) && Directory.Exists(dataDirectory)) ||
               (!string.IsNullOrEmpty(clientDataDirectory) && Directory.Exists(clientDataDirectory));
    }

    /// <summary>
    /// Gets the application settings loaded from appsettings.json.
    /// </summary>
    public ApplicationSettings ApplicationSettings => _settings;

    private Task<IEnumerable<string>> GetRealGamePackagesAsync()
    {
        var packages = new List<string>();

        var dataDirectory = _settings?.Game?.DataDirectory;
        if (string.IsNullOrEmpty(dataDirectory) || !Directory.Exists(dataDirectory))
        {
            return Task.FromResult<IEnumerable<string>>(packages);
        }

        var searchPaths = new[]
        {
            Path.Combine(dataDirectory, "Client"),
            Path.Combine(dataDirectory, "Shared")
        };

        var foundCount = 0;
        const int maxPackagesToUse = 10;

        foreach (var searchPath in searchPaths.Where(Directory.Exists))
        {
            if (foundCount >= maxPackagesToUse) break;

            var sourcePackages = Directory.GetFiles(searchPath, "*.package", SearchOption.TopDirectoryOnly)
                .Take(maxPackagesToUse - foundCount);

            foreach (var sourcePackage in sourcePackages)
            {
                try
                {
                    // Verify the file is accessible before adding it
                    if (File.Exists(sourcePackage))
                    {
                        packages.Add(sourcePackage);
                        foundCount++;

                        _logger.LogDebug($"Added {Path.GetFileName(sourcePackage)} for direct testing");

                        if (foundCount >= maxPackagesToUse) break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Failed to access {Path.GetFileName(sourcePackage)}: {ex.Message}");
                }
            }
        }

        return Task.FromResult<IEnumerable<string>>(packages);
    }

    private async Task<IEnumerable<string>> CreateMockPackagesAsync(int count)
    {
        var packages = new List<string>();

        // Ensure mock directory exists
        Directory.CreateDirectory(_testDataCachePath);

        var mockDefinitions = new[]
        {
            ("simple", "Simple mock package with minimal DBPF structure"),
            ("complex", "Complex mock package with multiple resource entries"),
            ("edge-case", "Edge case mock package for boundary testing")
        };

        for (var i = 0; i < Math.Min(count, mockDefinitions.Length); i++)
        {
            var (id, description) = mockDefinitions[i];
            var fileName = $"test-package-{id}.package";
            var filePath = Path.Combine(_testDataCachePath, fileName);

            if (!File.Exists(filePath))
            {
                var mockData = CreateMockDbpfPackage(id, i + 1);
                await File.WriteAllBytesAsync(filePath, mockData);
                _logger.LogDebug($"Created mock package: {fileName} - {description}");
            }

            packages.Add(filePath);
        }

        return packages;
    }

    private static byte[] CreateMockDbpfPackage(string id, int resourceCount)
    {
        // Create a minimal valid DBPF package for testing
        var data = new List<byte>();

        // DBPF Header (96 bytes)
        data.AddRange("DBPF"u8); // Magic (4 bytes)
        data.AddRange(BitConverter.GetBytes(2)); // Major version (4 bytes)
        data.AddRange(BitConverter.GetBytes(1)); // Minor version (4 bytes)
        data.AddRange(new byte[12]); // Reserved (12 bytes)
        data.AddRange(BitConverter.GetBytes(DateTime.UtcNow.Ticks)); // Created date (8 bytes)
        data.AddRange(BitConverter.GetBytes(DateTime.UtcNow.Ticks)); // Modified date (8 bytes)
        data.AddRange(BitConverter.GetBytes(1)); // Index major version (4 bytes)
        data.AddRange(BitConverter.GetBytes(resourceCount)); // Index entry count (4 bytes)
        data.AddRange(new byte[4]); // Index location (will be updated)
        data.AddRange(BitConverter.GetBytes(resourceCount * 24)); // Index size (4 bytes)
        data.AddRange(new byte[44]); // Reserved (44 bytes)

        // Simple resource index (each entry is 24 bytes)
        var indexStart = data.Count;
        for (var i = 0; i < resourceCount; i++)
        {
            data.AddRange(BitConverter.GetBytes(0x545AC67A + i)); // Type ID
            data.AddRange(BitConverter.GetBytes(0x12345678 + i)); // Group ID
            data.AddRange(BitConverter.GetBytes(0x87654321 + i)); // Instance ID
            data.AddRange(BitConverter.GetBytes(96 + (resourceCount * 24) + (i * 10))); // Location
            data.AddRange(BitConverter.GetBytes(10)); // Size
            data.AddRange(BitConverter.GetBytes(10)); // Uncompressed size
        }

        // Simple resource data (10 bytes per resource)
        for (var i = 0; i < resourceCount; i++)
        {
            data.AddRange(Enumerable.Repeat((byte)(0x41 + i), 10)); // 10 bytes of repeated character
        }

        // Update index location in header
        var indexLocationBytes = BitConverter.GetBytes(indexStart);
        for (var i = 0; i < 4; i++)
        {
            data[64 + i] = indexLocationBytes[i];
        }

        return data.ToArray();
    }

    private static ApplicationSettings LoadApplicationSettings()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var settings = new ApplicationSettings();
        configuration.Bind("ApplicationSettings", settings);

        return settings;
    }

    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            // Clean up only mock packages if they exist
            if (Directory.Exists(_testDataCachePath))
            {
                Directory.Delete(_testDataCachePath, recursive: true);
            }
        }
        catch (Exception ex)
        {
            // Log but don't throw during disposal
            Console.WriteLine($"Warning: Failed to clean up mock test data cache: {ex.Message}");
        }

        _disposed = true;
    }
}
