/***************************************************************************
 *  Copyright (C) 2009, 2010 by Peter L Jones                              *
 *  pljones@users.sf.net                                                   *
 *                                                                         *
 *  This file is part of the Sims 3 Package Interface (s3pi)               *
 *                                                                         *
 *  s3pi is free software: you can redistribute it and/or modify           *
 *  it under the terms of the GNU General Public License as published by   *
 *  the Free Software Foundation, either version 3 of the License, or      *
 *  (at your option) any later version.                                    *
 *                                                                         *
 *  s3pi is distributed in the hope that it will be useful,                *
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of         *
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the          *
 *  GNU General Public License for more details.                           *
 *                                                                         *
 *  You should have received a copy of the GNU General Public License      *
 *  along with s3pi.  If not, see <http://www.gnu.org/licenses/>.          *
 ***************************************************************************/

using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Reflection;
using TS4Tools.Core.System.Platform;

namespace TS4Tools.Core.System.Configuration;

/// <summary>
/// Modern replacement for PortableSettingsProvider using the .NET configuration system.
/// Provides cross-platform settings persistence using JSON files.
/// </summary>
public sealed class PortableConfiguration : IDisposable
{
    private readonly string _configurationPath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly Dictionary<string, object?> _settings;
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PortableConfiguration"/> class.
    /// </summary>
    /// <param name="applicationName">The name of the application (optional, uses executing assembly name if null).</param>
    /// <param name="configurationDirectory">The directory to store configuration files (optional, uses user's application data if null).</param>
    public PortableConfiguration(string? applicationName = null, string? configurationDirectory = null)
    {
        var appName = applicationName ?? GetExecutableName();
        var configDir = configurationDirectory ?? GetDefaultConfigurationDirectory();
        
        _configurationPath = Path.Combine(configDir, $"{appName}.config.json");
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
        
        _settings = LoadSettings();
    }

    /// <summary>
    /// Gets a setting value by key.
    /// </summary>
    /// <typeparam name="T">The type of the setting value.</typeparam>
    /// <param name="key">The setting key.</param>
    /// <param name="defaultValue">The default value if the setting doesn't exist.</param>
    /// <returns>The setting value or the default value.</returns>
    public T GetValue<T>(string key, T defaultValue = default!)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        
        lock (_lock)
        {
            if (_settings.TryGetValue(key, out var value))
            {
                try
                {
                    if (value is JsonElement jsonElement)
                    {
                        return jsonElement.Deserialize<T>(_jsonOptions) ?? defaultValue;
                    }
                    
                    if (value is T directValue)
                    {
                        return directValue;
                    }
                    
                    // Try to convert the value
                    var convertedValue = Convert.ChangeType(value, typeof(T));
                    return convertedValue is T result ? result : defaultValue;
                }
                catch
                {
                    return defaultValue;
                }
            }
            
            return defaultValue;
        }
    }

    /// <summary>
    /// Sets a setting value by key.
    /// </summary>
    /// <typeparam name="T">The type of the setting value.</typeparam>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The setting value.</param>
    public void SetValue<T>(string key, T value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        
        lock (_lock)
        {
            _settings[key] = value;
        }
    }

    /// <summary>
    /// Removes a setting by key.
    /// </summary>
    /// <param name="key">The setting key to remove.</param>
    /// <returns>True if the setting was removed; otherwise, false.</returns>
    public bool RemoveValue(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        
        lock (_lock)
        {
            return _settings.Remove(key);
        }
    }

    /// <summary>
    /// Checks if a setting exists.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <returns>True if the setting exists; otherwise, false.</returns>
    public bool ContainsKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        
        lock (_lock)
        {
            return _settings.ContainsKey(key);
        }
    }

    /// <summary>
    /// Gets all setting keys.
    /// </summary>
    /// <returns>A collection of all setting keys.</returns>
    public IReadOnlyCollection<string> GetKeys()
    {
        lock (_lock)
        {
            return _settings.Keys.ToArray();
        }
    }

    /// <summary>
    /// Saves the settings to disk.
    /// </summary>
    public void Save()
    {
        lock (_lock)
        {
            try
            {
                var directory = Path.GetDirectoryName(_configurationPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(_settings, _jsonOptions);
                File.WriteAllText(_configurationPath, json);
            }
            catch (Exception ex)
            {
                throw new ConfigurationException($"Failed to save configuration to '{_configurationPath}'", ex);
            }
        }
    }

    /// <summary>
    /// Reloads the settings from disk.
    /// </summary>
    public void Reload()
    {
        lock (_lock)
        {
            _settings.Clear();
            var loadedSettings = LoadSettings();
            foreach (var kvp in loadedSettings)
            {
                _settings[kvp.Key] = kvp.Value;
            }
        }
    }

    /// <summary>
    /// Gets the configuration file path.
    /// </summary>
    public string ConfigurationPath => _configurationPath;

    private Dictionary<string, object?> LoadSettings()
    {
        try
        {
            if (File.Exists(_configurationPath))
            {
                var json = File.ReadAllText(_configurationPath);
                return JsonSerializer.Deserialize<Dictionary<string, object?>>(json, _jsonOptions) 
                       ?? [];
            }
        }
        catch (Exception ex)
        {
            throw new ConfigurationException($"Failed to load configuration from '{_configurationPath}'", ex);
        }
        
        return new Dictionary<string, object?>();
    }

    private static string GetExecutableName()
    {
        return Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location) ?? "TS4Tools";
    }

    private static string GetDefaultConfigurationDirectory()
    {
        return PlatformService.Instance.GetConfigurationDirectory();
    }

    /// <summary>
    /// Disposes the configuration instance and saves any pending changes.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            Save();
            _disposed = true;
        }
    }
}

/// <summary>
/// Exception thrown when configuration operations fail.
/// </summary>
public sealed class ConfigurationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class.
    /// </summary>
    public ConfigurationException() { }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ConfigurationException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ConfigurationException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Extension methods for <see cref="IConfiguration"/> to work with PortableConfiguration.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Creates a portable configuration provider from the given configuration.
    /// </summary>
    /// <param name="configuration">The configuration to wrap.</param>
    /// <param name="applicationName">The application name.</param>
    /// <returns>A new <see cref="PortableConfiguration"/> instance.</returns>
    public static PortableConfiguration AsPortable(this IConfiguration configuration, string? applicationName = null)
    {
        var portable = new PortableConfiguration(applicationName);
        
        // Copy existing configuration values
        foreach (var section in configuration.AsEnumerable())
        {
            if (!string.IsNullOrEmpty(section.Key) && section.Value != null)
            {
                portable.SetValue(section.Key, section.Value);
            }
        }
        
        return portable;
    }
}
