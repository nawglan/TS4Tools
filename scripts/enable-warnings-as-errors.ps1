# Re-enable TreatWarningsAsErrors for all test projects
Write-Host "Re-enabling TreatWarningsAsErrors in test projects..."

$testProjects = Get-ChildItem -Path "tests" -Filter "*.csproj" -Recurse

foreach ($project in $testProjects) {
    Write-Host "Processing: $($project.Name)"

    $content = Get-Content $project.FullName -Raw

    if ($content -match '<TreatWarningsAsErrors>false</TreatWarningsAsErrors>') {
        $content = $content -replace '<TreatWarningsAsErrors>false</TreatWarningsAsErrors>', '<TreatWarningsAsErrors>true</TreatWarningsAsErrors>'
        Set-Content -Path $project.FullName -Value $content -NoNewline
        Write-Host "  Enabled TreatWarningsAsErrors"
    }
}

Write-Host "Completed re-enabling warnings as errors in test projects"
