#Requires -Version 5.1
<#
.SYNOPSIS
  Build and publish a versioned officer manual release bundle (media + MkDocs site).

.DESCRIPTION
  Staging/build-agent workflow:
    1. (Optional) Record EasyTest screenshots/videos into user-manual/assets/
    2. Publish media to MANUAL_MEDIA_ROOT (on-prem static tree)
    3. Build MkDocs with MANUAL_MEDIA_BASE_URL baked in
    4. Publish site to MANUAL_SITE_ROOT

  Does not modify the Visa2026 app Docker image. Restart or recreate the manual
  nginx service after publish (compose volume mounts pick up files immediately).

.PARAMETER ManualMediaBaseUrl
  Public HTTPS base for media (e.g. https://10.100.128.25:8082/manual-media).
  Required unless MANUAL_MEDIA_BASE_URL is already set.

.PARAMETER MediaTargetRoot
  Filesystem path for media publish (MANUAL_MEDIA_ROOT). Default: deploy/manual/media.

.PARAMETER SiteTargetDir
  Filesystem path for built site (MANUAL_SITE_ROOT). Default: deploy/manual/site.

.PARAMETER Record
  Run Record-PlaywrightE2e.ps1 before publish (Playwright E2E + screenshots).

.PARAMETER E2ETarget
  Playwright E2E target when -Record: Local (default, :5050) or Staging (live URL).

.PARAMETER E2EBaseUrl
  Override staging URL (e.g. https://10.100.128.25:8080) when E2ETarget=Staging.

.PARAMETER RecordFilter
  Optional Playwright test name fragment (default derived from E2ETarget).

.PARAMETER SkipBuild
  Skip MkDocs build (publish existing user-manual/site only).

.PARAMETER SkipMediaPublish
  Skip media copy (site-only publish).

.PARAMETER CleanSite
  Remove target site directory before copy.

.EXAMPLE
  $base = 'https://10.100.128.25:8082/manual-media'
  ./scripts/ci/Publish-ManualRelease.ps1 -ManualMediaBaseUrl $base

.EXAMPLE
  ./scripts/ci/Publish-ManualRelease.ps1 -Record -ManualMediaBaseUrl https://staging.example/manual-media
#>
[CmdletBinding()]
param(
    [string]$ManualMediaBaseUrl = '',
    [string]$MediaTargetRoot = '',
    [string]$SiteTargetDir = '',
    [switch]$Record,
    [ValidateSet('Local', 'Staging')]
    [string]$E2ETarget = 'Local',
    [string]$E2EBaseUrl = '',
    [string]$RecordFilter = '',
    [switch]$SkipBuild,
    [switch]$SkipMediaPublish,
    [switch]$CleanSite
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path

if ([string]::IsNullOrWhiteSpace($ManualMediaBaseUrl)) {
    $ManualMediaBaseUrl = $env:MANUAL_MEDIA_BASE_URL
}
if ([string]::IsNullOrWhiteSpace($ManualMediaBaseUrl)) {
    throw 'Set -ManualMediaBaseUrl or MANUAL_MEDIA_BASE_URL (e.g. https://host:8082/manual-media).'
}
$ManualMediaBaseUrl = $ManualMediaBaseUrl.Trim().TrimEnd('/')

if ([string]::IsNullOrWhiteSpace($MediaTargetRoot)) {
    $MediaTargetRoot = Join-Path $repoRoot 'deploy\manual\media'
}
if ([string]::IsNullOrWhiteSpace($SiteTargetDir)) {
    $SiteTargetDir = Join-Path $repoRoot 'deploy\manual\site'
}

$recordScript = Join-Path $repoRoot 'scripts\local\Record-PlaywrightE2e.ps1'
$buildScript = Join-Path $PSScriptRoot 'Build-UserManual.ps1'
$publishMediaScript = Join-Path $PSScriptRoot 'Publish-ManualMedia.ps1'
$publishSiteScript = Join-Path $PSScriptRoot 'Publish-UserManualSite.ps1'

Write-Host "=== Manual release bundle ==="
Write-Host "MANUAL_MEDIA_BASE_URL = $ManualMediaBaseUrl"
Write-Host "Media target          = $MediaTargetRoot"
Write-Host "Site target           = $SiteTargetDir"

if ($Record) {
    if (-not (Test-Path -LiteralPath $recordScript)) {
        throw "Record script not found: $recordScript"
    }
    Write-Host "--- Recording Playwright E2E media (Target=$E2ETarget) ---"
    $recordArgs = @{ Target = $E2ETarget }
    if ($E2EBaseUrl) { $recordArgs['BaseUrl'] = $E2EBaseUrl }
    if ($RecordFilter) { $recordArgs['Filter'] = $RecordFilter }
    & $recordScript @recordArgs
}

if (-not $SkipMediaPublish) {
    Write-Host '--- Publishing media ---'
    & $publishMediaScript -TargetRoot $MediaTargetRoot
}

if (-not $SkipBuild) {
    Write-Host '--- Building manual site ---'
    & $buildScript -SkipE2E -ManualMediaBaseUrl $ManualMediaBaseUrl
}

Write-Host '--- Publishing manual site ---'
$siteArgs = @{
    TargetDir = $SiteTargetDir
}
if ($CleanSite) {
    $siteArgs['Clean'] = $true
}
& $publishSiteScript @siteArgs

Write-Host ''
Write-Host 'Manual release bundle published.'
Write-Host "  Media : $MediaTargetRoot"
Write-Host "  Site  : $SiteTargetDir"
Write-Host '  Verify: open manual index and confirm images/videos load from MANUAL_MEDIA_BASE_URL.'
Write-Host '  Docker: ensure MANUAL_SITE_ROOT / MANUAL_MEDIA_ROOT point at these paths, then recreate manual service if needed.'
