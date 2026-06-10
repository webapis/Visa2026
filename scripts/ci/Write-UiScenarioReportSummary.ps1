#Requires -Version 5.1
<#
.SYNOPSIS
  Write GitHub Actions step summary for UI scenario report.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ReportDir,

    [string]$Version = $env:APP_VERSION
)

$ErrorActionPreference = 'Continue'

if (-not (Test-Path -LiteralPath $ReportDir)) {
    Write-Host "Report directory missing: $ReportDir"
    return
}

$jsonPath = Join-Path $ReportDir 'results.json'
$junitPath = Join-Path $ReportDir 'results.junit.xml'
$htmlPath = Join-Path $ReportDir 'index.html'

$passed = 0
$failed = 0
$total = 0
$scenarios = @()

if (Test-Path -LiteralPath $jsonPath) {
    $json = Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json
    $passed = [int]$json.passed
    $failed = [int]$json.failed
    $total = [int]$json.total
    $scenarios = @($json.scenarios)
    if (-not $Version -and $json.ApplicationVersion) {
        $Version = [string]$json.ApplicationVersion
    }
}

$summaryPath = $env:GITHUB_STEP_SUMMARY
if (-not $summaryPath) {
    Write-Host "GITHUB_STEP_SUMMARY not set; skipping summary markdown."
    return
}

$status = if ($failed -eq 0 -and $total -gt 0) { 'PASSED' } elseif ($total -eq 0) { 'NO RESULTS' } else { 'FAILED' }
$lines = @(
    '## UI scenario report',
    '',
    "| | |",
    "|---|---|",
    "| Status | **$status** |",
    "| Version | ``$Version`` |",
    "| Passed | $passed / $total |",
    "| Report | ``index.html`` in artifact **ui-scenario-report** |",
    ''
)

if ($env:GITHUB_REPOSITORY) {
    $parts = $env:GITHUB_REPOSITORY -split '/'
    if ($parts.Count -ge 2) {
        $pagesBase = "https://$($parts[0]).github.io/$($parts[1])/test-reports"
        $pagesLatest = "$pagesBase/latest/index.html"
        if ($Version) {
            $pagesVersion = "$pagesBase/$Version/index.html"
            $lines += "| Published (main) | [latest]($pagesLatest) · [$Version]($pagesVersion) |"
        } else {
            $lines += "| Published (main) | [latest]($pagesLatest) |"
        }

        $lines += ''
    }
}

if ($scenarios.Count -gt 0) {
    $lines += '### Scenarios'
    $lines += ''
    $lines += '| Scenario | Result |'
    $lines += '|----------|--------|'
    foreach ($s in $scenarios) {
        $icon = if ($s.ok) { 'pass' } else { '**FAIL**' }
        $lines += "| ``$($s.id)`` | $icon |"
    }

    $lines += ''
}

if (Test-Path -LiteralPath $junitPath) {
    $lines += "- JUnit: ``results.junit.xml`` (GitHub Checks tab)"
    $lines += ''
}

$lines | Out-File -FilePath $summaryPath -Encoding utf8 -Append
Write-Host "Wrote GitHub step summary."
