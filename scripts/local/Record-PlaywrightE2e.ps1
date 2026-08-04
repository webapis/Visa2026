#Requires -Version 5.1
<#
.SYNOPSIS
  Run Playwright E2E (Local or Staging) with optional screenshots/video for user-manual media.

.PARAMETER Target
  Local (:5050 + fresh DB) or Staging (live URL).

.PARAMETER BaseUrl
  Override VISA2026_E2E_BASE_URL (required for non-default Staging host).

.PARAMETER Filter
  Test name fragment (default: passport journey Local).

.PARAMETER SkipBuild
  Skip dotnet build -c EasyTest.

.PARAMETER NoScreenshots
  Disable milestone PNG capture.

.EXAMPLE
  .\scripts\local\Record-PlaywrightE2e.ps1 -Target Local

.EXAMPLE
  $env:VISA2026_E2E_USER = 'StandardUser'
  .\scripts\local\Record-PlaywrightE2e.ps1 -Target Staging -BaseUrl 'https://10.100.128.25:8080' `
    -Filter PersonOfficerJourney_LoginCreateEmployeeAddPassport_Staging
#>
[CmdletBinding()]
param(
    [ValidateSet('Local', 'Staging')]
    [string]$Target = 'Local',

    [string]$BaseUrl = '',

    [string]$Filter = '',

    [switch]$SkipBuild,

    [switch]$NoScreenshots
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
Set-Location -LiteralPath $repoRoot

$env:VISA2026_E2E_TARGET = $Target
$env:VISA2026_E2E_HEADED = 'true'
Remove-Item Env:\VISA2026_E2E_HEADLESS -ErrorAction SilentlyContinue

if ($BaseUrl) {
    $env:VISA2026_E2E_BASE_URL = $BaseUrl.Trim().TrimEnd('/')
}
elseif ($Target -eq 'Staging' -and -not $env:VISA2026_E2E_BASE_URL) {
    $env:VISA2026_E2E_BASE_URL = 'https://10.100.128.25:8080'
}

if (-not $Filter) {
    $Filter = if ($Target -eq 'Staging') {
        'PersonOfficerJourney_LoginCreateEmployeeAddPassport_Staging'
    } else {
        'PersonOfficerJourney_LoginCreateEmployeeAddPassport_Local'
    }
}

$runStamp = Get-Date -Format 'yyyyMMdd-HHmmss'
if ($NoScreenshots) {
    $env:VISA2026_E2E_SCREENSHOTS = 'false'
    Remove-Item Env:\VISA2026_E2E_SCREENSHOT_RUN -ErrorAction SilentlyContinue
}
else {
    $env:VISA2026_E2E_SCREENSHOTS = 'true'
    $env:VISA2026_E2E_SCREENSHOT_RUN = $runStamp
}

if (-not $SkipBuild) {
    Write-Host 'Building EasyTest configuration...'
    dotnet build Visa2026.slnx -c EasyTest
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed with exit code $LASTEXITCODE" }
}

Write-Host "Installing Playwright browsers (idempotent)..."
$playwrightScript = Join-Path $repoRoot 'Visa2026.E2E.Tests\bin\EasyTest\net8.0\playwright.ps1'
if (-not (Test-Path -LiteralPath $playwrightScript)) {
    throw "playwright.ps1 not found at $playwrightScript - run dotnet build first."
}
& $playwrightScript install msedge
if ($LASTEXITCODE -ne 0) {
    Write-Warning "playwright install msedge returned $LASTEXITCODE (often OK when system Edge is already present)."
}

Write-Host "Running Playwright E2E - Target=$Target Filter=$Filter"
$testArgs = @(
    'test', 'Visa2026.E2E.Tests/Visa2026.E2E.Tests.csproj',
    '-c', 'EasyTest', '--no-build',
    '--filter', "FullyQualifiedName~$Filter&Driver=Playwright",
    '--logger', 'console;verbosity=normal'
)
dotnet @testArgs
$testExit = $LASTEXITCODE

if (-not $NoScreenshots -and $testExit -eq 0) {
    $screenshotDir = Join-Path $repoRoot "Visa2026.E2E.Tests\recordings\screenshots\$runStamp"
    if (Test-Path -LiteralPath $screenshotDir) {
        & (Join-Path $repoRoot 'scripts\ci\Copy-EasyTestManualScreenshots.ps1') -ScreenshotRunDir $screenshotDir
    }
}

if ($testExit -ne 0) {
    throw "Playwright E2E failed with exit code $testExit"
}

Write-Host 'Playwright E2E completed successfully.'
