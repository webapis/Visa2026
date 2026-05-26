#Requires -Version 5.1
<#
.SYNOPSIS
  Run Visa2026 XAF database update on an IIS publish folder (no Docker).

.DESCRIPTION
  Invokes Visa2026.Blazor.Server.exe --updateDatabase. Use on the server after
  deploy or when schema drift is reported. Reads optional env from the current
  session (ConnectionStrings__DefaultConnection, etc.) — same as IIS app pool.

.EXAMPLE
  cd C:\inetpub\visa2026
  $env:ConnectionStrings__DefaultConnection = "Server=.;Database=Visa2026DbProd;..."
  $env:DEVEXPRESS_LICENSEKEY = "..."
  .\Update-Visa2026Database.ps1 -PublishPath C:\inetpub\visa2026 -ForceUpdate

.NOTES
  Runbook: docs/ON_PREM_WINDOWS_IIS.md
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$PublishPath,
    [switch]$ForceUpdate,
    [switch]$Silent
)

$ErrorActionPreference = "Stop"

$exe = Join-Path $PublishPath "Visa2026.Blazor.Server.exe"
if (-not (Test-Path -LiteralPath $exe)) {
    throw "Not found: $exe - run Publish-Visa2026ForIis.ps1 first or check -PublishPath."
}

$args = @("--updateDatabase")
if ($ForceUpdate) { $args += "--forceUpdate" }
if ($Silent) { $args += "--silent" }

Write-Host "==> $exe $($args -join ' ')" -ForegroundColor Cyan
& $exe @args
$code = $LASTEXITCODE

switch ($code) {
    0 { Write-Host "DB update completed." -ForegroundColor Green }
    1 { Write-Host "DB update error (exit 1)." -ForegroundColor Red }
    2 { Write-Host "DB update not needed (exit 2)." -ForegroundColor Yellow }
    default { Write-Host "Exit code: $code" -ForegroundColor Yellow }
}

exit $code
