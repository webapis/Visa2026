#Requires -Version 5.1
param(
    [ValidateSet("Production", "Staging", "Demo", "Legacy", "")]
    [string]$Profile = "",

    [string]$PublishPath = "",
    [switch]$ForceUpdate
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "Visa2026-IisSlots.ps1")

$ctx = Resolve-Visa2026IisSlotContext -Profile $Profile -PublishPath $PublishPath
$PublishPath = $ctx.PublishPath
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "DB update slot: $($ctx.Profile) -> $($ctx.DbName) ($PublishPath)" -ForegroundColor Cyan

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
