#Requires -Version 5.1
<#
.SYNOPSIS
  [LOCAL WORKSTATION] Confirm Visa2026.Module AssemblyVersion in repo matches the DLL in the running app container.

.DESCRIPTION
  Reads <AssemblyVersion> from Visa2026.Module/Visa2026.Module.csproj, copies /app/Visa2026.Module.dll
  from the compose app container, and compares assembly metadata. Use after Build-DockerImages +
  Compose-PullAndRecreateApp to ensure the container is running the build you think (not a stale :local
  layer or wrong registry tag).

.PARAMETER ComposeProject
  Docker Compose project name (default: visa2026-dev).

.EXAMPLE
  .\scripts\local\lifecycle-docker\Verify-AppModuleVersion.ps1

.EXAMPLE
  .\scripts\local\lifecycle-docker\Verify-AppModuleVersion.ps1 -ComposeProject visa2026-prod
#>
[CmdletBinding()]
param(
    [string]$ComposeProject = "visa2026-dev"
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
$csprojPath = Join-Path $RepoRoot "Visa2026.Module\Visa2026.Module.csproj"
if (-not (Test-Path -LiteralPath $csprojPath)) {
    throw "Visa2026.Module.csproj not found: $csprojPath"
}

$xml = [xml](Get-Content -Raw -LiteralPath $csprojPath)
$expectedRaw = $null
foreach ($pg in $xml.Project.PropertyGroup) {
    if ($pg.AssemblyVersion) {
        $expectedRaw = [string]$pg.AssemblyVersion
        break
    }
}
if ([string]::IsNullOrWhiteSpace($expectedRaw)) {
    throw "Could not read AssemblyVersion from $csprojPath"
}

$expectedVersion = [version]$expectedRaw

$pattern = "$ComposeProject-app-"
$containerName = docker ps --format "{{.Names}}" | Where-Object { $_ -like "$pattern*" } | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($containerName)) {
    throw "No running app container matching '$pattern*'. Start the stack or pass -ComposeProject. See Docker-ListContainers.ps1."
}

$tempDll = Join-Path ([System.IO.Path]::GetTempPath()) ("visa2026-module-verify-" + [Guid]::NewGuid().ToString("N") + ".dll")
try {
    docker cp "${containerName}:/app/Visa2026.Module.dll" $tempDll
    if ($LASTEXITCODE -ne 0) {
        throw "docker cp failed (exit $LASTEXITCODE)."
    }
    $asmName = [System.Reflection.AssemblyName]::GetAssemblyName($tempDll)
    $actualVersion = $asmName.Version
}
finally {
    if (Test-Path -LiteralPath $tempDll) {
        Remove-Item -LiteralPath $tempDll -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "Container: $containerName" -ForegroundColor Cyan
Write-Host "Repo Visa2026.Module AssemblyVersion (csproj): $expectedVersion" -ForegroundColor Gray
Write-Host "Running container Visa2026.Module.dll:          $actualVersion" -ForegroundColor Gray

if ($actualVersion -eq $expectedVersion) {
    Write-Host "OK: versions match." -ForegroundColor Green
    exit 0
}

Write-Host "MISMATCH: rebuild images and recreate the app container, or fix APP_IMAGE_TAG / stale image." -ForegroundColor Red
exit 1
