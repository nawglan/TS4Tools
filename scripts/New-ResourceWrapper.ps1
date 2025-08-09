# TS4Tools Resource Wrapper Generator
# This script generates a new resource wrapper project with all the boilerplate code.
# Usage: .\New-ResourceWrapper.ps1 -ResourceTypeName "ExampleResource" -TypeId "0x12345678" -Category "Gameplay"

param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceTypeName,

    [Parameter(Mandatory=$true)]
    [string]$TypeId,

    [Parameter(Mandatory=$true)]
    [string]$Category,

    [string]$ProjectRoot = $PSScriptRoot + "\.."
)

# Validate parameters
if (-not $ResourceTypeName.EndsWith("Resource")) {
    $ResourceTypeName = $ResourceTypeName + "Resource"
}

if (-not $TypeId.StartsWith("0x")) {
    $TypeId = "0x" + $TypeId
}

# Define paths
$ProjectPath = "$ProjectRoot\src\TS4Tools.Resources.$Category"
$TestProjectPath = "$ProjectRoot\tests\TS4Tools.Resources.$Category.Tests"
$ProjectName = "TS4Tools.Resources.$Category"
$TestProjectName = "TS4Tools.Resources.$Category.Tests"

Write-Host "Creating resource wrapper for $ResourceTypeName..." -ForegroundColor Green
Write-Host "Type ID: $TypeId" -ForegroundColor Yellow
Write-Host "Category: $Category" -ForegroundColor Yellow

# Create project directories
New-Item -ItemType Directory -Path $ProjectPath -Force | Out-Null
New-Item -ItemType Directory -Path "$ProjectPath\DependencyInjection" -Force | Out-Null
New-Item -ItemType Directory -Path $TestProjectPath -Force | Out-Null

# Create project file
$ProjectFileContent = @"
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\TS4Tools.Core.Interfaces\TS4Tools.Core.Interfaces.csproj" />
    <ProjectReference Include="..\TS4Tools.Core.Resources\TS4Tools.Core.Resources.csproj" />
    <ProjectReference Include="..\TS4Tools.Resources.Common\TS4Tools.Resources.Common.csproj" />
  </ItemGroup>

</Project>
"@

Set-Content -Path "$ProjectPath\$ProjectName.csproj" -Value $ProjectFileContent

# Create interface
$InterfaceContent = @"
namespace $ProjectName;

/// <summary>
/// Interface for $ResourceTypeName resources.
/// Resource Type: $TypeId
/// </summary>
public interface I$ResourceTypeName : IResource
{
    // TODO: Add resource-specific properties and methods
    // Example properties:
    // string Name { get; set; }
    // int Count { get; }
    // IReadOnlyList<DataEntry> Entries { get; }
}
"@

Set-Content -Path "$ProjectPath\I$ResourceTypeName.cs" -Value $InterfaceContent

# Create implementation class
$ImplementationContent = @"
using Microsoft.Extensions.Logging;
using TS4Tools.Core.Interfaces;

namespace $ProjectName;

/// <summary>
/// Implementation of $ResourceTypeName for resource type $TypeId.
/// </summary>
public sealed class $ResourceTypeName : I$ResourceTypeName, IDisposable
{
    private readonly ILogger<$ResourceTypeName> _logger;
    private bool _disposed;

    public $ResourceTypeName(ILogger<$ResourceTypeName> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region IResource Implementation

    public uint ResourceType => $TypeId;

    public async Task LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));

        using var reader = new BinaryReader(stream);

        try
        {
            // TODO: Implement binary format parsing
            // Example:
            // var magic = reader.ReadUInt32();
            // if (magic != ExpectedMagic)
            //     throw new InvalidDataException(`"Invalid magic number: 0x{magic:X8}`");

            _logger.LogDebug("Successfully loaded $ResourceTypeName from stream");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load $ResourceTypeName from stream");
            throw;
        }
    }

    public async Task<Stream> SerializeAsync(CancellationToken cancellationToken = default)
    {
        var stream = new MemoryStream();

        try
        {
            using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);

            // TODO: Implement binary format serialization
            // Example:
            // writer.Write(ExpectedMagic);
            // writer.Write(Version);
            // ... write data fields

            stream.Position = 0;
            _logger.LogDebug("Successfully serialized $ResourceTypeName to stream");
            return stream;
        }
        catch (Exception ex)
        {
            stream?.Dispose();
            _logger.LogError(ex, "Failed to serialize $ResourceTypeName");
            throw;
        }
    }

    #endregion

    #region I$ResourceTypeName Implementation

    // TODO: Implement resource-specific properties and methods

    #endregion

    #region IDisposable Implementation

    public void Dispose()
    {
        if (!_disposed)
        {
            // TODO: Dispose managed resources
            _disposed = true;
        }
    }

    #endregion
}
"@

Set-Content -Path "$ProjectPath\$ResourceTypeName.cs" -Value $ImplementationContent

# Create factory class
$FactoryContent = @"
using Microsoft.Extensions.Logging;
using TS4Tools.Core.Resources;

namespace $ProjectName;

/// <summary>
/// Factory for creating $ResourceTypeName instances.
/// </summary>
public sealed class ${ResourceTypeName}Factory : IResourceFactory
{
    private readonly ILogger<${ResourceTypeName}Factory> _logger;

    public ${ResourceTypeName}Factory(ILogger<${ResourceTypeName}Factory> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool CanCreateResource(uint resourceType)
    {
        return resourceType == $TypeId;
    }

    public async Task<IResource> CreateResourceAsync(uint resourceType, Stream stream,
        CancellationToken cancellationToken = default)
    {
        if (!CanCreateResource(resourceType))
            throw new ArgumentException(`"Unsupported resource type: 0x{resourceType:X8}`", nameof(resourceType));

        var resource = new $ResourceTypeName(_logger.CreateLogger<$ResourceTypeName>());

        if (stream.Length > 0)
        {
            await resource.LoadFromStreamAsync(stream, cancellationToken);
        }

        _logger.LogInformation("Successfully created $ResourceTypeName with resource type 0x{ResourceType:X8}", resourceType);
        return resource;
    }

    public IResource CreateResource(uint resourceType)
    {
        if (!CanCreateResource(resourceType))
            throw new ArgumentException(`"Unsupported resource type: 0x{resourceType:X8}`", nameof(resourceType));

        return new $ResourceTypeName(_logger.CreateLogger<$ResourceTypeName>());
    }
}
"@

Set-Content -Path "$ProjectPath\${ResourceTypeName}Factory.cs" -Value $FactoryContent

# Create dependency injection extensions
$DIContent = @"
using Microsoft.Extensions.DependencyInjection;
using TS4Tools.Core.Resources;

namespace $ProjectName.DependencyInjection;

/// <summary>
/// Dependency injection extensions for $Category resource services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds $Category resource services to the dependency injection container.
    /// </summary>
    public static IServiceCollection Add${Category}ResourceServices(this IServiceCollection services)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        services.AddTransient<IResourceFactory, ${ResourceTypeName}Factory>();

        return services;
    }
}
"@

Set-Content -Path "$ProjectPath\DependencyInjection\ServiceCollectionExtensions.cs" -Value $DIContent

# Create test project file
$TestProjectFileContent = @"
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="coverlet.collector" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\$ProjectName\$ProjectName.csproj" />
    <ProjectReference Include="..\TS4Tools.Tests.Common\TS4Tools.Tests.Common.csproj" />
  </ItemGroup>

</Project>
"@

Set-Content -Path "$TestProjectPath\$TestProjectName.csproj" -Value $TestProjectFileContent

# Create test file (copy and customize the template)
$TestContent = Get-Content "$ProjectRoot\tests\TS4Tools.Tests.Templates\TemplateResourceTests.cs" -Raw
$TestContent = $TestContent -replace "Template", $ResourceTypeName
$TestContent = $TestContent -replace "0x00000000", $TypeId
$TestContent = $TestContent -replace "TS4Tools.Tests.Templates", $TestProjectName

Set-Content -Path "$TestProjectPath\${ResourceTypeName}Tests.cs" -Value $TestContent

Write-Host ""
Write-Host "✅ Resource wrapper project created successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Add projects to solution:" -ForegroundColor White
Write-Host "   dotnet sln add src\$ProjectName\$ProjectName.csproj" -ForegroundColor Gray
Write-Host "   dotnet sln add tests\$TestProjectName\$TestProjectName.csproj" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Implement the binary format parsing in:" -ForegroundColor White
Write-Host "   - $ProjectPath\$ResourceTypeName.cs" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Update test data in:" -ForegroundColor White
Write-Host "   - $TestProjectPath\${ResourceTypeName}Tests.cs" -ForegroundColor Gray
Write-Host ""
Write-Host "4. Build and test:" -ForegroundColor White
Write-Host "   dotnet build" -ForegroundColor Gray
Write-Host "   dotnet test" -ForegroundColor Gray
