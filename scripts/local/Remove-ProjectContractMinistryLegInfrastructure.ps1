# Phase 5 cleanup: remove legacy ProjectContract.MinistryLegs infrastructure.
# Run from repo root after closing stuck dotnet/msbuild terminals.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$paths = @(
    'Visa2026.Module/BusinessObjects/ProjectContractMinistryLeg.cs',
    'Visa2026.Module/BusinessObjects/ProjectContractMinistryHelper.cs',
    'Visa2026.Module/DatabaseUpdate/LookupCatalogs/ProjectContractMinistryLegCatalog.cs',
    'Visa2026.Module/DatabaseUpdate/ProjectContractMinistrySeedUpdater.cs',
    'Visa2026.Module/Controllers/ProjectContractMinistryController.cs',
    'Visa2026.Module/Controllers/ProjectContractMinistryLegDetailDefaultsController.cs',
    'Visa2026.Module/Controllers/ProjectContractMinistryLegSaveGuardController.cs',
    'Visa2026.Module/Controllers/ProjectContractMinistryLegObjectSpaceHooks.cs',
    'Visa2026.Module/Controllers/ProjectContractMinistryLegCreationContext.cs',
    'Visa2026.Module.Tests/BusinessObjects/ProjectContractMinistryLegForeignKeySyncTests.cs',
    'Visa2026.Module.Tests/BusinessObjects/ProjectContractMinistryLegCatalogLoaderTests.cs'
)

foreach ($rel in $paths) {
    $full = Join-Path $repoRoot $rel
    if (Test-Path -LiteralPath $full) {
        Remove-Item -LiteralPath $full -Force
        Write-Host "Removed $rel"
    }
}

Write-Host 'Done. Run: dotnet build Visa2026.slnx -c Debug'
