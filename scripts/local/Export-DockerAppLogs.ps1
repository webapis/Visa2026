# [LOCAL WORKSTATION] Dumps recent Docker Compose service logs into agent-local/ for IDE/AI review.
# Run: pwsh -File scripts/local/Export-DockerAppLogs.ps1
param(
    [string] $ComposeFile = "docker-compose.prod.yml",
    [string] $Service = "app",
    [int] $Tail = 1000,
    [string] $OutFile = "agent-local/docker-app.log"
)
$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$composePath = Join-Path $repoRoot $ComposeFile
if (-not (Test-Path $composePath)) {
    Write-Error "Compose file not found: $composePath"
}
Set-Location $repoRoot
New-Item -ItemType Directory -Path (Join-Path $repoRoot "agent-local") -Force | Out-Null
$outPath = Join-Path $repoRoot $OutFile
docker compose -f $ComposeFile logs --no-color --tail $Tail $Service 2>&1 | Out-File -FilePath $outPath -Encoding utf8
Write-Host "Wrote $outPath (tail $Tail lines)."
