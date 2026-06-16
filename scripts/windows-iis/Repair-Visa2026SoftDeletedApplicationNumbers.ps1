#Requires -Version 5.1
<#
.SYNOPSIS
  Free application-number slots held by soft-deleted rows (GCRecord not null).
#>
param(
    [string]$AppSettingsPath = "C:\inetpub\visa2026-prod\appsettings.Production.json"
)

$ErrorActionPreference = "Stop"
$cfg = Get-Content -LiteralPath $AppSettingsPath -Raw | ConvertFrom-Json
$cs = $cfg.ConnectionStrings.DefaultConnection

Add-Type -AssemblyName System.Data
$cn = New-Object System.Data.SqlClient.SqlConnection $cs
$cn.Open()
try {
    $cmd = $cn.CreateCommand()
    $cmd.CommandText = @"
UPDATE Applications
SET ApplicationNumber = CONCAT('DEL-', REPLACE(CAST(ID AS varchar(36)), '-', '')),
    FullApplicationNumber = CONCAT('DEL-', REPLACE(CAST(ID AS varchar(36)), '-', ''))
WHERE GCRecord IS NOT NULL
  AND IsManualEntry = 0
  AND ApplicationNumber IS NOT NULL
  AND ApplicationNumber NOT LIKE 'DEL-%';

SELECT @@ROWCOUNT AS RowsUpdated;
"@
    $updated = $cmd.ExecuteScalar()
    Write-Host "Renamed soft-deleted application numbers: $updated"
}
finally {
    $cn.Close()
}
