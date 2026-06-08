#Requires -Version 5.1
<#
.SYNOPSIS
  [LOCAL] Fresh build, dedicated Blazor host (:5052), UiScenarioRunner, then stop host.

.DESCRIPTION
  Agent-friendly scenario run lifecycle (see .cursor/skills/visa2026-ui-scenarios/reference-run-lifecycle.md):
  stop port -> build Blazor.Server to isolated folder -> start on :5052 (Visa2026 LocalDB)
  -> run UiScenarioRunner with step screenshots -> stop host.

.PARAMETER Scenario
  Scenario id (YAML in tools/UiScenarioRunner/scenarios/<id>.yaml). Required unless -StopOnly.

.PARAMETER Port
  HTTP port (default: 5052). Must match launch profile Visa2026 - UI Scenarios (LocalDB).

.PARAMETER BuildOut
  Isolated Blazor build output (default: _scenario_build_out at repo root).

.PARAMETER ScreenshotRoot
  Parent folder for run screenshots (default: tools/UiScenarioRunner/screenshots).

.PARAMETER TimeoutMs
  Playwright per-action timeout (default: 90000).

.PARAMETER SlowMo
  Playwright slow-mo ms (default: 1000 when -Headed, else 0).

.PARAMETER SkipBuild
  Use existing _scenario_build_out DLL.

.PARAMETER SkipServer
  Do not start/stop host; only run UiScenarioRunner against -BaseUrl.

.PARAMETER KeepServer
  Leave dedicated host running after the run (debug only).

.PARAMETER StopOnly
  Stop scenario host (ui-scenario.pid + port) and exit.

.PARAMETER NoScreenshots
  Do not pass --screenshot-dir to the runner.

.PARAMETER Headed
  Show browser window (recommended for local review).

.EXAMPLE
  .\scripts\local\Invoke-UiScenarioRun.ps1 -Scenario person-employee-create -Headed

.EXAMPLE
  .\scripts\local\Invoke-UiScenarioRun.ps1 -StopOnly
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$Scenario = '',

    [int]$Port = 5052,

    [string]$BuildOut = '_scenario_build_out',

    [string]$ScreenshotRoot = 'tools/UiScenarioRunner/screenshots',

    [int]$TimeoutMs = 90000,

    [int]$SlowMo = -1,

    [switch]$SkipBuild,

    [switch]$SkipServer,

    [string]$BaseUrl = '',

    [switch]$KeepServer,

    [switch]$StopOnly,

    [switch]$NoScreenshots,

    [switch]$Headed,

    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'

function Stop-ScenarioServer {
    param(
        [string]$BuildOutDir,
        [int]$PortNumber,
        [switch]$Quiet
    )

    $pidFile = Join-Path $BuildOutDir 'ui-scenario.pid'
    if (Test-Path -LiteralPath $pidFile) {
        $processId = (Get-Content -LiteralPath $pidFile -Raw).Trim()
        Remove-Item -LiteralPath $pidFile -Force -ErrorAction SilentlyContinue
        if ($processId -match '^\d+$') {
            Stop-Process -Id ([int]$processId) -Force -ErrorAction SilentlyContinue
            if (-not $Quiet) {
                Write-Host "Stopped scenario server (PID $processId)." -ForegroundColor DarkGray
            }
        }
    }

    $conn = Get-NetTCPConnection -LocalPort $PortNumber -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($conn) {
        Stop-Process -Id $conn.OwningProcess -Force -ErrorAction SilentlyContinue
        if (-not $Quiet) {
            Write-Host "Stopped process on port $PortNumber (PID $($conn.OwningProcess))." -ForegroundColor DarkGray
        }
        Start-Sleep -Seconds 2
    }
}

function Wait-AppReady {
    param(
        [string]$Url,
        [int]$MaxAttempts = 60,
        [int]$SleepSeconds = 2
    )

    for ($i = 0; $i -lt $MaxAttempts; $i++) {
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 400) {
                Write-Host "App ready after $($i * $SleepSeconds)s - $Url" -ForegroundColor Green
                return $true
            }
        } catch {
            Write-Host "Waiting for app... $($i * $SleepSeconds)s" -ForegroundColor DarkGray
        }
        Start-Sleep -Seconds $SleepSeconds
    }

    return $false
}

function Write-ScenarioServerLogs {
    param([string]$BuildOutDir)

    foreach ($name in @('ui-scenario-out.log', 'ui-scenario-err.log')) {
        $path = Join-Path $BuildOutDir $name
        if (Test-Path -LiteralPath $path) {
            Write-Host "--- $name ---" -ForegroundColor Yellow
            Get-Content -LiteralPath $path -Tail 60 -ErrorAction SilentlyContinue
        }
    }
}

$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $RepoRoot

$BuildOutDir = if ([System.IO.Path]::IsPathRooted($BuildOut)) { $BuildOut } else { Join-Path $RepoRoot $BuildOut }

if ($StopOnly) {
    Stop-ScenarioServer -BuildOutDir $BuildOutDir -PortNumber $Port
    exit 0
}

if ([string]::IsNullOrWhiteSpace($Scenario)) {
    throw '-Scenario is required (unless -StopOnly).'
}

if ($BaseUrl -eq '') {
    $BaseUrl = "http://localhost:$Port"
}

if ($SlowMo -lt 0) {
    $SlowMo = if ($Headed) { 1000 } else { 0 }
}

$scenarioYaml = Join-Path $RepoRoot "tools/UiScenarioRunner/scenarios/$Scenario.yaml"
if (-not (Test-Path -LiteralPath $scenarioYaml)) {
    throw "Scenario not found: $scenarioYaml"
}

$runStamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$screenshotDir = Join-Path $RepoRoot $ScreenshotRoot
$screenshotDir = Join-Path $screenshotDir $Scenario
$screenshotDir = Join-Path $screenshotDir "run-$runStamp"

$serverStarted = $false
$exitCode = 0

try {
    if (-not $SkipServer) {
        Write-Host '==> Stopping any existing scenario host' -ForegroundColor Cyan
        Stop-ScenarioServer -BuildOutDir $BuildOutDir -PortNumber $Port -Quiet

        Write-Host '==> Ensuring LocalDB (MSSQLLocalDB) is running' -ForegroundColor Cyan
        sqllocaldb create MSSQLLocalDB 2>$null
        sqllocaldb start MSSQLLocalDB | Out-Null

        if (-not $SkipBuild) {
            Write-Host "==> Building Visa2026.Blazor.Server -> $BuildOutDir" -ForegroundColor Cyan
            New-Item -ItemType Directory -Force -Path $BuildOutDir | Out-Null
            dotnet build Visa2026.Blazor.Server/Visa2026.Blazor.Server.csproj `
                -c $Configuration `
                -o $BuildOutDir
            if ($LASTEXITCODE -ne 0) { throw "Blazor.Server build failed (exit $LASTEXITCODE)." }
        }

        $hostDll = Join-Path $BuildOutDir 'Visa2026.Blazor.Server.dll'
        if (-not (Test-Path -LiteralPath $hostDll)) {
            throw "Host DLL not found: $hostDll"
        }

        $connectionString = 'Server=(localdb)\mssqllocaldb;Database=Visa2026;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True'
        $outLog = Join-Path $BuildOutDir 'ui-scenario-out.log'
        $errLog = Join-Path $BuildOutDir 'ui-scenario-err.log'

        Remove-Item Env:FORCE_XAF_DB_UPDATE -ErrorAction SilentlyContinue
        $env:ASPNETCORE_ENVIRONMENT = 'Development'
        $env:ASPNETCORE_URLS = $BaseUrl
        $env:VISA2026_UI_SCENARIOS = 'true'
        $env:ConnectionStrings__DefaultConnection = $connectionString

        Write-Host "==> Starting scenario host on $BaseUrl (DB: Visa2026)" -ForegroundColor Cyan
        $proc = Start-Process -FilePath 'dotnet' -ArgumentList @($hostDll) `
            -WorkingDirectory $BuildOutDir -PassThru `
            -RedirectStandardOutput $outLog `
            -RedirectStandardError $errLog `
            -WindowStyle Hidden

        $proc.Id | Set-Content -LiteralPath (Join-Path $BuildOutDir 'ui-scenario.pid') -NoNewline
        $serverStarted = $true

        if (-not (Wait-AppReady -Url $BaseUrl)) {
            Write-ScenarioServerLogs -BuildOutDir $BuildOutDir
            throw "Scenario host did not become ready at $BaseUrl"
        }
    }

    Write-Host "==> Running UiScenarioRunner: $Scenario" -ForegroundColor Cyan
    $runnerArgs = @(
        'run', '--project', 'tools/UiScenarioRunner', '--',
        '--scenario', $Scenario,
        '--base-url', $BaseUrl,
        '--timeout', $TimeoutMs
    )
    if ($Headed) { $runnerArgs += '--headed' }
    if ($SlowMo -gt 0) { $runnerArgs += @('--slow-mo', $SlowMo) }
    if (-not $NoScreenshots) {
        $runnerArgs += @('--screenshot-dir', $screenshotDir, '--screenshot-steps', '--pause-after-save', '5000')
    }

    dotnet @runnerArgs
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0) {
        Write-Host "Scenario run failed (exit $exitCode)." -ForegroundColor Red
    } else {
        Write-Host "Scenario passed. Screenshots: $screenshotDir" -ForegroundColor Green
    }
}
finally {
    if ($serverStarted -and -not $KeepServer) {
        Write-Host '==> Stopping scenario host' -ForegroundColor Cyan
        Stop-ScenarioServer -BuildOutDir $BuildOutDir -PortNumber $Port
    } elseif ($serverStarted -and $KeepServer) {
        Write-Host "Scenario host left running on $BaseUrl (PID file: $(Join-Path $BuildOutDir 'ui-scenario.pid'))." -ForegroundColor Yellow
    }
}

exit $exitCode
