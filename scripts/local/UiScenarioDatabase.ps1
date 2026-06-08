#Requires -Version 5.1
<#
  Shared LocalDB helpers for UI scenario runs (Visa2026UiScenario).
  Dot-sourced by Reset-UiScenarioDatabase.ps1 and Invoke-UiScenarioRun.ps1.
#>

$script:UiScenarioDatabaseName = 'Visa2026UiScenario'
$script:UiScenarioServerInstance = '(localdb)\mssqllocaldb'
$script:UiScenarioBaselineRelativePath = 'tools/UiScenarioRunner/baseline/Visa2026UiScenario-lookup-baseline.bak'

function Get-UiScenarioConnectionString {
    return "Server=$script:UiScenarioServerInstance;Database=$script:UiScenarioDatabaseName;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}

function Get-UiScenarioBaselinePath {
    param([string]$RepoRoot)

    $relative = $script:UiScenarioBaselineRelativePath
    if ([System.IO.Path]::IsPathRooted($relative)) {
        return $relative
    }
    return Join-Path $RepoRoot $relative
}

function Ensure-UiScenarioLocalDb {
    sqllocaldb create MSSQLLocalDB 2>$null
    sqllocaldb start MSSQLLocalDB | Out-Null
}

function Invoke-UiScenarioSqlNonQuery {
    param(
        [string]$Sql,
        [string]$Database = 'master',
        [int]$TimeoutSeconds = 0
    )

    $connString = "Server=$script:UiScenarioServerInstance;Database=$Database;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
    $cn = New-Object System.Data.SqlClient.SqlConnection($connString)
    $cn.Open()
    try {
        $cmd = $cn.CreateCommand()
        $cmd.CommandText = $Sql
        if ($TimeoutSeconds -gt 0) {
            $cmd.CommandTimeout = $TimeoutSeconds
        }
        $null = $cmd.ExecuteNonQuery()
    }
    finally {
        $cn.Close()
    }
}

function Test-UiScenarioDatabaseExists {
    $db = $script:UiScenarioDatabaseName.Replace("'", "''")
    $sql = "SELECT CASE WHEN DB_ID(N'$db') IS NULL THEN 0 ELSE 1 END;"
    $connString = "Server=$script:UiScenarioServerInstance;Database=master;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
    $cn = New-Object System.Data.SqlClient.SqlConnection($connString)
    $cn.Open()
    try {
        $cmd = $cn.CreateCommand()
        $cmd.CommandText = $sql
        return [int]$cmd.ExecuteScalar() -eq 1
    }
    finally {
        $cn.Close()
    }
}

function Ensure-UiScenarioDatabaseCreated {
    if (Test-UiScenarioDatabaseExists) {
        return
    }

    $db = $script:UiScenarioDatabaseName.Replace("'", "''")
    $dbBracket = '[' + ($script:UiScenarioDatabaseName -replace '\]', ']]') + ']'
    $sql = @"
IF DB_ID(N'$db') IS NULL
BEGIN
    CREATE DATABASE $dbBracket;
END
"@

    Write-Host "Creating empty database $($script:UiScenarioDatabaseName)..." -ForegroundColor Cyan
    Invoke-UiScenarioSqlNonQuery -Sql $sql
}

function Remove-UiScenarioDatabase {
    if (-not (Test-UiScenarioDatabaseExists)) {
        Write-Host "Database $($script:UiScenarioDatabaseName) does not exist - nothing to drop." -ForegroundColor DarkGray
        return
    }

    $db = $script:UiScenarioDatabaseName.Replace("'", "''")
    $dbBracket = '[' + ($script:UiScenarioDatabaseName -replace '\]', ']]') + ']'
    $sql = @"
IF DB_ID(N'$db') IS NOT NULL
BEGIN
    ALTER DATABASE $dbBracket SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE $dbBracket;
END
"@

    Write-Host "Dropping database $($script:UiScenarioDatabaseName)..." -ForegroundColor Yellow
    Invoke-UiScenarioSqlNonQuery -Sql $sql
    Write-Host "Database dropped." -ForegroundColor Green
}

function Invoke-UiScenarioGreenfieldSeed {
    param(
        [string]$RepoRoot,
        [ValidateSet('Debug', 'Release')]
        [string]$Configuration = 'Debug',

        [switch]$SkipBuild
    )

    $resetScript = Join-Path $RepoRoot 'scripts/local/Reset-UiScenarioDatabase.ps1'
    & $resetScript -Mode DropOnly
    if ($LASTEXITCODE -ne 0) {
        throw "DropOnly failed (exit $LASTEXITCODE)."
    }

    Ensure-UiScenarioDatabaseCreated

    $updateArgs = @{
        Profile       = 'UiScenario'
        Configuration = $Configuration
    }
    if ($SkipBuild) { $updateArgs['SkipBuild'] = $true }

    Write-Host '==> Greenfield seed via XAF --updateDatabase (LookupCatalogs + security users)' -ForegroundColor Cyan
    & (Join-Path $RepoRoot 'scripts/local/Update-LocalDatabase.ps1') @updateArgs
    if ($LASTEXITCODE -ne 0 -and $LASTEXITCODE -ne 2) {
        throw "Greenfield database update failed (exit $LASTEXITCODE)."
    }
}

function Backup-UiScenarioDatabase {
    param(
        [string]$BackupFile
    )

    if (-not (Test-UiScenarioDatabaseExists)) {
        throw "Cannot backup - database $($script:UiScenarioDatabaseName) does not exist."
    }

    $dir = Split-Path -Parent $BackupFile
    if (-not (Test-Path -LiteralPath $dir)) {
        New-Item -ItemType Directory -Force -Path $dir | Out-Null
    }

    $backupPath = [System.IO.Path]::GetFullPath($BackupFile)
    $sqlPath = $backupPath.Replace("'", "''")
    $dbBracket = '[' + ($script:UiScenarioDatabaseName -replace '\]', ']]') + ']'
    $sql = @"
BACKUP DATABASE $dbBracket
  TO DISK = N'$sqlPath'
  WITH INIT, COPY_ONLY, FORMAT;
"@

    Write-Host "Backing up $($script:UiScenarioDatabaseName) -> $backupPath" -ForegroundColor Cyan
    Invoke-UiScenarioSqlNonQuery -Sql $sql -TimeoutSeconds 0
    Write-Host 'Baseline backup complete.' -ForegroundColor Green
}
