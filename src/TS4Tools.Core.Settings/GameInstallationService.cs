/***************************************************************************
 *  Copyright (C) 2025 by the TS4Tools contributors                       *
 *                                                                         *
 *  This file is part of TS4Tools                                         *
 *                                                                         *
 *  TS4Tools is free software: you can redistribute it and/or modify      *
 *  it under the terms of the GNU General Public License as published by  *
 *  the Free Software Foundation, either version 3 of the License, or     *
 *  (at your option) any later version.                                   *
 *                                                                         *
 *  TS4Tools is distributed in the hope that it will be useful,           *
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the          *
 *  GNU General Public License for more details.                          *
 *                                                                         *
 *  You should have received a copy of the GNU General Public License     *
 *  along with TS4Tools. If not, see <http://www.gnu.org/licenses/>.      *
 ***************************************************************************/

using Microsoft.Extensions.Options;
using System.Runtime.InteropServices;

namespace TS4Tools.Core.Settings;

/// <summary>
/// Service for detecting and resolving The Sims 4 installation directories.
/// Implements auto-detection logic and validation of game installations.
/// </summary>
public interface IGameInstallationService
{
    /// <summary>
    /// Gets the resolved Sims 4 installation directory.
    /// </summary>
    Task<string?> GetInstallationDirectoryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the data directory path (typically InstallationDirectory\Data).
    /// </summary>
    Task<string?> GetDataDirectoryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the client data directory path (typically InstallationDirectory\Data\Client).
    /// </summary>
    Task<string?> GetClientDataDirectoryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that the specified directory contains a valid Sims 4 installation.
    /// </summary>
    Task<bool> ValidateInstallationAsync(string directory, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of game installation service using modern .NET patterns.
/// Follows the AI guidelines for dependency injection and async operations.
/// </summary>
public sealed class GameInstallationService : IGameInstallationService
{
    private readonly GameSettings _gameSettings;
    private readonly ILogger<GameInstallationService> _logger;

    /// <summary>
    /// Common Sims 4 installation paths by platform and launcher.
    /// </summary>
    private static readonly string[] CommonInstallationPaths = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? [
            @"C:\Program Files (x86)\Steam\steamapps\common\The Sims 4",
            @"C:\Program Files (x86)\Origin Games\The Sims 4",
            @"C:\Program Files\EA Games\The Sims 4",
            @"C:\Program Files\Steam\steamapps\common\The Sims 4"
        ]
        : [
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library/Application Support/Steam/steamapps/common/The Sims 4"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Origin/Games/The Sims 4")
        ];

    /// <summary>
    /// Initializes a new instance of the GameInstallationService class.
    /// </summary>
    /// <param name="options">Application settings containing game configuration.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    public GameInstallationService(IOptions<ApplicationSettings> options, ILogger<GameInstallationService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _gameSettings = options.Value?.Game ?? throw new ArgumentNullException(nameof(options), "Game settings not configured");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<string?> GetInstallationDirectoryAsync(CancellationToken cancellationToken = default)
    {
        // First, check if explicitly configured
        if (!string.IsNullOrWhiteSpace(_gameSettings.InstallationDirectory))
        {
            var configuredPath = _gameSettings.InstallationDirectory;
            if (Directory.Exists(configuredPath))
            {
                _logger.LogDebug("Using configured installation directory: {Path}", configuredPath);

                if (_gameSettings.ValidateInstallation)
                {
                    var isValid = await ValidateInstallationAsync(configuredPath, cancellationToken).ConfigureAwait(false);
                    if (!isValid)
                    {
                        _logger.LogWarning("Configured installation directory failed validation: {Path}", configuredPath);
                        return null;
                    }
                }

                return configuredPath;
            }

            _logger.LogWarning("Configured installation directory does not exist: {Path}", configuredPath);
        }

        // Auto-detection if enabled
        if (_gameSettings.EnableAutoDetection)
        {
            _logger.LogDebug("Auto-detecting Sims 4 installation directory");
            return await DetectInstallationDirectoryAsync(cancellationToken).ConfigureAwait(false);
        }

        _logger.LogError("No valid Sims 4 installation directory found and auto-detection is disabled");
        return null;
    }

    /// <inheritdoc/>
    public async Task<string?> GetDataDirectoryAsync(CancellationToken cancellationToken = default)
    {
        // Use explicitly configured data directory if available
        if (!string.IsNullOrWhiteSpace(_gameSettings.DataDirectory) && Directory.Exists(_gameSettings.DataDirectory))
        {
            _logger.LogDebug("Using configured data directory: {Path}", _gameSettings.DataDirectory);
            return _gameSettings.DataDirectory;
        }

        // Default to InstallationDirectory\Data
        var installationDir = await GetInstallationDirectoryAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(installationDir))
        {
            return null;
        }

        var dataDir = Path.Combine(installationDir, "Data");
        if (Directory.Exists(dataDir))
        {
            _logger.LogDebug("Using default data directory: {Path}", dataDir);
            return dataDir;
        }

        _logger.LogWarning("Data directory not found at expected location: {Path}", dataDir);
        return null;
    }

    /// <inheritdoc/>
    public async Task<string?> GetClientDataDirectoryAsync(CancellationToken cancellationToken = default)
    {
        // Use explicitly configured client data directory if available
        if (!string.IsNullOrWhiteSpace(_gameSettings.ClientDataDirectory) && Directory.Exists(_gameSettings.ClientDataDirectory))
        {
            _logger.LogDebug("Using configured client data directory: {Path}", _gameSettings.ClientDataDirectory);
            return _gameSettings.ClientDataDirectory;
        }

        // Default to DataDirectory\Client
        var dataDir = await GetDataDirectoryAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(dataDir))
        {
            return null;
        }

        var clientDataDir = Path.Combine(dataDir, "Client");
        if (Directory.Exists(clientDataDir))
        {
            _logger.LogDebug("Using default client data directory: {Path}", clientDataDir);
            return clientDataDir;
        }

        _logger.LogWarning("Client data directory not found at expected location: {Path}", clientDataDir);
        return null;
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateInstallationAsync(string directory, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        if (!Directory.Exists(directory))
        {
            _logger.LogDebug("Installation validation failed: directory does not exist: {Path}", directory);
            return false;
        }

        // Check for essential game files
        var gameExecutable = Path.Combine(directory, "Game", "Bin", "TS4_x64.exe");
        var gameExecutableAlt = Path.Combine(directory, "Game", "Bin", "TS4.exe");

        var hasGameExecutable = File.Exists(gameExecutable) || File.Exists(gameExecutableAlt);
        if (!hasGameExecutable)
        {
            _logger.LogDebug("Installation validation failed: game executable not found in {Path}", directory);
            return false;
        }

        // Check for Data directory
        var dataDir = Path.Combine(directory, "Data");
        if (!Directory.Exists(dataDir))
        {
            _logger.LogDebug("Installation validation failed: Data directory not found in {Path}", directory);
            return false;
        }

        // Check for Client directory with package files
        var clientDir = Path.Combine(dataDir, "Client");
        if (Directory.Exists(clientDir))
        {
            // Look for common package files asynchronously
            var packageFiles = await Task.Run(() =>
                Directory.GetFiles(clientDir, "*.package", SearchOption.TopDirectoryOnly).Length,
                cancellationToken).ConfigureAwait(false);

            if (packageFiles == 0)
            {
                _logger.LogDebug("Installation validation warning: no package files found in Client directory");
            }
        }

        _logger.LogDebug("Installation validation passed for: {Path}", directory);
        return true;
    }

    private async Task<string?> DetectInstallationDirectoryAsync(CancellationToken cancellationToken = default)
    {
        foreach (var path in CommonInstallationPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (Directory.Exists(path))
            {
                _logger.LogDebug("Checking potential installation directory: {Path}", path);

                var isValid = _gameSettings.ValidateInstallation
                    ? await ValidateInstallationAsync(path, cancellationToken).ConfigureAwait(false)
                    : true;

                if (isValid)
                {
                    _logger.LogInformation("Auto-detected Sims 4 installation at: {Path}", path);
                    return path;
                }
            }
        }

        _logger.LogWarning("Could not auto-detect Sims 4 installation directory");
        return null;
    }
}
