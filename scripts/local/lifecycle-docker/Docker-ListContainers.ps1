#Requires -Version 5.1
<#
.SYNOPSIS
  [LOCAL WORKSTATION] List Docker containers (friendly table).

.EXAMPLE
  .\scripts\local\lifecycle-docker\Docker-ListContainers.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

docker ps -a --format "table {{.Names}}\t{{.Status}}\t{{.Image}}"
if ($LASTEXITCODE -ne 0) {
    throw "docker ps failed (exit $LASTEXITCODE)."
}

