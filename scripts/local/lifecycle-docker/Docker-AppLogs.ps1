#Requires -Version 5.1
<#
.SYNOPSIS
  [LOCAL WORKSTATION] Tail app container logs for a compose project.

.DESCRIPTION
  Resolves the current app container name for the given compose project by searching
  for containers named like: <project>-app-*

.PARAMETER ComposeProject
  Docker Compose project name (default: visa2026-local).

.PARAMETER Tail
  How many lines to show (default: 200).

.EXAMPLE
  .\scripts\local\lifecycle-docker\Docker-AppLogs.ps1

.EXAMPLE
  .\scripts\local\lifecycle-docker\Docker-AppLogs.ps1 -ComposeProject visa2026-prod -Tail 400
#>
[CmdletBinding()]
param(
    [string]$ComposeProject = "visa2026-local",
    [int]$Tail = 200
)

$ErrorActionPreference = "Stop"

$pattern = "$ComposeProject-app-"
$name = docker ps -a --format "{{.Names}}" | Where-Object { $_ -like "$pattern*" } | Select-Object -First 1

if ([string]::IsNullOrWhiteSpace($name)) {
    throw "Could not find an app container matching '$pattern*'. Run .\scripts\local\lifecycle-docker\Docker-ListContainers.ps1 and pass the correct -ComposeProject."
}

Write-Host "docker logs $name --tail $Tail" -ForegroundColor DarkGray
docker logs $name --tail $Tail
if ($LASTEXITCODE -ne 0) {
    throw "docker logs failed (exit $LASTEXITCODE)."
}

