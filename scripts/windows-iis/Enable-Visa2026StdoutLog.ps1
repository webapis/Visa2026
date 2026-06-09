#Requires -Version 5.1
param(
    [ValidateSet("Production", "Staging", "Demo", "Legacy", "")]
    [string]$Profile = "",

    [string]$PublishPath = ""
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "Visa2026-IisSlots.ps1")
$ctx = Resolve-Visa2026IisSlotContext -Profile $Profile -PublishPath $PublishPath
$PublishPath = $ctx.PublishPath
$appPoolName = $ctx.AppPoolName
$logDir = Join-Path $PublishPath "logs"
New-Item -ItemType Directory -Force -Path $logDir | Out-Null
$webConfig = Join-Path $PublishPath "web.config"
[xml]$xml = Get-Content -LiteralPath $webConfig
$asp = $xml.configuration.location.system.webServer.aspNetCore
$asp.stdoutLogEnabled = "true"
$xml.Save($webConfig)
Write-Host "stdoutLogEnabled=true; logs: $logDir"
& "$env:windir\System32\inetsrv\appcmd.exe" recycle apppool $appPoolName | Out-Null
Start-Sleep -Seconds 8
try {
    $r = Invoke-WebRequest -Uri $ctx.LoginPageUrl -UseBasicParsing -TimeoutSec 90
    Write-Host "HTTP $($r.StatusCode)"
}
catch {
    Write-Host "Request failed: $($_.Exception.Message)"
}
$latest = Get-ChildItem $logDir -Filter "stdout_*" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($latest) {
    Write-Host "=== $($latest.FullName) (last 40 lines) ==="
    Get-Content -LiteralPath $latest.FullName -Tail 40
}
else {
    Write-Host "No stdout log file yet."
}
