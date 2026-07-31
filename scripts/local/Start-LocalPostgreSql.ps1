#Requires -Version 5.1
# Starts local Visa2026 PostgreSQL 16 (binaries at C:\PostgreSQL\16) if not already running.
$ErrorActionPreference = "Stop"
$dest = "C:\PostgreSQL\16"
$data = "$dest\data"
$svcName = "postgresql-x64-16"

if (-not (Test-Path "$dest\bin\pg_ctl.exe")) {
    throw "PostgreSQL not found at $dest - see .cursor/skills/visa2026-postgresql"
}

$status = & "$dest\bin\pg_ctl.exe" -D $data status 2>&1 | Out-String
if ($status -match "server is running") {
    Write-Host $status.Trim()
    exit 0
}

$svc = Get-Service -Name $svcName -ErrorAction SilentlyContinue
if ($svc) {
    if ($svc.Status -ne "Running") {
        Start-Service $svc.Name
        Start-Sleep 2
    }
    Get-Service $svc.Name | Format-Table Name, Status -AutoSize
    exit 0
}

& "$dest\bin\pg_ctl.exe" -D $data -l "$dest\logfile.log" start
& "$dest\bin\pg_ctl.exe" -D $data status
