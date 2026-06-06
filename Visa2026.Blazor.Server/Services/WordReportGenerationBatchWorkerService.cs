using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.WordReports;

namespace Visa2026.Blazor.Server.Services;

public sealed class WordReportGenerationBatchWorkerService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    private readonly IServiceScopeFactory scopeFactory;
    private readonly XafApplicationHolder appHolder;
    private readonly ILogger<WordReportGenerationBatchWorkerService> logger;

    public WordReportGenerationBatchWorkerService(
        IServiceScopeFactory scopeFactory,
        XafApplicationHolder appHolder,
        ILogger<WordReportGenerationBatchWorkerService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.appHolder = appHolder;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("WordReportGenerationBatchWorkerService is starting.");
        await BatchWorkerSchemaGate.WaitForBatchTablesAsync(scopeFactory, appHolder, logger, stoppingToken)
            .ConfigureAwait(false);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOneBatchAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // shutting down
            }
            catch (Exception ex) when (BatchWorkerSchemaGate.IsMissingBatchTableException(ex)
                                       || BatchWorkerSchemaGate.IsMissingBatchColumnException(ex))
            {
                logger.LogWarning("WordReportGenerationBatchWorkerService: batch tables not ready yet; retrying.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "WordReportGenerationBatchWorkerService loop error.");
            }

            await Task.Delay(PollInterval, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task ProcessOneBatchAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var osFactory = scope.ServiceProvider.GetRequiredService<INonSecuredObjectSpaceFactory>();
        var bundleBuilder = scope.ServiceProvider.GetRequiredService<IWordReportBundleBuilder>();

        using var os = osFactory.CreateNonSecuredObjectSpace<WordReportGenerationBatch>();

        var batch = os.GetObjectsQuery<WordReportGenerationBatch>()
            .Where(b => b.Status == WordReportGenerationBatchStatus.Queued)
            .OrderBy(b => b.CreatedOnUtc)
            .FirstOrDefault();

        if (batch == null)
            return;

        logger.LogInformation(
            "Picked queued Resminamalar batch. BatchId={BatchId} ApplicationId={ApplicationId} RequestedBy={RequestedBy}",
            os.GetKeyValue(batch),
            batch.ApplicationID,
            batch.RequestedBy);

        batch.Status = WordReportGenerationBatchStatus.Running;
        batch.ErrorMessage = null;
        batch.ProcessedReports = 0;
        os.CommitChanges();

        try
        {
            if (!batch.ApplicationID.HasValue)
                throw new InvalidOperationException("Resminamalar batch has no ApplicationID.");

            var applicationId = batch.ApplicationID.Value;
            var application = os.GetObjectsQuery<Application>()
                .Include(a => a.ApplicationType)
                .Include(a => a.ProjectContract)
                .Include(a => a.ApplicationItems)
                .FirstOrDefault(a => a.ID == applicationId);

            if (application == null)
                throw new InvalidOperationException($"Application {applicationId} was not found or is deleted.");

            using var zipStream = new MemoryStream();
            var selectedEntryKeys = ApplicationWordReportPackageSelectionHelper.Deserialize(batch.SelectedReportKeysJson);
            var result = await bundleBuilder
                .BuildZipAsync(application, os, zipStream, selectedEntryKeys, stoppingToken)
                .ConfigureAwait(false);

            batch.TotalReports = result.ReportCount;
            batch.ProcessedReports = result.ReportCount;

            batch.ZipFile ??= os.CreateObject<FileData>();
            zipStream.Position = 0;
            batch.ZipFile.LoadFromStream(result.ZipFileName, zipStream);

            batch.Status = WordReportGenerationBatchStatus.Completed;
            os.CommitChanges();

            logger.LogInformation(
                "Completed Resminamalar batch. BatchId={BatchId} Reports={ReportCount} ZipSize={ZipSize}",
                os.GetKeyValue(batch),
                result.ReportCount,
                batch.ZipFile?.Size);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Resminamalar batch failed. BatchId={BatchId}", os.GetKeyValue(batch));
            batch.Status = WordReportGenerationBatchStatus.Failed;
            batch.ErrorMessage = ex.Message;
            os.CommitChanges();
        }
    }
}
