#Requires -Version 5.1
param(
    [string]$PublishPath = "C:\inetpub\visa2026",
    [switch]$ForceUpdate
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

$envJson = Join-Path $PublishPath "iis-apppool-env.json"
$appSettings = Join-Path $PublishPath "appsettings.Production.json"

$e = Get-Content -LiteralPath $envJson | ConvertFrom-Json
$cfg = Get-Content -LiteralPath $appSettings | ConvertFrom-Json

$env:ASPNETCORE_ENVIRONMENT = $e.ASPNETCORE_ENVIRONMENT
$env:DEVEXPRESS_LICENSEKEY = $e.DEVEXPRESS_LICENSEKEY
$env:ASPNETCORE_DATA_PROTECTION_KEYS = $e.ASPNETCORE_DATA_PROTECTION_KEYS
$env:ConnectionStrings__DefaultConnection = $cfg.ConnectionStrings.DefaultConnection

$params = @{ PublishPath = $PublishPath; Silent = $true }
if ($ForceUpdate) { $params.ForceUpdate = $true }

& (Join-Path $scriptDir "Update-Visa2026Database.ps1") @params
exit $LASTEXITCODE
