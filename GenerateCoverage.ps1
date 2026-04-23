# GenerateCoverage.ps1
# Runs all test projects with coverlet, merges results, and generates an HTML report.
# Output: CoverageReport/ (tracked by Git)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root         = $PSScriptRoot
$reportDir    = Join-Path $root "CoverageReport"
$rawDir       = Join-Path $root ".coverage-raw"
$unitProj     = Join-Path $root "NQueen.UnitTests\NQueen.UnitTests.csproj"
$vmProj       = Join-Path $root "NQueen.ViewModelTests\NQueen.ViewModelTests.csproj"
$mergedFile   = Join-Path $rawDir "merged.xml"

# Clean up previous raw output
if (Test-Path $rawDir) { Remove-Item $rawDir -Recurse -Force }
New-Item -ItemType Directory -Path $rawDir | Out-Null

Write-Host "`n--- Running UnitTests with coverage ---" -ForegroundColor Cyan
dotnet test $unitProj `
    --configuration Release `
    --filter "Category!=Slow" `
    --collect:"XPlat Code Coverage" `
    --results-directory "$rawDir\UnitTests" `
    -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura

Write-Host "`n--- Running ViewModelTests with coverage ---" -ForegroundColor Cyan
dotnet test $vmProj `
    --configuration Release `
    --collect:"XPlat Code Coverage" `
    --results-directory "$rawDir\ViewModelTests" `
    -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura

# Collect all coverage.cobertura.xml files
$reports = Get-ChildItem -Path $rawDir -Recurse -Filter "coverage.cobertura.xml" |
           Select-Object -ExpandProperty FullName

if (-not $reports) {
    Write-Error "No coverage files found. Aborting."
    exit 1
}

$reportArgs = $reports -join ";"

Write-Host "`n--- Generating HTML report ---" -ForegroundColor Cyan
reportgenerator `
    "-reports:$reportArgs" `
    "-targetdir:$reportDir" `
    "-reporttypes:Html;Badges" `
    "-assemblyfilters:+NQueen.Domain;+NQueen.Kernel;+NQueen.Shared" `
    "-title:NQueen Code Coverage"

Write-Host "`nDone! Open CoverageReport\index.html to view the report." -ForegroundColor Green
