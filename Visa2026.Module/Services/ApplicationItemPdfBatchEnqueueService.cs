using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services;

public sealed class ApplicationItemPdfBatchEnqueueResult
{
    public Guid BatchId { get; init; }

    public int ItemCount { get; init; }

    public bool PassportZipWillSkip { get; init; }

    public int ItemsMissingCurrentPassport { get; init; }
}

/// <summary>
/// Queues a <see cref="PdfGenerationBatch"/> using <see cref="ApplicationItemDocumentPackageOptions"/> or defaults.
/// </summary>
public sealed class ApplicationItemPdfBatchEnqueueService
{
    private readonly INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory;

    public ApplicationItemPdfBatchEnqueueService(INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory)
    {
        this.nonSecuredObjectSpaceFactory = nonSecuredObjectSpaceFactory;
    }

    public bool TryEnqueueDefaultPackage(
        IReadOnlyList<Guid> applicationItemIds,
        string requestedBy,
        string requestedCulture,
        out ApplicationItemPdfBatchEnqueueResult? result,
        out string? errorMessageKey) =>
        TryEnqueuePackage(
            applicationItemIds,
            ApplicationItemDocumentPackageOptions.CreateDefaults(),
            requestedBy,
            requestedCulture,
            out result,
            out errorMessageKey);

    public bool TryEnqueuePackage(
        IReadOnlyList<Guid> applicationItemIds,
        ApplicationItemDocumentPackageOptions packageOptions,
        string requestedBy,
        string requestedCulture,
        out ApplicationItemPdfBatchEnqueueResult? result,
        out string? errorMessageKey)
    {
        result = null;
        errorMessageKey = null;
        packageOptions ??= ApplicationItemDocumentPackageOptions.CreateDefaults();

        if (applicationItemIds == null || applicationItemIds.Count == 0)
        {
            errorMessageKey = "Pdf.SelectAtLeastOneItem";
            return false;
        }

        if (string.IsNullOrWhiteSpace(requestedBy))
        {
            errorMessageKey = "ApplicationItemDocumentCopies.Package.ErrorNotSignedIn";
            return false;
        }

        var itemIds = applicationItemIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (itemIds.Count == 0)
        {
            errorMessageKey = "Pdf.SelectAtLeastOneItem";
            return false;
        }

        var opts = new PdfBatchEnqueueOptions();
        packageOptions.ApplyTo(opts);

        if (opts.IncludePassportCopies)
            SnapshotCurrentPassportOntoLinesFromPersonIfMissing(itemIds);
        int itemsMissingCurrentPassport = 0;
        if (opts.IncludePassportCopies)
        {
            using var countOs = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<ApplicationItem>();
            foreach (var itemId in itemIds)
            {
                var item = countOs.GetObjectByKey<ApplicationItem>(itemId);
                if (item != null && item.CurrentPassport == null)
                    itemsMissingCurrentPassport++;
            }
        }

        // Store Guid keys (same as ApplicationItemPdfController), not typeof(ApplicationItem).
        var keyType = typeof(Guid);
        var keyStrings = itemIds
            .Select(id => Convert.ToString(id, CultureInfo.InvariantCulture))
            .ToList();

        using (var os = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<PdfGenerationBatch>())
        {
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
        }

        return true;
    }

    private void SnapshotCurrentPassportOntoLinesFromPersonIfMissing(IReadOnlyList<Guid> itemIds)
    {
        using var os = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<ApplicationItem>();
        bool changed = false;

        foreach (var itemId in itemIds)
        {
            var item = os.GetObjectByKey<ApplicationItem>(itemId);
            if (item == null || item.CurrentPassport != null || item.Person == null)
                continue;

            var person = os.GetObject(item.Person);
            var currentPassport = PersonCurrentItems.GetCurrentPassport(person);
            if (currentPassport == null)
                continue;

            item.CurrentPassport = os.GetObject(currentPassport);
            changed = true;
        }

        if (changed)
            os.CommitChanges();
    }
}
