#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Ensure Visa2026 IIS site and app pool start automatically after server reboot.

.DESCRIPTION
  - Enables serverAutoStart and preload for the Visa2026 site
  - Stops Default Web Site and disables its serverAutoStart (avoids IIS welcome page on :80)
  - Starts Visa2026 app pool and site
  - Optionally registers a one-shot task if W3SVC starts before SQL (see -WaitForSqlSeconds)

.PARAMETER SiteName
  Default Visa2026.

.PARAMETER AppPoolName
  Default Visa2026.

.PARAMETER DefaultSiteName
  Default IIS site to disable on port 80 (default "Default Web Site").

.NOTES
  Runbook: docs/ON_PREM_WINDOWS_IIS.md
  Run once after install and after any IIS reset that re-enables Default Web Site.
#>
param(
    [string]$SiteName = "Visa2026",
    [string]$AppPoolName = "Visa2026",
    [string]$DefaultSiteName = "Default Web Site",
    [int]$WaitForSqlSeconds = 0
)

$ErrorActionPreference = "Stop"
$appcmd = "$env:windir\System32\inetsrv\appcmd.exe"

function Invoke-AppCmd([string[]]$AppCmdArgs) {
    & $appcmd @AppCmdArgs
    if ($LASTEXITCODE -ne 0) {
        throw "appcmd failed ($LASTEXITCODE): appcmd $($AppCmdArgs -join ' ')"
    }
}

Write-Host "==> SQL Server Express service: Automatic startup" -ForegroundColor Cyan
$sqlSvcName = "MSSQL`$SQLEXPRESS"
$sqlSvc = Get-Service -Name $sqlSvcName -ErrorAction SilentlyContinue
if ($sqlSvc) {
    if ($sqlSvc.StartType -ne "Automatic") {
        Set-Service -Name $sqlSvc.Name -StartupType Automatic
        Write-Host "  Set $($sqlSvc.Name) StartType to Automatic" -ForegroundColor Yellow
    }
    if ($sqlSvc.Status -ne "Running") {
        Write-Host "  Starting $($sqlSvc.Name)..." -ForegroundColor Yellow
        Start-Service -Name $sqlSvc.Name
    }
}
else {
    Write-Warning "MSSQL`$SQLEXPRESS service not found."
}

Write-Host "==> IIS starts after SQL (W3SVC dependency)" -ForegroundColor Cyan
if ($sqlSvc) {
    $qc = sc.exe qc W3SVC 2>&1 | Out-String
    if ($qc -notmatch [regex]::Escape($sqlSvcName)) {
        # Keep existing deps (usually HTTP) and add SQL Express.
        $newDeps = "HTTP/$sqlSvcName"
        if ($qc -match "DEPENDENCIES\s*:\s*(.+)") {
            $existing = $Matches[1].Trim()
            if ($existing -and $existing -ne "N/A" -and $existing -notmatch [regex]::Escape($sqlSvcName)) {
                $newDeps = "$existing/$sqlSvcName"
            }
        }
        sc.exe config W3SVC depend= $newDeps | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  W3SVC depend= $newDeps" -ForegroundColor Green
        }
        else {
            Write-Warning "Could not set W3SVC dependency on SQL (exit $LASTEXITCODE). Use Register-Visa2026IisBootTask.ps1."
        }
    }
}

Write-Host "==> W3SVC: Automatic startup" -ForegroundColor Cyan
$w3svc = Get-Service -Name "W3SVC" -ErrorAction SilentlyContinue
if ($w3svc -and $w3svc.StartType -ne "Automatic") {
    Set-Service -Name "W3SVC" -StartupType Automatic
    Write-Host "  Set W3SVC StartType to Automatic" -ForegroundColor Yellow
}

Write-Host "==> App pool $AppPoolName (AlwaysRunning)" -ForegroundColor Cyan
Invoke-AppCmd @("set", "apppool", $AppPoolName, "/startMode:AlwaysRunning")
Invoke-AppCmd @("set", "apppool", $AppPoolName, "/processModel.idleTimeout:00:00:00")
Invoke-AppCmd @("start", "apppool", $AppPoolName)

Write-Host "==> Site $SiteName (serverAutoStart + preload)" -ForegroundColor Cyan
Invoke-AppCmd @("set", "site", $SiteName, "/serverAutoStart:true")
try {
    Invoke-AppCmd @("set", "app", "$SiteName/", "/preloadEnabled:true")
}
catch {
    Write-Warning "Could not enable preload (non-fatal): $($_.Exception.Message)"
}

Write-Host "==> Disable $DefaultSiteName on port 80 (IIS welcome page)" -ForegroundColor Cyan
$defaultListed = & $appcmd list site /name:"$DefaultSiteName" 2>$null
if ($LASTEXITCODE -eq 0 -and $defaultListed) {
    # Move default site off :80 so only Visa2026 serves LAN HTTP after reboot.
    Invoke-AppCmd @("set", "site", "`"$DefaultSiteName`"", "/bindings:http/127.0.0.1:8080:")
    Invoke-AppCmd @("set", "site", "`"$DefaultSiteName`"", "/serverAutoStart:false")
    Invoke-AppCmd @("stop", "site", "`"$DefaultSiteName`"")
}
else {
    Write-Host "  Site '$DefaultSiteName' not found (skipped)." -ForegroundColor DarkGray
}

Write-Host "==> Remove stale port 80 portproxy (WSL/Docker)" -ForegroundColor Cyan
netsh interface portproxy delete v4tov4 listenaddress=0.0.0.0 listenport=80 2>$null | Out-Null

if ($WaitForSqlSeconds -gt 0) {
    Write-Host "==> Wait up to ${WaitForSqlSeconds}s for SQL Express" -ForegroundColor Cyan
    $deadline = (Get-Date).AddSeconds($WaitForSqlSeconds)
    while ((Get-Date) -lt $deadline) {
        $s = Get-Service -Name "MSSQL`$SQLEXPRESS" -ErrorAction SilentlyContinue
        if ($s -and $s.Status -eq "Running") { break }
        Start-Sleep -Seconds 2
    }
}

Write-Host "==> Start site $SiteName" -ForegroundColor Cyan
Invoke-AppCmd @("start", "site", $SiteName)

Write-Host "==> Recycle app pool (pick up SQL after boot)" -ForegroundColor Cyan
Invoke-AppCmd @("recycle", "apppool", $AppPoolName)

Write-Host ""
Invoke-AppCmd @("list", "site")
Write-Host ""
Write-Host "Visa2026 configured for automatic start after reboot." -ForegroundColor Green
Write-Host "Smoke test: http://localhost/LoginPage" -ForegroundColor DarkGray
