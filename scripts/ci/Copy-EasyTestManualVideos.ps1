#Requires -Version 5.1
<#
.SYNOPSIS
  Copy EasyTest journey MP4s into officer manual video assets (static storage).

.DESCRIPTION
  Promotes gitignored recordings from Visa2026.E2E.Tests/recordings/ to
  user-manual/assets/videos/v{version}/{locale}/ for MkDocs <video> embeds.
  English captures are replicated to tr/tk/ru until per-locale recordings (D12).

.PARAMETER SourceVideo
  Path to source MP4 (e.g. recordings/person-officer-journey.mp4).

.PARAMETER Version
  videosVersion folder segment (default 2026.08).

.PARAMETER Locales
  Locale codes to receive files (default en,tr,tk,ru).

.PARAMETER GuideVideo
  One or more guide video file names to write (e.g. person-register.mp4).
  When omitted, copies to all pilot guide video files.

.EXAMPLE
  ./scripts/ci/Copy-EasyTestManualVideos.ps1 `
    -SourceVideo Visa2026.E2E.Tests/recordings/person-officer-journey.mp4

.EXAMPLE
  ./scripts/ci/Copy-EasyTestManualVideos.ps1 `
    -SourceVideo Visa2026.E2E.Tests/recordings/passport-create-with-shots.mp4 `
    -GuideVideo person-add-passport.mp4
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$SourceVideo,

    [string]$Version = '2026.08',

    [string[]]$Locales = @('en', 'tr', 'tk', 'ru'),

    [string[]]$GuideVideo = @()
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$sourcePath = if ([System.IO.Path]::IsPathRooted($SourceVideo)) {
    $SourceVideo
} else {
    Join-Path $repoRoot $SourceVideo
}

if (-not (Test-Path -LiteralPath $sourcePath)) {
    throw "Source video not found: $sourcePath"
}

$pilotGuideVideos = @(
    'login-sign-in.mp4',
    'navigation-shell.mp4',
    'person-register.mp4',
    'person-add-passport.mp4'
)

$targets = if ($GuideVideo.Count -gt 0) { $GuideVideo } else { $pilotGuideVideos }
foreach ($name in $targets) {
    if ($name -notlike '*.mp4') {
        throw "Guide video name must end with .mp4: $name"
    }
}

$assetsRoot = Join-Path $repoRoot "user-manual\assets\videos\v$Version"
$copied = 0

foreach ($locale in $Locales) {
    $localeDir = Join-Path $assetsRoot $locale
    New-Item -ItemType Directory -Force -Path $localeDir | Out-Null
    foreach ($destName in $targets) {
        $destPath = Join-Path $localeDir $destName
        Copy-Item -LiteralPath $sourcePath -Destination $destPath -Force
        Write-Host "  -> $destPath"
        $copied++
    }
}

Write-Host "Copied $copied manual video file(s) to $assetsRoot"
