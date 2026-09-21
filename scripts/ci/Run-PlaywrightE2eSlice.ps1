#Requires -Version 5.1
<#
.SYNOPSIS
  Run one Playwright Local E2E slice (Sign in, Register, Find, Add passport).
  Restores visa2026_easytest from the gitignored pg_dump when present.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Filter,

    [string]$TrxName = 'e2e-slice.trx',

    [switch]$BlameHang
)

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
Set-Location -LiteralPath $repoRoot

$env:CI = 'true'
$env:VISA2026_E2E_TARGET = 'Local'
$env:VISA2026_E2E_SNAPSHOT_TRUST = 'true'
if (-not $env:VISA2026_E2E_SCREENSHOTS) { $env:VISA2026_E2E_SCREENSHOTS = 'true' }

$trxDir = Join-Path $repoRoot 'TestResults'
if (-not (Test-Path -LiteralPath $trxDir)) {
    New-Item -ItemType Directory -Force -Path $trxDir | Out-Null
}

$testArgs = @(
    'test', 'Visa2026.E2E.Tests/Visa2026.E2E.Tests.csproj',
    '-c', 'EasyTest', '--no-build',
    '--filter', "FullyQualifiedName~$Filter&Driver=Playwright",
    '--logger', 'console;verbosity=normal',
    '--logger', "trx;LogFileName=$TrxName",
    '--results-directory', $trxDir
)
if ($BlameHang) {
    $testArgs += @('--blame-hang', '--blame-hang-timeout', '10m')
}

Write-Host "E2E slice Filter=$Filter Trx=$TrxName"
& dotnet @testArgs
exit $LASTEXITCODE
