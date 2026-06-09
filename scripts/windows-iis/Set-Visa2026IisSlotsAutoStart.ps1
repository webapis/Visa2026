#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Configure auto-start for all Visa2026 IIS slots; keep Default Web Site off LAN :80.

.NOTES
  Default Web Site moves to 127.0.0.1:8090 (staging uses *:8080).
#>
param(
    [string]$DefaultSiteName = "Default Web Site",
    [int]$DefaultSitePort = 8090
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "Visa2026-IisSlots.ps1")

$autoStartScript = Join-Path $PSScriptRoot "Set-Visa2026IisAutoStart.ps1"
if (-not (Test-Path -LiteralPath $autoStartScript)) {
    throw "Missing $autoStartScript"
}

$prod = Resolve-Visa2026IisSlotContext -Profile Production
Write-Host "==> SQL / W3SVC / Default Web Site (via Production slot)" -ForegroundColor Cyan
& $autoStartScript -SiteName $prod.SiteName -AppPoolName $prod.AppPoolName -DefaultSiteName $DefaultSiteName

$appcmd = "$env:windir\System32\inetsrv\appcmd.exe"
$defaultListed = & $appcmd list site /name:"$DefaultSiteName" 2>$null
if ($LASTEXITCODE -eq 0 -and $defaultListed) {
    & $appcmd set site "$DefaultSiteName" "/bindings:http/127.0.0.1:${DefaultSitePort}:" | Out-Null
    Write-Host "Default Web Site binding: 127.0.0.1:$DefaultSitePort (staging uses *:8080)." -ForegroundColor DarkGray
}

foreach ($name in Get-Visa2026IisSlotProfiles) {
    if ($name -eq "Production") { continue }
    $ctx = Resolve-Visa2026IisSlotContext -Profile $name
    Write-Host "==> Auto-start $($ctx.Profile) ($($ctx.SiteName) :$($ctx.HttpPort))" -ForegroundColor Cyan
    & $appcmd set apppool $ctx.AppPoolName /startMode:AlwaysRunning | Out-Null
    & $appcmd set apppool $ctx.AppPoolName /processModel.idleTimeout:00:00:00 | Out-Null
    & $appcmd set site $ctx.SiteName /serverAutoStart:true | Out-Null
    & $appcmd start apppool $ctx.AppPoolName | Out-Null
    & $appcmd start site $ctx.SiteName | Out-Null
}

Write-Host "All Visa2026 slots configured for reboot auto-start." -ForegroundColor Green
