#Requires -Version 5.1
<#
.SYNOPSIS
  Compare legacy VISA2015 active row counts vs Visa2026 migrated BO totals.
.EXAMPLE
  .\scripts\visa2014-migration\Compare-LegacyMigratedCounts.ps1 -ShowIdMap
#>
[CmdletBinding()]
param(
    [string]$LegacyServer = "localhost\SQLEXPRESS",
    [string]$LegacyDatabase = "VISA2015",
    [string]$TargetServer = "(localdb)\mssqllocaldb",
    [string]$TargetDatabase = "Visa2026",
    [string]$LegacySource = "calik-energi",
    [switch]$ShowIdMap
)
$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot '_lib\Get-RepoRoot.ps1')
$mapRoot = Join-Path (Get-Visa2026RepoRoot) "Visa2026.DataImporter/legacy/visa2014/id-maps/$LegacySource"
function Invoke-SqlCount([string]$Server,[string]$Database,[string]$Query) {
    $lines = @(sqlcmd -S $Server -E -C -d $Database -Q "SET NOCOUNT ON; $Query" -W -h-1 | Where-Object { $_ -match '^\s*\d+\s*$' })
    if ($lines.Count -eq 0) { return $null }
    [int]($lines[0].Trim())
}
function Get-IdMapCount([string]$Entity) {
    $p = Join-Path $mapRoot "$Entity.json"
    if (-not (Test-Path $p)) { return $null }
    $pattern = '"[0-9a-fA-F-]{36}"\s*:'
    return ([regex]::Matches((Get-Content $p -Raw), $pattern)).Count
}
$rows = @(
    @{ BO='Person'; L='SELECT COUNT(*) FROM dbo.Person WHERE GCRecord IS NULL'; M='SELECT COUNT(*) FROM People'; N='' }
    @{ BO='Passport'; L='SELECT COUNT(*) FROM dbo.Passport pp INNER JOIN dbo.Person p ON pp.Person = p.Oid AND p.GCRecord IS NULL WHERE pp.GCRecord IS NULL'; M='SELECT COUNT(*) FROM Passports'; N='legacy = import SQL scope' }
    @{ BO='Visa'; L='SELECT COUNT(*) FROM dbo.Visa WHERE GCRecord IS NULL'; M='SELECT COUNT(*) FROM Visas'; N='' }
    @{ BO='Education'; L='SELECT COUNT(*) FROM dbo.Education WHERE GCRecord IS NULL'; M='SELECT COUNT(*) FROM Educations'; N='' }
    @{ BO='EmployeePositionHistory'; L='SELECT COUNT(*) FROM dbo.WorkHistoryOfEmployee WHERE GCRecord IS NULL'; M='SELECT COUNT(*) FROM EmployeePositionHistories'; N='' }
    @{ BO='EmployeeSalary'; L='SELECT COUNT(*) FROM dbo.Employee e INNER JOIN dbo.Person p ON p.Oid = e.Oid AND p.GCRecord IS NULL'; M='SELECT COUNT(*) FROM EmployeeSalaries'; N='legacy = Employee scope' }
    @{ BO='AddressOfResidence'; L='SELECT COUNT(*) FROM dbo.AddressOfResidence WHERE GCRecord IS NULL'; M='SELECT COUNT(*) FROM AddressesOfResidence'; N='PIA inference may add rows' }
    @{ BO='MedicalRecord'; L='SELECT COUNT(*) FROM dbo.IPersonn_SpidKepilnama WHERE GCRecord IS NULL'; M='SELECT COUNT(*) FROM MedicalRecords'; N='' }
    @{ BO='Application'; L='SELECT COUNT(*) FROM dbo.Application WHERE GCRecord IS NULL'; M='SELECT COUNT(*) FROM Applications WHERE IsManualEntry = 1 AND (GCRecord IS NULL OR GCRecord = 0)'; N='manual-entry only' }
    @{ BO='ApplicationItem'; L='SELECT COUNT(*) FROM dbo.PersonInApplication WHERE GCRecord IS NULL'; M='SELECT COUNT(*) FROM ApplicationItems ai INNER JOIN Applications a ON ai.ApplicationID = a.ID WHERE a.IsManualEntry = 1 AND (a.GCRecord IS NULL OR a.GCRecord = 0) AND (ai.GCRecord IS NULL OR ai.GCRecord = 0)'; N='manual-entry items' }
    @{ BO='ApplicationProfileInstanceProgress'; L='SELECT COUNT(*) FROM dbo.Application WHERE GCRecord IS NULL'; M='SELECT COUNT(*) FROM ApplicationProgresses ap INNER JOIN Applications a ON ap.ApplicationID = a.ID WHERE a.IsManualEntry = 1 AND (a.GCRecord IS NULL OR a.GCRecord = 0) AND (ap.GCRecord IS NULL OR ap.GCRecord = 0)'; N='synthetic multi-step per app' }
)
Write-Host "Legacy: $LegacyServer / $LegacyDatabase" -ForegroundColor Cyan
Write-Host "Target: $TargetServer / $TargetDatabase" -ForegroundColor Cyan
$rows | ForEach-Object {
    $legacy = Invoke-SqlCount $LegacyServer $LegacyDatabase $_.L
    $migrated = Invoke-SqlCount $TargetServer $TargetDatabase $_.M
    $gap = if ($null -ne $legacy -and $null -ne $migrated) { $migrated - $legacy } else { $null }
    $obj = [ordered]@{ BusinessObject = $_.BO; Legacy = $legacy; Migrated = $migrated; Gap = $gap }
    if ($ShowIdMap) { $obj.IdMap = Get-IdMapCount $_.BO }
    if ($_.N) { $obj.Note = $_.N }
    [pscustomobject]$obj
} | Format-Table -AutoSize
if ($ShowIdMap) { Write-Host "Id-map: $mapRoot" -ForegroundColor DarkGray }