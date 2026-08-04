#Requires -Version 5.1
<#
.SYNOPSIS
  Copy EasyTest milestone PNGs into officer manual screenshot assets.

.DESCRIPTION
  Maps Visa2026.E2E.Tests/recordings/screenshots/{run}/ labels to
  user-manual/assets/screenshots/v{version}/{locale}/ files referenced by guides.
  English captures are replicated to tr/tk/ru until per-locale EasyTest runs (D12).

.PARAMETER ScreenshotRunDir
  Directory containing 00-logon-page.png, 01-after-login.png, etc.

.PARAMETER Version
  screenshotsVersion folder segment (default 2026.08).

.PARAMETER Locales
  Locale codes to receive files (default en,tr,tk,ru).

.EXAMPLE
  ./scripts/ci/Copy-EasyTestManualScreenshots.ps1 -ScreenshotRunDir Visa2026.E2E.Tests/recordings/screenshots/20260804-120000
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ScreenshotRunDir,

    [string]$Version = '2026.08',

    [string[]]$Locales = @('en', 'tr', 'tk', 'ru')
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$runDir = if ([System.IO.Path]::IsPathRooted($ScreenshotRunDir)) {
    $ScreenshotRunDir
} else {
    Join-Path $repoRoot $ScreenshotRunDir
}

if (-not (Test-Path -LiteralPath $runDir)) {
    throw "Screenshot run directory not found: $runDir"
}

$assetsRoot = Join-Path $repoRoot "user-manual\assets\screenshots\v$Version"

# Source label (without .png) -> destination file names under each locale folder
$map = [ordered]@{
    '00-logon-page' = @('login-step-01-logon.png')
    '01-after-login' = @(
        'login-step-02-report-dashboard.png',
        'navigation-step-01-shell.png',
        'navigation-step-02-left-menu.png'
    )
    '02-employees-list' = @(
        'navigation-step-03-employees-list.png',
        'person-register-step-01-employees-list.png'
    )
    '03-employee-created' = @('person-register-step-02-saved-detail.png')
    '04-employee-detail' = @(
        'navigation-step-04-detail-form.png',
        'person-register-step-03-open-from-list.png',
        'person-add-passport-step-01-employee-detail.png'
    )
    '05-passport-detail-new' = @('person-add-passport-step-02-passport-form-new.png')
    '06-passport-fields-filled' = @('person-add-passport-step-03-passport-fields-filled.png')
    '07-passport-saved' = @('person-add-passport-step-04-passport-saved.png')
}

function Copy-MappedScreenshot {
    param(
        [string]$SourcePath,
        [string]$TargetPath
    )

    $targetParent = Split-Path -Parent $TargetPath
    New-Item -ItemType Directory -Force -Path $targetParent | Out-Null
    Copy-Item -LiteralPath $SourcePath -Destination $TargetPath -Force
    Write-Host "  -> $TargetPath"
}

$copied = 0
foreach ($entry in $map.GetEnumerator()) {
    $matches = @(Get-ChildItem -LiteralPath $runDir -Filter ($entry.Key + '*.png') -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending)
    if ($matches.Count -eq 0) {
        Write-Warning "Missing source screenshot for label '$($entry.Key)' in $runDir"
        continue
    }

    $sourceFile = $matches[0].FullName
    foreach ($locale in $Locales) {
        $localeDir = Join-Path $assetsRoot $locale
        foreach ($destName in $entry.Value) {
            $destPath = Join-Path $localeDir $destName
            Copy-MappedScreenshot -SourcePath $sourceFile -TargetPath $destPath
            $copied++
        }
    }
}

if ($copied -eq 0) {
    throw "No screenshots copied from $runDir"
}

Write-Host "Copied $copied manual screenshot file(s) to $assetsRoot"
