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
/// Hosted batch workers start with the web host before XAF <see cref="XafApplication.CheckCompatibility"/>
/// may create new tables. Waits until <c>PdfGenerationBatches</c> and <c>WordReportGenerationBatches</c> exist.
/// </summary>
internal static class BatchWorkerSchemaGate
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

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

            if (BatchTablesExist(scope.ServiceProvider))
                return;

            logger.LogInformation(
                "Batch worker waiting for PdfGenerationBatches / WordReportGenerationBatches (XAF database update).");
            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }
    }

    public static bool IsMissingBatchTableException(Exception ex)
    {
        for (Exception? current = ex; current != null; current = current.InnerException)
        {
            if (current is SqlException sql && sql.Number == 208)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Lightweight existence check — avoids EF queries that log SqlException 208 while schema is catching up.
    /// </summary>
    private static bool BatchTablesExist(IServiceProvider services)
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
                     AND OBJECT_ID(N'dbo.WordReportGenerationBatches', N'U') IS NOT NULL
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
}
