#Requires -Version 5.1
[CmdletBinding()]
<#
.SYNOPSIS
  Build Visa2026 without fighting Visual Studio F5 bin/obj locks (or wedged Roslyn).

.DESCRIPTION
  Sets VISA2026_ISOLATED_BUILD=1 (Directory.Build.props: analyzers off, no shared Roslyn,
  short copy retries). Writes under %LOCALAPPDATA%\Visa2026\agent-builds\ so F5 can keep
  the host running. -BlazorOnly skips Module compile and seeds Module.dll from repo Debug
  into the artifacts folder when needed.

.EXAMPLE
  ./scripts/local/Build-Isolated.ps1 -BlazorOnly

.EXAMPLE
  ./scripts/local/Build-Isolated.ps1 -Project Visa2026.slnx -SessionId myfix
#>
param(
    [string]$Project = '',
    [string]$Configuration = 'Debug',
    [string]$SessionId = 'dev',
    [switch]$BlazorOnly,
    [switch]$SkipStopHung,
    [switch]$NoRestore
)

$ErrorActionPreference = 'Stop'
$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $RepoRoot

if (-not $SkipStopHung) {
    & (Join-Path $PSScriptRoot 'Stop-HungBuilds.ps1')
}

$env:VISA2026_ISOLATED_BUILD = '1'
$env:MSBUILDDISABLENODEREUSE = '1'
$env:DOTNET_CLI_USE_MSBUILD_SERVER = '0'

$artifacts = Join-Path $env:LOCALAPPDATA "Visa2026\agent-builds\$SessionId\"
if (-not $artifacts.EndsWith('\')) { $artifacts += '\' }
New-Item -ItemType Directory -Force -Path $artifacts | Out-Null
Write-Host "ARTIFACTS=$artifacts"

if ([string]::IsNullOrWhiteSpace($Project)) {
    $Project = if ($BlazorOnly) {
        'Visa2026.Blazor.Server\Visa2026.Blazor.Server.csproj'
    } else {
        'Visa2026.slnx'
    }
}

$configFolder = $Configuration.ToLowerInvariant()
$args = @(
    'build', $Project,
    '-c', $Configuration,
    '--artifacts-path', $artifacts,
    '-p:VISA2026_ISOLATED_BUILD=1',
    '-v', 'minimal'
)
if ($NoRestore) { $args += '--no-restore' }

if ($BlazorOnly) {
    $repoMod = Join-Path $RepoRoot "Visa2026.Module\bin\$Configuration\net8.0\Visa2026.Module.dll"
    $artModBin = Join-Path $artifacts "bin\Visa2026.Module\$configFolder"
    $artModRef = Join-Path $artifacts "obj\Visa2026.Module\$configFolder\ref"
    $artModDll = Join-Path $artModBin 'Visa2026.Module.dll'
    if (-not (Test-Path $artModDll)) {
        if (-not (Test-Path $repoMod)) {
            throw "BlazorOnly needs Module at $repoMod (or a prior isolated Module build in artifacts). Run without -BlazorOnly once, or F5 first."
        }
        New-Item -ItemType Directory -Force -Path $artModBin, $artModRef | Out-Null
        Copy-Item $repoMod $artModDll -Force
        Copy-Item $repoMod (Join-Path $artModRef 'Visa2026.Module.dll') -Force
        $pdb = [System.IO.Path]::ChangeExtension($repoMod, '.pdb')
        if (Test-Path $pdb) {
            Copy-Item $pdb (Join-Path $artModBin 'Visa2026.Module.pdb') -Force
            Copy-Item $pdb (Join-Path $artModRef 'Visa2026.Module.pdb') -Force
        }
        Write-Host "Seeded Module from repo into artifacts for BlazorOnly."
    }
    $args += '-p:BuildProjectReferences=false'
}

Write-Host "dotnet $($args -join ' ')"
& dotnet @args
exit $LASTEXITCODE
