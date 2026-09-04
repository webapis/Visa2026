using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;

namespace Visa2026.Module.Services.ApplicationPersonRoster;

public sealed class ApplicationItemPdfBatchEnqueueResult
{
    public Guid BatchId { get; init; }

    public int ItemCount { get; init; }

    public bool PassportZipWillSkip { get; init; }

    public int ItemsMissingCurrentPassport { get; init; }
}

/// <summary>
/// Queues a <see cref="PdfGenerationBatch"/> keyed by Person ids on one ApplicationProfileInstance.
/// </summary>
public sealed class ApplicationProfileInstancePersonPdfBatchEnqueueService
{
    private readonly INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory;

    public ApplicationProfileInstancePersonPdfBatchEnqueueService(INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory)
    {
        this.nonSecuredObjectSpaceFactory = nonSecuredObjectSpaceFactory;
    }

    public bool TryEnqueuePackage(
        IReadOnlyList<Guid> personIds,
        ApplicationItemDocumentPackageOptions packageOptions,
        string requestedBy,
        string requestedCulture,
        out ApplicationItemPdfBatchEnqueueResult? result,
        out string? errorMessageKey)
        => TryEnqueuePackage(personIds, Guid.Empty, packageOptions, requestedBy, requestedCulture, out result, out errorMessageKey);

    public bool TryEnqueuePackage(
        IReadOnlyList<Guid> personIds,
        Guid applicationProfileInstanceId,
        ApplicationItemDocumentPackageOptions packageOptions,
        string requestedBy,
        string requestedCulture,
        out ApplicationItemPdfBatchEnqueueResult? result,
        out string? errorMessageKey)
    {
        result = null;
        errorMessageKey = null;
        packageOptions ??= ApplicationItemDocumentPackageOptions.CreateDefaults();

        if (personIds == null || personIds.Count == 0)
        {
            errorMessageKey = "Pdf.SelectAtLeastOneItem";
            return false;
        }

        if (string.IsNullOrWhiteSpace(requestedBy))
        {
            errorMessageKey = "ApplicationItemDocumentCopies.Package.ErrorNotSignedIn";
            return false;
        }

        var rowIds = personIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (rowIds.Count == 0)
        {
            errorMessageKey = "Pdf.SelectAtLeastOneItem";
            return false;
        }

        var opts = new PdfBatchEnqueueOptions();
        packageOptions.ApplyTo(opts);

        RefreshResolvedLinksAndCountMissingPassports(
            rowIds,
            applicationProfileInstanceId,
            opts.IncludePassportCopies,
            out var itemsMissingCurrentPassport);

        var keyType = typeof(Person);
        var payload = new PdfBatchRosterKeyPayload
        {
            ApplicationProfileInstanceId = applicationProfileInstanceId,
            PersonIds = rowIds.Select(id => Convert.ToString(id, CultureInfo.InvariantCulture)!).ToList(),
        };

        using var os = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<PdfGenerationBatch>();
        var batch = os.CreateObject<PdfGenerationBatch>();
        batch.RequestedBy = requestedBy;
        batch.RequestedCulture = requestedCulture;
        batch.ItemKeyType = keyType.AssemblyQualifiedName ?? keyType.FullName ?? keyType.Name;
        batch.ItemKeysJson = JsonSerializer.Serialize(payload);
        batch.TotalItems = rowIds.Count;
        batch.Status = PdfGenerationBatchStatus.Queued;
        opts.CopyTo(batch);
        os.CommitChanges();

        result = new ApplicationItemPdfBatchEnqueueResult
        {
            BatchId = (Guid)os.GetKeyValue(batch)!,
            ItemCount = rowIds.Count,
            PassportZipWillSkip = opts.IncludePassportCopies && itemsMissingCurrentPassport > 0,
            ItemsMissingCurrentPassport = itemsMissingCurrentPassport
        };

        return true;
    }

    private void RefreshResolvedLinksAndCountMissingPassports(
        IReadOnlyList<Guid> personIds,
        Guid applicationProfileInstanceId,
        bool includePassportCopies,
        out int itemsMissingCurrentPassport)
    {
        itemsMissingCurrentPassport = 0;
        using var os = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<ApplicationProfileInstance>();
        if (!ApplicationRosterHelper.TryLoadSharedApplicationPeople(
                os,
                personIds,
                applicationProfileInstanceId,
                out var application,
                out var people)
            || application == null)
        {
            return;
        }

        foreach (var person in people)
        {
            ApplicationProfileInstancePersonResolver.RefreshResolvedLinks(os, application, person);

            if (!includePassportCopies)
                continue;

            var projection = ApplicationProfileInstancePersonPdfPackageLineHydrator.Hydrate(os, application, person);
            if (projection.CurrentPassport == null)
                itemsMissingCurrentPassport++;
        }

        os.CommitChanges();
    }
}

public sealed class PdfBatchRosterKeyPayload
{
    public Guid ApplicationProfileInstanceId { get; set; }

    public List<string> PersonIds { get; set; } = [];
}
