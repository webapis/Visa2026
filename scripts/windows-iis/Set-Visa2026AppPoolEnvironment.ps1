#Requires -Version 5.1
#Requires -RunAsAdministrator
param(
    [string]$AppPoolName = "Visa2026",
    [string]$EnvJsonPath = "C:\inetpub\visa2026\iis-apppool-env.json"
)

$ErrorActionPreference = "Stop"
$appcmd = Join-Path $env:Windir "System32\inetsrv\appcmd.exe"

if (-not (Test-Path -LiteralPath $EnvJsonPath)) {
    throw "Missing $EnvJsonPath - run Configure-Visa2026Production.ps1 first."
}

$poolCheck = & $appcmd list apppool $AppPoolName 2>&1
if ($LASTEXITCODE -ne 0) {
    throw "App pool not found: $AppPoolName"
}

$vars = Get-Content -Raw -LiteralPath $EnvJsonPath | ConvertFrom-Json

# Remove existing environment variables for this pool
& $appcmd list config -section:system.applicationHost/applicationPools /"[name='$AppPoolName'].environmentVariables.[name='ASPNETCORE_ENVIRONMENT']" 2>$null | Out-Null
foreach ($prop in $vars.PSObject.Properties) {
    $name = $prop.Name
    $value = [string]$prop.Value
    & $appcmd set config -section:system.applicationHost/applicationPools `
        /-"[name='$AppPoolName'].environmentVariables.[name='$name']" 2>$null | Out-Null
    & $appcmd set config -section:system.applicationHost/applicationPools `
        /+"[name='$AppPoolName'].environmentVariables.[name='$name',value='$value']"
    if ($LASTEXITCODE -ne 0) {
        throw "appcmd failed setting $name (exit $LASTEXITCODE)"
    }
}

Write-Host "App pool $AppPoolName environment variables set (appcmd)." -ForegroundColor Green
