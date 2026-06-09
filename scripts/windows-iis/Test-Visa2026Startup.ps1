#Requires -Version 5.1
<#
.SYNOPSIS
  Run Visa2026.Blazor.Server.exe once on the server and print startup errors (IIS triage).
#>
param(
    [ValidateSet("Production", "Staging", "Demo", "Legacy", "")]
    [string]$Profile = "",

    [string]$PublishPath = "",
    [int]$TimeoutSeconds = 25
)

$ErrorActionPreference = "Continue"
. (Join-Path $PSScriptRoot "Visa2026-IisSlots.ps1")
$ctx = Resolve-Visa2026IisSlotContext -Profile $Profile -PublishPath $PublishPath
$PublishPath = $ctx.PublishPath
$envJson = Join-Path $PublishPath "iis-apppool-env.json"
$appSettings = Join-Path $PublishPath "appsettings.Production.json"
$exe = Join-Path $PublishPath "Visa2026.Blazor.Server.exe"

if (-not (Test-Path -LiteralPath $exe)) { throw "Missing $exe" }

$e = Get-Content -LiteralPath $envJson | ConvertFrom-Json
$cfg = Get-Content -LiteralPath $appSettings | ConvertFrom-Json
$env:ASPNETCORE_ENVIRONMENT = $e.ASPNETCORE_ENVIRONMENT
$env:DEVEXPRESS_LICENSEKEY = $e.DEVEXPRESS_LICENSEKEY
$env:ASPNETCORE_DATA_PROTECTION_KEYS = $e.ASPNETCORE_DATA_PROTECTION_KEYS
$env:ConnectionStrings__DefaultConnection = $cfg.ConnectionStrings.DefaultConnection

Push-Location $PublishPath
try {
    $job = Start-Job -ScriptBlock {
        param($Path)
        Set-Location $Path
        & (Join-Path $Path "Visa2026.Blazor.Server.exe") 2>&1
    } -ArgumentList $PublishPath

    Wait-Job $job -Timeout $TimeoutSeconds | Out-Null
    Receive-Job $job | Select-Object -First 60
    Stop-Job $job -ErrorAction SilentlyContinue
    Remove-Job $job -Force -ErrorAction SilentlyContinue
}
finally {
    Pop-Location
}
