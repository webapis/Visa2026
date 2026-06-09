#Requires -Version 5.1
<#
.SYNOPSIS
  Start Visa2026 Blazor host for UI scenario CI with durable log capture.

.DESCRIPTION
  Uses cmd.exe append redirect (1>> / 2>>) so host stdout/stderr land in files
  while the process runs. Optionally waits for /LoginPage in the same step.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$HostDir,

    [Parameter(Mandatory = $true)]
    [string]$HostDll,

    [string]$BaseUrl = 'http://localhost:5000',

    [string]$ConnectionString = '',

    [string]$OutLog = '',

    [string]$ErrLog = '',

    [switch]$WaitForLoginPage,

    [int]$MaxWaitAttempts = 120,

    [int]$WaitSleepSeconds = 3
)

$ErrorActionPreference = 'Stop'

if (-not $OutLog) { $OutLog = Join-Path $HostDir 'ui-scenario-out.log' }
if (-not $ErrLog) { $ErrLog = Join-Path $HostDir 'ui-scenario-err.log' }
Remove-Item -LiteralPath $OutLog, $ErrLog -ErrorAction SilentlyContinue
New-Item -ItemType File -Path $OutLog, $ErrLog -Force | Out-Null

$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
$env:VISA2026_UI_SCENARIOS = 'true'
$env:ASPNETCORE_URLS = $BaseUrl
if ($ConnectionString) {
    $env:ConnectionStrings__DefaultConnection = $ConnectionString
}
Remove-Item Env:FORCE_XAF_DB_UPDATE -ErrorAction SilentlyContinue

$cmdLine = "dotnet `"$HostDll`" 1>> `"$OutLog`" 2>> `"$ErrLog`""
$proc = Start-Process -FilePath 'cmd.exe' `
    -ArgumentList @('/c', $cmdLine) `
    -WorkingDirectory $HostDir `
    -PassThru `
    -WindowStyle Hidden

$pidLine = "BLAZOR_PID=$($proc.Id)"
Write-Host $pidLine
if ($env:GITHUB_ENV) {
    $pidLine | Out-File -FilePath $env:GITHUB_ENV -Append -Encoding utf8
}

Write-Host "Host logs (cmd redirect): $OutLog | $ErrLog"

function Write-LogFileToConsole([string]$Path, [string]$Label) {
    Write-Host "::group::$Label"
    if (Test-Path -LiteralPath $Path) {
        $size = (Get-Item -LiteralPath $Path).Length
        Write-Host "$Label ($size bytes)"
        Get-Content -LiteralPath $Path -ErrorAction SilentlyContinue | ForEach-Object { Write-Host $_ }
    } else {
        Write-Host "$Label missing: $Path"
    }
    Write-Host '::endgroup::'
}

if (-not $WaitForLoginPage) {
    return
}

$loginUrl = "$BaseUrl/LoginPage"
$ready = $false
$diagPath = Join-Path (Get-Location) 'wait-diagnostics.log'
Remove-Item -LiteralPath $diagPath -ErrorAction SilentlyContinue

function Write-WaitDiag([string]$Line) {
    $timestamp = (Get-Date).ToString('o')
    $entry = "[$timestamp] $Line"
    Write-Host $entry
    $entry | Add-Content -LiteralPath $diagPath -Encoding utf8
}

Write-WaitDiag "wait start url=$loginUrl pid=$($proc.Id)"

for ($i = 0; $i -lt $MaxWaitAttempts; $i++) {
    if ($proc.HasExited) {
        Write-WaitDiag "host exited code=$($proc.ExitCode) before LoginPage ready"
        break
    }

    try {
        $response = Invoke-WebRequest -Uri $loginUrl -UseBasicParsing -TimeoutSec 10
        if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 400) {
            $ready = $true
            Write-WaitDiag "LoginPage ready after $($i * $WaitSleepSeconds)s status=$($response.StatusCode)"
            break
        }
        Write-WaitDiag "unexpected status $($response.StatusCode) at $($i * $WaitSleepSeconds)s"
    } catch {
        Write-WaitDiag "attempt $($i + 1)/$MaxWaitAttempts elapsed=$($i * $WaitSleepSeconds)s error=$($_.Exception.Message)"
    }

    Start-Sleep -Seconds $WaitSleepSeconds
}

if (-not $ready) {
    Start-Sleep -Seconds 1
    Write-LogFileToConsole $OutLog 'ui-scenario-out.log'
    Write-LogFileToConsole $ErrLog 'ui-scenario-err.log'
    throw "Blazor Server did not become ready on $loginUrl"
}
