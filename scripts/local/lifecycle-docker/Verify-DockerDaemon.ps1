#Requires -Version 5.1
<#
.SYNOPSIS
  [LOCAL WORKSTATION] Verify the Docker engine daemon is reachable from this host.

.DESCRIPTION
  Fails fast with a clear message when Docker Desktop (or the engine) is not running,
  so later steps (image build, compose) do not fail with obscure npipe/API errors.

.EXAMPLE
  .\scripts\local\lifecycle-docker\Verify-DockerDaemon.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

docker info 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw @"
Docker daemon is not reachable (docker info exit $LASTEXITCODE).

Start Docker Desktop (or the Docker engine) on this host and wait until it is fully running, then retry.

Typical Windows message when the engine is down: cannot find dockerDesktopLinuxEngine named pipe.
"@
}

Write-Host "Docker daemon is reachable."
