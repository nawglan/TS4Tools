param(
    [switch]$Fix,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"
$solutionFile = "TS4Tools.sln"

Write-Host "TS4Tools Code Quality Checker" -ForegroundColor Cyan
Write-Host "==============================" -ForegroundColor Cyan

# Check if solution file exists
if (-not (Test-Path $solutionFile)) {
    Write-Host "Solution file '$solutionFile' not found!" -ForegroundColor Red
    Write-Host "Please run this script from the repository root." -ForegroundColor Red
    exit 1
}

# Step 1: Restore dependencies
Write-Host "Restoring NuGet packages..." -ForegroundColor Cyan
dotnet restore $solutionFile --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to restore dependencies" -ForegroundColor Red
    exit 1
}
Write-Host "Dependencies restored successfully" -ForegroundColor Green

# Step 2: Code formatting
Write-Host "Checking code formatting..." -ForegroundColor Cyan
if ($Fix) {
    Write-Host "Auto-fixing formatting issues..." -ForegroundColor Yellow
    dotnet format $solutionFile
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to apply formatting" -ForegroundColor Red
        exit 1
    }
    Write-Host "Code formatting applied" -ForegroundColor Green
} else {
    $verbosity = if ($Verbose) { "diagnostic" } else { "minimal" }
    dotnet format $solutionFile --verify-no-changes --verbosity $verbosity
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Code formatting issues detected!" -ForegroundColor Red
        Write-Host "To fix automatically:" -ForegroundColor Yellow
        Write-Host "  .\scripts\check-code-quality.ps1 -Fix" -ForegroundColor Yellow
        Write-Host "  or: dotnet format $solutionFile" -ForegroundColor Yellow
        exit 1
    }
    Write-Host "All code is properly formatted" -ForegroundColor Green
}

# Step 3: Build with analyzers
Write-Host "Running .NET analyzers..." -ForegroundColor Cyan
$buildVerbosity = if ($Verbose) { "normal" } else { "minimal" }
dotnet build $solutionFile --no-restore --configuration Release --verbosity $buildVerbosity --property:TreatWarningsAsErrors=true
if ($LASTEXITCODE -ne 0) {
    Write-Host "Analyzer warnings detected!" -ForegroundColor Red
    Write-Host "Common solutions:" -ForegroundColor Yellow
    Write-Host "  - Check for unused variables or imports" -ForegroundColor Yellow
    Write-Host "  - Ensure proper null handling" -ForegroundColor Yellow
    Write-Host "  - Follow naming conventions" -ForegroundColor Yellow
    Write-Host "  - Add XML documentation for public APIs" -ForegroundColor Yellow
    exit 1
}
Write-Host "All analyzer checks passed" -ForegroundColor Green

# Step 4: Run tests
Write-Host "Running unit tests..." -ForegroundColor Cyan
dotnet test $solutionFile --no-build --configuration Release --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    Write-Host "Some tests failed!" -ForegroundColor Red
    Write-Host "Please fix failing tests before committing." -ForegroundColor Red
    exit 1
}
Write-Host "All tests passed" -ForegroundColor Green

# Summary
Write-Host ""
Write-Host "All code quality checks passed!" -ForegroundColor Green
Write-Host "Code formatting: OK" -ForegroundColor Green
Write-Host ".NET analyzers: OK" -ForegroundColor Green
Write-Host "Unit tests: OK" -ForegroundColor Green
Write-Host ""
Write-Host "Your code is ready to commit!" -ForegroundColor Green
