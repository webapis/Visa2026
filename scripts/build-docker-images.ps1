# Forwarder — use scripts/local/Build-DockerImages.ps1 (see scripts/README.md).
$ErrorActionPreference = "Stop"
& "$PSScriptRoot\local\Build-DockerImages.ps1" @args
