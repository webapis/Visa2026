#Requires -Version 5.1
. (Join-Path $PSScriptRoot '..\..\_lib\Get-RepoRoot.ps1')
$repoRoot = Get-Visa2026RepoRoot
<#
.SYNOPSIS
  Generate hotel.calik-energi.json and hospital.calik-energi.json from preview exports.
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

& (Join-Path $PSScriptRoot 'Hotel-CalikEnergi.ps1')
& (Join-Path $PSScriptRoot 'Hospital-CalikEnergi.ps1')
