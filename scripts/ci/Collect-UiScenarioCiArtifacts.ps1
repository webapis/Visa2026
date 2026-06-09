#Requires -Version 5.1
<#
.SYNOPSIS
  Bundle Blazor host logs and CI diagnostics for GitHub Actions artifact upload.

.DESCRIPTION
  Called from .github/workflows/ui-scenario-tests.yml after the host stops (or fails).
  Copies host stdout/stderr, wait-loop diagnostics, and environment snapshots into
  artifacts/ui-scenario-ci/ for download and offline triage.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$HostDir,

    [Parameter(Mandatory = $true)]
    [string]$ArtifactDir,

    [string]$BaseUrl = 'http://localhost:5000',

    [int]$BlazorPid = 0,

    [string]$Phase = 'unknown',

    [int]$Port = 5000
)

$ErrorActionPreference = 'Continue'

function Write-DiagnosticLine {
    param([string]$Path, [string]$Line)
    $timestamp = (Get-Date).ToString('o')
    "[$timestamp] $Line" | Add-Content -LiteralPath $Path -Encoding utf8
}

function Copy-IfExists {
    param([string]$Source, [string]$Destination)
    if (Test-Path -LiteralPath $Source) {
        Copy-Item -LiteralPath $Source -Destination $Destination -Force
        return $true
    }
    return $false
}

New-Item -ItemType Directory -Force -Path $ArtifactDir | Out-Null

$manifestPath = Join-Path $ArtifactDir 'manifest.txt'
Write-DiagnosticLine $manifestPath "phase=$Phase"
Write-DiagnosticLine $manifestPath "hostDir=$HostDir"
Write-DiagnosticLine $manifestPath "baseUrl=$BaseUrl"
Write-DiagnosticLine $manifestPath "blazorPid=$BlazorPid"
Write-DiagnosticLine $manifestPath "runnerOs=$env:RUNNER_OS"
Write-DiagnosticLine $manifestPath "githubRunId=$env:GITHUB_RUN_ID"
Write-DiagnosticLine $manifestPath "githubRunAttempt=$env:GITHUB_RUN_ATTEMPT"
Write-DiagnosticLine $manifestPath "githubSha=$env:GITHUB_SHA"
Write-DiagnosticLine $manifestPath "githubRef=$env:GITHUB_REF"

$hostLogNames = @(
    'ui-scenario-out.log',
    'ui-scenario-err.log',
    'blazor-out.log',
    'blazor-err.log'
)

$copiedLogs = @()
foreach ($name in $hostLogNames) {
    $source = Join-Path $HostDir $name
    $dest = Join-Path $ArtifactDir $name
    if (Copy-IfExists -Source $source -Destination $dest) {
        $copiedLogs += $name
    }
}

$repoRoot = Get-Location
$repoLogNames = @('blazor-out.log', 'blazor-err.log', 'wait-diagnostics.log', 'scenario-runner.log')
foreach ($name in $repoLogNames) {
    $source = Join-Path $repoRoot $name
    if ($copiedLogs -notcontains $name -and (Copy-IfExists -Source $source -Destination (Join-Path $ArtifactDir $name))) {
        $copiedLogs += $name
    }
}

$waitDiag = Join-Path $ArtifactDir 'wait-diagnostics.log'
if (-not (Test-Path -LiteralPath $waitDiag)) {
    New-Item -ItemType File -Path $waitDiag -Force | Out-Null
}

$systemPath = Join-Path $ArtifactDir 'system-diagnostics.txt'
Write-DiagnosticLine $systemPath '--- environment (secrets redacted) ---'
foreach ($key in @(
        'ASPNETCORE_ENVIRONMENT',
        'DOTNET_ENVIRONMENT',
        'ASPNETCORE_URLS',
        'VISA2026_UI_SCENARIOS',
        'UI_SCENARIO_BASE_URL',
        'UI_SCENARIO_HOST_DIR'
    )) {
    $value = [Environment]::GetEnvironmentVariable($key)
    if ($value) {
        Write-DiagnosticLine $systemPath "$key=$value"
    }
}

$conn = [Environment]::GetEnvironmentVariable('UI_SCENARIO_CONNECTION')
if ($conn) {
    $redacted = $conn -replace '(?i)(Password|Pwd)=([^;]+)', '$1=***'
    Write-DiagnosticLine $systemPath "UI_SCENARIO_CONNECTION=$redacted"
}

Write-DiagnosticLine $systemPath '--- host directory ---'
if (Test-Path -LiteralPath $HostDir) {
    Get-ChildItem -LiteralPath $HostDir -File |
        Sort-Object Name |
        ForEach-Object { Write-DiagnosticLine $systemPath "$($_.Name) $($_.Length) bytes" }
} else {
    Write-DiagnosticLine $systemPath "HostDir missing: $HostDir"
}

Write-DiagnosticLine $systemPath '--- port listeners ---'
try {
    $listeners = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue |
        Select-Object LocalAddress, LocalPort, State, OwningProcess -Unique
    if ($listeners) {
        foreach ($listener in $listeners) {
            $procName = (Get-Process -Id $listener.OwningProcess -ErrorAction SilentlyContinue).ProcessName
            Write-DiagnosticLine $systemPath "port $Port $($listener.State) pid=$($listener.OwningProcess) name=$procName"
        }
    } else {
        Write-DiagnosticLine $systemPath "No listener on port $Port"
    }
} catch {
    Write-DiagnosticLine $systemPath "Port check failed: $($_.Exception.Message)"
}

Write-DiagnosticLine $systemPath '--- dotnet / blazor processes ---'
Get-Process -Name 'dotnet', 'Visa2026.Blazor.Server' -ErrorAction SilentlyContinue |
    Select-Object Id, ProcessName, StartTime |
    ForEach-Object {
        Write-DiagnosticLine $systemPath "pid=$($_.Id) name=$($_.ProcessName) started=$($_.StartTime)"
    }

if ($BlazorPid -gt 0) {
    $tracked = Get-Process -Id $BlazorPid -ErrorAction SilentlyContinue
    if ($tracked) {
        Write-DiagnosticLine $systemPath "Tracked Blazor PID $BlazorPid is still running ($($tracked.ProcessName))"
    } else {
        Write-DiagnosticLine $systemPath "Tracked Blazor PID $BlazorPid is not running"
    }
}

Write-DiagnosticLine $systemPath '--- HTTP probe ---'
try {
    $response = Invoke-WebRequest -Uri "$BaseUrl/LoginPage" -UseBasicParsing -TimeoutSec 15
    Write-DiagnosticLine $systemPath "GET $BaseUrl/LoginPage -> $($response.StatusCode)"
} catch {
    Write-DiagnosticLine $systemPath "GET $BaseUrl/LoginPage failed: $($_.Exception.Message)"
}

Write-DiagnosticLine $manifestPath "copiedLogs=$($copiedLogs -join ', ')"

$readme = @"
Visa2026 UI scenario CI diagnostics
===================================

Download this folder from the GitHub Actions run (artifact: ui-scenario-ci-logs).

Files:
  manifest.txt           - run metadata
  system-diagnostics.txt - port/process/HTTP snapshot at collection time
  wait-diagnostics.log   - wait-loop trace (each HTTP attempt)
  ui-scenario-out.log    - Blazor host stdout
  ui-scenario-err.log    - Blazor host stderr
  scenario-runner.log    - UiScenarioRunner output (if scenarios ran)

Share the whole zip with the agent for triage.
"@
Set-Content -LiteralPath (Join-Path $ArtifactDir 'README.txt') -Value $readme -Encoding utf8

Write-Host "Collected UI scenario CI artifacts in $ArtifactDir"
Get-ChildItem -LiteralPath $ArtifactDir -File | ForEach-Object {
    Write-Host "  $($_.Name) ($($_.Length) bytes)"
}
