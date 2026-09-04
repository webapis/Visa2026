#Requires -Version 5.1
<#
.SYNOPSIS
  PATCH ApplicationProfile.NestedTemplates from tenant JSON (Wave 3).

.EXAMPLE
  .\scripts\visa2014-migration\patch\Application-Profile-NestedTemplates.ps1 -DryRun
  .\scripts\visa2014-migration\patch\Application-Profile-NestedTemplates.ps1
#>
[CmdletBinding()]
param(
    [string]$TargetConnection = $(if ($env:ConnectionStrings__DefaultConnection) { $env:ConnectionStrings__DefaultConnection } else { "Host=localhost;Port=5432;Database=visa2026;Username=postgres;Password=Visa2026Local;Persist Security Info=True;EFCoreProvider=Postgres" }),
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [switch]$DryRun
)

. (Join-Path $PSScriptRoot '..\_lib\Get-RepoRoot.ps1')
$repoRoot = Get-Visa2026RepoRoot
$ErrorActionPreference = "Stop"
Set-Location $repoRoot

$patchArgs = @(
    "run", "--project", "Visa2026.DataImporter", "-c", $Configuration, "--",
    "--patch-visa2014-application-profile-nested-templates",
    "--target-connection", $TargetConnection
)
if ($DryRun) { $patchArgs += "--dry-run" }

Write-Host "=== Build Module + DataImporter ($Configuration) ===" -ForegroundColor Cyan
dotnet build Visa2026.Module/Visa2026.Module.csproj -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "Module build failed" }
dotnet build Visa2026.DataImporter/Visa2026.DataImporter.csproj -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "DataImporter build failed" }

Write-Host "=== PATCH ApplicationProfile nested templates (Wave 3) ===" -ForegroundColor Cyan
& dotnet @patchArgs
exit $LASTEXITCODE
