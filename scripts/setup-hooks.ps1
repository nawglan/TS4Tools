# Setup Pre-Commit Hooks for TS4Tools
# Run this script to configure automatic code formatting before commits

Write-Host "Setting up TS4Tools pre-commit hooks..." -ForegroundColor Cyan

$RepoRoot = git rev-parse --show-toplevel
Set-Location $RepoRoot

# Check if we're in a git repository
if (-not (Test-Path ".git")) {
    Write-Host "ERROR: Not in a git repository!" -ForegroundColor Red
    exit 1
}

# Configure Git to use our custom hooks directory
Write-Host "Configuring Git hooks path..." -ForegroundColor Yellow
git config core.hooksPath .githooks

if ($LASTEXITCODE -eq 0) {
    Write-Host "SUCCESS: Git hooks path configured successfully." -ForegroundColor Green
} else {
    Write-Host "ERROR: Failed to configure Git hooks path." -ForegroundColor Red
    exit 1
}

# Make the hooks executable (important for Unix-like systems)
if ($IsLinux -or $IsMacOS) {
    Write-Host "Making hooks executable..." -ForegroundColor Yellow
    chmod +x .githooks/pre-commit
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "SUCCESS: Hooks made executable." -ForegroundColor Green
    } else {
        Write-Host "ERROR: Failed to make hooks executable." -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "SUCCESS: Pre-commit hooks setup complete!" -ForegroundColor Green
Write-Host ""
Write-Host "What happens now:" -ForegroundColor Cyan
Write-Host "  * Code will be automatically formatted before each commit" -ForegroundColor White
Write-Host "  * Only staged files will be processed" -ForegroundColor White
Write-Host "  * A quick build check will run to catch compilation errors" -ForegroundColor White
Write-Host ""
Write-Host "Commands:" -ForegroundColor Cyan
Write-Host "  * Normal commit: git commit -m `"your message`"" -ForegroundColor White
Write-Host "  * Skip hooks (not recommended): git commit --no-verify -m `"your message`"" -ForegroundColor White
Write-Host "  * Manual format: dotnet format TS4Tools.sln" -ForegroundColor White
Write-Host ""
Write-Host "To disable hooks later:" -ForegroundColor Yellow
Write-Host "   git config --unset core.hooksPath" -ForegroundColor White
