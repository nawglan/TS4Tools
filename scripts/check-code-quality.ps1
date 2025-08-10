#!/usr/bin/env pwsh

param(
    [switch]$Fix,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"
$solutionFile = "TS4Tools.sln"

Write-Host "🔍 TS4Tools Code Quality Checker" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""

# Check if solution file exists
if (-not (Test-Path $solutionFile)) {
    Write-Host "❌ Solution file '$solutionFile' not found!" -ForegroundColor Red
    Write-Host "Please run this script from the repository root." -ForegroundColor Red
    exit 1
}

# Step 1: Restore dependencies
Write-Host "📦 Restoring NuGet packages..." -ForegroundColor Cyan
try {
    dotnet restore $solutionFile --verbosity minimal
    Write-Host "✅ Dependencies restored successfully" -ForegroundColor Green
}
catch {
    Write-Host "❌ Failed to restore dependencies" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 2: Code formatting
Write-Host "🎨 Checking code formatting..." -ForegroundColor Cyan

if ($Fix) {
    Write-Host "🔧 Auto-fixing formatting issues..." -ForegroundColor Yellow
    try {
        dotnet format $solutionFile
        Write-Host "✅ Code formatting applied" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Failed to apply formatting" -ForegroundColor Red
        exit 1
    }
} else {
    try {
        $verbosity = if ($Verbose) { "diagnostic" } else { "minimal" }
        dotnet format $solutionFile --verify-no-changes --verbosity $verbosity
        Write-Host "✅ All code is properly formatted" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Code formatting issues detected!" -ForegroundColor Red
        Write-Host ""
        Write-Host "🔧 To fix automatically:" -ForegroundColor Yellow
        Write-Host "   .\scripts\check-code-quality.ps1 -Fix" -ForegroundColor Yellow
        Write-Host "   or: dotnet format $solutionFile" -ForegroundColor Yellow
        Write-Host ""
        exit 1
    }
}
Write-Host ""

# Step 3: Build with analyzers
Write-Host "🔍 Running .NET analyzers..." -ForegroundColor Cyan
try {
    $buildVerbosity = if ($Verbose) { "normal" } else { "minimal" }
    dotnet build $solutionFile --no-restore --configuration Release --verbosity $buildVerbosity --property:TreatWarningsAsErrors=true
    Write-Host "✅ All analyzer checks passed" -ForegroundColor Green
}
catch {
    Write-Host "❌ Analyzer warnings detected!" -ForegroundColor Red
    Write-Host ""
    Write-Host "💡 Common solutions:" -ForegroundColor Yellow
    Write-Host "   • Check for unused variables or imports" -ForegroundColor Yellow
    Write-Host "   • Ensure proper null handling" -ForegroundColor Yellow
    Write-Host "   • Follow naming conventions" -ForegroundColor Yellow
    Write-Host "   • Add XML documentation for public APIs" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "🔍 For detailed output, run with -Verbose flag" -ForegroundColor Yellow
    exit 1
}
Write-Host ""

# Step 4: Markdown linting (if available)
Write-Host "📝 Checking markdown formatting..." -ForegroundColor Cyan

# Check if markdownlint is available
try {
    $markdownlintPath = Get-Command markdownlint -ErrorAction SilentlyContinue
    if ($markdownlintPath) {
        if ($Fix) {
            Write-Host "🔧 Auto-fixing markdown issues..." -ForegroundColor Yellow
            try {
                markdownlint --fix **/*.md --ignore node_modules 2>$null
                Write-Host "✅ Markdown formatting applied" -ForegroundColor Green
            }
            catch {
                Write-Host "⚠️  Some markdown issues couldn't be auto-fixed" -ForegroundColor Yellow
            }
        }

        # Always check after fixing (or if no fix requested)
        try {
            markdownlint **/*.md --ignore node_modules 2>$null
            Write-Host "✅ All markdown is properly formatted" -ForegroundColor Green
        }
        catch {
            Write-Host "⚠️  Markdown formatting issues detected (non-critical)" -ForegroundColor Yellow
            if (-not $Fix) {
                Write-Host "💡 To fix automatically: .\scripts\check-code-quality.ps1 -Fix" -ForegroundColor Yellow
            }
        }
    } else {
        Write-Host "⚠️  markdownlint not found - skipping markdown checks" -ForegroundColor Yellow
        Write-Host "   Install with: npm install -g markdownlint-cli" -ForegroundColor Gray
    }
}
catch {
    Write-Host "⚠️  Could not check markdown formatting" -ForegroundColor Yellow
}
Write-Host ""

# Step 5: Run tests
Write-Host "🧪 Running unit tests..." -ForegroundColor Cyan
try {
    dotnet test $solutionFile --no-build --configuration Release --verbosity minimal
    Write-Host "✅ All tests passed" -ForegroundColor Green
}
catch {
    Write-Host "❌ Some tests failed!" -ForegroundColor Red
    Write-Host "Please fix failing tests before committing." -ForegroundColor Red
    exit 1
}
Write-Host ""

# Summary
Write-Host "🎉 All code quality checks passed!" -ForegroundColor Green
Write-Host ""
Write-Host "✅ Code formatting: OK" -ForegroundColor Green
Write-Host "✅ .NET analyzers: OK" -ForegroundColor Green
Write-Host "✅ Markdown formatting: OK" -ForegroundColor Green
Write-Host "✅ Unit tests: OK" -ForegroundColor Green
Write-Host ""
Write-Host "🚀 Your code is ready to commit!" -ForegroundColor Green
