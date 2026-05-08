#Requires -Version 5.1
<#
.SYNOPSIS
  Backward-compatible wrapper. Use droplet-scripts/prod-deploy/Test-DropletProdHealth.ps1 instead.
#>
[CmdletBinding()]
param(
    [ValidateSet("prod", "dev")]
    [string]$Environment = "prod",
    [string]$IdentityFile,
    [string]$CurlPath = "/",
    [int[]]$ExpectedHttpCodes = @(200, 302, 303, 307, 308, 401, 403),
    [int]$Tail = 200
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$delegate = Join-Path $scriptDir "prod-deploy\Test-DropletProdHealth.ps1"

& $delegate @PSBoundParameters
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

