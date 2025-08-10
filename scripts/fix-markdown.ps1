#!/usr/bin/env pwsh
# Fix common markdown linting issues

param(
    [string]$FilePath = $null,
    [switch]$DryRun = $false
)

function Fix-MarkdownFile {
    param([string]$Path)

    Write-Host "Fixing markdown file: $Path" -ForegroundColor Yellow

    $content = Get-Content -Path $Path -Raw
    $lines = Get-Content -Path $Path
    $newLines = @()

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        $nextLine = if ($i + 1 -lt $lines.Count) { $lines[$i + 1] } else { $null }
        $prevLine = if ($i - 1 -ge 0) { $lines[$i - 1] } else { $null }

        # Fix trailing spaces (MD009)
        $line = $line -replace '\s+$', ''

        # Add blank line before headings if missing (MD022)
        if ($line -match '^#+\s+' -and $prevLine -and $prevLine.Trim() -ne '') {
            $newLines += ''
        }

        $newLines += $line

        # Add blank line after headings if missing (MD022)
        if ($line -match '^#+\s+' -and $nextLine -and $nextLine.Trim() -ne '' -and $nextLine -notmatch '^#+\s+') {
            $newLines += ''
        }

        # Add blank line before lists if missing (MD032)
        if ($line -match '^\s*[-\*\+]\s+' -and $prevLine -and $prevLine.Trim() -ne '' -and $prevLine -notmatch '^#+\s+' -and $prevLine -notmatch '^\s*[-\*\+]\s+') {
            # Insert blank line before current line
            $newLines[$newLines.Count - 1] = ''
            $newLines += $line
        }
    }

    # Add final newline if missing
    if ($newLines[-1] -ne '') {
        $newLines += ''
    }

    if (-not $DryRun) {
        $newLines | Set-Content -Path $Path -Encoding UTF8
        Write-Host "Fixed: $Path" -ForegroundColor Green
    } else {
        Write-Host "Would fix: $Path" -ForegroundColor Cyan
    }
}

# Get markdown files
$markdownFiles = @()
if ($FilePath) {
    $markdownFiles = @($FilePath)
} else {
    $markdownFiles = Get-ChildItem -Path . -Filter "*.md" -Recurse | Select-Object -ExpandProperty FullName
}

foreach ($file in $markdownFiles) {
    Fix-MarkdownFile -Path $file
}

Write-Host "`nMarkdown fixing complete!" -ForegroundColor Green
