using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;

namespace Visa2026.Module.Services.ApplicationPersonRoster;

/// <summary>
/// Queues a <see cref="PdfGenerationBatch"/> keyed by <see cref="ApplicationPerson"/> roster line ids.
/// </summary>
public sealed class ApplicationPersonPdfBatchEnqueueService
{
    private readonly INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory;

    public ApplicationPersonPdfBatchEnqueueService(INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory)
    {
        this.nonSecuredObjectSpaceFactory = nonSecuredObjectSpaceFactory;
    }

    public bool TryEnqueuePackage(
        IReadOnlyList<Guid> applicationPersonIds,
        ApplicationItemDocumentPackageOptions packageOptions,
        string requestedBy,
        string requestedCulture,
        out ApplicationItemPdfBatchEnqueueResult? result,
        out string? errorMessageKey)
    {
        result = null;
        errorMessageKey = null;
        packageOptions ??= ApplicationItemDocumentPackageOptions.CreateDefaults();

        if (applicationPersonIds == null || applicationPersonIds.Count == 0)
        {
            errorMessageKey = "Pdf.SelectAtLeastOneItem";
            return false;
        }

        if (string.IsNullOrWhiteSpace(requestedBy))
        {
            errorMessageKey = "ApplicationItemDocumentCopies.Package.ErrorNotSignedIn";
            return false;
        }

        var rowIds = applicationPersonIds
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

        RefreshResolvedLinksAndCountMissingPassports(rowIds, opts.IncludePassportCopies, out var itemsMissingCurrentPassport);

        var keyType = typeof(ApplicationPerson);
        var keyStrings = rowIds
            .Select(id => Convert.ToString(id, CultureInfo.InvariantCulture))
            .ToList();

        using var os = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<PdfGenerationBatch>();
        var batch = os.CreateObject<PdfGenerationBatch>();
        batch.RequestedBy = requestedBy;
        batch.RequestedCulture = requestedCulture;
        batch.ItemKeyType = keyType.AssemblyQualifiedName ?? keyType.FullName ?? keyType.Name;
        batch.ItemKeysJson = JsonSerializer.Serialize(keyStrings);
        batch.TotalItems = keyStrings.Count;
        batch.Status = PdfGenerationBatchStatus.Queued;
        opts.CopyTo(batch);
        os.CommitChanges();

        result = new ApplicationItemPdfBatchEnqueueResult
        {
            BatchId = (Guid)os.GetKeyValue(batch)!,
            ItemCount = keyStrings.Count,
            PassportZipWillSkip = opts.IncludePassportCopies && itemsMissingCurrentPassport > 0,
            ItemsMissingCurrentPassport = itemsMissingCurrentPassport
        };

        return true;
    }

    private void RefreshResolvedLinksAndCountMissingPassports(
        IReadOnlyList<Guid> rowIds,
        bool includePassportCopies,
        out int itemsMissingCurrentPassport)
    {
        itemsMissingCurrentPassport = 0;
        using var os = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<ApplicationPerson>();
        bool changed = false;

        foreach (var rowId in rowIds)
        {
            var row = os.GetObjectByKey<ApplicationPerson>(rowId);
            if (row == null)
                continue;

            ApplicationPersonResolver.RefreshResolvedLinks(os, row);
            changed = true;

            if (!includePassportCopies)
                continue;

            var projection = ApplicationPersonPdfPackageLineHydrator.Hydrate(os, row);
            if (projection.CurrentPassport == null)
                itemsMissingCurrentPassport++;
        }

        if (changed)
            os.CommitChanges();
    }
}
