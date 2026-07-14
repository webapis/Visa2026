#Requires -Version 5.1
#Requires -RunAsAdministrator
param(
    [string]$EnvFile = "C:\visa2026\env\demo.env",
    [string]$PostgresPassword = "",
    [string]$DatabaseName = "visa2026_demo",
    [string]$PostgresUser = "postgres",
    [int]$Port = 5432,
    [string]$InstallerUrl = "https://get.enterprisedb.com/postgresql/postgresql-16.9-1-windows-x64.exe",
    [string]$InstallerPath = "C:\visa2026\downloads\postgresql-16-windows-x64.exe"
)

$ErrorActionPreference = "Stop"

function Read-DotEnvMap([string]$Path) {
    $map = @{}
    if (-not (Test-Path -LiteralPath $Path)) { return $map }
    Get-Content -LiteralPath $Path -Encoding UTF8 | ForEach-Object {
        $line = $_.Trim()
        if ($line -eq "" -or $line.StartsWith("#")) { return }
        $i = $line.IndexOf("=")
        if ($i -lt 1) { return }
        $map[$line.Substring(0, $i).Trim()] = $line.Substring($i + 1).Trim()
    }
    return $map
}

function Find-Psql {
    $roots = @(
        "C:\PostgreSQL",
        "C:\Program Files\PostgreSQL"
    )
    foreach ($root in $roots) {
        if (-not (Test-Path -LiteralPath $root)) { continue }
        $hit = Get-ChildItem $root -Recurse -Filter psql.exe -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending |
            Select-Object -First 1 -ExpandProperty FullName
        if ($hit) { return $hit }
    }
    return $null
}

$envMap = Read-DotEnvMap $EnvFile
if ([string]::IsNullOrWhiteSpace($PostgresPassword)) {
    if ($envMap.ContainsKey("PG_PASSWORD") -and $envMap["PG_PASSWORD"]) {
        $PostgresPassword = $envMap["PG_PASSWORD"]
    }
    elseif ($envMap.ContainsKey("SA_PASSWORD") -and $envMap["SA_PASSWORD"]) {
        $PostgresPassword = $envMap["SA_PASSWORD"]
    }
}
if ([string]::IsNullOrWhiteSpace($PostgresPassword)) {
    throw "Set -PostgresPassword or PG_PASSWORD/SA_PASSWORD in $EnvFile"
}
if ($envMap.ContainsKey("DB_NAME") -and $envMap["DB_NAME"]) {
    $DatabaseName = $envMap["DB_NAME"]
}
if ($envMap.ContainsKey("PG_USER") -and $envMap["PG_USER"]) {
    $PostgresUser = $envMap["PG_USER"]
}
if ($envMap.ContainsKey("PG_PORT") -and $envMap["PG_PORT"]) {
    [int]$Port = $envMap["PG_PORT"]
}

$psql = Find-Psql
if (-not $psql) {
    Write-Host "PostgreSQL not found - downloading EDB installer..." -ForegroundColor Cyan
    $dlDir = Split-Path -Parent $InstallerPath
    if (-not (Test-Path -LiteralPath $dlDir)) {
        New-Item -ItemType Directory -Force -Path $dlDir | Out-Null
    }
    if (-not (Test-Path -LiteralPath $InstallerPath)) {
        Invoke-WebRequest -Uri $InstallerUrl -OutFile $InstallerPath -UseBasicParsing
    }
    if (-not (Test-Path -LiteralPath $InstallerPath)) {
        throw "Installer missing: $InstallerPath"
    }

    Write-Host "Running unattended PostgreSQL install (port $Port)..." -ForegroundColor Cyan
    $installArgs = @(
        "--mode", "unattended",
        "--unattendedmodeui", "none",
        "--superaccount", $PostgresUser,
        "--superpassword", $PostgresPassword,
        "--servicename", "postgresql-x64-16",
        "--serviceaccount", "NT AUTHORITY\NetworkService",
        "--serverport", "$Port",
        "--locale", "English, United States",
        "--disable-components", "stackbuilder"
    )
    $p = Start-Process -FilePath $InstallerPath -ArgumentList $installArgs -Wait -PassThru
    if ($p.ExitCode -ne 0) {
        throw "PostgreSQL installer exited $($p.ExitCode)"
    }

    $psql = Find-Psql
    if (-not $psql) { throw "psql.exe not found after install under C:\PostgreSQL or C:\Program Files\PostgreSQL" }
}

Write-Host "Using psql: $psql" -ForegroundColor Green
$createdb = Join-Path (Split-Path -Parent $psql) "createdb.exe"

$env:PGPASSWORD = $PostgresPassword
try {
    $svc = Get-Service -Name "postgresql*" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($svc -and $svc.Status -ne "Running") {
        Start-Service $svc.Name
        Start-Sleep -Seconds 3
    }

    $escapedDb = $DatabaseName.Replace("'", "''")
    $exists = & $psql -h localhost -p $Port -U $PostgresUser -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='$escapedDb'"
    if (("" + $exists).Trim() -ne "1") {
        Write-Host "Creating database $DatabaseName..." -ForegroundColor Cyan
        if (-not (Test-Path -LiteralPath $createdb)) { throw "createdb.exe not found next to psql" }
        & $createdb -h localhost -p $Port -U $PostgresUser -E UTF8 $DatabaseName
        if ($LASTEXITCODE -ne 0) { throw "createdb failed (exit $LASTEXITCODE)" }
    }
    else {
        Write-Host "Database $DatabaseName already exists." -ForegroundColor Yellow
    }

    & $psql -h localhost -p $Port -U $PostgresUser -d $DatabaseName -c "SELECT version();" | Out-Host
}
finally {
    Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "PostgreSQL ready for Demo." -ForegroundColor Green
Write-Host "Next: Configure-Visa2026Production.ps1 -Profile Demo, deploy + ForceUpdate."