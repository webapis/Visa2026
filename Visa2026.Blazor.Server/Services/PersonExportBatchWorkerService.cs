using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.PersonDossier;
using Visa2026.Module.Services.RuntimeLogging;

namespace Visa2026.Blazor.Server.Services;

/// <summary>
/// Builds queued director hand-over exports (see <c>docs/PERSON_DOSSIER.md</c> phase 4).
/// Mirrors <see cref="WordReportGenerationBatchWorkerService"/>: poll for a queued row, build the
/// ZIP, store it on the batch as <c>FileData</c>.
/// </summary>
public sealed class PersonExportBatchWorkerService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    private readonly IServiceScopeFactory scopeFactory;
    private readonly XafApplicationHolder appHolder;
    private readonly ILogger<PersonExportBatchWorkerService> logger;

    public PersonExportBatchWorkerService(
        IServiceScopeFactory scopeFactory,
        XafApplicationHolder appHolder,
        ILogger<PersonExportBatchWorkerService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.appHolder = appHolder;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("PersonExportBatchWorkerService is starting.");
        await BatchWorkerSchemaGate.WaitForBatchTablesAsync(scopeFactory, appHolder, logger, stoppingToken)
            .ConfigureAwait(false);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                ProcessOneBatch();
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // shutting down
            }
            catch (Exception ex) when (BatchWorkerSchemaGate.IsMissingBatchTableException(ex)
                                       || BatchWorkerSchemaGate.IsMissingBatchColumnException(ex))
            {
                logger.LogWarningWithCode(
                    ApplicationRuntimeLogErrorCodes.PersonExportBatchWait,
                    "PersonExportBatchWorkerService: batch tables not ready yet; retrying.");
            }
            catch (Exception ex)
            {
                logger.LogErrorWithCode(
                    ApplicationRuntimeLogErrorCodes.PersonExportWorkerLoop,
                    ex,
                    "PersonExportBatchWorkerService loop error.");
            }

            await Task.Delay(PollInterval, stoppingToken).ConfigureAwait(false);
        }
    }

    private void ProcessOneBatch()
    {
        using var scope = scopeFactory.CreateScope();
        var osFactory = scope.ServiceProvider.GetRequiredService<INonSecuredObjectSpaceFactory>();
        var packer = scope.ServiceProvider.GetRequiredService<PersonExportPacker>();

        using var os = osFactory.CreateNonSecuredObjectSpace<PersonExportBatch>();

        var batch = os.GetObjectsQuery<PersonExportBatch>()
            .Where(b => b.Status == PersonExportBatchStatus.Queued)
            .OrderBy(b => b.CreatedOnUtc)
            .FirstOrDefault();

        if (batch == null)
            return;

        logger.LogInformation(
            "Picked queued person export batch. BatchId={BatchId} PersonId={PersonId} RequestedBy={RequestedBy}",
            os.GetKeyValue(batch),
            batch.PersonID,
            batch.RequestedBy);

        batch.Status = PersonExportBatchStatus.Running;
        batch.ErrorMessage = null;
        batch.ProcessedRecords = 0;
        os.CommitChanges();

        try
        {
            if (!batch.PersonID.HasValue)
                throw new InvalidOperationException("Person export batch has no PersonID.");

            var personId = batch.PersonID.Value;
            var person = os.GetObjectByKey<Person>(personId)
                ?? throw new InvalidOperationException($"Person {personId} was not found or is deleted.");

            using var zipStream = new MemoryStream();
            var result = packer.BuildZip(
                os,
                person,
                batch.RequestedCulture,
                zipStream,
                onProgress: (processed, total) =>
                {
                    batch.ProcessedRecords = processed;
                    batch.TotalRecords = total;
                });

            batch.TotalRecords = result.RecordCount;
            batch.ProcessedRecords = result.RecordCount;
            batch.ExportNotes = result.Notes;

            batch.ZipFile ??= os.CreateObject<FileData>();
            zipStream.Position = 0;
            batch.ZipFile.LoadFromStream(result.ZipFileName, zipStream);

            batch.Status = PersonExportBatchStatus.Completed;
            os.CommitChanges();

            logger.LogInformation(
                "Completed person export batch. BatchId={BatchId} Records={RecordCount} Written={Written} ZipSize={ZipSize}",
                os.GetKeyValue(batch),
                result.RecordCount,
                result.WrittenRecordCount,
                batch.ZipFile?.Size);
        }
        catch (Exception ex)
        {
            logger.LogErrorWithCode(
                ApplicationRuntimeLogErrorCodes.PersonExportBatchFailed,
                ex,
                "Person export batch failed. BatchId={BatchId}",
                os.GetKeyValue(batch));
            batch.Status = PersonExportBatchStatus.Failed;
            batch.ErrorMessage = ex.Message;
            os.CommitChanges();
        }
    }
}
