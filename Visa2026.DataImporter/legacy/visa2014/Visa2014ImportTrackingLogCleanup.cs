using Microsoft.Data.SqlClient;
using Visa2026.Module.DatabaseUpdate;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Clears migration tracking artifacts after a successful <c>--import-visa2014</c> entity run.
/// Does not delete id-map JSON files.
/// </summary>
internal static class Visa2014ImportTrackingLogCleanup
{
    public sealed record CleanupResult
    {
        public int ImportLogFilesDeleted { get; init; }
        public int DataImporterSessionLogsDeleted { get; init; }
        public int AuditRowsDeleted { get; init; }
        public int RuntimeLogRowsDeleted { get; init; }
        public int OrphanWeakReferenceRowsDeleted { get; init; }
        public bool SkippedTargetDatabase { get; init; }
    }

    public static async Task<CleanupResult> ClearAfterSuccessfulImportAsync(
        string? dataImporterRoot,
        string? targetConnectionString,
        DateTime sessionStartedUtc,
        bool verbose,
        CancellationToken cancellationToken = default)
    {
        var result = new CleanupResult
        {
            ImportLogFilesDeleted = ClearImportLogDirectory(dataImporterRoot),
            DataImporterSessionLogsDeleted = ClearDataImporterSessionLogs(AppContext.BaseDirectory),
        };

        if (string.IsNullOrWhiteSpace(targetConnectionString))
        {
            if (verbose)
                Console.WriteLine("INF Import log cleanup: skipped target DB (no --target-connection / ConnectionStrings__DefaultConnection).");
            return result with { SkippedTargetDatabase = true };
        }

        if (DatabaseProviderDetector.IsPostgreSql(targetConnectionString))
        {
            if (verbose)
                Console.WriteLine("INF Import log cleanup: skipped target DB (PostgreSQL — T-SQL OBJECT_ID cleanup not used).");
            return result with { SkippedTargetDatabase = true };
        }

        try
        {
            var dbCounts = await ClearTargetDatabaseTrackingAsync(
                targetConnectionString,
                sessionStartedUtc,
                cancellationToken);
            result = result with
            {
                AuditRowsDeleted = dbCounts.AuditRowsDeleted,
                RuntimeLogRowsDeleted = dbCounts.RuntimeLogRowsDeleted,
                OrphanWeakReferenceRowsDeleted = dbCounts.OrphanWeakReferenceRowsDeleted,
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WRN Import log cleanup: target DB cleanup failed — {ex.Message}");
            if (verbose)
                Console.WriteLine(ex);
        }

        if (verbose || result.ImportLogFilesDeleted + result.DataImporterSessionLogsDeleted > 0
            || result.AuditRowsDeleted + result.RuntimeLogRowsDeleted > 0)
        {
            Console.WriteLine(
                $"INF Import tracking logs cleared: import-logs={result.ImportLogFilesDeleted}, " +
                $"session={result.DataImporterSessionLogsDeleted}, auditRows={result.AuditRowsDeleted}, " +
                $"runtimeLogRows={result.RuntimeLogRowsDeleted}, orphanWeakRefs={result.OrphanWeakReferenceRowsDeleted}");
        }

        return result;
    }

    public static int ClearDataImporterSessionLogs(string baseDirectory)
    {
        if (!Directory.Exists(baseDirectory))
            return 0;

        int deleted = 0;
        foreach (var path in Directory.EnumerateFiles(baseDirectory, "import_*.log"))
        {
            try
            {
                File.Delete(path);
                deleted++;
            }
            catch (IOException)
            {
                // File may still be open; caller can retry after Log.Close().
            }
        }

        return deleted;
    }

    private static int ClearImportLogDirectory(string? dataImporterRoot)
    {
        if (string.IsNullOrWhiteSpace(dataImporterRoot))
            return 0;

        var logDir = Visa2014ContentRoot.ImportLogsDirectory(dataImporterRoot);
        if (!Directory.Exists(logDir))
            return 0;

        int deleted = 0;
        foreach (var path in Directory.EnumerateFiles(logDir, "*.log"))
        {
            try
            {
                File.Delete(path);
                deleted++;
            }
            catch (IOException)
            {
                // Log still held open by a live console/Tee writer; it will be cleared next run.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        return deleted;
    }

    private static async Task<(int AuditRowsDeleted, int RuntimeLogRowsDeleted, int OrphanWeakReferenceRowsDeleted)>
        ClearTargetDatabaseTrackingAsync(
            string targetConnectionString,
            DateTime sessionStartedUtc,
            CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(targetConnectionString);
        await connection.OpenAsync(cancellationToken);

        // Audit is suppressed during import, so these normally delete nothing — they are a
        // defensive sweep for rows written before suppression takes effect. Table names match
        // the DevExpress EF Core audit schema (AuditData) and the app runtime log (ApplicationRuntimeLogs).
        var auditDeleted = await ExecuteDeleteIfTableExistsAsync(
            connection,
            "AuditData",
            "DELETE FROM AuditData WHERE ModifiedOn >= @sessionStart",
            sessionStartedUtc,
            cancellationToken);

        var runtimeDeleted = await ExecuteDeleteIfTableExistsAsync(
            connection,
            "ApplicationRuntimeLogs",
            "DELETE FROM ApplicationRuntimeLogs WHERE OccurredAtUtc >= @sessionStart",
            sessionStartedUtc,
            cancellationToken);

        var orphanWeakRefsDeleted = 0;

        return (auditDeleted, runtimeDeleted, orphanWeakRefsDeleted);
    }

    private static async Task<int> ExecuteDeleteIfTableExistsAsync(
        SqlConnection connection,
        string tableName,
        string sql,
        DateTime sessionStartedUtc,
        CancellationToken cancellationToken)
    {
        await using var existsCommand = new SqlCommand(
            "SELECT CASE WHEN OBJECT_ID(@table, 'U') IS NULL THEN 0 ELSE 1 END",
            connection);
        existsCommand.Parameters.AddWithValue("@table", tableName);
        var exists = (int)(await existsCommand.ExecuteScalarAsync(cancellationToken) ?? 0) == 1;
        if (!exists)
            return 0;

        await using var command = new SqlCommand(sql, connection)
        {
            CommandTimeout = 180,
        };
        if (sql.Contains("@sessionStart", StringComparison.Ordinal))
            command.Parameters.AddWithValue("@sessionStart", sessionStartedUtc);

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
