#Requires -Version 5.1
<#
.SYNOPSIS
  Trim doc-anchored guide videos from a single E2E journey recording.

.DESCRIPTION
  Reads user-manual/media-capture-registry.yaml (videos: section) and
  recordings/screenshots/{run}/video-markers.json to ffmpeg-trim one source MP4
  into per-guide files under user-manual/assets/videos/v{version}/{locale}/.

  Replaces legacy fan-out copy (same bytes to every pilot videoFile).

.PARAMETER SourceVideo
  Journey MP4 (default: Visa2026.E2E.Tests/recordings/person-officer-journey.mp4).

.PARAMETER MarkersPath
  video-markers.json from the E2E run. Defaults to newest under recordings/screenshots/*/.

.PARAMETER Version
  videosVersion folder segment (default 2026.08).

.PARAMETER Locales
  Locale codes (default en,tr,tk,ru).

.PARAMETER VideoCaptureKey
  Trim only these registry video keys (default: all registry videos).

.EXAMPLE
  ./scripts/ci/Copy-EasyTestManualVideos.ps1 `
    -SourceVideo Visa2026.E2E.Tests/recordings/person-officer-journey.mp4 `
    -MarkersPath Visa2026.E2E.Tests/recordings/screenshots/20260805-134241/video-markers.json
#>
[CmdletBinding()]
param(
    [string]$SourceVideo = '',

    [string]$MarkersPath = '',

    [string]$Version = '2026.08',

    [string[]]$Locales = @('en', 'tr', 'tk', 'ru'),

    [string[]]$VideoCaptureKey = @()
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$manualRoot = Join-Path $repoRoot 'user-manual'
$registryPath = Join-Path $manualRoot 'media-capture-registry.yaml'

function Get-FfmpegPath {
    $local = Join-Path $repoRoot 'Visa2026.E2E.Tests\.tools\ffmpeg\ffmpeg.exe'
    if (Test-Path -LiteralPath $local) {
        return $local
    }

    $cmd = Get-Command ffmpeg -ErrorAction SilentlyContinue
    if ($cmd) {
        return $cmd.Source
    }

    throw 'ffmpeg not found (install or use Visa2026.E2E.Tests\.tools\ffmpeg\ffmpeg.exe).'
}

function Read-RegistryVideos {
    param([string]$RegistryText)

    $videos = @{}
    $currentKey = $null
    $current = $null
    $inGuideSlugs = $false

    foreach ($line in $RegistryText -split '\r?\n') {
        if ($line -match '^videos:\s*$') {
            continue
        }

        if ($line -match '^\s{2}([a-z0-9][a-z0-9-]+):\s*$' -and $line -notmatch '^\s{4}') {
            if ($currentKey -and $current) {
                $videos[$currentKey] = $current
            }

            $currentKey = $Matches[1]
            $current = [ordered]@{
                captureKey = $currentKey
                guideSlugs = New-Object 'System.Collections.Generic.List[string]'
            }
            $inGuideSlugs = $false
            continue
        }

        if ($null -eq $current) {
            continue
        }

        if ($line -match '^\s{4}guideSlugs:\s*$') {
            $inGuideSlugs = $true
            continue
        }

        if ($inGuideSlugs -and $line -match '^\s{6}-\s+(.+)$') {
            [void]$current.guideSlugs.Add($Matches[1].Trim().Trim('"').Trim("'"))
            continue
        }

        if ($line -match '^\s{4}([A-Za-z0-9_]+):\s*(.+)$') {
            $inGuideSlugs = $false
            $name = $Matches[1]
            $value = $Matches[2].Trim().Trim('"').Trim("'")
            $current[$name] = $value
        }
    }

    if ($currentKey -and $current) {
        $videos[$currentKey] = $current
    }

    return $videos
}

function Get-LatestMarkersPath {
    $screenshotsRoot = Join-Path $repoRoot 'Visa2026.E2E.Tests\recordings\screenshots'
    if (-not (Test-Path -LiteralPath $screenshotsRoot)) {
        return $null
    }

    $runs = Get-ChildItem -LiteralPath $screenshotsRoot -Directory |
        Sort-Object Name -Descending
    foreach ($run in $runs) {
        $candidate = Join-Path $run.FullName 'video-markers.json'
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }

    return $null
}

function ConvertTo-NormalizedMp4 {
    param(
        [string]$Ffmpeg,
        [string]$InputPath,
        [string]$OutputPath
    )

    if ($InputPath -eq $OutputPath) {
        return
    }

    $parent = Split-Path -Parent $OutputPath
    if ($parent) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }

    $tempOut = "$OutputPath.partial.mp4"
    if (Test-Path -LiteralPath $tempOut) {
        Remove-Item -LiteralPath $tempOut -Force
    }

    & $Ffmpeg -y -i $InputPath -c:v libx264 -pix_fmt yuv420p -movflags +faststart $tempOut
    if ($LASTEXITCODE -ne 0) {
        throw "ffmpeg normalize failed for $InputPath"
    }

    Move-Item -LiteralPath $tempOut -Destination $OutputPath -Force
}

function Export-TrimmedGuideVideo {
    param(
        [string]$Ffmpeg,
        [string]$SourcePath,
        [string]$DestPath,
        [double]$StartSeconds,
        [double]$EndSeconds
    )

    if ($EndSeconds -le $StartSeconds) {
        throw "Invalid trim range for $DestPath ($StartSeconds -> $EndSeconds)"
    }

    $parent = Split-Path -Parent $DestPath
    if ($parent) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }

    $start = [Math]::Max(0, $StartSeconds).ToString('0.###', [System.Globalization.CultureInfo]::InvariantCulture)
    $end = $EndSeconds.ToString('0.###', [System.Globalization.CultureInfo]::InvariantCulture)

    & $Ffmpeg -y -ss $start -to $end -i $SourcePath -c:v libx264 -pix_fmt yuv420p -movflags +faststart $DestPath
    if ($LASTEXITCODE -ne 0) {
        throw "ffmpeg trim failed for $DestPath"
    }
}

if (-not (Test-Path -LiteralPath $registryPath)) {
    throw "Media capture registry not found: $registryPath"
}

$registryText = Get-Content -LiteralPath $registryPath -Raw -Encoding UTF8
$registryVideos = Read-RegistryVideos -RegistryText $registryText
if ($registryVideos.Count -eq 0) {
    throw 'No videos: entries found in media-capture-registry.yaml'
}

if ([string]::IsNullOrWhiteSpace($MarkersPath)) {
    $MarkersPath = Get-LatestMarkersPath
}

if ([string]::IsNullOrWhiteSpace($MarkersPath) -or -not (Test-Path -LiteralPath $MarkersPath)) {
    throw "video-markers.json not found. Run UserManual Playwright E2E with screenshots enabled."
}

$markerJson = Get-Content -LiteralPath $MarkersPath -Raw -Encoding UTF8 | ConvertFrom-Json
$markerMap = @{}
if ($markerJson.markers) {
    foreach ($prop in $markerJson.markers.PSObject.Properties) {
        $markerMap[$prop.Name] = [double]$prop.Value
    }
}

$resolvedSource = ''
if (-not [string]::IsNullOrWhiteSpace($markerJson.sourceVideoPath) -and (Test-Path -LiteralPath $markerJson.sourceVideoPath)) {
    $resolvedSource = (Resolve-Path -LiteralPath $markerJson.sourceVideoPath).Path
}
elseif (-not [string]::IsNullOrWhiteSpace($SourceVideo)) {
    $candidate = if ([System.IO.Path]::IsPathRooted($SourceVideo)) { $SourceVideo } else { Join-Path $repoRoot $SourceVideo }
    if (Test-Path -LiteralPath $candidate) {
        $resolvedSource = (Resolve-Path -LiteralPath $candidate).Path
    }
}
else {
    $defaultMp4 = Join-Path $repoRoot 'Visa2026.E2E.Tests\recordings\person-officer-journey.mp4'
    if (Test-Path -LiteralPath $defaultMp4) {
        $resolvedSource = (Resolve-Path -LiteralPath $defaultMp4).Path
    }
}

if ([string]::IsNullOrWhiteSpace($resolvedSource)) {
    throw 'Source journey video not found. Pass -SourceVideo or run Playwright E2E with video recording enabled.'
}

$ffmpeg = Get-FfmpegPath
$normalizedSource = Join-Path $repoRoot 'Visa2026.E2E.Tests\recordings\person-officer-journey.mp4'
if ($resolvedSource -ne $normalizedSource) {
    Write-Host "Normalizing source video -> $normalizedSource"
    ConvertTo-NormalizedMp4 -Ffmpeg $ffmpeg -InputPath $resolvedSource -OutputPath $normalizedSource
    $resolvedSource = $normalizedSource
}

$assetsRoot = Join-Path $repoRoot "user-manual\assets\videos\v$Version"
$selectedKeys = if ($VideoCaptureKey.Count -gt 0) { $VideoCaptureKey } else { @($registryVideos.Keys) }
$copied = 0
$trimManifest = [ordered]@{
    version            = 1
    mediaE2eRunId      = [string]$markerJson.mediaE2eRunId
    sourceRecording    = 'person-officer-journey.mp4'
    markersPath        = $MarkersPath
    videosVersion      = $Version
    clips              = @{}
}

foreach ($captureKey in $selectedKeys) {
    if (-not $registryVideos.ContainsKey($captureKey)) {
        Write-Warning "Unknown video capture key: $captureKey"
        continue
    }

    $entry = $registryVideos[$captureKey]
    $videoFile = [string]$entry.videoFile
    if ([string]::IsNullOrWhiteSpace($videoFile)) {
        Write-Warning "Video entry '$captureKey' missing videoFile"
        continue
    }

    $fromKey = [string]$entry.fromCaptureKey
    $toKey = [string]$entry.toCaptureKey
    if (-not $markerMap.ContainsKey($fromKey) -or -not $markerMap.ContainsKey($toKey)) {
        throw "Missing video markers for '$captureKey' ($fromKey / $toKey). Re-run UserManual E2E."
    }

    $paddingStart = 0.5
    $paddingEnd = 2.0
    if ($entry.paddingStartSeconds) { $paddingStart = [double]$entry.paddingStartSeconds }
    if ($entry.paddingEndSeconds) { $paddingEnd = [double]$entry.paddingEndSeconds }

    $start = [Math]::Max(0, $markerMap[$fromKey] - $paddingStart)
    $end = $markerMap[$toKey] + $paddingEnd

    foreach ($locale in $Locales) {
        $destPath = Join-Path (Join-Path $assetsRoot $locale) $videoFile
        Export-TrimmedGuideVideo -Ffmpeg $ffmpeg -SourcePath $resolvedSource -DestPath $destPath -StartSeconds $start -EndSeconds $end
        Write-Host "  -> $destPath ($start s - $end s)"
        $copied++
    }

    $trimManifest.clips[$captureKey] = [ordered]@{
        videoFile       = $videoFile
        fromCaptureKey  = $fromKey
        toCaptureKey    = $toKey
        startSeconds    = $start
        endSeconds      = $end
        guideSlugs      = @($entry.guideSlugs)
    }
}

if ($copied -eq 0) {
    throw 'No guide videos were trimmed.'
}

$manifestPath = Join-Path $assetsRoot 'video-trim-manifest.json'
$utf8 = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($manifestPath, ($trimManifest | ConvertTo-Json -Depth 6), $utf8)
Write-Host "Wrote video trim manifest: $manifestPath"
Write-Host "Trimmed $copied guide video file(s) to $assetsRoot"
