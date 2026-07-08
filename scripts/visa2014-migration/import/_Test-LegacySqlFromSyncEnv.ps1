#Requires -Version 5.1
param(
    [string]$SyncEnvPath = 'C:\visa2026-sync\config\sync.env'
)

$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $SyncEnvPath)) {
    Write-Output "FAIL missing $SyncEnvPath"
    exit 1
}

$password = $null
[System.IO.File]::ReadAllText($SyncEnvPath) -split "`r?`n" | ForEach-Object {
    $line = $_.Trim()
    if ($line -match '^VISA2014_SQL_PASSWORD=(.*)$') {
        $password = $Matches[1]
    }
}

if ([string]::IsNullOrWhiteSpace($password)) {
    Write-Output 'FAIL VISA2014_SQL_PASSWORD empty in sync.env'
    exit 1
}

$cs = "Server=10.100.128.15;Database=VISA2015;User Id=ReadOnlyUser;Password=$password;TrustServerCertificate=True;Connect Timeout=15"
try {
    $c = New-Object System.Data.SqlClient.SqlConnection $cs
    $c.Open()
    $cmd = $c.CreateCommand()
    $cmd.CommandText = 'SELECT DB_NAME(), (SELECT COUNT(*) FROM Person)'
    $r = $cmd.ExecuteReader()
    if ($r.Read()) {
        Write-Output ("OK db={0} personCount={1}" -f $r.GetString(0), $r.GetInt32(1))
    }
    $r.Close()
    $c.Close()
    exit 0
}
catch {
    Write-Output ("FAIL {0}" -f $_.Exception.Message)
    exit 1
}