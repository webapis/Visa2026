#Requires -Version 5.1
<#
.SYNOPSIS
  [LOCAL WORKSTATION] Lifecycle wrapper to recreate the app container (no rebuild).

.DESCRIPTION
  Delegates to scripts/local/Recreate-LocalApp.ps1 so the visa2026-lifecycle-docker
  skill can reference only scripts under scripts/local/lifecycle-docker/.
#>
[CmdletBinding()]
param(
    [string]$ComposeProject = "visa2026-dev",
    [string]$ComposeFile = "docker-compose.dev.yml",
    [string]$EnvFile = ".env.dev"
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
$delegate = Join-Path $RepoRoot "scripts\local\Recreate-LocalApp.ps1"

& $delegate @PSBoundParameters
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

