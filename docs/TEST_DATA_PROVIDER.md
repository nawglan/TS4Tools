# TS4Tools Test Data Provider

This document explains how the TS4Tools test suite accesses real Sims 4 game data for comprehensive testing.

## Overview

The TS4Tools tests have been updated to use real game package files directly from your Sims 4 installation, as configured in `appsettings.json`. This provides several advantages:

- **Authentic Testing**: Tests use actual game files instead of synthetic test data
- **No File Copying**: Tests access game files directly, avoiding unnecessary disk I/O
- **Fallback Support**: Automatically falls back to mock data if game files aren't available
- **Configuration-Driven**: Uses the same appsettings.json configuration as the main application

## How It Works

### 1. Primary Data Source: Real Game Installation

The tests first attempt to use real package files from the directories configured in your `appsettings.json`:

```json
{
  "ApplicationSettings": {
    "Game": {
      "InstallationDirectory": "C:\\Program Files (x86)\\Steam\\steamapps\\common\\The Sims 4",
      "DataDirectory": "C:\\Program Files (x86)\\Steam\\steamapps\\common\\The Sims 4\\Data",
      "ClientDataDirectory": "C:\\Users\\nawgl\\code\\Test Data\\Client"
    }
  }
}
```

**Search Order:**
1. `DataDirectory/Client` - Main game package files
2. `DataDirectory/Shared` - Shared game package files  
3. `ClientDataDirectory` - Smaller client-specific packages
4. Standard installation paths (if config paths don't exist)

### 2. Fallback: Mock Data

If real game files cannot be accessed, the tests automatically create mock DBPF packages with valid structure for framework testing.

## Usage in Tests

### Golden Master Tests

The `PackageCompatibilityTests` class uses this approach to:
- Test with real Sims 4 package files when available
- Validate byte-perfect compatibility with actual game data
- Fall back gracefully when game installation isn't available

### Using TestDataProvider

For other test projects, you can use the `TestDataProvider` class:

```csharp
using TS4Tools.Tests.Common;

public class MyTests
{
    [Fact]
    public async Task TestWithRealGameData()
    {
        using var testDataProvider = new TestDataProvider();
        
        // Get real game packages if available, mock packages otherwise
        var packages = await testDataProvider.GetTestPackageFilesAsync(maxCount: 5);
        
        // Use packages in your tests...
        foreach (var packagePath in packages)
        {
            // Test logic here
        }
    }
}
```

## Benefits

### For Developers
- **Comprehensive Testing**: Tests run against real game data structures
- **No Setup Required**: Tests work whether or not you have the game installed
- **Fast Execution**: No file copying or cache management overhead
- **Consistent Configuration**: Same settings used by main application

### For CI/CD
- **Graceful Degradation**: Tests pass in environments without game installation
- **No External Dependencies**: Mock data ensures tests can always run
- **Reliable Results**: Consistent test behavior across different environments

## Configuration

To use real game data in tests:

1. Ensure your `appsettings.json` has correct game directory paths
2. Verify the directories exist and contain `.package` files
3. Tests will automatically detect and use available game files

No additional configuration is required - the test framework handles everything automatically.

## File Access

The tests access game files in **read-only mode** and **never modify** the original game installation. Files are accessed directly from their original locations without copying or caching.

## Mock Data Fallback

When real game files aren't available, the system creates minimal mock DBPF packages with:
- Valid DBPF headers (magic, version, timestamps)
- Proper resource index structure
- Sample resource data for testing
- Multiple package variants (simple, complex, edge-case)

This ensures the test framework can validate core functionality even without a game installation.
