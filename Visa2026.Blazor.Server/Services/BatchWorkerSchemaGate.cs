using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Visa2026.Module.BusinessObjects;

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
            var osFactory = scope.ServiceProvider.GetRequiredService<INonSecuredObjectSpaceFactory>();

            if (BatchTablesExist(osFactory))
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

    private static bool BatchTablesExist(INonSecuredObjectSpaceFactory osFactory)
    {
        try
        {
            using (var pdfOs = osFactory.CreateNonSecuredObjectSpace<PdfGenerationBatch>())
                _ = pdfOs.GetObjectsQuery<PdfGenerationBatch>().Take(1).ToList();

            using (var wordOs = osFactory.CreateNonSecuredObjectSpace<WordReportGenerationBatch>())
                _ = wordOs.GetObjectsQuery<WordReportGenerationBatch>().Take(1).ToList();

            return true;
        }
        catch (SqlException ex) when (ex.Number == 208)
        {
            return false;
        }
    }
}
