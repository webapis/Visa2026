#Requires -Version 5.1
<#
.SYNOPSIS
  Backwards-compatible shim. Use scripts/local/lifecycle-docker/Compose-PullAndRecreateApp.ps1
#>
[CmdletBinding()]
param(
    [string]$ComposeProject = "visa2026-local",
    [string]$ComposeFile = "docker-compose.prod.yml",
    [string]$EnvFile = ".env.local"
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$target = Join-Path $RepoRoot "scripts\local\lifecycle-docker\Compose-PullAndRecreateApp.ps1"
& $target @PSBoundParameters
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
