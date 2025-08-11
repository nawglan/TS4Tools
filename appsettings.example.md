# TS4Tools Configuration Example

This is an example configuration file that demonstrates how to set up TS4Tools with your Sims 4 installation directory.

## Setup Instructions

1. **Copy this file** to `appsettings.json` in the same directory
2. **Update the InstallationDirectory** path to match your Sims 4 installation
3. **Customize other settings** as needed

## Common Installation Paths

- **Steam**: `C:\Program Files (x86)\Steam\steamapps\common\The Sims 4`
- **Origin**: `C:\Program Files (x86)\Origin Games\The Sims 4`
- **EA Desktop**: `C:\Program Files\EA Games\The Sims 4`
- **Custom**: Check your game launcher for the actual installation path

## Configuration Options

### Game Settings

- `InstallationDirectory`: Path to your Sims 4 installation (required)
- `DataDirectory`: Game data folder (auto-detected if null)
- `ClientDataDirectory`: Package files location (auto-detected if null)
- `EnableAutoDetection`: Automatically find installation (recommended: true)
- `ValidateInstallation`: Verify installation integrity (recommended: true)

### Performance Settings

- `MaxResourceCacheSize`: Memory cache size for resources (default: 1000)
- `EnableDetailedLogging`: Enable verbose logging (use only for debugging)
- `AsyncOperationTimeoutMs`: Timeout for async operations (default: 30 seconds)

### Package Settings

- `CreateBackups`: Automatically backup files when saving (recommended: true)
- `MaxBackupCount`: Number of backup files to keep (default: 10)
- `EnableCompression`: Compress package files (recommended: true)

## Important Notes

- This file should be added to your `.gitignore` to avoid committing personal paths
- Use forward slashes `/` or escaped backslashes `\\` in JSON paths
- The application will auto-detect Data and Client directories if not specified
- Enable detailed logging only when troubleshooting issues

## Security Warning

âš ï¸ **Never commit this file to version control** - it contains local system paths and should remain private.

