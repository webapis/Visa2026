# Generates Visa2026.Module/Localization/CountryLookupStrings.json from country.json seed.
# Requires scripts/local/data/*.json (see script header). Run once after seed changes.
param(
    [string]$CountrySeedPath = (Join-Path $PSScriptRoot '..\..\Visa2026.Module\DatabaseUpdate\LookupCatalogs\country.json'),
    [string]$OutputPath = (Join-Path $PSScriptRoot '..\..\Visa2026.Module\Localization\CountryLookupStrings.json'),
    [string]$DataDir = (Join-Path $PSScriptRoot 'data'),
    [switch]$UpdateSeed
)

$ErrorActionPreference = 'Stop'

function Read-CldrTerritoryNames([string]$Path) {
    $json = Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
    $localeKey = ($json.main.PSObject.Properties | Select-Object -First 1).Name
    $territories = $json.main.$localeKey.localeDisplayNames.territories
    $map = @{}
    foreach ($prop in $territories.PSObject.Properties) {
        if ($prop.Name -match '^[A-Z]{2}$') {
            $map[$prop.Name] = [string]$prop.Value
        }
    }
    return $map
}

function Read-Alpha3ToAlpha2Map([string]$Path) {
    $rows = Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
    $map = @{}
    foreach ($row in $rows) {
        $a3 = [string]$row.'alpha-3'
        $a2 = [string]$row.'alpha-2'
        if ($a3 -and $a2) { $map[$a3.ToUpperInvariant()] = $a2.ToUpperInvariant() }
    }
    return $map
}

$isoPath = Join-Path $DataDir 'iso3166-all.json'
$enPath = Join-Path $DataDir 'cldr-territories-en.json'
$trPath = Join-Path $DataDir 'cldr-territories-tr.json'
$ruPath = Join-Path $DataDir 'cldr-territories-ru.json'
foreach ($p in @($isoPath, $enPath, $trPath, $ruPath, (Join-Path $DataDir 'country-legacy-overrides.json'))) {
    if (-not (Test-Path -LiteralPath $p)) {
        throw "Missing data file: $p. Download via docs in scripts/local/Generate-CountryLookupStrings.ps1 or curl from repo README."
    }
}

Write-Host "Loading reference data..."
$alpha3ToAlpha2 = Read-Alpha3ToAlpha2Map $isoPath
$legacyPath = Join-Path $DataDir 'country-legacy-overrides.json'
$legacy = Get-Content -LiteralPath $legacyPath -Raw -Encoding UTF8 | ConvertFrom-Json
foreach ($prop in $legacy.alpha3ToAlpha2.PSObject.Properties) {
    $alpha3ToAlpha2[$prop.Name] = [string]$prop.Value
}
$customCountryNames = @{}
foreach ($prop in $legacy.custom.PSObject.Properties) {
    $customCountryNames[$prop.Name] = $prop.Value
}
$enNames = Read-CldrTerritoryNames $enPath
$trNames = Read-CldrTerritoryNames $trPath
$ruNames = Read-CldrTerritoryNames $ruPath

Write-Host "Loading country seed..."
$seed = Get-Content -LiteralPath $CountrySeedPath -Raw -Encoding UTF8 | ConvertFrom-Json

$countryCatalog = [ordered]@{}
$missing = New-Object System.Collections.Generic.List[string]

foreach ($row in $seed.rows) {
    $code = ([string]$row.Code).Trim().ToUpperInvariant()
    if ([string]::IsNullOrWhiteSpace($code)) { continue }

    if ($UpdateSeed) {
        $row | Add-Member -NotePropertyName LocalizationKey -NotePropertyValue $code -Force
    }

    if ($customCountryNames.ContainsKey($code)) {
        $entry = $customCountryNames[$code]
        $countryCatalog[$code] = [ordered]@{
            'en-US' = [string]$entry.'en-US'
            'tr-TR' = [string]$entry.'tr-TR'
            'tk-TM' = [string]$entry.'tk-TM'
            'ru-RU' = [string]$entry.'ru-RU'
        }
        continue
    }

    $alpha2 = $null
    if ($alpha3ToAlpha2.ContainsKey($code)) {
        $alpha2 = $alpha3ToAlpha2[$code]
    }
    else {
        $missing.Add($code)
    }

    $en = if ($alpha2 -and $enNames.ContainsKey($alpha2)) { $enNames[$alpha2] } else { [string]$row.NameTm }
    $tr = if ($alpha2 -and $trNames.ContainsKey($alpha2)) { $trNames[$alpha2] } else { $en }
    $ru = if ($alpha2 -and $ruNames.ContainsKey($alpha2)) { $ruNames[$alpha2] } else { $en }
    $tk = [string]$row.NameTm
    if ([string]::IsNullOrWhiteSpace($tk)) { $tk = [string]$row.Name }

    $countryCatalog[$code] = [ordered]@{
        'en-US' = $en
        'tr-TR' = $tr
        'tk-TM' = $tk
        'ru-RU' = $ru
    }
}

$root = [ordered]@{ 'country' = $countryCatalog }
$json = $root | ConvertTo-Json -Depth 6
[System.IO.File]::WriteAllText($OutputPath, $json + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))
Write-Host "Wrote $($countryCatalog.Count) country strings to $OutputPath"

if ($UpdateSeed) {
    $seedJson = $seed | ConvertTo-Json -Depth 6
    [System.IO.File]::WriteAllText($CountrySeedPath, $seedJson + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))
    Write-Host "Updated LocalizationKey on seed rows in $CountrySeedPath"
}

if ($missing.Count -gt 0) {
    Write-Warning "No alpha-2 mapping for: $($missing -join ', ')"
}
