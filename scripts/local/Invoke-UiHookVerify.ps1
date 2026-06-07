#Requires -Version 5.1
<#
.SYNOPSIS
  [LOCAL] Build, start an isolated Blazor host, run VerifyUiTestHooks, then stop the host.

.DESCRIPTION
  Agent-friendly hook verification: does not use the developer's IDE host on :5000/:5001.
  Builds Visa2026.Blazor.Server to _agent_build_out (default), starts it on a dedicated port
  (default http://localhost:5051), waits for /LoginPage, runs tools/VerifyUiTestHooks, then
  stops the process.

  Uses LocalDB database Visa2026HookVerify by default (separate from dev Docker/LocalDB Visa2026).

.PARAMETER Scenario
  One or more hooks-manifest scenario ids (e.g. login, nav-people). Default: all scenarios.

.PARAMETER Port
  HTTP port for the isolated host (default: 5051).

.PARAMETER BuildOut
  Output folder for Blazor build (default: _agent_build_out at repo root).

.PARAMETER LaunchProfile
  launchSettings.json profile for LocalDB hook verify (default: Visa2026 - Hook Verify (LocalDB)).
  Port :5051 and database Visa2026HookVerify are defined there — keep in sync with -Port.

.PARAMETER Profile
  SQL source: LocalDB (uses -LaunchProfile) or DockerDev (manual env; reads .env.dev).

.PARAMETER Password
  Admin password for authenticated scenarios (default: empty - local XAF default).

.PARAMETER StartUrl
  Optional path for Person detail scenarios (e.g. /Person_DetailView_Employee/{guid}).

.PARAMETER SkipBuild
  Skip dotnet build; run existing _agent_build_out DLL.

.PARAMETER SkipServer
  Do not start/stop a host; only run VerifyUiTestHooks against -BaseUrl (advanced / manual server).

.PARAMETER KeepServer
  Leave the isolated host running after verify (for DevTools debugging).

.PARAMETER StopOnly
  Stop a previously started hook-verify server (reads hook-verify.pid) and exit.

.PARAMETER Headed
  Pass --headed to Playwright.

.EXAMPLE
  .\scripts\local\Invoke-UiHookVerify.ps1 -Scenario login

.EXAMPLE
  .\scripts\local\Invoke-UiHookVerify.ps1 -Scenario nav-people,login

.EXAMPLE
  .\scripts\local\Invoke-UiHookVerify.ps1 -Scenario person-scalar-fields `
    -StartUrl "/Person_DetailView_Employee/00000000-0000-0000-0000-000000000001"
#>
[CmdletBinding()]
param(
    [string[]]$Scenario = @(),

    [int]$Port = 5051,

    [string]$BuildOut = '_agent_build_out',

    [ValidateSet('LocalDB', 'DockerDev')]
    [string]$Profile = 'LocalDB',

    [string]$LaunchProfile = 'Visa2026 - Hook Verify (LocalDB)',

    [string]$EnvFile = '.env.dev',

    [string]$ConnectionString = '',

    [string]$Password = '',

    [string]$StartUrl = '',

    [switch]$SkipBuild,

    [switch]$SkipServer,

    [string]$BaseUrl = '',

    [switch]$KeepServer,

    [switch]$StopOnly,

    [switch]$Headed,

    [switch]$ForceDbUpdate,

    [int]$TimeoutMs = 60000,

    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'

function Read-DotEnvMap {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Env file not found: $Path"
    }
    $map = @{}
    Get-Content -LiteralPath $Path | ForEach-Object {
        $line = $_.Trim()
        if ($line -match '^\s*#' -or $line -eq '') { return }
        if ($line -match '^\s*([^#=]+)=(.*)$') {
            $k = $matches[1].Trim()
            $v = $matches[2].Trim()
            if ($v.Length -ge 2 -and $v.StartsWith('"') -and $v.EndsWith('"')) {
                $v = $v.Substring(1, $v.Length - 2)
            }
            $map[$k] = $v
        }
    }
    $map
}

function Get-HookVerifyConnectionString {
    param(
        [string]$ProfileName,
        [string]$RepoRoot,
        [string]$EnvFilePath
    )

    if ($ProfileName -eq 'DockerDev') {
        $envPath = if ([System.IO.Path]::IsPathRooted($EnvFilePath)) {
            $EnvFilePath
        } else {
            Join-Path $RepoRoot $EnvFilePath
        }
        $m = Read-DotEnvMap $envPath
        $db = if ($m.ContainsKey('DB_NAME')) { $m['DB_NAME'] } else { 'Visa2026DbDev' }
        $pwd = if ($m.ContainsKey('SA_PASSWORD')) { $m['SA_PASSWORD'] } else { 'CHANGE_ME_STRONG_PASSWORD' }
        $port = if ($m.ContainsKey('MSSQL_HOST_PORT')) { $m['MSSQL_HOST_PORT'] } else { '1433' }
        return "Server=127.0.0.1,$port;Database=$db;User Id=sa;Password=$pwd;TrustServerCertificate=True;MultipleActiveResultSets=true"
    }

    return 'Server=(localdb)\mssqllocaldb;Database=Visa2026HookVerify;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True'
}

function Stop-HookVerifyServer {
    param(
        [string]$BuildOutDir,
        [switch]$Quiet
    )

    $pidFile = Join-Path $BuildOutDir 'hook-verify.pid'
    if (-not (Test-Path -LiteralPath $pidFile)) {
        if (-not $Quiet) {
            Write-Host 'No hook-verify.pid - server may already be stopped.' -ForegroundColor DarkGray
        }
        return
    }

    $processId = (Get-Content -LiteralPath $pidFile -Raw).Trim()
    Remove-Item -LiteralPath $pidFile -Force -ErrorAction SilentlyContinue

    if ($processId -match '^\d+$') {
        Stop-Process -Id ([int]$processId) -Force -ErrorAction SilentlyContinue
        if (-not $Quiet) {
            Write-Host "Stopped hook-verify server (PID $processId)." -ForegroundColor DarkGray
        }
    }
}

function Wait-LoginPage {
    param(
        [string]$Url,
        [int]$MaxAttempts = 90,
        [int]$SleepSeconds = 2
    )

    for ($i = 0; $i -lt $MaxAttempts; $i++) {
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 400) {
                Write-Host "LoginPage ready after $($i * $SleepSeconds) seconds - $Url" -ForegroundColor Green
                return $true
            }
        } catch {
            $elapsedSec = $i * $SleepSeconds
            Write-Host "Waiting for app... ${elapsedSec}s" -ForegroundColor DarkGray
        }
        Start-Sleep -Seconds $SleepSeconds
    }

    return $false
}

function Write-BlazorLogs {
    param([string]$BuildOutDir)

    foreach ($name in @('hook-verify-out.log', 'hook-verify-err.log')) {
        $path = Join-Path $BuildOutDir $name
        if (Test-Path -LiteralPath $path) {
            Write-Host "--- $name ---" -ForegroundColor Yellow
            Get-Content -LiteralPath $path -Tail 80 -ErrorAction SilentlyContinue
        }
    }
}

$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $RepoRoot

$BuildOutDir = if ([System.IO.Path]::IsPathRooted($BuildOut)) {
    $BuildOut
} else {
    Join-Path $RepoRoot $BuildOut
}

if ($StopOnly) {
    Stop-HookVerifyServer -BuildOutDir $BuildOutDir
    exit 0
}

if ($BaseUrl -eq '') {
    $BaseUrl = "http://localhost:$Port"
}

$serverStarted = $false
$exitCode = 0

try {
    if (-not $SkipServer) {
        Stop-HookVerifyServer -BuildOutDir $BuildOutDir -Quiet

        if ($Profile -eq 'LocalDB') {
            Write-Host '==> Ensuring LocalDB (MSSQLLocalDB) is running' -ForegroundColor Cyan
            sqllocaldb create MSSQLLocalDB 2>$null
            sqllocaldb start MSSQLLocalDB | Out-Null
        }

        if ($ForceDbUpdate) {
            $env:FORCE_XAF_DB_UPDATE = 'true'
            Write-Host '==> FORCE_XAF_DB_UPDATE=true (first-time / schema refresh — slow)' -ForegroundColor Yellow
        } else {
            Remove-Item Env:FORCE_XAF_DB_UPDATE -ErrorAction SilentlyContinue
        }

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
            throw "Host DLL not found: $hostDll (build first or omit -SkipBuild)."
        }

        if ($Profile -eq 'LocalDB' -and $ConnectionString -eq '') {
            $ConnectionString = Get-HookVerifyConnectionString -ProfileName $Profile -RepoRoot $RepoRoot -EnvFilePath $EnvFile
        } elseif ($Profile -eq 'DockerDev') {
            if ($ConnectionString -eq '') {
                $ConnectionString = Get-HookVerifyConnectionString -ProfileName $Profile -RepoRoot $RepoRoot -EnvFilePath $EnvFile
            }
        }

        $outLog = Join-Path $BuildOutDir 'hook-verify-out.log'
        $errLog = Join-Path $BuildOutDir 'hook-verify-err.log'
        $projectPath = Join-Path $RepoRoot 'Visa2026.Blazor.Server/Visa2026.Blazor.Server.csproj'

        if ($Profile -eq 'LocalDB') {
            if ($Port -ne 5051) {
                Write-Warning "LocalDB hook verify uses launch profile port 5051; -Port $Port ignored for server start."
            }
            $BaseUrl = 'http://localhost:5051'

            Remove-Item Env:ASPNETCORE_URLS -ErrorAction SilentlyContinue
            $env:ASPNETCORE_ENVIRONMENT = 'Development'
            $env:ASPNETCORE_URLS = $BaseUrl
            if ($ForceDbUpdate) { $env:FORCE_XAF_DB_UPDATE = 'true' }
            # Do not rely on --launch-profile (spaces/parens break Start-Process argv); Development appsettings would otherwise point at Docker SQL.
            $env:ConnectionStrings__DefaultConnection = $ConnectionString

            Write-Host "==> Starting isolated Blazor host on $BaseUrl" -ForegroundColor Cyan
            Write-Host '    DB: LocalDB Visa2026HookVerify' -ForegroundColor DarkGray

            $proc = Start-Process -FilePath 'dotnet' -ArgumentList @(
                $hostDll
            ) -WorkingDirectory $BuildOutDir -PassThru `
                -RedirectStandardOutput $outLog `
                -RedirectStandardError $errLog `
                -WindowStyle Hidden
        } else {
            $env:ASPNETCORE_ENVIRONMENT = 'Development'
            $env:ASPNETCORE_URLS = $BaseUrl
            if ($ForceDbUpdate) { $env:FORCE_XAF_DB_UPDATE = 'true' }
            $env:ConnectionStrings__DefaultConnection = $ConnectionString

            Write-Host "==> Starting isolated Blazor host on $BaseUrl" -ForegroundColor Cyan
            Write-Host "    DB: $($ConnectionString -replace 'Password=[^;]+', 'Password=***')" -ForegroundColor DarkGray

            $hostDll = Join-Path $BuildOutDir 'Visa2026.Blazor.Server.dll'
            $proc = Start-Process -FilePath 'dotnet' -ArgumentList @(
                $hostDll
            ) -WorkingDirectory $BuildOutDir -PassThru `
                -RedirectStandardOutput $outLog `
                -RedirectStandardError $errLog `
                -WindowStyle Hidden
        }

        $proc.Id | Set-Content -LiteralPath (Join-Path $BuildOutDir 'hook-verify.pid') -NoNewline
        $serverStarted = $true

        $loginUrl = "$($BaseUrl.TrimEnd('/'))/LoginPage"
        if (-not (Wait-LoginPage -Url $loginUrl)) {
            Write-BlazorLogs -BuildOutDir $BuildOutDir
            throw "Blazor host did not become ready at $loginUrl within timeout."
        }
    }

    Write-Host '==> Building VerifyUiTestHooks' -ForegroundColor Cyan
    dotnet build tools/VerifyUiTestHooks/VerifyUiTestHooks.csproj -c Debug
    if ($LASTEXITCODE -ne 0) { throw "VerifyUiTestHooks build failed (exit $LASTEXITCODE)." }

    $playwrightScript = Join-Path $RepoRoot 'tools/VerifyUiTestHooks/bin/Debug/net8.0/playwright.ps1'
    if (-not (Test-Path -LiteralPath $playwrightScript)) {
        Write-Warning "Playwright script not found at $playwrightScript - run: dotnet build tools/VerifyUiTestHooks; then playwright.ps1 install chromium"
    }

    $verifyArgs = @(
        'run', '--project', 'tools/VerifyUiTestHooks/VerifyUiTestHooks.csproj',
        '--no-build', '-c', 'Debug', '--',
        '--base-url', $BaseUrl,
        '--timeout', $TimeoutMs.ToString()
    )

    if ($Password -ne '') {
        $verifyArgs += @('--password', $Password)
    }

    if ($StartUrl -ne '') {
        $verifyArgs += @('--start-url', $StartUrl)
    }

    if ($Headed) {
        $verifyArgs += '--headed'
    }

    if ($Scenario.Count -gt 0) {
        foreach ($id in $Scenario) {
            $verifyArgs += @('--scenario', $id)
        }
    }

    Write-Host "==> VerifyUiTestHooks ($BaseUrl)" -ForegroundColor Cyan
    & dotnet @verifyArgs
    $exitCode = $LASTEXITCODE
}
catch {
    Write-Host $_.Exception.Message -ForegroundColor Red
    if ($serverStarted) {
        Write-BlazorLogs -BuildOutDir $BuildOutDir
    }
    $exitCode = 1
}
finally {
    if ($serverStarted -and -not $KeepServer) {
        Stop-HookVerifyServer -BuildOutDir $BuildOutDir
    } elseif ($KeepServer -and $serverStarted) {
        Write-Host "Server left running at $BaseUrl (PID file: $(Join-Path $BuildOutDir 'hook-verify.pid'))." -ForegroundColor Yellow
        Write-Host "Stop later: .\scripts\local\Invoke-UiHookVerify.ps1 -BuildOut '$BuildOut' -StopOnly" -ForegroundColor DarkGray
    }
}

exit $exitCode
