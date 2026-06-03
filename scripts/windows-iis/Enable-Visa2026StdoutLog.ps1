#Requires -Version 5.1
param([string]$PublishPath = "C:\inetpub\visa2026")

$ErrorActionPreference = "Stop"
$logDir = Join-Path $PublishPath "logs"
New-Item -ItemType Directory -Force -Path $logDir | Out-Null
$webConfig = Join-Path $PublishPath "web.config"
[xml]$xml = Get-Content -LiteralPath $webConfig
$asp = $xml.configuration.location.system.webServer.aspNetCore
$asp.stdoutLogEnabled = "true"
$xml.Save($webConfig)
Write-Host "stdoutLogEnabled=true; logs: $logDir"
& "$env:windir\System32\inetsrv\appcmd.exe" recycle apppool Visa2026 | Out-Null
Start-Sleep -Seconds 8
try {
    $r = Invoke-WebRequest -Uri "http://127.0.0.1/LoginPage" -UseBasicParsing -TimeoutSec 90
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
