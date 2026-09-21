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

.PARAMETER KeepDb
  Reuse visa2026_easytest (skip drop + --updateDatabase). Use after a fresh Local run.
  Unique-key Facts (Register employee) may fail if the same personal number already exists.

.PARAMETER KeepHost
  Reuse an HTTP-ready :5050 EasyTest host and leave it running after tests.
  Implies KeepDb. Pair with -SkipBuild (the running .exe locks EasyTest output).

.PARAMETER SkipBrowserInstall
  Skip playwright.ps1 install msedge (use when Edge is already installed).

.PARAMETER NoSnapshot
  Do not restore or capture the local pg_dump of visa2026_easytest (always --updateDatabase).

.PARAMETER RefreshSnapshot
  Ignore an existing dump, run --updateDatabase, then recapture the snapshot.

.PARAMETER NoScreenshots
  Disable milestone PNG capture.

.PARAMETER WriteTrx
  Write a TRX file for manual-test-reports (pipeline).

.PARAMETER TrxPath
  TRX output path when -WriteTrx is set.

.EXAMPLE
  .\scripts\local\Record-PlaywrightE2e.ps1 -Target Local

.EXAMPLE
  .\scripts\local\Record-PlaywrightE2e.ps1 -Target Local -SkipBuild -KeepDb -KeepHost -SkipBrowserInstall `
    -Filter PersonOfficerJourney_FindEmployee_Local

.EXAMPLE
  .\scripts\local\Record-PlaywrightE2e.ps1 -Target Local -SkipBuild -SkipBrowserInstall -RefreshSnapshot `
    -Filter PersonOfficerJourney_SignIn_Local

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

    [switch]$KeepDb,

    [switch]$KeepHost,

    [switch]$SkipBrowserInstall,

    [switch]$NoSnapshot,

    [switch]$RefreshSnapshot,

    [switch]$NoScreenshots,

    [switch]$EnableVideo,

    [switch]$WriteTrx,

    [string]$TrxPath = ''
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

if ($EnableVideo) {
    $env:VISA2026_E2E_VIDEO_RECORDING = 'true'
}
else {
    $env:VISA2026_E2E_VIDEO_RECORDING = 'false'
}

if ($KeepHost) {
    $env:VISA2026_E2E_KEEP_HOST = 'true'
    $env:VISA2026_E2E_KEEP_DB = 'true'
}
else {
    Remove-Item Env:\VISA2026_E2E_KEEP_HOST -ErrorAction SilentlyContinue
    if ($KeepDb) {
        $env:VISA2026_E2E_KEEP_DB = 'true'
    }
    else {
        Remove-Item Env:\VISA2026_E2E_KEEP_DB -ErrorAction SilentlyContinue
    }
}

if ($KeepHost -and -not $SkipBuild) {
    Write-Warning 'KeepHost leaves Visa2026.Blazor.Server.exe running on :5050. EasyTest build may lock that output — prefer -SkipBuild.'
}

if ($NoSnapshot) {
    $env:VISA2026_E2E_SNAPSHOT = 'false'
}
else {
    Remove-Item Env:\VISA2026_E2E_SNAPSHOT -ErrorAction SilentlyContinue
}

if ($RefreshSnapshot) {
    $env:VISA2026_E2E_REFRESH_SNAPSHOT = 'true'
}
else {
    Remove-Item Env:\VISA2026_E2E_REFRESH_SNAPSHOT -ErrorAction SilentlyContinue
}

if (-not $SkipBuild) {
    Write-Host 'Building EasyTest configuration...'
    dotnet build Visa2026.slnx -c EasyTest
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed with exit code $LASTEXITCODE" }
}

if ($SkipBrowserInstall) {
    Write-Host 'Skipping Playwright browser install (-SkipBrowserInstall).'
}
else {
    Write-Host "Installing Playwright browsers (idempotent)..."
    $playwrightScript = Join-Path $repoRoot 'Visa2026.E2E.Tests\bin\EasyTest\net8.0\playwright.ps1'
    if (-not (Test-Path -LiteralPath $playwrightScript)) {
        throw "playwright.ps1 not found at $playwrightScript - run dotnet build first."
    }
    & $playwrightScript install msedge
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "playwright install msedge returned $LASTEXITCODE (often OK when system Edge is already present)."
    }
}

$keepDbEffective = [bool]($KeepHost -or $KeepDb)
$snapshotEffective = -not $NoSnapshot
Write-Host "Running Playwright E2E - Target=$Target Filter=$Filter KeepDb=$keepDbEffective KeepHost=$KeepHost Snapshot=$snapshotEffective RefreshSnapshot=$RefreshSnapshot"
$testArgs = @(
    'test', 'Visa2026.E2E.Tests/Visa2026.E2E.Tests.csproj',
    '-c', 'EasyTest', '--no-build',
    '--filter', "FullyQualifiedName~$Filter&Driver=Playwright&Category=UserManual",
    '--logger', 'console;verbosity=normal'
)
if ($WriteTrx) {
    if (-not $TrxPath) {
        $TrxPath = Join-Path $repoRoot 'TestResults\user-manual-e2e-playwright-local.trx'
    }
    $trxDir = Split-Path -Parent $TrxPath
    if ($trxDir -and -not (Test-Path -LiteralPath $trxDir)) {
        New-Item -ItemType Directory -Force -Path $trxDir | Out-Null
    }
    $trxName = Split-Path -Leaf $TrxPath
    $testArgs += @('--logger', "trx;LogFileName=$trxName", '--results-directory', $trxDir)
}
dotnet @testArgs
$testExit = $LASTEXITCODE

if (-not $NoScreenshots -and $testExit -eq 0) {
    $screenshotDir = Join-Path $repoRoot "Visa2026.E2E.Tests\recordings\screenshots\$runStamp"
    if (Test-Path -LiteralPath $screenshotDir) {
        & (Join-Path $repoRoot 'scripts\ci\Copy-EasyTestManualScreenshots.ps1') -ScreenshotRunDir $screenshotDir
    }

    if ($EnableVideo) {
        $markersPath = Join-Path $screenshotDir 'video-markers.json'
        $videoScript = Join-Path $repoRoot 'scripts\ci\Copy-EasyTestManualVideos.ps1'
        if ((Test-Path -LiteralPath $videoScript) -and (Test-Path -LiteralPath $markersPath)) {
            & $videoScript -MarkersPath $markersPath
            if ($LASTEXITCODE -ne 0) {
                throw 'Copy-EasyTestManualVideos.ps1 failed.'
            }
        }
        else {
            Write-Warning "Skipping video trim (EnableVideo set but markers missing at $markersPath)."
        }
    }
}

if ($testExit -ne 0) {
    throw "Playwright E2E failed with exit code $testExit"
}

Write-Host 'Playwright E2E completed successfully.'
