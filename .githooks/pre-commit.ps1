# TS4Tools Pre-Commit Hook (PowerShell)
# Automatically formats code before commits

Write-Host "🔍 Running pre-commit checks..." -ForegroundColor Cyan

# Check if dotnet is available
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "❌ dotnet CLI not found. Please install .NET SDK." -ForegroundColor Red
    exit 1
}

# Get the root directory of the git repository
$RepoRoot = git rev-parse --show-toplevel
Set-Location $RepoRoot

# Check if solution file exists
if (-not (Test-Path "TS4Tools.sln")) {
    Write-Host "❌ TS4Tools.sln not found in repository root." -ForegroundColor Red
    exit 1
}

Write-Host "📁 Repository root: $RepoRoot" -ForegroundColor Gray

# Format only staged files
Write-Host "🔧 Formatting staged files..." -ForegroundColor Yellow

# Get list of staged C# files
$StagedFiles = git diff --cached --name-only --diff-filter=ACM | Where-Object { $_ -match '\.(cs|csproj|props|targets)$' }

if (-not $StagedFiles) {
    Write-Host "ℹ️  No C# files staged for commit." -ForegroundColor Gray
} else {
    Write-Host "📝 Staged C# files:" -ForegroundColor Gray
    $StagedFiles | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
    
    # Run dotnet format on the solution
    Write-Host "🎨 Running dotnet format..." -ForegroundColor Yellow
    
    # Create a temporary file list for --include parameter
    $FileList = $StagedFiles -join ','
    
    dotnet format TS4Tools.sln --verbosity minimal --include $FileList | Out-Null
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Code formatting completed successfully." -ForegroundColor Green
        
        # Re-stage any files that were formatted
        Write-Host "📋 Re-staging formatted files..." -ForegroundColor Yellow
        $StagedFiles | ForEach-Object { git add $_ }
    } else {
        Write-Host "❌ Code formatting failed!" -ForegroundColor Red
        Write-Host ""
        Write-Host "🔧 To fix manually:" -ForegroundColor Yellow
        Write-Host "   dotnet format TS4Tools.sln" -ForegroundColor White
        Write-Host ""
        Write-Host "💡 To skip this check (not recommended):" -ForegroundColor Yellow
        Write-Host "   git commit --no-verify" -ForegroundColor White
        exit 1
    }
}

# Optional: Run quick build check to catch compilation errors
Write-Host "🔨 Running quick build check..." -ForegroundColor Yellow
dotnet build TS4Tools.sln --verbosity quiet --configuration Debug --no-restore 2>$null | Out-Null

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Build check passed." -ForegroundColor Green
} else {
    Write-Host "⚠️  Build check failed - there may be compilation issues." -ForegroundColor Yellow
    Write-Host "   This won't block the commit, but consider running 'dotnet build' to check." -ForegroundColor Gray
}

Write-Host "🎉 Pre-commit checks completed successfully!" -ForegroundColor Green
Write-Host ""
