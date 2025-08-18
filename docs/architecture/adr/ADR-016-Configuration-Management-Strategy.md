# ADR-016: Configuration Management Strategy

**Status:** Proposed
**Date:** August 17, 2025
**Deciders:** Architecture Team, Security Team, Operations Team

## Context

The TS4Tools application requires secure, flexible configuration management for:

1. **Security Credentials**: API keys, connection strings, and authentication tokens
1. **Environment Variations**: Different settings for development, testing, and production
1. **User Preferences**: Application settings and customization options
1. **Feature Toggles**: Enabling/disabling features for testing and rollout
1. **Performance Tuning**: Adjustable parameters for optimization

Current configuration management has security vulnerabilities with sensitive data in source control and lacks proper environment separation.

## Decision

We will implement a **hierarchical configuration management strategy** with the following components:

1. **Configuration Hierarchy**: Environment variables > User secrets > Configuration files > Defaults
1. **Security-First Design**: No sensitive data in source control or configuration files
1. **Environment Separation**: Clear separation between development, testing, and production
1. **Validation and Defaults**: Comprehensive validation with sensible defaults
1. **Hot Reload Support**: Dynamic configuration updates without application restart

## Rationale

### Current Security Issues

#### Sensitive Data Exposure

- Configuration files contain placeholder sensitive data
- No clear separation between public and private configuration
- Risk of accidental commit of production credentials
- No encryption for sensitive configuration values

#### Environment Management

- No standardized environment-specific configuration
- Manual configuration management across environments
- Inconsistent settings between development and production
- No validation of configuration completeness

### Benefits of Structured Approach

#### Security

- Zero sensitive data in source control
- Encrypted storage for production secrets
- Audit trail for configuration changes
- Role-based access to configuration data

#### Maintainability

- Clear configuration schema and validation
- Consistent configuration patterns across components
- Automatic detection of configuration issues
- Self-documenting configuration structure

#### Operational Excellence

- Environment-specific configuration automation
- Configuration drift detection
- Centralized configuration management
- Automated configuration backup and restore

## Architecture Design

### Configuration Hierarchy

```csharp
public class ConfigurationBuilder
{
    public static IConfiguration BuildConfiguration(string[] args, string environment)
    {
        var builder = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            // 1. Base configuration (lowest priority)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            
            // 2. Environment-specific configuration
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            
            // 3. User secrets (development only)
            .AddUserSecretsIfDevelopment(environment)
            
            // 4. Environment variables (highest priority)
            .AddEnvironmentVariables("TS4TOOLS_")
            
            // 5. Command line arguments (override all)
            .AddCommandLine(args);

        return builder.Build();
    }

    private static IConfigurationBuilder AddUserSecretsIfDevelopment(
        this IConfigurationBuilder builder, string environment)
    {
        if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
        {
            builder.AddUserSecrets<Program>();
        }
        return builder;
    }
}
```

### Configuration Schema and Validation

```csharp
// Root configuration class
public class ApplicationConfiguration
{
    public const string SectionName = "TS4Tools";

    [Required]
    public DatabaseConfiguration Database { get; set; } = new();

    [Required]
    public LoggingConfiguration Logging { get; set; } = new();

    [Required]
    public SecurityConfiguration Security { get; set; } = new();

    [Required]
    public PerformanceConfiguration Performance { get; set; } = new();

    [Required]
    public FeatureConfiguration Features { get; set; } = new();

    [Required]
    public UserInterfaceConfiguration UserInterface { get; set; } = new();
}

// Database configuration with validation
public class DatabaseConfiguration
{
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string ConnectionString { get; set; } = string.Empty;

    [Range(1, 300)]
    public int CommandTimeout { get; set; } = 30;

    [Range(1, 100)]
    public int MaxRetryAttempts { get; set; } = 3;

    [Range(100, 10000)]
    public int RetryDelayMs { get; set; } = 1000;

    public bool EnableSensitiveDataLogging { get; set; } = false;
}

// Security configuration
public class SecurityConfiguration
{
    [Required]
    [StringLength(256, MinimumLength = 32)]
    public string EncryptionKey { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string ApiKey { get; set; } = string.Empty;

    [Range(300, 86400)] // 5 minutes to 24 hours
    public int TokenExpirationSeconds { get; set; } = 3600;

    public bool RequireHttps { get; set; } = true;

    public bool EnableCors { get; set; } = false;

    [Required]
    public List<string> AllowedOrigins { get; set; } = new();
}

// Performance configuration
public class PerformanceConfiguration
{
    [Range(1, 100)]
    public int MaxConcurrentOperations { get; set; } = 4;

    [Range(1024, 1073741824)] // 1KB to 1GB
    public long MaxMemoryUsageBytes { get; set; } = 536870912; // 512MB

    [Range(1, 3600)] // 1 second to 1 hour
    public int CacheExpirationSeconds { get; set; } = 300;

    [Range(1000, 300000)] // 1 second to 5 minutes
    public int OperationTimeoutMs { get; set; } = 30000;

    public bool EnablePerformanceCounters { get; set; } = true;
}

// Feature toggles
public class FeatureConfiguration
{
    public bool EnableExperimentalFeatures { get; set; } = false;
    public bool EnableBetaFeatures { get; set; } = false;
    public bool EnableLegacyCompatibility { get; set; } = true;
    public bool EnableAdvancedLogging { get; set; } = false;
    public bool EnablePerformanceMetrics { get; set; } = true;
    public bool EnableAutoUpdates { get; set; } = true;

    public Dictionary<string, bool> CustomFeatures { get; set; } = new();
}

// User interface configuration
public class UserInterfaceConfiguration
{
    [Required]
    [StringLength(20, MinimumLength = 2)]
    public string DefaultLanguage { get; set; } = "en-US";

    [Required]
    [StringLength(20, MinimumLength = 2)]
    public string DefaultTheme { get; set; } = "Light";

    [Range(1, 10)]
    public int MaxRecentFiles { get; set; } = 10;

    public bool EnableAnimations { get; set; } = true;
    public bool ShowToolTips { get; set; } = true;
    public bool AutoSaveEnabled { get; set; } = true;

    [Range(30, 3600)] // 30 seconds to 1 hour
    public int AutoSaveIntervalSeconds { get; set; } = 300;
}
```

### Configuration Validation Service

```csharp
public interface IConfigurationValidator
{
    ValidationResult ValidateConfiguration<T>(T configuration) where T : class;
    ValidationResult ValidateAllConfigurations();
    Task<bool> TestConnectionsAsync();
}

public class ConfigurationValidator : IConfigurationValidator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ConfigurationValidator> _logger;

    public ConfigurationValidator(IServiceProvider serviceProvider, ILogger<ConfigurationValidator> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public ValidationResult ValidateConfiguration<T>(T configuration) where T : class
    {
        var validationContext = new ValidationContext(configuration, _serviceProvider, null);
        var validationResults = new List<ValidationResult>();

        bool isValid = Validator.TryValidateObject(configuration, validationContext, validationResults, true);

        if (!isValid)
        {
            foreach (var result in validationResults)
            {
                _logger.LogError("Configuration validation error: {ErrorMessage}", result.ErrorMessage);
            }
        }

        return new ValidationResult(isValid, validationResults);
    }

    public ValidationResult ValidateAllConfigurations()
    {
        var overallResult = new ValidationResult(true, new List<ValidationResult>());

        // Validate each configuration section
        var configurations = new List<(string Name, object Config)>
        {
            ("Database", _serviceProvider.GetRequiredService<IOptions<DatabaseConfiguration>>().Value),
            ("Security", _serviceProvider.GetRequiredService<IOptions<SecurityConfiguration>>().Value),
            ("Performance", _serviceProvider.GetRequiredService<IOptions<PerformanceConfiguration>>().Value),
            ("Features", _serviceProvider.GetRequiredService<IOptions<FeatureConfiguration>>().Value),
            ("UserInterface", _serviceProvider.GetRequiredService<IOptions<UserInterfaceConfiguration>>().Value)
        };

        foreach (var (name, config) in configurations)
        {
            var result = ValidateConfiguration(config);
            if (!result.IsValid)
            {
                _logger.LogError("Configuration section {SectionName} validation failed", name);
                overallResult.IsValid = false;
                overallResult.ValidationResults.AddRange(result.ValidationResults);
            }
        }

        return overallResult;
    }

    public async Task<bool> TestConnectionsAsync()
    {
        try
        {
            // Test database connection
            var dbConfig = _serviceProvider.GetRequiredService<IOptions<DatabaseConfiguration>>().Value;
            await TestDatabaseConnectionAsync(dbConfig.ConnectionString);

            // Test external API connections
            var securityConfig = _serviceProvider.GetRequiredService<IOptions<SecurityConfiguration>>().Value;
            await TestApiConnectionAsync(securityConfig.ApiKey);

            _logger.LogInformation("All configuration connections tested successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Configuration connection test failed");
            return false;
        }
    }

    private async Task TestDatabaseConnectionAsync(string connectionString)
    {
        // Implementation depends on database provider
        await Task.Delay(100); // Placeholder
    }

    private async Task TestApiConnectionAsync(string apiKey)
    {
        // Test external API connectivity
        await Task.Delay(100); // Placeholder
    }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationResult> ValidationResults { get; set; } = new();

    public ValidationResult(bool isValid, List<ValidationResult> validationResults)
    {
        IsValid = isValid;
        ValidationResults = validationResults;
    }
}
```

### Secure Configuration Management

```csharp
public interface ISecureConfigurationManager
{
    Task<string> GetSecretAsync(string key);
    Task SetSecretAsync(string key, string value);
    Task<bool> SecretExistsAsync(string key);
    Task DeleteSecretAsync(string key);
    Task<Dictionary<string, string>> GetAllSecretsAsync();
}

public class SecureConfigurationManager : ISecureConfigurationManager
{
    private readonly IDataProtector _dataProtector;
    private readonly ILogger<SecureConfigurationManager> _logger;
    private readonly string _configurationPath;

    public SecureConfigurationManager(
        IDataProtectionProvider dataProtectionProvider,
        ILogger<SecureConfigurationManager> logger,
        IOptions<SecurityConfiguration> securityOptions)
    {
        _dataProtector = dataProtectionProvider.CreateProtector("TS4Tools.Configuration");
        _logger = logger;
        _configurationPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TS4Tools", "secure-config.json");
    }

    public async Task<string> GetSecretAsync(string key)
    {
        try
        {
            var secrets = await LoadSecretsAsync();
            if (secrets.TryGetValue(key, out var encryptedValue))
            {
                return _dataProtector.Unprotect(encryptedValue);
            }
            throw new KeyNotFoundException($"Secret key '{key}' not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve secret for key: {Key}", key);
            throw;
        }
    }

    public async Task SetSecretAsync(string key, string value)
    {
        try
        {
            var secrets = await LoadSecretsAsync();
            secrets[key] = _dataProtector.Protect(value);
            await SaveSecretsAsync(secrets);
            
            _logger.LogInformation("Secret updated for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set secret for key: {Key}", key);
            throw;
        }
    }

    public async Task<bool> SecretExistsAsync(string key)
    {
        var secrets = await LoadSecretsAsync();
        return secrets.ContainsKey(key);
    }

    public async Task DeleteSecretAsync(string key)
    {
        var secrets = await LoadSecretsAsync();
        if (secrets.Remove(key))
        {
            await SaveSecretsAsync(secrets);
            _logger.LogInformation("Secret deleted for key: {Key}", key);
        }
    }

    public async Task<Dictionary<string, string>> GetAllSecretsAsync()
    {
        var secrets = await LoadSecretsAsync();
        var decryptedSecrets = new Dictionary<string, string>();

        foreach (var kvp in secrets)
        {
            try
            {
                decryptedSecrets[kvp.Key] = _dataProtector.Unprotect(kvp.Value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to decrypt secret for key: {Key}", kvp.Key);
            }
        }

        return decryptedSecrets;
    }

    private async Task<Dictionary<string, string>> LoadSecretsAsync()
    {
        if (!File.Exists(_configurationPath))
            return new Dictionary<string, string>();

        try
        {
            var json = await File.ReadAllTextAsync(_configurationPath);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load secrets from: {Path}", _configurationPath);
            return new Dictionary<string, string>();
        }
    }

    private async Task SaveSecretsAsync(Dictionary<string, string> secrets)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_configurationPath)!);
        
        var json = JsonSerializer.Serialize(secrets, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_configurationPath, json);
    }
}
```

### Configuration Hot Reload

```csharp
public interface IConfigurationChangeNotifier
{
    event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;
    void StartMonitoring();
    void StopMonitoring();
}

public class ConfigurationChangeNotifier : IConfigurationChangeNotifier, IDisposable
{
    public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigurationChangeNotifier> _logger;
    private readonly List<IDisposable> _changeTokenRegistrations = new();
    private bool _disposed;

    public ConfigurationChangeNotifier(IConfiguration configuration, ILogger<ConfigurationChangeNotifier> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public void StartMonitoring()
    {
        _logger.LogInformation("Starting configuration change monitoring");

        // Monitor specific configuration sections
        var sectionsToMonitor = new[]
        {
            "TS4Tools:Database",
            "TS4Tools:Security",
            "TS4Tools:Performance",
            "TS4Tools:Features",
            "TS4Tools:UserInterface"
        };

        foreach (var section in sectionsToMonitor)
        {
            var changeToken = _configuration.GetReloadToken();
            var registration = changeToken.RegisterChangeCallback(OnConfigurationChanged, section);
            _changeTokenRegistrations.Add(registration);
        }
    }

    public void StopMonitoring()
    {
        _logger.LogInformation("Stopping configuration change monitoring");
        
        foreach (var registration in _changeTokenRegistrations)
        {
            registration.Dispose();
        }
        _changeTokenRegistrations.Clear();
    }

    private void OnConfigurationChanged(object? state)
    {
        var sectionName = state as string ?? "Unknown";
        
        _logger.LogInformation("Configuration changed for section: {SectionName}", sectionName);

        var eventArgs = new ConfigurationChangedEventArgs(sectionName, DateTime.UtcNow);
        ConfigurationChanged?.Invoke(this, eventArgs);

        // Re-register for future changes
        var changeToken = _configuration.GetReloadToken();
        var registration = changeToken.RegisterChangeCallback(OnConfigurationChanged, state);
        _changeTokenRegistrations.Add(registration);
    }

    public void Dispose()
    {
        if (_disposed) return;

        StopMonitoring();
        _disposed = true;
    }
}

public class ConfigurationChangedEventArgs : EventArgs
{
    public string SectionName { get; }
    public DateTime ChangedAt { get; }

    public ConfigurationChangedEventArgs(string sectionName, DateTime changedAt)
    {
        SectionName = sectionName;
        ChangedAt = changedAt;
    }
}
```

### Environment-Specific Configuration Files

```json
// appsettings.json (base configuration)
{
  "TS4Tools": {
    "Database": {
      "ConnectionString": "Data Source=ts4tools.db",
      "CommandTimeout": 30,
      "MaxRetryAttempts": 3,
      "RetryDelayMs": 1000,
      "EnableSensitiveDataLogging": false
    },
    "Performance": {
      "MaxConcurrentOperations": 4,
      "MaxMemoryUsageBytes": 536870912,
      "CacheExpirationSeconds": 300,
      "OperationTimeoutMs": 30000,
      "EnablePerformanceCounters": true
    },
    "Features": {
      "EnableExperimentalFeatures": false,
      "EnableBetaFeatures": false,
      "EnableLegacyCompatibility": true,
      "EnableAdvancedLogging": false,
      "EnablePerformanceMetrics": true,
      "EnableAutoUpdates": true
    },
    "UserInterface": {
      "DefaultLanguage": "en-US",
      "DefaultTheme": "Light",
      "MaxRecentFiles": 10,
      "EnableAnimations": true,
      "ShowToolTips": true,
      "AutoSaveEnabled": true,
      "AutoSaveIntervalSeconds": 300
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "TS4Tools": "Information",
      "Microsoft": "Warning"
    }
  }
}

// appsettings.Development.json
{
  "TS4Tools": {
    "Database": {
      "EnableSensitiveDataLogging": true
    },
    "Features": {
      "EnableExperimentalFeatures": true,
      "EnableBetaFeatures": true,
      "EnableAdvancedLogging": true
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "TS4Tools": "Debug"
    }
  }
}

// appsettings.Production.json
{
  "TS4Tools": {
    "Performance": {
      "MaxConcurrentOperations": 8,
      "MaxMemoryUsageBytes": 1073741824,
      "EnablePerformanceCounters": true
    },
    "Features": {
      "EnableExperimentalFeatures": false,
      "EnableBetaFeatures": false,
      "EnableAdvancedLogging": false
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "TS4Tools": "Information"
    }
  }
}
```

## Implementation Guidelines

### Service Registration

```csharp
public static class ConfigurationServiceExtensions
{
    public static IServiceCollection AddConfigurationManagement(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Configure and validate all configuration sections
        services.Configure<ApplicationConfiguration>(configuration.GetSection(ApplicationConfiguration.SectionName));
        services.Configure<DatabaseConfiguration>(configuration.GetSection($"{ApplicationConfiguration.SectionName}:Database"));
        services.Configure<SecurityConfiguration>(configuration.GetSection($"{ApplicationConfiguration.SectionName}:Security"));
        services.Configure<PerformanceConfiguration>(configuration.GetSection($"{ApplicationConfiguration.SectionName}:Performance"));
        services.Configure<FeatureConfiguration>(configuration.GetSection($"{ApplicationConfiguration.SectionName}:Features"));
        services.Configure<UserInterfaceConfiguration>(configuration.GetSection($"{ApplicationConfiguration.SectionName}:UserInterface"));

        // Register configuration services
        services.AddSingleton<IConfigurationValidator, ConfigurationValidator>();
        services.AddSingleton<ISecureConfigurationManager, SecureConfigurationManager>();
        services.AddSingleton<IConfigurationChangeNotifier, ConfigurationChangeNotifier>();

        // Add data protection for secure configuration
        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TS4Tools", "keys")));

        // Validate configuration on startup
        services.AddHostedService<ConfigurationValidationService>();

        return services;
    }
}

public class ConfigurationValidationService : IHostedService
{
    private readonly IConfigurationValidator _validator;
    private readonly ILogger<ConfigurationValidationService> _logger;

    public ConfigurationValidationService(
        IConfigurationValidator validator,
        ILogger<ConfigurationValidationService> logger)
    {
        _validator = validator;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating application configuration...");

        var validationResult = _validator.ValidateAllConfigurations();
        if (!validationResult.IsValid)
        {
            _logger.LogCritical("Configuration validation failed. Application cannot start.");
            throw new InvalidOperationException("Configuration validation failed");
        }

        var connectionsValid = await _validator.TestConnectionsAsync();
        if (!connectionsValid)
        {
            _logger.LogWarning("Some configuration connections failed validation");
        }

        _logger.LogInformation("Configuration validation completed successfully");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

## Migration Strategy

### Phase 1: Security Foundation (Week 1)

1. Implement secure configuration manager with encryption
1. Remove all sensitive data from configuration files
1. Set up environment variable and user secrets infrastructure
1. Create configuration validation framework

### Phase 2: Configuration Structure (Week 2)

1. Implement typed configuration classes with validation
1. Create environment-specific configuration files
1. Add configuration change monitoring
1. Update service registration patterns

### Phase 3: Integration and Testing (Week 3)

1. Update all components to use new configuration patterns
1. Implement configuration hot reload where appropriate
1. Add comprehensive configuration testing
1. Create configuration management documentation

### Phase 4: Production Readiness (Week 4)

1. Set up production configuration management
1. Implement configuration backup and restore
1. Add configuration drift monitoring
1. Create operational runbooks for configuration management

## Success Criteria

### Security Metrics

- [ ] Zero sensitive data in source control
- [ ] All production secrets encrypted at rest
- [ ] Configuration access properly audited
- [ ] Secure configuration deployment process

### Operational Metrics

- [ ] Configuration validation catches 100% of invalid configurations
- [ ] Hot reload works for all non-critical configuration changes
- [ ] Configuration drift detection and alerting
- [ ] Automated configuration backup and restore

### Developer Experience

- [ ] Clear configuration documentation and examples
- [ ] Easy local development setup with user secrets
- [ ] Configuration IntelliSense and validation in IDE
- [ ] Automated configuration testing in CI/CD

## Consequences

### Positive

- **Security**: No sensitive data exposure in source control
- **Flexibility**: Easy environment-specific configuration
- **Reliability**: Configuration validation prevents runtime errors
- **Maintainability**: Typed configuration with compile-time checking
- **Operational Excellence**: Configuration monitoring and management

### Negative

- **Complexity**: Additional infrastructure for configuration management
- **Learning Curve**: Team must learn new configuration patterns
- **Initial Setup**: More complex initial environment setup
- **Dependency**: Additional dependency on data protection services

### Mitigation Strategies

- Provide comprehensive documentation and examples
- Create configuration setup scripts and tools
- Implement gradual migration from existing configuration
- Provide training on new configuration patterns

## Related ADRs

- ADR-002: Dependency Injection (service registration patterns)
- ADR-014: Error Handling and Exception Strategy (configuration error handling)
- ADR-015: Logging and Observability Framework (configuration logging)
