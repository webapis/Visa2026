#Requires -Version 5.1
<#
.SYNOPSIS
  Publish officer manual media (screenshots + videos) to on-prem static storage.

.DESCRIPTION
  Copies user-manual/assets/screenshots and user-manual/assets/videos to a target
  directory that nginx/IIS serves as MANUAL_MEDIA_BASE_URL (e.g. /manual-media/).

.PARAMETER SourceRoot
  Source assets root (default: repo user-manual/assets).

.PARAMETER TargetRoot
  Destination root (e.g. E:\visa2026-manual-media, /opt/visa2026/manual/media).

.EXAMPLE
  ./scripts/ci/Publish-ManualMedia.ps1 -TargetRoot \\10.100.128.25\visa2026-manual-media

.EXAMPLE
  ./scripts/ci/Publish-ManualMedia.ps1 -TargetRoot C:\deploy\manual\media
#>
[CmdletBinding()]
param(
    [string]$SourceRoot = '',
    [Parameter(Mandatory = $true)]
    [string]$TargetRoot
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
if ([string]::IsNullOrWhiteSpace($SourceRoot)) {
    $SourceRoot = Join-Path $repoRoot 'user-manual\assets'
}

$sourceRoot = (Resolve-Path -LiteralPath $SourceRoot).Path
$targetRoot = $TargetRoot.TrimEnd('\', '/')

foreach ($subdir in @('screenshots', 'videos')) {
    $source = Join-Path $sourceRoot $subdir
    if (-not (Test-Path -LiteralPath $source)) {
        Write-Warning "Missing source folder (skipped): $source"
        continue
    }

    $dest = Join-Path $targetRoot $subdir
    New-Item -ItemType Directory -Force -Path $dest | Out-Null
    Write-Host "Publishing $subdir -> $dest"
    robocopy $source $dest /E /XO /NFL /NDL /NJH /NJS | Out-Null
    if ($LASTEXITCODE -ge 8) {
        throw "robocopy failed for $subdir (exit $LASTEXITCODE)"
    }
}

Write-Host "Manual media published to $targetRoot"
