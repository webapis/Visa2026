<#
  Writes ConnectionStrings:DefaultConnection to Visa2026.Blazor.Server user secrets
  using SA_PASSWORD from .env.dev (Docker dev SQL on 127.0.0.1:1433, database Visa2026DbDev).

  Run from repo root after changing SA_PASSWORD so Visual Studio matches docker compose without editing appsettings.

  Usage:
    .\migration-scripts\Sync-BlazorDevConnectionUserSecret.ps1
#>

param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$EnvFile = ".env.dev"
)

$ErrorActionPreference = "Stop"

$envPath = Join-Path $RepoRoot $EnvFile
if (-not (Test-Path -LiteralPath $envPath)) {
    Write-Error "Missing $EnvFile at repo root. Create it from .env.dev.example."
}

$line = Select-String -LiteralPath $envPath -Pattern "^SA_PASSWORD=" | Select-Object -First 1
if (-not $line) {
    Write-Error "SA_PASSWORD not found in $EnvFile"
}

$pw = ($line.Line -split "=", 2)[1].Trim().Trim('"')
if ([string]::IsNullOrWhiteSpace($pw)) {
    Write-Error "SA_PASSWORD is empty in $EnvFile"
}

$cs = "Server=127.0.0.1,1433;Database=Visa2026DbDev;User Id=sa;Password=$pw;TrustServerCertificate=True;MultipleActiveResultSets=true"
$projDir = Join-Path $RepoRoot "Visa2026.Blazor.Server"
Push-Location $projDir
try {
    dotnet user-secrets set "ConnectionStrings:DefaultConnection" $cs | Out-Null
}
finally {
    Pop-Location
}

Write-Host "User secret ConnectionStrings:DefaultConnection set from $EnvFile (Docker SQL, Visa2026DbDev)." -ForegroundColor Green
