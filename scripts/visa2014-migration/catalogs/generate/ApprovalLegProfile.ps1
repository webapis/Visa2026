#Requires -Version 5.1

<#

.SYNOPSIS

  Generate tenant/approval-leg-profile.json from a project-contract catalog (VISA2015-derived).



.DESCRIPTION

  Deduplicates ministry-leg chains from project-contract*.json into the ~10 shared

  ApprovalLegProfile rows seeded by ApprovalLegProfileSeedUpdater.



  Typical Çalik pipeline:

    1. catalogs/generate/ProjectContract-CalikEnergi.ps1  (VISA2015 SQL → project-contract.calik-energi.json)

    2. This script                                       (→ approval-leg-profile.json)

    3. Optional -StripContractLegs                       (identity-only contracts)



  Do not edit approval-leg-profile.json by hand after migration review.



.EXAMPLE

  .\scripts\visa2014-migration\catalogs/generate/ApprovalLegProfile.ps1



.EXAMPLE

  .\scripts\visa2014-migration\catalogs/generate/ApprovalLegProfile.ps1 `

    -ContractCatalogPath Visa2026.Module\DatabaseUpdate/LookupCatalogs/tenant/project-contract.calik-energi.json `

    -StripContractLegs

#>

param(

    [string]$ContractCatalogPath,

    [string]$OutFile,

    [switch]$StripContractLegs

)




. (Join-Path $PSScriptRoot '..\..\_lib\Get-RepoRoot.ps1')
$repoRoot = Get-Visa2026RepoRoot
Set-StrictMode -Version Latest

$ErrorActionPreference = 'Stop'
$tenantDir = Join-Path $repoRoot 'Visa2026.Module\DatabaseUpdate\LookupCatalogs\tenant'

$calikContract = Join-Path $tenantDir 'project-contract.calik-energi.json'

$defaultContract = Join-Path $tenantDir 'project-contract.json'



if (-not $ContractCatalogPath) {

    $ContractCatalogPath = if (Test-Path -LiteralPath $calikContract) { $calikContract } else { $defaultContract }

} elseif (-not [System.IO.Path]::IsPathRooted($ContractCatalogPath)) {

    $ContractCatalogPath = Join-Path $repoRoot $ContractCatalogPath

}



$OutFile = if ($OutFile) {

    if ([System.IO.Path]::IsPathRooted($OutFile)) { $OutFile } else { Join-Path $repoRoot $OutFile }

} else {

    Join-Path $tenantDir 'approval-leg-profile.json'

}



if (-not (Test-Path -LiteralPath $ContractCatalogPath)) {

    throw "Contract catalog not found: $ContractCatalogPath. Run catalogs/generate/ProjectContract-CalikEnergi.ps1 first."

}



$toolProj = Join-Path $repoRoot 'tools\GenerateApprovalLegProfileCatalog\GenerateApprovalLegProfileCatalog.csproj'

$dotnetArgs = @('run', '--project', $toolProj, '--', $ContractCatalogPath, $OutFile)

if ($StripContractLegs) { $dotnetArgs += '--strip' }



Write-Host "=== Generate approval-leg-profile.json ===" -ForegroundColor Cyan

Write-Host "INF Input:  $ContractCatalogPath"

Write-Host "INF Output: $OutFile"

& dotnet @dotnetArgs

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

