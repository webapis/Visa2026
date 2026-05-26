#Requires -Version 5.1
param(
    [string]$PublishPath = "C:\inetpub\visa2026",
    [int]$StartupWaitSeconds = 90
)

$ErrorActionPreference = "Continue"
$appcmd = "$env:windir\System32\inetsrv\appcmd.exe"

Write-Host "=== Services ===" -ForegroundColor Cyan
Get-Service W3SVC -ErrorAction SilentlyContinue | Format-Table Name, Status, StartType
Get-Service -Name "MSSQL`$SQLEXPRESS" -ErrorAction SilentlyContinue | Format-Table Name, Status, StartType

Write-Host "=== IIS ===" -ForegroundColor Cyan
& $appcmd list site
& $appcmd list apppool Visa2026

Write-Host "=== App pool env (names only) ===" -ForegroundColor Cyan
& $appcmd list config "Visa2026" -section:system.applicationHost/applicationPools /text:* 2>$null | Select-String -Pattern "DEVEXPRESS|ASPNETCORE|Connection"

Write-Host "=== Config files ===" -ForegroundColor Cyan
$prod = Join-Path $PublishPath "appsettings.Production.json"
Write-Host "appsettings.Production.json exists: $(Test-Path $prod)"
$webConfig = Join-Path $PublishPath "web.config"
if (Test-Path $webConfig) {
    [xml]$wc = Get-Content $webConfig
    $asp = $wc.configuration.location.'system.webServer'.aspNetCore
    if ($asp) {
        Write-Host "stdoutLogEnabled: $($asp.stdoutLogEnabled)"
        Write-Host "stdoutLogFile: $($asp.stdoutLogFile)"
    }
}

Write-Host "=== Wait for SQL (up to $StartupWaitSeconds s) ===" -ForegroundColor Cyan
$deadline = (Get-Date).AddSeconds($StartupWaitSeconds)
$sqlUp = $false
while ((Get-Date) -lt $deadline) {
    $s = Get-Service -Name "MSSQL`$SQLEXPRESS" -ErrorAction SilentlyContinue
    if ($s -and $s.Status -eq "Running") {
        $sqlUp = $true
        break
    }
    Start-Sleep -Seconds 3
}
Write-Host "SQL Express running: $sqlUp"

Write-Host "=== Run exe once (capture startup errors) ===" -ForegroundColor Cyan
$envFile = "C:\inetpub\visa2026\iis-apppool-env.json"
$appSettings = Join-Path $PublishPath "appsettings.Production.json"
if ((Test-Path $envFile) -and (Test-Path $appSettings)) {
    $e = Get-Content $envFile | ConvertFrom-Json
    $cfg = Get-Content $appSettings | ConvertFrom-Json
    $env:ASPNETCORE_ENVIRONMENT = $e.ASPNETCORE_ENVIRONMENT
    $env:DEVEXPRESS_LICENSEKEY = $e.DEVEXPRESS_LICENSEKEY
    $env:ASPNETCORE_DATA_PROTECTION_KEYS = $e.ASPNETCORE_DATA_PROTECTION_KEYS
    $env:ConnectionStrings__DefaultConnection = $cfg.ConnectionStrings.DefaultConnection
}

$exe = Join-Path $PublishPath "Visa2026.Blazor.Server.exe"
if (-not (Test-Path $exe)) {
    Write-Host "EXE not found: $exe" -ForegroundColor Red
    exit 1
}

$p = Start-Process -FilePath $exe -WorkingDirectory $PublishPath -NoNewWindow -PassThru -RedirectStandardOutput "$env:TEMP\visa2026-stdout.txt" -RedirectStandardError "$env:TEMP\visa2026-stderr.txt"
Start-Sleep -Seconds 15
if (-not $p.HasExited) { Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue }
Write-Host "--- stderr (last 40 lines) ---"
if (Test-Path "$env:TEMP\visa2026-stderr.txt") { Get-Content "$env:TEMP\visa2026-stderr.txt" -Tail 40 }
Write-Host "--- stdout (last 20 lines) ---"
if (Test-Path "$env:TEMP\visa2026-stdout.txt") { Get-Content "$env:TEMP\visa2026-stdout.txt" -Tail 20 }

Write-Host "=== Recent Application event log (ASP.NET / .NET) ===" -ForegroundColor Cyan
Get-WinEvent -FilterHashtable @{ LogName = "Application"; StartTime = (Get-Date).AddHours(-2) } -MaxEvents 15 -ErrorAction SilentlyContinue |
    Where-Object { $_.ProviderName -match "IIS|ASP|\.NET|Core" } |
    Select-Object TimeCreated, ProviderName, Id, Message -First 8 |
    ForEach-Object {
        Write-Host "[$($_.TimeCreated)] $($_.ProviderName) $($_.Id)"
        Write-Host ($_.Message -split "`n" | Select-Object -First 6) -join "`n"
        Write-Host "---"
    }
