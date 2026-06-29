#Requires -Version 5.1
<#
.SYNOPSIS
  Generate hotel.calik-energi.json and hospital.calik-energi.json from preview exports.
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

& (Join-Path $PSScriptRoot 'Generate-HotelCalikEnergiCatalog.ps1')
& (Join-Path $PSScriptRoot 'Generate-HospitalCalikEnergiCatalog.ps1')
