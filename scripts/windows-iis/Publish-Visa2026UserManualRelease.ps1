#Requires -Version 5.1
<#
.SYNOPSIS
  On-prem release: record (optional), build, and publish officer manual to IIS paths.

.DESCRIPTION
  Windows build-agent entry point for 10.100.128.25 (or any IIS manual host).
  Reads C:\visa2026\env\manual-release.env (or -EnvFile) and calls
  scripts/ci/Publish-ManualRelease.ps1 with on-prem paths.

  Prerequisites on the build host:
    - Git repo at REPO_ROOT (or run from your dev checkout with -EnvFile)
    - Python + MkDocs deps (Build-UserManual.ps1 bootstraps portable Python)
    - For -Record: Edge WebDriver, ffmpeg, local PostgreSQL, EasyTest :5050

.PARAMETER EnvFile
  Dotenv file (default C:\visa2026\env\manual-release.env).

.PARAMETER Record
  Run Record-PlaywrightE2e.ps1 before publish.

.PARAMETER E2ETarget
  Local (default) or Staging when -Record.

.PARAMETER E2EBaseUrl
  Staging URL override (e.g. https://10.100.128.25:8080).

.PARAMETER SkipBuild
  Publish existing user-manual/site only.

.PARAMETER CleanSite
  Remove site target before copy.

.EXAMPLE
  # One-time server setup
  .\Install-Visa2026ManualIisSite.ps1
  .\Enable-Visa2026ManualFirewall.ps1

.EXAMPLE
  # Publish after guide/media changes (no re-record)
  .\Publish-Visa2026UserManualRelease.ps1

.EXAMPLE
  # Full release with fresh screenshots/videos
  .\Publish-Visa2026UserManualRelease.ps1 -Record -CleanSite

.NOTES
  Runbook: docs/USER_MANUAL_RELEASE.md
#>
[CmdletBinding()]
param(
    [string]$EnvFile = 'C:\visa2026\env\manual-release.env',
    [switch]$Record,
    [ValidateSet('Local', 'Staging')]
    [string]$E2ETarget = 'Local',
    [string]$E2EBaseUrl = '',
    [switch]$SkipBuild,
    [switch]$CleanSite,
    [string]$ManualMediaBaseUrl = '',
    [string]$MediaTargetRoot = '',
    [string]$SiteTargetDir = '',
    [string]$RepoRoot = '',
    [string]$RecordFilter = ''
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Visa2026-IisSlots.ps1')

$defaultEnvExample = Join-Path $PSScriptRoot 'env\manual-release.env.example'
if (-not (Test-Path -LiteralPath $EnvFile)) {
    if (Test-Path -LiteralPath $defaultEnvExample) {
        Write-Warning "Env file not found: $EnvFile"
        Write-Warning "Copy env\manual-release.env.example to C:\visa2026\env\manual-release.env and edit paths."
    }
    throw "Missing env file: $EnvFile"
}

$envMap = Read-Visa2026DotEnvMap -Path $EnvFile

function Resolve-ManualReleaseValue {
    param(
        [string]$ParamValue,
        [string]$EnvKey,
        [string]$Fallback = ''
    )

    if (-not [string]::IsNullOrWhiteSpace($ParamValue)) {
        return $ParamValue.Trim()
    }
    if ($envMap.ContainsKey($EnvKey) -and -not [string]::IsNullOrWhiteSpace($envMap[$EnvKey])) {
        return $envMap[$EnvKey].Trim()
    }
    return $Fallback
}

$resolvedRepoRoot = Resolve-ManualReleaseValue -ParamValue $RepoRoot -EnvKey 'REPO_ROOT'
if ([string]::IsNullOrWhiteSpace($resolvedRepoRoot)) {
    $resolvedRepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
}
$resolvedRepoRoot = (Resolve-Path -LiteralPath $resolvedRepoRoot).Path

$mediaBaseUrl = Resolve-ManualReleaseValue -ParamValue $ManualMediaBaseUrl -EnvKey 'MANUAL_MEDIA_BASE_URL'
$mediaRoot = Resolve-ManualReleaseValue -ParamValue $MediaTargetRoot -EnvKey 'MANUAL_MEDIA_ROOT' -Fallback 'C:\visa2026\manual\media'
$siteRoot = Resolve-ManualReleaseValue -ParamValue $SiteTargetDir -EnvKey 'MANUAL_SITE_ROOT' -Fallback 'C:\visa2026\manual\site'
$filter = Resolve-ManualReleaseValue -ParamValue $RecordFilter -EnvKey 'MANUAL_RECORD_FILTER' -Fallback 'PersonOfficerJourney_LoginCreateEmployeeAddPassport'

$releaseScript = Join-Path $resolvedRepoRoot 'scripts\ci\Publish-ManualRelease.ps1'
if (-not (Test-Path -LiteralPath $releaseScript)) {
    throw "Publish-ManualRelease.ps1 not found under REPO_ROOT: $resolvedRepoRoot"
}

Write-Host "=== Visa2026 on-prem manual release ===" -ForegroundColor Cyan
Write-Host "REPO_ROOT             = $resolvedRepoRoot"
Write-Host "MANUAL_MEDIA_BASE_URL = $mediaBaseUrl"
Write-Host "MANUAL_MEDIA_ROOT     = $mediaRoot"
Write-Host "MANUAL_SITE_ROOT      = $siteRoot"

$releaseArgs = @{
    ManualMediaBaseUrl = $mediaBaseUrl
    MediaTargetRoot    = $mediaRoot
    SiteTargetDir      = $siteRoot
}
if ($Record) {
    $releaseArgs['Record'] = $true
    $releaseArgs['E2ETarget'] = $E2ETarget
    if ($E2EBaseUrl) { $releaseArgs['E2EBaseUrl'] = $E2EBaseUrl }
}
if ($SkipBuild) { $releaseArgs['SkipBuild'] = $true }
if ($CleanSite) { $releaseArgs['CleanSite'] = $true }
if ($filter) { $releaseArgs['RecordFilter'] = $filter }

Push-Location $resolvedRepoRoot
try {
    & $releaseScript @releaseArgs
}
finally {
    Pop-Location
}

Write-Host ''
Write-Host 'On-prem manual release complete.' -ForegroundColor Green
Write-Host "  Open: $($mediaBaseUrl -replace '/manual-media$','')/manual/"
