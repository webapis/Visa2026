#Requires -Version 5.1
# Start the GHA Windows PostgreSQL service (preinstalled, else Chocolatey).
$ErrorActionPreference = 'Stop'

function Get-PgBin {
    $root = Get-ChildItem 'C:\Program Files\PostgreSQL' -Directory -ErrorAction SilentlyContinue |
        Sort-Object { [int]($_.Name -replace '\D', '0') } -Descending | Select-Object -First 1
    if ($root) { return (Join-Path $root.FullName 'bin') }
    return $null
}

$usedPreinstalled = $false
$svc = Get-Service -Name 'postgresql*' -ErrorAction SilentlyContinue | Select-Object -First 1
$bin = Get-PgBin

if ($svc -and $bin) {
    Write-Host "Found preinstalled PostgreSQL service '$($svc.Name)' with binaries at $bin"
    try {
        Set-Service -Name $svc.Name -StartupType Automatic
        if ((Get-Service $svc.Name).Status -ne 'Running') { Start-Service $svc.Name }
        $env:Path = "$bin;$env:Path"
        $env:PGPASSWORD = 'root'
        & psql -h $env:PG_HOST -p $env:PG_PORT -U $env:PG_USER -d postgres `
            -c "ALTER USER $env:PG_USER WITH PASSWORD '$env:PG_PASSWORD';" 2>$null
        if ($LASTEXITCODE -ne 0) {
            $env:PGPASSWORD = $env:PG_PASSWORD
            & psql -h $env:PG_HOST -p $env:PG_PORT -U $env:PG_USER -d postgres -c "SELECT 1;"
            if ($LASTEXITCODE -ne 0) { throw "Cannot authenticate against preinstalled PostgreSQL." }
        }
        $usedPreinstalled = $true
    } catch {
        Write-Host "::warning::Preinstalled PostgreSQL unusable ($($_.Exception.Message)); falling back to Chocolatey."
    }
}

if (-not $usedPreinstalled) {
    choco install postgresql16 --params "/Password:$env:PG_PASSWORD" -y --no-progress
    $bin = Get-PgBin
    if (-not $bin) { throw "PostgreSQL install directory not found." }
    $env:Path = "$bin;$env:Path"
    $svc = Get-Service -Name 'postgresql*' -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($svc -and $svc.Status -ne 'Running') { Start-Service $svc.Name }
}

Add-Content -Path $env:GITHUB_PATH -Value $bin
Add-Content -Path $env:GITHUB_ENV -Value "VISA2026_E2E_PG_BIN=$bin"
$env:PGPASSWORD = $env:PG_PASSWORD
& psql -h $env:PG_HOST -p $env:PG_PORT -U $env:PG_USER -d postgres -c "SELECT version();"
if ($LASTEXITCODE -ne 0) { throw "PostgreSQL is not reachable with PG_PASSWORD." }
