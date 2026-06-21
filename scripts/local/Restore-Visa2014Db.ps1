#Requires -Version 5.1
<#
.SYNOPSIS
  [LOCAL] Restore a VISA2014/VISA2015 production .bak as database VISA2015.

.DESCRIPTION
  Wrapper around migration-scripts/Restore-BackupToLocalSql.ps1.
  Uses SA_PASSWORD from .env.dev (local container — not VISA2014 prod credentials).

  Place the backup at repo root (default: .\visa2015-prod.bak) or pass -BackupFile.

.EXAMPLE
  .\scripts\local\Restore-Visa2014Db.ps1

.EXAMPLE
  .\scripts\local\Restore-Visa2014Db.ps1 -BackupFile D:\backups\visa2015-prod.bak
#>

[CmdletBinding()]
param(
    [string]$BackupFile = "visa2015-prod.bak",
    [string]$DatabaseName = "VISA2015"
)

$ErrorActionPreference = "Stop"

function Resolve-RepoRoot {
    return Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
}

$repoRoot = Resolve-RepoRoot
$restoreScript = Join-Path $repoRoot "migration-scripts\Restore-BackupToLocalSql.ps1"

if (-not (Test-Path -LiteralPath $restoreScript)) {
    Write-Error "Missing restore script: $restoreScript"
}

$bakPath = if ([System.IO.Path]::IsPathRooted($BackupFile)) {
    $BackupFile
} else {
    Join-Path $repoRoot $BackupFile
}

Write-Host "Restoring VISA2014 backup into local SQL database '$DatabaseName'..." -ForegroundColor Cyan
Write-Host "  Backup: $bakPath"
Write-Host "  Target DB: $DatabaseName (legacy read-only reference for migration)"
Write-Host ""

& $restoreScript -BackupFile $bakPath -DatabaseName $DatabaseName
