#Requires -Version 5.1
param(
    [ValidateSet("Production", "Staging", "Demo")]
    [string]$Profile = "Production"
)
$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "Visa2026-IisSlots.ps1")
$ctx = Resolve-Visa2026IisSlotContext -Profile $Profile
function Read-DotEnvMap([string]$Path) {
    $map = @{}
    Get-Content -LiteralPath $Path | ForEach-Object {
        $line = $_.Trim()
        if ($line -match '^\s*#' -or $line -eq "") { return }
        if ($line -match '^\s*([^#=]+)=(.*)$') {
            $k = $matches[1].Trim()
            $v = $matches[2].Trim().Trim('"')
            $map[$k] = $v
        }
    }
    $map
}
$envMap = Read-DotEnvMap $ctx.EnvFile
$db = if ($envMap["DB_NAME"]) { $envMap["DB_NAME"] } else { $ctx.DbName }
$pwd = $envMap["SA_PASSWORD"]
if (-not $pwd) { throw "SA_PASSWORD missing in $($ctx.EnvFile)" }
Add-Type -AssemblyName System.Data
$cs = "Server=localhost\SQLEXPRESS;Database=$db;User ID=sa;Password=$pwd;TrustServerCertificate=True;Encrypt=False;"
$queries = @(
    @{ Title = "Database size (MB)"; Sql = "SELECT CAST(SUM(size) * 8.0 / 1024 AS decimal(18,1)) AS SizeMb FROM sys.database_files;" },
    @{ Title = "Application / progress row counts"; Sql = "SELECT (SELECT COUNT(*) FROM dbo.Applications WHERE GCRecord IS NULL OR GCRecord = 0) AS ActiveApplications, (SELECT COUNT(*) FROM dbo.ApplicationProgresses WHERE GCRecord IS NULL OR GCRecord = 0) AS ActiveProgressRows, CAST((SELECT COUNT(*) * 1.0 / NULLIF((SELECT COUNT(*) FROM dbo.Applications WHERE GCRecord IS NULL OR GCRecord = 0), 0) FROM dbo.ApplicationProgresses WHERE GCRecord IS NULL OR GCRecord = 0) AS decimal(10,2)) AS AvgProgressRowsPerApplication;" },
    @{ Title = "Applications by route"; Sql = "SELECT at.ApplicationProgressRoute, COUNT(*) AS ApplicationCount FROM dbo.Applications a INNER JOIN dbo.ApplicationTypes at ON at.ID = a.ApplicationTypeID WHERE a.GCRecord IS NULL OR a.GCRecord = 0 GROUP BY at.ApplicationProgressRoute ORDER BY ApplicationCount DESC;" },
    @{ Title = "Applications GCRecord buckets"; Sql = "SELECT CASE WHEN GCRecord IS NULL THEN 'NULL' WHEN GCRecord = 0 THEN 'ZERO' ELSE 'OTHER' END AS GcBucket, COUNT(*) AS Cnt FROM dbo.Applications GROUP BY CASE WHEN GCRecord IS NULL THEN 'NULL' WHEN GCRecord = 0 THEN 'ZERO' ELSE 'OTHER' END ORDER BY Cnt DESC;" },
    @{ Title = "Latest progress denormalization"; Sql = "SELECT COUNT(*) AS TotalActiveApplications, SUM(CASE WHEN LatestProgressId IS NOT NULL THEN 1 ELSE 0 END) AS WithLatestProgressId, SUM(CASE WHEN LatestProgressDisplay IS NOT NULL AND LatestProgressDisplay <> '' THEN 1 ELSE 0 END) AS WithLatestProgressDisplay FROM dbo.Applications WHERE GCRecord IS NULL OR GCRecord = 0;" },
    @{ Title = "List performance indexes"; Sql = "SELECT t.name AS TableName, i.name AS IndexName FROM sys.tables t INNER JOIN sys.indexes i ON i.object_id = t.object_id WHERE t.name IN (N'Applications', N'ApplicationProgresses', N'ApplicationApprovalLegSnapshots') AND i.name IN (N'IX_ApplicationProgresses_ApplicationID_ProgressOrder', N'IX_Applications_ApplicationTypeID_List', N'IX_ApplicationApprovalLegSnapshots_ApplicationId') ORDER BY t.name, i.name;" },
    @{ Title = "Largest tables (top 10)"; Sql = "SELECT TOP 10 s.name AS SchemaName, t.name AS TableName, SUM(p.rows) AS ApproxRows FROM sys.tables t INNER JOIN sys.schemas s ON s.schema_id = t.schema_id INNER JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id IN (0, 1) GROUP BY s.name, t.name ORDER BY ApproxRows DESC;" }
)
Write-Host "==> Application ListView performance diagnostics ($Profile / $db)" -ForegroundColor Cyan
$connection = New-Object System.Data.SqlClient.SqlConnection $cs
$connection.Open()
try {
    foreach ($q in $queries) {
        Write-Host "`n--- $($q.Title) ---" -ForegroundColor Yellow
        $command = $connection.CreateCommand()
        $command.CommandText = $q.Sql
        $command.CommandTimeout = 120
        $adapter = New-Object System.Data.SqlClient.SqlDataAdapter $command
        $table = New-Object System.Data.DataTable
        [void]$adapter.Fill($table)
        if ($table.Rows.Count -eq 0) { Write-Host "(no rows)"; continue }
        $table | Format-Table -AutoSize | Out-String | Write-Host
    }
}
finally { $connection.Close() }