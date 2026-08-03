#Requires -Version 5.1
<#
.SYNOPSIS
  Build AddressCity-HumanReview.xlsx (near-duplicate Cities + lodging refs + prod usage).

.EXAMPLE
  .\scripts\visa2014-migration\Export-AddressCityHumanReview.ps1 -ViaSsh
#>
[CmdletBinding()]
param(
    [switch]$ViaSsh,
    [string]$SshHost = 'visa2026-onprem',
    [string]$OutputPath = '',
    [switch]$SkipProdUsage
)

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$cityJson = Join-Path $repoRoot 'Visa2026.Module\DatabaseUpdate\LookupCatalogs\city.json'
$tenant = Join-Path $repoRoot 'Visa2026.Module\DatabaseUpdate\LookupCatalogs\tenant'
$toolDir = Join-Path $PSScriptRoot 'tools\AddressCityHumanReview'
$sqlFile = Join-Path $PSScriptRoot '_lib\city-usage-prod.sql'
$outDir = Join-Path $repoRoot 'Visa2026.DataImporter\legacy\visa2014\preview-export'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $outDir 'AddressCity-HumanReview.xlsx'
}

$usageCsv = Join-Path $outDir 'AddressCity-prod-usage.csv'
$utf8 = New-Object System.Text.UTF8Encoding $false

if (-not $SkipProdUsage -and $ViaSsh) {
    Write-Host '>>> Collecting prod City usage counts via SSH...' -ForegroundColor Cyan
    scp $sqlFile "${SshHost}:C:/visa2026-sync/logs/city-usage-prod.sql" | Out-Null
    $pull = @'
$ErrorActionPreference = "Stop"
$pgPass = $null
Get-Content "C:\visa2026\env\prod.env" | ForEach-Object {
  if ($_ -match "^\s*PG_PASSWORD=(.*)$") { $pgPass = $Matches[1].Trim().Trim([char]34) }
}
$env:PGPASSWORD = $pgPass
& "C:\PostgreSQL\16\bin\psql.exe" -h localhost -U postgres -d visa2026_prod -f "C:\visa2026-sync\logs\city-usage-prod.sql" |
  Set-Content -LiteralPath "C:\visa2026-sync\logs\AddressCity-prod-usage.csv" -Encoding utf8
Write-Host "WROTE_USAGE"
'@
    $pullPath = Join-Path $env:TEMP 'pull-city-usage.ps1'
    [System.IO.File]::WriteAllText($pullPath, $pull, $utf8)
    scp $pullPath "${SshHost}:C:/visa2026-sync/logs/pull-city-usage.ps1" | Out-Null
    ssh $SshHost "powershell -NoProfile -ExecutionPolicy Bypass -File C:\visa2026-sync\logs\pull-city-usage.ps1"
    scp "${SshHost}:C:/visa2026-sync/logs/AddressCity-prod-usage.csv" $usageCsv | Out-Null
    # Normalize header for the C# tool
    $raw = Get-Content -LiteralPath $usageCsv -Encoding UTF8
    if ($raw.Count -gt 0 -and $raw[0] -match 'region,name_tm') {
        $raw[0] = 'Region,NameTm,AoR,Lodging,Hotel,Hospital,RegionLinked'
        [System.IO.File]::WriteAllLines($usageCsv, $raw, $utf8)
    }
    Write-Host "INF Prod usage -> $usageCsv" -ForegroundColor DarkGray
}
elseif (-not $SkipProdUsage -and -not $ViaSsh) {
    Write-Host 'WRN Pass -ViaSsh to include prod usage; continuing without.' -ForegroundColor Yellow
    $usageCsv = ''
}

Write-Host '>>> Building workbook with AddressCityHumanReview tool...' -ForegroundColor Cyan
$toolArgs = @(
    'run', '--project', $toolDir, '-c', 'Release', '--',
    '--city-json', $cityJson,
    '--lodging-json', (Join-Path $tenant 'lodging.json'),
    '--hotel-json', (Join-Path $tenant 'hotel.json'),
    '--hospital-json', (Join-Path $tenant 'hospital.json'),
    '--other-site-json', (Join-Path $tenant 'other-site.json'),
    '--output', $OutputPath
)
if ($usageCsv -and (Test-Path $usageCsv)) {
    $toolArgs += @('--prod-usage-csv', $usageCsv)
}
& dotnet @toolArgs
if ($LASTEXITCODE -ne 0) { throw "AddressCityHumanReview failed exit=$LASTEXITCODE" }

Write-Host "OK $OutputPath" -ForegroundColor Green
Write-Host 'Next: review NearDuplicates Decision column (green=auto High). Then Apply-AddressCityHumanReviewDecisions.ps1' -ForegroundColor Cyan
