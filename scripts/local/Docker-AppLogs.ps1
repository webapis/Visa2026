#Requires -Version 5.1
<#
.SYNOPSIS
  Backwards-compatible shim. Use scripts/local/lifecycle-docker/Docker-AppLogs.ps1
#>
[CmdletBinding()]
param(
    [string]$ComposeProject = "visa2026-local",
    [int]$Tail = 200
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$target = Join-Path $RepoRoot "scripts\local\lifecycle-docker\Docker-AppLogs.ps1"
& $target @PSBoundParameters
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
