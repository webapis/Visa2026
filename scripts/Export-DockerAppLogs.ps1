# Forwarder — use scripts/local/Export-DockerAppLogs.ps1 (see scripts/README.md).
$ErrorActionPreference = "Stop"
& "$PSScriptRoot\local\Export-DockerAppLogs.ps1" @args
