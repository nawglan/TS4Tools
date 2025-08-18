# Pre-Commit Hooks Setup

This repository includes pre-commit hooks that automatically format your code before each commit,
ensuring consistent code style across the project.

## Quick Setup

Run this command from the repository root:

```powershell

# Windows (PowerShell)

.\scripts\setup-hooks.ps1
```

```bash

# Linux/macOS

chmod +x .githooks/pre-commit
git config core.hooksPath .githooks
```

## What the Pre-Commit Hook Does

1. **ðŸ” Checks for staged C# files** - Only processes files you're actually committing
1. **ðŸŽ¨ Runs dotnet format** - Automatically formats staged files according to project standards
1. **ðŸ“‹ Re-stages formatted files** - Ensures formatted changes are included in your commit
1. **ðŸ”¨ Quick build check** - Catches compilation errors before commit (non-blocking)

## Usage

### Normal Workflow

```bash
git add .
git commit -m "your commit message"

# Hook runs automatically, formats code, and commits

```

### Skip Hooks (Not Recommended)

```bash
git commit --no-verify -m "your commit message"

# Bypasses all pre-commit checks

```

### Manual Formatting

```bash
dotnet format TS4Tools.sln

# Format all files manually

```

## Troubleshooting

### Hook Not Running

- Verify hooks are configured: `git config core.hooksPath`
- Should show: `.githooks`
- Re-run setup script if needed

### Permission Errors (Linux/macOS)

```bash
chmod +x .githooks/pre-commit
```

### Format Failures

- Check that .NET SDK is installed: `dotnet --version`
- Ensure you're in the repository root
- Try manual formatting: `dotnet format TS4Tools.sln`

### Disable Hooks

```bash
git config --unset core.hooksPath

# Removes custom hooks configuration

```

## Files Structure

```text
.githooks/
â”œâ”€â”€ pre-commit          # Unix/Linux/macOS hook (bash)
â”œâ”€â”€ pre-commit.ps1      # Windows PowerShell version
â””â”€â”€ README.md          # This file

scripts/
â””â”€â”€ setup-hooks.ps1    # Setup script
```

## Integration with CI

The pre-commit hooks ensure that code reaching the CI pipeline is already properly formatted,
reducing CI failures and maintaining code quality standards.

## Customization

To modify the formatting rules:

1. Edit `.editorconfig` in the repository root
1. Modify `Directory.Packages.props` analyzer settings
1. Update project-specific `.csproj` analyzer configurations

The hooks will automatically use your updated formatting rules.
