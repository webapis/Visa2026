#Requires -Version 5.1
<#
.SYNOPSIS
  [LOCAL WORKSTATION] Lifecycle wrapper for building Visa2026 Docker images.

.DESCRIPTION
  Delegates to scripts/local/Build-DockerImages.ps1 so the visa2026-lifecycle-docker
  skill can reference only scripts under scripts/local/lifecycle-docker/.
#>
[CmdletBinding()]
param(
    [switch]$AppOnly,
    [switch]$ImporterOnly,
    [string]$ImagePrefix = "webapia",
    [switch]$DeployLocal,
    [string]$ComposeProject = "visa2026-local",
    [string]$ComposeFile = "docker-compose.prod.yml",
    [string]$EnvFile = ".env.local"
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
$delegate = Join-Path $RepoRoot "scripts\local\Build-DockerImages.ps1"

& $delegate @PSBoundParameters
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

