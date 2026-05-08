#Requires -Version 5.1
<#
.SYNOPSIS
  Backward-compatible wrapper. Use droplet-scripts/prod-deploy/backup-prod.ps1 instead.
#>
[CmdletBinding()]
param(
    [ValidateSet("prod", "dev")]
    [string]$Environment = "prod",
    [string]$IdentityFile,
    [string]$DownloadTo = ""
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$delegate = Join-Path $scriptDir "prod-deploy\backup-prod.ps1"

& $delegate @PSBoundParameters
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

