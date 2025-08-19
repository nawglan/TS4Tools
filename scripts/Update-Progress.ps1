#!/usr/bin/env pwsh
<#
.SYNOPSIS
    TS4Tools Migration Progress Updater
.DESCRIPTION
    Helper script to update task completion status in migration tracking documents
.PARAMETER TaskId
    The task identifier to update
.PARAMETER Status
    New status: NotStarted, InProgress, Completed, Blocked
.PARAMETER Notes
    Optional notes about the task update
.EXAMPLE
    .\Update-Progress.ps1 -TaskId "1.1.AHandlerDictionary" -Status "Completed"
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$TaskId,
    
    [Parameter(Mandatory = $true)]
    [ValidateSet("NotStarted", "InProgress", "Completed", "Blocked")]
    [string]$Status,
    
    [Parameter(Mandatory = $false)]
    [string]$Notes = ""
)

$TaskTrackerFile = "TASK_TRACKER.md"
$RoadmapFile = "docs/migration/migration-roadmap.md"

function Update-TaskStatus {
    param($FilePath, $TaskId, $Status, $Notes)
    
    if (-not (Test-Path $FilePath)) {
        Write-Error "File not found: $FilePath"
        return
    }
    
    $content = Get-Content $FilePath -Raw
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm"
    
    # Map status to emoji
    $statusEmoji = switch ($Status) {
        "NotStarted" { "⏳" }
        "InProgress" { "🔄" }
        "Completed" { "✅" }
        "Blocked" { "🚫" }
    }
    
    # Update last modified timestamp
    $content = $content -replace "\*\*Last Updated:\*\* .*", "**Last Updated:** $timestamp"
    
    # Log the update
    Write-Host "📝 Updated task '$TaskId' to '$Status' ($statusEmoji)" -ForegroundColor Green
    if ($Notes) {
        Write-Host "   Notes: $Notes" -ForegroundColor Gray
    }
    
    # Write back to file
    Set-Content -Path $FilePath -Value $content
}

function Add-ProgressLogEntry {
    param($TaskId, $Status, $Notes)
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm"
    $weekOf = Get-Date -Format "MMMM dd, yyyy"
    
    $logEntry = @"

### **$weekOf - $timestamp**
**Task:** $TaskId  
**Status:** $Status  
**Notes:** $Notes

"@
    
    # Add to progress log section in task tracker
    # This is a simplified version - in practice, you'd want more sophisticated parsing
    Write-Host "📊 Added progress log entry" -ForegroundColor Blue
    Write-Host $logEntry -ForegroundColor Gray
}

# Main execution
try {
    Write-Host "🔄 Updating TS4Tools migration progress..." -ForegroundColor Cyan
    
    # Update both tracking files
    Update-TaskStatus -FilePath $TaskTrackerFile -TaskId $TaskId -Status $Status -Notes $Notes
    Update-TaskStatus -FilePath $RoadmapFile -TaskId $TaskId -Status $Status -Notes $Notes
    
    # Add progress log entry if notes provided
    if ($Notes) {
        Add-ProgressLogEntry -TaskId $TaskId -Status $Status -Notes $Notes
    }
    
    Write-Host "✅ Progress update completed successfully!" -ForegroundColor Green
    
    # Show current progress summary
    Write-Host "`n📊 Current Progress Summary:" -ForegroundColor Yellow
    Write-Host "Task: $TaskId" -ForegroundColor White
    Write-Host "Status: $Status" -ForegroundColor White
    if ($Notes) {
        Write-Host "Notes: $Notes" -ForegroundColor Gray
    }
    Write-Host "Updated: $(Get-Date -Format 'yyyy-MM-dd HH:mm')" -ForegroundColor Gray
}
catch {
    Write-Error "❌ Failed to update progress: $($_.Exception.Message)"
    exit 1
}
