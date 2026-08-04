#Requires -Version 5.1
<#
.SYNOPSIS
  Publish built MkDocs officer manual site to on-prem static hosting.

.DESCRIPTION
  Copies user-manual/site/ to a target directory mounted by the manual nginx
  container (MANUAL_SITE_ROOT) or IIS virtual directory.

.PARAMETER SourceDir
  Built MkDocs output (default: user-manual/site).

.PARAMETER TargetDir
  Destination directory (e.g. /opt/visa2026/manual/site or .\deploy\manual\site).

.PARAMETER Clean
  Remove existing files under TargetDir before copy.

.EXAMPLE
  ./scripts/ci/Publish-UserManualSite.ps1 -TargetDir C:\deploy\manual\site

.EXAMPLE
  ./scripts/ci/Publish-UserManualSite.ps1 -TargetDir \\server\share\manual\site -Clean
#>
[CmdletBinding()]
param(
    [string]$SourceDir = '',
    [Parameter(Mandatory = $true)]
    [string]$TargetDir,
    [switch]$Clean
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
if ([string]::IsNullOrWhiteSpace($SourceDir)) {
    $SourceDir = Join-Path $repoRoot 'user-manual\site'
}

$sourceDir = (Resolve-Path -LiteralPath $SourceDir).Path
$targetDir = $TargetDir.TrimEnd('\', '/')

if (-not (Test-Path -LiteralPath (Join-Path $sourceDir 'index.html'))) {
    throw "Built manual site not found at $sourceDir. Run Build-UserManual.ps1 first."
}

New-Item -ItemType Directory -Force -Path $targetDir | Out-Null

if ($Clean -and (Test-Path -LiteralPath $targetDir)) {
    Write-Host "Cleaning $targetDir"
    Get-ChildItem -LiteralPath $targetDir -Force | Remove-Item -Recurse -Force
}

Write-Host "Publishing manual site $sourceDir -> $targetDir"
robocopy $sourceDir $targetDir /E /XO /NFL /NDL /NJH /NJS | Out-Null
if ($LASTEXITCODE -ge 8) {
    throw "robocopy failed (exit $LASTEXITCODE)"
}

Write-Host "Manual site published to $targetDir"
