using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.RuntimeLogging;

namespace Visa2026.Blazor.Server.Services;

public sealed class PdfGenerationBatchWorkerService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory scopeFactory;
    private readonly XafApplicationHolder appHolder;
    private readonly ILogger<PdfGenerationBatchWorkerService> logger;

    public PdfGenerationBatchWorkerService(
        IServiceScopeFactory scopeFactory,
        XafApplicationHolder appHolder,
        ILogger<PdfGenerationBatchWorkerService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.appHolder = appHolder;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("PdfGenerationBatchWorkerService is starting.");
        await BatchWorkerSchemaGate.WaitForBatchTablesAsync(scopeFactory, appHolder, logger, stoppingToken)
            .ConfigureAwait(false);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOneBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // host is shutting down
            }
            catch (Exception ex) when (BatchWorkerSchemaGate.IsMissingBatchTableException(ex))
            {
                logger.LogWarningWithCode(
                    ApplicationRuntimeLogErrorCodes.PdfBatchWait,
                    "PdfGenerationBatchWorkerService: batch tables not ready yet; retrying.");
            }
            catch (Exception ex)
            {
                logger.LogErrorWithCode(
                    ApplicationRuntimeLogErrorCodes.PdfWorkerLoop,
                    ex,
                    "PdfGenerationBatchWorkerService loop error.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ProcessOneBatchAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();

        // Designed for background work (no user principal, no logon required).
        var nonSecuredOsFactory = scope.ServiceProvider.GetRequiredService<INonSecuredObjectSpaceFactory>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var pdfFillerService = scope.ServiceProvider.GetRequiredService<IPdfFormFillerService>();

        using var os = nonSecuredOsFactory.CreateNonSecuredObjectSpace<PdfGenerationBatch>();

        var batch = os.GetObjectsQuery<PdfGenerationBatch>()
            .Where(b => b.Status == PdfGenerationBatchStatus.Queued)
            .OrderBy(b => b.CreatedOnUtc)
            .FirstOrDefault();

        if (batch == null)
            return;

        logger.LogInformation(
            "Picked queued PDF batch. BatchId={BatchId} RequestedBy={RequestedBy} TotalItems={TotalItems}",
            os.GetKeyValue(batch),
            batch.RequestedBy,
            batch.TotalItems);

        batch.Status = PdfGenerationBatchStatus.Running;
        batch.ErrorMessage = null;
        batch.PdfMappingVisibilityNotes = null;
        batch.PdfPackagingNotes = null;
        batch.ProcessedItems = 0;
        os.CommitChanges();

        try
        {
            var keyType = ResolveKeyType(batch.ItemKeyType);
            var (rosterApplicationId, keys) = ParseRosterKeys(batch.ItemKeysJson);
            batch.TotalItems = keys.Count;
            os.CommitChanges();

            string relativeTemplatePath = configuration["PdfSettings:TemplatePath"];
            if (string.IsNullOrWhiteSpace(relativeTemplatePath))
                throw new InvalidOperationException("PdfSettings:TemplatePath is not configured.");

            string templatePath = null;
            string tempTemplatePath = null;
            try
            {
                templatePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relativeTemplatePath));
                if (!File.Exists(templatePath))
                {
                    // Fallback: load embedded resource from Module assembly (same template used by UI controller).
                    var asm = typeof(PdfMappingHelper).Assembly;
                    const string resourceName = "Visa2026.Module.Resources.Visa_Application_TM_QR_08.pdf";
                    using var resStream = asm.GetManifestResourceStream(resourceName);
                    if (resStream == null)
                        throw new FileNotFoundException($"PDF template not found as file or embedded resource: {relativeTemplatePath}", templatePath);

                    tempTemplatePath = Path.Combine(Path.GetTempPath(), $"visa_template_{Guid.NewGuid():N}.pdf");
                    using (var fs = File.Create(tempTemplatePath))
                        resStream.CopyTo(fs);
                    templatePath = tempTemplatePath;
                }
            }
            catch
            {
                try { if (tempTemplatePath != null) File.Delete(tempTemplatePath); } catch { }
                throw;
            }

            var mappings = PdfMappingHelper.GetMappings(os);

            if (!batch.IncludeDiplomaFiles && !batch.IncludePassportCopies && !batch.IncludeVisaCopies
                && !batch.IncludeMedicalRecordCopies && !batch.IncludeAddressOfResidenceCopies
                && !batch.IncludeWorkPermitCopies && !batch.IncludeInvitationCopies && !batch.IncludeFamilyRelationshipCopies)
            {
                logger.LogWarningWithCode(
                    ApplicationRuntimeLogErrorCodes.PdfBatchFlags,
                    "PDF batch {BatchId}: all attachment Include* flags were false (likely uninitialized row). Applying default full package and persisting before ZIP.",
                    os.GetKeyValue(batch));
                batch.IncludeDiplomaFiles = true;
                batch.DiplomaScope = PdfBatchDiplomaScope.AllEducations;
                batch.IncludeMergedDiplomaPdf = false;
                batch.SupportingZipMergeOption = PdfSupportingZipMergeOption.IndividualFilesAndMergedPdfs;
                batch.IncludePassportCopies = true;
                batch.IncludeVisaCopies = true;
                batch.IncludeMedicalRecordCopies = true;
                batch.IncludeAddressOfResidenceCopies = true;
                batch.IncludeWorkPermitCopies = true;
                batch.IncludeInvitationCopies = true;
                batch.IncludeFamilyRelationshipCopies = true;
                os.CommitChanges();
            }

            bool includeMergedPdfs = batch.SupportingZipMergeOption != PdfSupportingZipMergeOption.IndividualFilesOnly;
            bool emitIndividualSupporting = batch.SupportingZipMergeOption != PdfSupportingZipMergeOption.MergedPdfSummariesOnly;

            logger.LogInformation(
                "PDF batch {BatchId} attachment flags: Diploma={Diploma} SupportingZipMerge={ZipMerge} EmitIndividualSupporting={EmitInd} Passport={Passport} Visa={Visa} Medical={Medical} Address={Address} WorkPermit={Wp} Invitation={Inv} Family={Fam}",
                os.GetKeyValue(batch),
                batch.IncludeDiplomaFiles,
                batch.SupportingZipMergeOption,
                emitIndividualSupporting,
                batch.IncludePassportCopies,
                batch.IncludeVisaCopies,
                batch.IncludeMedicalRecordCopies,
                batch.IncludeAddressOfResidenceCopies,
                batch.IncludeWorkPermitCopies,
                batch.IncludeInvitationCopies,
                batch.IncludeFamilyRelationshipCopies);

            string zipName = BuildZipName(os, keys, relativeTemplatePath, rosterApplicationId);
            // Filled PDFs only under PDF_Form/; passport and other attachments use zipInnerRoot null (archive root).
            string filledPdfZipFolder = ApplicationSupportingDocumentsPacker.FilledApplicationFormsZipFolderName;
            string tempZipPath = Path.Combine(Path.GetTempPath(), $"visa_batch_{Guid.NewGuid():N}.zip");

            var visibilityNotesAggregate = new List<string>();
            var packagingNotes = new List<string>();
            Guid? packagingApplicationProfileInstanceId = null;
            var usedZipEntryPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<MemoryStream>? currentPassportPdfMergeSlices = batch.IncludePassportCopies && includeMergedPdfs ? new List<MemoryStream>() : null;
            List<MemoryStream>? currentVisaPdfMergeSlices = batch.IncludeVisaCopies && includeMergedPdfs ? new List<MemoryStream>() : null;
            List<MemoryStream>? currentWorkPermitPdfMergeSlices = batch.IncludeWorkPermitCopies && includeMergedPdfs ? new List<MemoryStream>() : null;
            HashSet<Guid>? workPermitIdsContributedToCurrentBatchMerge = batch.IncludeWorkPermitCopies && includeMergedPdfs ? new HashSet<Guid>() : null;
            List<MemoryStream>? batchDiplomaPdfMergeSlices = batch.IncludeDiplomaFiles && includeMergedPdfs ? new List<MemoryStream>() : null;
            bool flatDiplomaMergedLinePath = batch.SupportingZipMergeOption == PdfSupportingZipMergeOption.MergedPdfSummariesOnly;

            string packagingCulture = PdfPackagingNotesCultureResolver.Resolve(os, batch.RequestedBy, batch.RequestedCulture);
            string packagingTextForBatch = null;
            try
            {
                using (var fs = new FileStream(tempZipPath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read, 64 * 1024, FileOptions.SequentialScan))
                using (var archive = new ZipArchive(fs, ZipArchiveMode.Create, leaveOpen: true))
                {
                    int idx = 1;
                    foreach (var keyString in keys)
                    {
                        stoppingToken.ThrowIfCancellationRequested();

                        var key = ConvertKey(keyType, keyString);
                        var item = LoadPackageLineForPdfBatch(os, keyType, key, rosterApplicationId);
                        if (item == null || item.ApplicationProfileInstance == null)
                            continue;

                        packagingApplicationProfileInstanceId ??= item.ApplicationProfileInstance?.ID;

                        var data = new Dictionary<string, object>();
                        var itemVisibilityNotes = new List<string>();
                        PdfMappingHelper.MapApplicationData(data, item.ApplicationProfileInstance, item, os, null, mappings, itemVisibilityNotes);
                        if (itemVisibilityNotes.Count > 0)
                        {
                            string personLabel = item.Person != null ? item.Person.FullName : "Unknown person";
                            visibilityNotesAggregate.Add($"— Item {idx}: {personLabel} —");
                            visibilityNotesAggregate.AddRange(itemVisibilityNotes);
                        }

                        string personName = item.Person != null ? $"{item.Person.FirstName}_{item.Person.LastName}" : "Unknown";
                        string entryName = $"{filledPdfZipFolder}/{idx:00}_{personName}.pdf";
                        entryName = ApplicationSupportingDocumentsPacker.ReserveZipEntryPath(usedZipEntryPaths, entryName);

                        using var pdfStream = new MemoryStream();
                        pdfFillerService.FillForm(templatePath, pdfStream, data);
                        pdfStream.Position = 0;

                        // ZipArchive allows only one open entry stream at a time; dispose before packing attachments.
                        {
                            var entry = archive.CreateEntry(entryName, CompressionLevel.Fastest);
                            await using (var entryStream = entry.Open())
                            {
                                await pdfStream.CopyToAsync(entryStream, 64 * 1024, stoppingToken).ConfigureAwait(false);
                            }
                        }

                        string itemSlug = ApplicationSupportingDocumentsPacker.BuildItemSlug(idx, item.Person);
                        var diplomaMergeBuffers = batch.IncludeMergedDiplomaPdf && includeMergedPdfs ? new List<MemoryStream>() : null;
                        await ApplicationSupportingDocumentsPacker.AppendSupportingDocumentsForItemAsync(
                            os,
                            batch,
                            archive,
                            usedZipEntryPaths,
                            zipInnerRoot: null,
                            item,
                            idx,
                            diplomaMergeBuffers,
                            currentPassportPdfMergeSlices,
                            currentVisaPdfMergeSlices,
                            currentWorkPermitPdfMergeSlices,
                            workPermitIdsContributedToCurrentBatchMerge,
                            batchDiplomaPdfMergeSlices,
                            emitIndividualSupporting,
                            packagingCulture,
                            packagingNotes,
                            logger,
                            stoppingToken);

                        logger.LogInformation(
                            "PDF batch {BatchId} item {ItemIndex} ({ItemSlug}): ZIP attachment pass finished (search console for \"ZIP packer: Passport\", \"ZIP packer: Visa\", \"ZIP packer: MedicalRecord\", \"ZIP packer: AddressOfResidence\", \"ZIP packer: FamilyRelationship\", \"ZIP packer: WorkPermit\", \"ZIP packer: Invitation\", \"ZIP packer: CurrentPassports\", \"ZIP packer: CurrentVisas\", \"ZIP packer: CurrentWorkPermits\", or \"ZIP packer: AllDiplomas\").",
                            os.GetKeyValue(batch),
                            idx,
                            itemSlug);

                        if (batch.IncludeMergedDiplomaPdf && includeMergedPdfs && diplomaMergeBuffers is { Count: > 0 })
                        {
                            await ApplicationSupportingDocumentsPacker.WriteMergedDiplomaPdfIfNeededAsync(
                                archive,
                                usedZipEntryPaths,
                                zipInnerRoot: null,
                                itemSlug,
                                diplomaMergeBuffers,
                                flatDiplomaMergedLinePath,
                                packagingCulture,
                                packagingNotes,
                                logger,
                                stoppingToken);
                        }

                        batch.ProcessedItems++;
                        os.CommitChanges();
                        idx++;
                    }

                    if (currentPassportPdfMergeSlices is { Count: > 0 })
                    {
                        await ApplicationSupportingDocumentsPacker.WriteMergedCurrentPassportsPdfIfNeededAsync(
                            archive,
                            usedZipEntryPaths,
                            zipInnerRoot: null,
                            currentPassportPdfMergeSlices,
                            packagingCulture,
                            packagingNotes,
                            logger,
                            stoppingToken);
                    }

                    if (currentVisaPdfMergeSlices is { Count: > 0 })
                    {
                        await ApplicationSupportingDocumentsPacker.WriteMergedCurrentVisasPdfIfNeededAsync(
                            archive,
                            usedZipEntryPaths,
                            zipInnerRoot: null,
                            currentVisaPdfMergeSlices,
                            packagingCulture,
                            packagingNotes,
                            logger,
                            stoppingToken);
                    }

                    if (currentWorkPermitPdfMergeSlices is { Count: > 0 })
                    {
                        await ApplicationSupportingDocumentsPacker.WriteMergedCurrentWorkPermitsPdfIfNeededAsync(
                            archive,
                            usedZipEntryPaths,
                            zipInnerRoot: null,
                            currentWorkPermitPdfMergeSlices,
                            packagingCulture,
                            packagingNotes,
                            logger,
                            stoppingToken);
                    }

                    if (batchDiplomaPdfMergeSlices is { Count: > 0 })
                    {
                        await ApplicationSupportingDocumentsPacker.WriteMergedAllDiplomasPdfIfNeededAsync(
                            archive,
                            usedZipEntryPaths,
                            zipInnerRoot: null,
                            batchDiplomaPdfMergeSlices,
                            packagingCulture,
                            packagingNotes,
                            logger,
                            stoppingToken);
                    }

                    packagingTextForBatch = ApplicationSupportingDocumentsPacker.BuildPackagingNotesText(
                        packagingNotes,
                        (Guid)os.GetKeyValue(batch),
                        packagingApplicationProfileInstanceId,
                        DateTime.UtcNow,
                        packagingCulture);
                    await ApplicationSupportingDocumentsPacker.WritePackagingNotesZipEntryAsync(
                        archive,
                        usedZipEntryPaths,
                        zipInnerRoot: null,
                        packagingTextForBatch,
                        logger,
                        stoppingToken).ConfigureAwait(false);
                }

                batch.ZipFile ??= os.CreateObject<DevExpress.Persistent.BaseImpl.EF.FileData>();
                await using (var readStream = new FileStream(tempZipPath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.SequentialScan))
                {
                    batch.ZipFile.LoadFromStream(zipName, readStream);
                }

                batch.Status = PdfGenerationBatchStatus.Completed;
                batch.PdfMappingVisibilityNotes = visibilityNotesAggregate.Count == 0
                    ? null
                    : TruncatePdfNotes(string.Join(Environment.NewLine, visibilityNotesAggregate));
                batch.PdfPackagingNotes = string.IsNullOrEmpty(packagingTextForBatch)
                    ? null
                    : TruncatePdfNotes(packagingTextForBatch);
                os.CommitChanges();

                logger.LogInformation(
                    "Completed PDF batch. BatchId={BatchId} ProcessedItems={ProcessedItems} ZipSize={ZipSize}",
                    os.GetKeyValue(batch),
                    batch.ProcessedItems,
                    batch.ZipFile?.Size);
            }
            finally
            {
                try { File.Delete(tempZipPath); } catch { }
                try { if (tempTemplatePath != null) File.Delete(tempTemplatePath); } catch { }
            }
        }
        catch (Exception ex)
        {
            logger.LogErrorWithCode(
                ResolvePdfBatchErrorCode(ex),
                ex,
                "PDF batch failed. BatchId={BatchId}",
                os.GetKeyValue(batch));
            batch.Status = PdfGenerationBatchStatus.Failed;
            batch.ErrorMessage = ex.Message;
            batch.PdfMappingVisibilityNotes = null;
            batch.PdfPackagingNotes = null;
            os.CommitChanges();
        }
    }

    private static string ResolvePdfBatchErrorCode(Exception ex)
    {
        if (ex is FileNotFoundException)
            return ApplicationRuntimeLogErrorCodes.PdfTemplateMissing;

        if (ex is InvalidOperationException
            && ex.Message.Contains("TemplatePath", StringComparison.OrdinalIgnoreCase))
            return ApplicationRuntimeLogErrorCodes.PdfTemplateMissing;

        return ApplicationRuntimeLogErrorCodes.PdfBatchFailed;
    }

    private static ApplicationRosterMergeLine? LoadPackageLineForPdfBatch(
        IObjectSpace os,
        Type keyType,
        object key,
        Guid applicationProfileInstanceId)
    {
        if (keyType != typeof(Person) && keyType != typeof(Guid))
            throw new InvalidOperationException(
                "This package was queued for application roster rows that no longer exist. " +
                "Request the package again from the application roster.");

        if (key is not Guid personId || personId == Guid.Empty)
            return null;

        if (!ApplicationRosterHelper.TryLoadSharedApplicationPeople(
                os,
                [personId],
                applicationProfileInstanceId,
                out var application,
                out var people)
            || application == null
            || people.Count == 0)
        {
            return null;
        }

        return ApplicationProfileInstancePersonPdfPackageLineHydrator.Hydrate(os, application, people[0]);
    }

    private static (Guid ApplicationId, List<string> Keys) ParseRosterKeys(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return (Guid.Empty, []);

        var trimmed = json.TrimStart();
        if (trimmed.StartsWith('{'))
        {
            var payload = JsonSerializer.Deserialize<PdfBatchRosterKeyPayload>(json);
            return (payload?.ApplicationProfileInstanceId ?? Guid.Empty, payload?.PersonIds ?? []);
        }

        return (Guid.Empty, JsonSerializer.Deserialize<List<string>>(json) ?? []);
    }

    private static string TruncatePdfNotes(string text, int maxChars = 100_000)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxChars)
            return text;
        const string suffix = "\n… (truncated)";
        int take = maxChars - suffix.Length;
        if (take < 1)
            take = maxChars;
        return text.Substring(0, take) + suffix;
    }

    private static Type ResolveKeyType(string assemblyQualifiedName)
    {
        var t = Type.GetType(assemblyQualifiedName, throwOnError: false);
        // ItemKeysJson holds stringified Guid keys. Some enqueue paths incorrectly stored
        // typeof(ApplicationRosterMergeLine); treat entity types as Guid for backward compatibility.
        if (t == null || t == typeof(ApplicationRosterMergeLine))
            return typeof(Guid);
        if (t == typeof(Person))
            return typeof(Person);
        return t;
    }

    private static object ConvertKey(Type keyType, string keyString)
    {
        if (keyType == typeof(Guid) || keyType == typeof(Person))
            return Guid.Parse(keyString);
        if (keyType == typeof(int))
            return int.Parse(keyString, CultureInfo.InvariantCulture);
        if (keyType == typeof(long))
            return long.Parse(keyString, CultureInfo.InvariantCulture);
        if (keyType == typeof(string))
            return keyString;

        return Convert.ChangeType(keyString, keyType, CultureInfo.InvariantCulture);
    }

    private static string BuildZipName(IObjectSpace os, List<string> keyStrings, string relativeTemplatePath, Guid applicationProfileInstanceId)
    {
        // Template hint (file name without extension)
        var templateHint = Path.GetFileNameWithoutExtension(relativeTemplatePath ?? string.Empty);
        templateHint = SanitizeFileNamePart(string.IsNullOrWhiteSpace(templateHint) ? "PDFForm" : templateHint);
        if (templateHint.StartsWith("Visa_", StringComparison.OrdinalIgnoreCase))
            templateHint = SanitizeFileNamePart(templateHint["Visa_".Length..]);

        string appPart = "MULTIAPP";
        string datePart = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        try
        {
            ApplicationProfileInstance? application = null;
            if (applicationProfileInstanceId != Guid.Empty)
                application = os.GetObjectByKey<ApplicationProfileInstance>(applicationProfileInstanceId);

            if (application == null)
            {
                var guids = keyStrings
                    .Select(s => Guid.TryParse(s, out var g) ? (Guid?)g : null)
                    .Where(g => g != null)
                    .Select(g => g!.Value)
                    .ToList();
                if (guids.Count > 0
                    && ApplicationRosterHelper.TryLoadSharedApplicationPeople(os, guids, Guid.Empty, out var shared, out _)
                    && shared != null)
                {
                    application = shared;
                }
            }

            if (application != null)
            {
                appPart = SanitizeFileNamePart(string.IsNullOrWhiteSpace(application.FullApplicationNumber) ? "APP" : application.FullApplicationNumber);
                datePart = application.ApplicationDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
        }
        catch
        {
            // Best-effort only; fall back to MULTIAPP + current date.
        }

        string ts = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        int count = Math.Max(0, keyStrings?.Count ?? 0);
        return $"{appPart}_{datePart}_{templateHint}_{count}items_{ts}.zip";
    }

    private static string SanitizeFileNamePart(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "NA";

        var invalid = Path.GetInvalidFileNameChars();
        var filtered = new string(value
            .Trim()
            .Select(ch => invalid.Contains(ch) ? '_' : ch)
            .ToArray());

        // Keep names readable; avoid very long file names.
        return filtered.Length > 80 ? filtered.Substring(0, 80).TrimEnd('_', ' ') : filtered;
    }
}
