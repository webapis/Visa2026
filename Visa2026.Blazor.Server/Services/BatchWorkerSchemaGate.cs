using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Visa2026.Blazor.Server.Services;

/// <summary>
/// Hosted batch workers and batch APIs can run before XAF <see cref="XafApplication.CheckCompatibility"/>
/// finishes. Ensures batch tables and additive columns exist via idempotent SQL (mirrors ModuleUpdaters).
/// </summary>
internal static class BatchWorkerSchemaGate
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private static int schemaEnsured;

    public static void EnsureBatchSchemaColumns(IServiceProvider services, ILogger? logger = null)
    {
        if (Interlocked.Exchange(ref schemaEnsured, 1) == 1)
            return;

        var connectionString = services.GetService<IConfiguration>()?.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Interlocked.Exchange(ref schemaEnsured, 0);
            return;
        }

        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            EnsureColumn(connection, "PdfGenerationBatches", "RequestedCulture", "nvarchar(10) NULL");
            EnsureColumn(connection, "WordReportGenerationBatches", "SelectedReportKeysJson", "nvarchar(max) NULL");
            EnsureColumn(connection, "WordReportGenerationBatches", "SelectedApplicationItemIdsJson", "nvarchar(max) NULL");

            logger?.LogInformation("Batch schema columns verified (PdfGenerationBatches / WordReportGenerationBatches).");
        }
        catch (Exception ex)
        {
            Interlocked.Exchange(ref schemaEnsured, 0);
            logger?.LogWarning(ex, "Batch schema column ensure failed; will retry on next wait cycle.");
        }
    }

    public static async Task WaitForBatchTablesAsync(
        IServiceScopeFactory scopeFactory,
        XafApplicationHolder appHolder,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        while (appHolder.Application == null && !cancellationToken.IsCancellationRequested)
            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);

        while (!cancellationToken.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            EnsureBatchSchemaColumns(scope.ServiceProvider, logger);

            if (BatchSchemaReady(scope.ServiceProvider))
                return;

            logger.LogInformation(
                "Batch worker waiting for PdfGenerationBatches / WordReportGenerationBatches schema (XAF database update).");
            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }
    }

    public static bool IsMissingBatchTableException(Exception ex) => ContainsSqlError(ex, 208);

    public static bool IsMissingBatchColumnException(Exception ex) => ContainsSqlError(ex, 207);

    private static bool ContainsSqlError(Exception ex, int errorNumber)
    {
        for (Exception? current = ex; current != null; current = current.InnerException)
        {
            if (current is SqlException sql && sql.Number == errorNumber)
                return true;
        }

        return false;
    }

    private static bool BatchSchemaReady(IServiceProvider services)
    {
        var connectionString = services.GetService<IConfiguration>()?.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            return false;

        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT CASE
                    WHEN OBJECT_ID(N'dbo.PdfGenerationBatches', N'U') IS NOT NULL
                     AND COL_LENGTH(N'dbo.PdfGenerationBatches', N'RequestedCulture') IS NOT NULL
                     AND OBJECT_ID(N'dbo.WordReportGenerationBatches', N'U') IS NOT NULL
                     AND COL_LENGTH(N'dbo.WordReportGenerationBatches', N'SelectedReportKeysJson') IS NOT NULL
                     AND COL_LENGTH(N'dbo.WordReportGenerationBatches', N'SelectedApplicationItemIdsJson') IS NOT NULL
                    THEN 1 ELSE 0 END
                """;
            var scalar = command.ExecuteScalar();
            return Convert.ToInt32(scalar, CultureInfo.InvariantCulture) == 1;
        }
        catch (SqlException)
        {
            return false;
        }
    }

    private static void EnsureColumn(SqlConnection connection, string tableName, string columnName, string columnDefinition)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $@"
IF OBJECT_ID(N'dbo.{tableName}', N'U') IS NULL
    RETURN;

IF COL_LENGTH(N'dbo.{tableName}', N'{columnName}') IS NULL
    ALTER TABLE dbo.{tableName} ADD {columnName} {columnDefinition};";
        command.ExecuteNonQuery();
    }
}
