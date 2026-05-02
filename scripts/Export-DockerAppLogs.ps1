# Dumps recent Docker Compose service logs into agent-local/ so Cursor can read the file from the workspace.
# Run from anywhere:  pwsh -File scripts/Export-DockerAppLogs.ps1
# Then in chat ask the agent to read agent-local/docker-app.log (or @-mention the file).
param(
    [string] $ComposeFile = "docker-compose.prod.yml",
    [string] $Service = "app",
    [int] $Tail = 1000,
    [string] $OutFile = "agent-local/docker-app.log"
)
$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$composePath = Join-Path $repoRoot $ComposeFile
if (-not (Test-Path $composePath)) {
    Write-Error "Compose file not found: $composePath"
}
Set-Location $repoRoot
New-Item -ItemType Directory -Path (Join-Path $repoRoot "agent-local") -Force | Out-Null
$outPath = Join-Path $repoRoot $OutFile
docker compose -f $ComposeFile logs --no-color --tail $Tail $Service 2>&1 | Out-File -FilePath $outPath -Encoding utf8
Write-Host "Wrote $outPath (tail $Tail lines)."
