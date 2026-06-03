#Requires -Version 5.1
<#
.SYNOPSIS
  [LOCAL] Run Visa2026 XAF database update without starting the web UI or logging in.

.DESCRIPTION
  Invokes Visa2026.Blazor.Server with --updateDatabase (schema + ModuleUpdaters).
  Does not open a browser and does not require an application user account.

  Stop debugging in Visual Studio first if build fails with "file is locked".

.PARAMETER Profile
  Connection preset: LocalDB (default), DockerDev (127.0.0.1 + .env.dev), or EasyTest.

.PARAMETER ConnectionString
  Full connection string (overrides -Profile).

.PARAMETER EnvFile
  For DockerDev: path to .env.dev (or .env.prod) for SA_PASSWORD and DB_NAME.

.PARAMETER ForceUpdate
  Pass --forceUpdate so all ModuleUpdaters run even when the DB version matches the app.

.PARAMETER Silent
  Pass --silent (no interactive backup prompt). Default: on.

.PARAMETER SkipBuild
  Use the built Visa2026.Blazor.Server.exe under bin\Debug or bin\Release instead of dotnet run.

.PARAMETER Configuration
  Build configuration when using dotnet run (default: Debug).

.EXAMPLE
  .\scripts\local\Update-LocalDatabase.ps1

.EXAMPLE
  .\scripts\local\Update-LocalDatabase.ps1 -Profile DockerDev -EnvFile .env.dev -ForceUpdate

.EXAMPLE
  .\scripts\local\Update-LocalDatabase.ps1 -ConnectionString "Server=(localdb)\mssqllocaldb;Database=Visa2026;Trusted_Connection=True;MultipleActiveResultSets=true"
#>
[CmdletBinding()]
param(
    [ValidateSet('LocalDB', 'DockerDev', 'EasyTest')]
    [string]$Profile = 'LocalDB',

    [string]$ConnectionString = '',

    [string]$EnvFile = '.env.dev',

    [switch]$ForceUpdate,

    [switch]$Silent = $true,

    [switch]$SkipBuild,

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

function Get-ProfileConnectionString {
    param(
        [string]$ProfileName,
        [string]$RepoRoot,
        [string]$EnvFilePath
    )

    switch ($ProfileName) {
        'LocalDB' {
            return 'Server=(localdb)\mssqllocaldb;Database=Visa2026;Trusted_Connection=True;MultipleActiveResultSets=true'
        }
        'EasyTest' {
            return 'Server=(localdb)\mssqllocaldb;Database=Visa2026EasyTest;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True'
        }
        'DockerDev' {
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
        default { throw "Unknown profile: $ProfileName" }
    }
}

$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $RepoRoot

if ($ConnectionString -eq '') {
    $ConnectionString = Get-ProfileConnectionString -ProfileName $Profile -RepoRoot $RepoRoot -EnvFilePath $EnvFile
}

$running = Get-Process -Name 'Visa2026.Blazor.Server' -ErrorAction SilentlyContinue
if ($running) {
    Write-Warning (
        'Visa2026.Blazor.Server is still running (Visual Studio debug?). Stop it (Shift+F5) before building, ' +
        'or pass -SkipBuild to use the existing exe.'
    )
}

$env:ConnectionStrings__DefaultConnection = $ConnectionString

$updateArgs = @('--updateDatabase')
if ($ForceUpdate) { $updateArgs += '--forceUpdate' }
if ($Silent) { $updateArgs += '--silent' }

Write-Host '==> Visa2026 database update (no login required)' -ForegroundColor Cyan
Write-Host "    Profile: $Profile" -ForegroundColor DarkGray
Write-Host "    Database: $($ConnectionString -replace 'Password=[^;]+', 'Password=***')" -ForegroundColor DarkGray
Write-Host "    Args: $($updateArgs -join ' ')" -ForegroundColor DarkGray

if ($SkipBuild) {
    $exe = Join-Path $RepoRoot "Visa2026.Blazor.Server\bin\$Configuration\net8.0\Visa2026.Blazor.Server.exe"
    if (-not (Test-Path -LiteralPath $exe)) {
        throw "Not found: $exe - build first or omit -SkipBuild."
    }
    Write-Host "==> $exe $($updateArgs -join ' ')" -ForegroundColor Cyan
    & $exe @updateArgs
} else {
    Write-Host '==> dotnet run --project Visa2026.Blazor.Server --no-launch-profile ...' -ForegroundColor Cyan
    dotnet run --project Visa2026.Blazor.Server --no-launch-profile -c $Configuration -- @updateArgs
}

$code = $LASTEXITCODE
switch ($code) {
    0 { Write-Host 'Database update completed.' -ForegroundColor Green }
    1 { Write-Host 'Database update failed (exit 1).' -ForegroundColor Red }
    2 { Write-Host 'Database update not needed (exit 2).' -ForegroundColor Yellow }
    default { Write-Host "Exit code: $code" -ForegroundColor Yellow }
}

exit $code
