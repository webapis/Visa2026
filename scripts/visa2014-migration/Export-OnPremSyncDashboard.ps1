#Requires -Version 5.1
<#
.SYNOPSIS
  One-shot export of sync-dashboard.json (+ optional HTML) for Blazor dashboard or browser view.

.EXAMPLE
  .\scripts\visa2014-migration\Export-OnPremSyncDashboard.ps1 `
    -LegacySource calik-energi-onprem-prod -LoadProdConnectionFromSsh -IncludeHtml

.EXAMPLE
  # On sync host (.25):
  .\Export-OnPremSyncDashboard.ps1 -SyncHostRoot C:\visa2026-sync -IncludeHtml
#>
[CmdletBinding()]
param(
    [string]$LegacyServer = '10.100.128.15',
    [string]$LegacyDatabase = 'VISA2015',
    [string]$LegacyUser = 'ReadOnlyUser',
    [string]$LegacyPassword = '',
    [string]$TargetConnection = '',
    [string]$TargetServer = '10.100.128.25\SQLEXPRESS',
    [string]$TargetDatabase = 'Visa2026DbProd',
    [string]$TargetUser = 'sa',
    [string]$TargetPassword = '',
    [string]$LegacySource = 'calik-energi-onprem-prod',
    [string]$SyncHostRoot = '',
    [switch]$LoadProdConnectionFromSsh,
    [string]$SshHost = 'visa2026-onprem',
    [switch]$IncludeHtml
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_lib\Get-RepoRoot.ps1')
. (Join-Path $PSScriptRoot '_lib\OnPremSyncState.ps1')
. (Join-Path $PSScriptRoot '_lib\OnPremSyncRunStatus.ps1')
. (Join-Path $PSScriptRoot '_lib\Export-OnPremSyncDashboardCore.ps1')

$repoRoot = Get-Visa2026RepoRoot
if ($LoadProdConnectionFromSsh) {
    Set-OnPremProdConnectionFromSsh -SshHost $SshHost | Out-Null
}

$config = Resolve-OnPremSyncStateConfig `
    -LegacyServer $LegacyServer `
    -LegacyDatabase $LegacyDatabase `
    -LegacyUser $LegacyUser `
    -LegacyPassword $LegacyPassword `
    -TargetConnection $TargetConnection `
    -TargetServer $TargetServer `
    -TargetDatabase $TargetDatabase `
    -TargetUser $TargetUser `
    -TargetPassword $TargetPassword `
    -LegacySource $LegacySource `
    -RepoRoot $repoRoot

$outputRoot = if ($SyncHostRoot) {
    (Resolve-Path -LiteralPath $SyncHostRoot).Path
} else {
    Resolve-OnPremSyncStatusRoot -RepoRoot $repoRoot
}

Write-Host "INF Exporting dashboard to $outputRoot ..." -ForegroundColor Green
Test-OnPremSqlConnections -Config $config
# Always include document-copy / FileData rows for Operations report.
$rows = Get-OnPremSyncStateSnapshot -Config $config -IncludeFileData
$result = Export-OnPremSyncDashboard -Config $config -EntityRows $rows -OutputRoot $outputRoot -IncludeHtml:$IncludeHtml
Write-Host "INF JSON: $($result.JsonPath)" -ForegroundColor Green
if ($result.HtmlPath) {
    Write-Host "INF HTML: $($result.HtmlPath)" -ForegroundColor Green
}
