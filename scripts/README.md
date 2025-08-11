# Development Scripts

This directory contains utility scripts to help with development and code quality checks.

## Code Quality Checker

### PowerShell (Windows/Cross-platform)

```powershell

# Run all checks

./scripts/check-code-quality.ps1

# Fix formatting issues automatically

./scripts/check-code-quality.ps1 -Fix

# Run with verbose output

./scripts/check-code-quality.ps1 -Verbose

# Combine flags

./scripts/check-code-quality.ps1 -Fix -Verbose
```

### Bash (Linux/macOS)

```bash

# Make script executable (first time only)

chmod +x scripts/check-code-quality.sh

# Run all checks

./scripts/check-code-quality.sh

# Fix formatting issues automatically

./scripts/check-code-quality.sh --fix

# Run with verbose output

./scripts/check-code-quality.sh --verbose

# Get help

./scripts/check-code-quality.sh --help
```

## What the Script Does

The code quality checker performs the same checks as the CI/CD pipeline:

1. **ðŸ“¦ Dependency Restoration**: Restores NuGet packages
2. **ðŸŽ¨ Code Formatting**: Checks/fixes code formatting using `dotnet format`
3. **ðŸ” Analyzer Checks**: Runs .NET analyzers (Microsoft.CodeAnalysis.NetAnalyzers, SonarAnalyzer.CSharp)
4. **ðŸ§ª Unit Tests**: Runs unit tests to ensure functionality isn't broken

## Integration with Git Hooks

You can set up a pre-commit hook to automatically run these checks:

### Option 1: Simple pre-commit hook

```bash

# Create .git/hooks/pre-commit

#!/bin/sh
./scripts/check-code-quality.sh --fix
```

### Option 2: Using pre-commit framework

Install [pre-commit](https://pre-commit.com/) and create `.pre-commit-config.yaml`:

```yaml
repos:

  - repo: local
    hooks:

      - id: dotnet-format
        name: dotnet format
        entry: dotnet format --verify-no-changes
        language: system
        files: \\.cs$

      - id: dotnet-build
        name: dotnet build (analyzers)
        entry: dotnet build --no-restore --verbosity minimal --property:TreatWarningsAsErrors=true
        language: system
        files: \\.(cs|csproj|sln)$
        pass_filenames: false
```

## IDE Integration

### Visual Studio Code

Install the C# Dev Kit extension which includes:

- Real-time analyzer feedback
- Format-on-save capability
- IntelliSense with analyzer suggestions

### Visual Studio

Analyzers run automatically and show warnings/errors in the Error List.

## Troubleshooting

### Common Issues

#### Formatting Errors

- Run `dotnet format TS4Tools.sln` to auto-fix
- Check `.editorconfig` settings if issues persist

#### Analyzer Warnings

- CA1515: Make test classes internal â†’ Already suppressed in test projects
- CA1812: Unused classes â†’ Remove or make them static if containing only static members
- CS8618: Non-nullable field uninitialized â†’ Add null checks or nullable annotations

#### Build Errors

- Ensure you're using .NET 9.0 SDK
- Run `dotnet restore` if package references are missing
- Check for missing project references

### Getting Help

- Check [.NET Code Analysis Rules](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/code-quality-rules/)
- Review [EditorConfig documentation](https://editorconfig.org/)
- See [dotnet format documentation](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-format)

