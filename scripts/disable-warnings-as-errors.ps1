# Temporarily disable TreatWarningsAsErrors for all test projects
Write-Host "Temporarily disabling TreatWarningsAsErrors in test projects..."

$testProjects = Get-ChildItem -Path "tests" -Filter "*.csproj" -Recurse

foreach ($project in $testProjects) {
    Write-Host "Processing: $($project.Name)"

    $content = Get-Content $project.FullName -Raw

    if ($content -match '<TreatWarningsAsErrors>true</TreatWarningsAsErrors>') {
        $content = $content -replace '<TreatWarningsAsErrors>true</TreatWarningsAsErrors>', '<TreatWarningsAsErrors>false</TreatWarningsAsErrors>'
        Set-Content -Path $project.FullName -Value $content -NoNewline
        Write-Host "  Disabled TreatWarningsAsErrors"
    }
}

Write-Host "Completed disabling warnings as errors in test projects"
