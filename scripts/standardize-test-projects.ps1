#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Standardizes all test project files to use consistent properties.

.DESCRIPTION
    This script updates all test project (.csproj) files in the tests directory
    to use a consistent set of properties for better maintainability and reliability.

.EXAMPLE
    .\scripts\standardize-test-projects.ps1
#>

[CmdletBinding()]
param()

Write-Host "Standardizing test project files..." -ForegroundColor Cyan

# Find all test project files
$testProjects = Get-ChildItem -Path "tests" -Filter "*.csproj" -Recurse

if ($testProjects.Count -eq 0) {
    Write-Warning "No test projects found in the tests directory."
    exit 0
}

Write-Host "Found $($testProjects.Count) test project(s) to standardize:" -ForegroundColor Green
$testProjects | ForEach-Object { Write-Host "  - $($_.FullName)" -ForegroundColor Gray }

$standardizedCount = 0
$errors = @()

foreach ($project in $testProjects) {
    try {
        Write-Host ""
        Write-Host "Processing: $($project.Name)" -ForegroundColor Yellow

        # Read the project file
        [xml]$projectXml = Get-Content $project.FullName

        # Get or create PropertyGroup
        $propertyGroup = $projectXml.Project.PropertyGroup
        if (-not $propertyGroup) {
            $propertyGroup = $projectXml.CreateElement("PropertyGroup")
            $projectXml.Project.AppendChild($propertyGroup)
        }

        # If there are multiple PropertyGroup elements, use the first one
        if ($propertyGroup -is [System.Array]) {
            $propertyGroup = $propertyGroup[0]
        }

        # Function to set or update property
        function Set-ProjectProperty {
            param([string]$Name, [string]$Value, [System.Xml.XmlElement]$PropertyGroup, [System.Xml.XmlDocument]$Document)

            $existingProperty = $PropertyGroup.SelectSingleNode($Name)
            if ($existingProperty) {
                if ($existingProperty.InnerText -ne $Value) {
                    Write-Host "    Updating ${Name}: '$($existingProperty.InnerText)' -> '$Value'" -ForegroundColor Cyan
                    $existingProperty.InnerText = $Value
                }
            } else {
                Write-Host "    Adding ${Name}: '$Value'" -ForegroundColor Green
                $newProperty = $Document.CreateElement($Name)
                $newProperty.InnerText = $Value
                $PropertyGroup.AppendChild($newProperty)
            }
        }

        # Standardize properties
        Set-ProjectProperty -Name "TargetFramework" -Value "net9.0" -PropertyGroup $propertyGroup -Document $projectXml
        Set-ProjectProperty -Name "LangVersion" -Value "latest" -PropertyGroup $propertyGroup -Document $projectXml
        Set-ProjectProperty -Name "Nullable" -Value "enable" -PropertyGroup $propertyGroup -Document $projectXml
        Set-ProjectProperty -Name "ImplicitUsings" -Value "enable" -PropertyGroup $propertyGroup -Document $projectXml
        Set-ProjectProperty -Name "IsPackable" -Value "false" -PropertyGroup $propertyGroup -Document $projectXml
        Set-ProjectProperty -Name "IsTestProject" -Value "true" -PropertyGroup $propertyGroup -Document $projectXml
        Set-ProjectProperty -Name "TreatWarningsAsErrors" -Value "true" -PropertyGroup $propertyGroup -Document $projectXml
        Set-ProjectProperty -Name "EnableNETAnalyzers" -Value "true" -PropertyGroup $propertyGroup -Document $projectXml
        Set-ProjectProperty -Name "AnalysisLevel" -Value "latest-recommended" -PropertyGroup $propertyGroup -Document $projectXml

        # Remove any duplicate LangVersion entries
        $langVersionNodes = $propertyGroup.SelectNodes("LangVersion")
        if ($langVersionNodes.Count -gt 1) {
            Write-Host "    Removing duplicate LangVersion entries" -ForegroundColor Yellow
            for ($i = 1; $i -lt $langVersionNodes.Count; $i++) {
                $propertyGroup.RemoveChild($langVersionNodes[$i])
            }
        }

        # Save the updated project file
        $projectXml.Save($project.FullName)
        Write-Host "    $($project.Name) standardized successfully" -ForegroundColor Green
        $standardizedCount++

    } catch {
        $errorMsg = "Failed to process $($project.Name): $($_.Exception.Message)"
        Write-Error $errorMsg
        $errors += $errorMsg
    }
}

# Summary
Write-Host ""
Write-Host "Standardization Summary:" -ForegroundColor Cyan
Write-Host "  Successfully standardized: $standardizedCount/$($testProjects.Count) projects" -ForegroundColor Green

if ($errors.Count -gt 0) {
    Write-Host "  Errors encountered: $($errors.Count)" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host "    - $_" -ForegroundColor Red }
    exit 1
} else {
    Write-Host ""
    Write-Host "All test projects have been standardized successfully!" -ForegroundColor Green
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "  1. Review the changes: git diff" -ForegroundColor Gray
    Write-Host "  2. Test the build: dotnet build TS4Tools.sln" -ForegroundColor Gray
    Write-Host "  3. Run tests: dotnet test TS4Tools.sln" -ForegroundColor Gray
    Write-Host "  4. Commit changes: git add . && git commit -m 'Standardize test project properties'" -ForegroundColor Gray
}
