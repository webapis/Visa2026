#Requires -Version 5.1
<#
.SYNOPSIS
  Print recent Application log errors (IIS / .NET Core startup failures).
#>
$ErrorActionPreference = "Continue"
$events = Get-WinEvent -LogName Application -MaxEvents 200 -ErrorAction SilentlyContinue |
    Where-Object {
        $_.LevelDisplayName -eq 'Error' -or $_.LevelDisplayName -eq 'Warning'
    } |
    Where-Object {
        $_.ProviderName -match 'IIS|AspNetCore|\.NET|Application Error'
    } |
    Select-Object -First 15

if (-not $events) {
    Write-Host "No recent IIS/ASP.NET errors in Application log (last 200 events)."
    exit 0
}

foreach ($e in $events) {
    Write-Host "--- $($e.TimeCreated) $($e.ProviderName) [$($e.Id)] ---"
    Write-Host $e.Message
    Write-Host ""
}
