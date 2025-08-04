using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace TS4Tools.Core.System.Platform;

/// <summary>
/// Default implementation of <see cref="IPlatformService"/> that provides platform-specific functionality.
/// </summary>
public class PlatformService : IPlatformService
{
    private static readonly Lazy<PlatformService> _instance = new(() => new PlatformService());
    
    /// <summary>
    /// Gets the singleton instance of the platform service.
    /// </summary>
    public static PlatformService Instance => _instance.Value;
    
    /// <summary>
    /// Windows reserved filenames that cannot be used.
    /// </summary>
    private static readonly string[] WindowsReservedNames =
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };
    
    /// <summary>
    /// Invalid characters for Windows filenames.
    /// </summary>
    private static readonly char[] WindowsInvalidChars = { '<', '>', ':', '"', '|', '?', '*', '/', '\\' };
    
    private readonly PlatformType _currentPlatform;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="PlatformService"/> class.
    /// </summary>
    public PlatformService()
    {
        _currentPlatform = DetectPlatform();
    }
    
    /// <inheritdoc />
    public PlatformType CurrentPlatform => _currentPlatform;
    
    /// <inheritdoc />
    public bool SupportsApplicationManifest => _currentPlatform == PlatformType.Windows;
    
    /// <inheritdoc />
    public string GetConfigurationDirectory()
    {
        return _currentPlatform switch
        {
            PlatformType.Windows => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            PlatformType.MacOS => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library", "Preferences"),
            PlatformType.Linux => Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") ??
                                 Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config"),
            _ => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
        };
    }
    
    /// <inheritdoc />
    public string GetApplicationDataDirectory()
    {
        return _currentPlatform switch
        {
            PlatformType.Windows => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            PlatformType.MacOS => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library", "Application Support"),
            PlatformType.Linux => Environment.GetEnvironmentVariable("XDG_DATA_HOME") ??
                                 Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share"),
            _ => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
        };
    }
    
    /// <inheritdoc />
    public bool IsValidFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;
            
        return _currentPlatform switch
        {
            PlatformType.Windows => IsValidWindowsFileName(fileName),
            PlatformType.MacOS => IsValidUnixFileName(fileName),
            PlatformType.Linux => IsValidUnixFileName(fileName),
            _ => IsValidUnixFileName(fileName)
        };
    }
    
    /// <inheritdoc />
    public string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "unnamed";
            
        return _currentPlatform switch
        {
            PlatformType.Windows => SanitizeWindowsFileName(fileName),
            PlatformType.MacOS => SanitizeUnixFileName(fileName),
            PlatformType.Linux => SanitizeUnixFileName(fileName),
            _ => SanitizeUnixFileName(fileName)
        };
    }
    
    /// <inheritdoc />
    public string GetLineEnding()
    {
        return _currentPlatform switch
        {
            PlatformType.Windows => "\r\n",
            PlatformType.MacOS => "\n",
            PlatformType.Linux => "\n",
            _ => Environment.NewLine
        };
    }
    
    private static PlatformType DetectPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return PlatformType.Windows;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return PlatformType.MacOS;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return PlatformType.Linux;
            
        return PlatformType.Unknown;
    }
    
    private static bool IsValidWindowsFileName(string fileName)
    {
        // Check for reserved names
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        if (Array.Exists(WindowsReservedNames, name => 
            string.Equals(name, nameWithoutExtension, StringComparison.OrdinalIgnoreCase)))
            return false;
            
        // Check for invalid characters
        if (fileName.IndexOfAny(WindowsInvalidChars) >= 0)
            return false;
            
        // Check for control characters (0-31)
        if (HasControlCharacters(fileName))
            return false;
            
        // Check for names ending with period or space
        if (fileName.EndsWith('.') || fileName.EndsWith(' '))
            return false;
            
        // Check for maximum path length (255 characters for filename)
        return fileName.Length <= 255;
    }
    
    private static bool IsValidUnixFileName(string fileName)
    {
        // Check for null character
        if (fileName.Contains('\0'))
            return false;
            
        // Check for reserved names
        if (fileName is "." or "..")
            return false;
            
        // Check for maximum path length (255 bytes for filename on most Unix systems)
        return global::System.Text.Encoding.UTF8.GetByteCount(fileName) <= 255;
    }
    
    private static string SanitizeWindowsFileName(string fileName)
    {
        // Replace invalid characters with underscore
        foreach (var invalidChar in WindowsInvalidChars)
        {
            fileName = fileName.Replace(invalidChar, '_');
        }
        
        // Remove control characters
        fileName = Regex.Replace(fileName, @"[\x00-\x1F]", "");
        
        // Remove trailing periods and spaces
        fileName = fileName.TrimEnd('.', ' ');
        
        // Handle reserved names by adding suffix
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        
        if (Array.Exists(WindowsReservedNames, name => 
            string.Equals(name, nameWithoutExtension, StringComparison.OrdinalIgnoreCase)))
        {
            fileName = $"{nameWithoutExtension}_file{extension}";
        }
        
        // Ensure filename is not empty and not too long
        if (string.IsNullOrWhiteSpace(fileName))
            fileName = "unnamed";
        if (fileName.Length > 255)
            fileName = fileName[..255];
            
        return fileName;
    }
    
    private static string SanitizeUnixFileName(string fileName)
    {
        // Replace null characters and path separators for consistency
        fileName = fileName.Replace('\0', '_');
        fileName = fileName.Replace('/', '_');
        fileName = fileName.Replace('\\', '_');
        
        // Handle reserved names
        if (fileName is "." or "..")
            fileName = $"_{fileName}";
            
        // Ensure filename is not empty
        if (string.IsNullOrWhiteSpace(fileName))
            fileName = "unnamed";
            
        // Truncate if too long (accounting for UTF-8 encoding)
        while (global::System.Text.Encoding.UTF8.GetByteCount(fileName) > 255 && fileName.Length > 0)
        {
            fileName = fileName[..^1];
        }
        
        return fileName;
    }
    
    private static bool HasControlCharacters(string fileName)
    {
        foreach (var c in fileName)
        {
            if (c <= 31)
                return true;
        }
        return false;
    }
}
