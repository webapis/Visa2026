using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services;

/// <summary>
/// Packs <c>*Document</c> / <see cref="FileData"/> attachments into a background ZIP alongside filled application PDFs.
/// Passport copies: <see cref="ApplicationItem.CurrentPassport"/> / <see cref="ApplicationItem.PreviousPassport"/> only
/// (line-frozen; not <see cref="Person.CurrentPassport"/>), from <see cref="Passport.Documents"/> (each row is a <see cref="PassportDocument"/> with <see cref="DocumentBase.File"/>).
/// Other categories use the same pattern: only aggregated <c>*Document</c> rows with <see cref="DocumentBase.File"/>, not image editors.
/// Rules: <c>docs/APPLICATION_DIPLOMA_PACKAGE_PLAN.md</c> (§1.3 eligibility, §4.4 naming, §4.5–§4.7 dedupe).
/// Console verification: search for <c>ZIP packer: Passport</c> / <c>ZIP packer: Visa</c> / <c>ZIP packer: MedicalRecord</c> / <c>ZIP packer: AddressOfResidence</c> / <c>ZIP packer: FamilyRelationship</c> / <c>ZIP packer: WorkPermit</c> / <c>ZIP packer: Invitation</c> / <c>ZIP packer: CurrentWorkPermits</c> / <c>ZIP packer: AllDiplomas</c> after a batch run.
/// Batch-wide merges combine per-attachment PDF slices with PdfSharpCore; raster slices use PdfSharpCore first,
/// then Spire.PDF fallback when the image format is not decodable (e.g. many TIFF scans), before merge:
/// <c>Passport/CurrentPassports.pdf</c>, <c>Visa/CurrentVisas.pdf</c>,
/// <c>WorkPermit/CurrentWorkPermits.pdf</c>, <c>Diplomas/AllDiplomas.pdf</c> (batch item order). Per-item merged diplomas use the same PdfSharpCore merge.
/// </summary>
public static class ApplicationSupportingDocumentsPacker
{
    private const int MaxPathLength = 220;
    private const int MaxPassportPathsLogChars = 900;

    /// <summary>Batch-wide merge of every application line's <i>current</i> passport document files (ZIP root).</summary>
    public const string MergedCurrentPassportsZipRelativePath = "Passport/CurrentPassports.pdf";

    /// <summary>Batch-wide merge of every application line's <i>current</i> visa document files (ZIP root).</summary>
    public const string MergedCurrentVisasZipRelativePath = "Visa/CurrentVisas.pdf";

    /// <summary>Batch-wide merge of every application line's <i>current</i> work permit document files (ZIP root).</summary>
    public const string MergedCurrentWorkPermitsZipRelativePath = "WorkPermit/CurrentWorkPermits.pdf";

    /// <summary>Batch-wide merge of all diploma attachments in batch item order (ZIP root).</summary>
    public const string MergedBatchDiplomasZipRelativePath = "Diplomas/AllDiplomas.pdf";

    /// <summary>
    /// ZIP folder for filled application PDFs. Supporting attachments use an empty <c>zipInnerRoot</c> prefix so
    /// <c>Passport/</c>, <c>Diplomas/</c>, etc. are siblings at the archive root (not nested under this folder).
    /// </summary>
    public const string FilledApplicationFormsZipFolderName = "PDF_Form";

    /// <summary>
    /// Optional folder prefix plus relative path using forward slashes.
    /// When <paramref name="prefix"/> is null or whitespace, entries are placed at the archive root (no leading slash).
    /// </summary>
    private static string ZipEntryPath(string? prefix, string relativePath)
    {
        string r = (relativePath ?? string.Empty).Replace('\\', '/').TrimStart('/');
        string p = (prefix ?? string.Empty).Trim().Replace('\\', '/').Trim('/');
        return string.IsNullOrEmpty(p) ? r : $"{p}/{r}";
    }

    /// <summary>Per-ZIP set of normalized entry paths already reserved (case-insensitive).</summary>
    public static string BuildItemSlug(int itemIndex1Based, Person person)
    {
        string first = SanitizeSegment(person?.FirstName ?? "NA", 24);
        string last = SanitizeSegment(person?.LastName ?? "NA", 24);
        return $"{itemIndex1Based:00}_{first}_{last}";
    }

    /// <param name="zipInnerRoot">
    /// Optional path prefix for attachment entries. Use null or whitespace so paths are
    /// <c>Passport/…</c>, <c>Diplomas/…</c> at the archive root next to <see cref="FilledApplicationFormsZipFolderName"/>.
    /// </param>
    /// <param name="emitIndividualZipEntries">
    /// When false (<see cref="PdfSupportingZipMergeOption.MergedPdfSummariesOnly"/>), per-file ZIP entries are not written for
    /// passport, visa, and diplomas; merge buffers are still filled so batch merged PDFs can be produced. Work permit uses
    /// <paramref name="currentWorkPermitPdfMergeSlices"/> the same way. Categories without batch merges (medical, address, invitation, family) are skipped entirely when this is false.
    /// </param>
    /// <param name="workPermitIdsContributedToCurrentBatchMerge">
    /// When non-null with <paramref name="currentWorkPermitPdfMergeSlices"/>, each <see cref="WorkPermit"/> ID is recorded only after
    /// at least one merge slice is produced; later lines referencing the same permit skip duplicate merge slices (shared permit business rule).
    /// </param>
    public static async Task AppendSupportingDocumentsForItemAsync(
        IObjectSpace objectSpace,
        PdfGenerationBatch batch,
        ZipArchive archive,
        HashSet<string> reservedZipPaths,
        string? zipInnerRoot,
        ApplicationItem item,
        int itemIndex1Based,
        List<MemoryStream>? diplomaPdfBuffersForMerge,
        List<MemoryStream>? currentPassportPdfMergeSlices,
        List<MemoryStream>? currentVisaPdfMergeSlices,
        List<MemoryStream>? currentWorkPermitPdfMergeSlices,
        HashSet<Guid>? workPermitIdsContributedToCurrentBatchMerge,
        List<MemoryStream>? batchDiplomaPdfMergeSlices,
        bool emitIndividualZipEntries,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (item?.Person == null)
            return;

        string itemSlug = BuildItemSlug(itemIndex1Based, item.Person);

        if (batch.IncludeDiplomaFiles)
            await AppendDiplomasAsync(objectSpace, batch, archive, reservedZipPaths, zipInnerRoot, item, itemSlug, emitIndividualZipEntries, diplomaPdfBuffersForMerge, batchDiplomaPdfMergeSlices, logger, cancellationToken).ConfigureAwait(false);

        if (batch.IncludePassportCopies)
            await AppendPassportAsync(objectSpace, archive, reservedZipPaths, zipInnerRoot, item, itemSlug, emitIndividualZipEntries, currentPassportPdfMergeSlices, logger, cancellationToken).ConfigureAwait(false);

        if (batch.IncludeVisaCopies)
            await AppendVisaAsync(objectSpace, archive, reservedZipPaths, zipInnerRoot, item, itemSlug, emitIndividualZipEntries, currentVisaPdfMergeSlices, logger, cancellationToken).ConfigureAwait(false);

        if (emitIndividualZipEntries && batch.IncludeMedicalRecordCopies)
            await AppendMedicalRecordAsync(objectSpace, archive, reservedZipPaths, zipInnerRoot, item, itemSlug, logger, cancellationToken).ConfigureAwait(false);

        if (emitIndividualZipEntries && batch.IncludeAddressOfResidenceCopies)
            await AppendAddressOfResidenceAsync(objectSpace, archive, reservedZipPaths, zipInnerRoot, item, itemSlug, logger, cancellationToken).ConfigureAwait(false);

        if (batch.IncludeWorkPermitCopies && item.Person.IsEmployee
            && (emitIndividualZipEntries || currentWorkPermitPdfMergeSlices != null))
            await AppendWorkPermitAsync(objectSpace, archive, reservedZipPaths, zipInnerRoot, item, itemSlug, emitIndividualZipEntries, currentWorkPermitPdfMergeSlices, workPermitIdsContributedToCurrentBatchMerge, logger, cancellationToken).ConfigureAwait(false);

        if (emitIndividualZipEntries && batch.IncludeInvitationCopies)
            await AppendInvitationAsync(objectSpace, archive, reservedZipPaths, zipInnerRoot, item, itemSlug, logger, cancellationToken).ConfigureAwait(false);

        if (emitIndividualZipEntries && batch.IncludeFamilyRelationshipCopies && !item.Person.IsEmployee)
            await AppendFamilyRelationshipAsync(objectSpace, archive, reservedZipPaths, zipInnerRoot, item, itemSlug, logger, cancellationToken).ConfigureAwait(false);
    }

    /// <param name="useFlatMergedLinePath">
    /// When true (<see cref="PdfSupportingZipMergeOption.MergedPdfSummariesOnly"/>), per-line merged diplomas are written under
    /// <c>Diplomas/MergedByLine/</c> so the archive does not create one folder per person under <c>Diplomas/</c> only for the merge file.
    /// </param>
    /// <param name="zipInnerRoot">Same semantics as <see cref="AppendSupportingDocumentsForItemAsync"/> (null/empty = archive root).</param>
    public static async Task WriteMergedDiplomaPdfIfNeededAsync(
        ZipArchive archive,
        HashSet<string> reservedZipPaths,
        string? zipInnerRoot,
        string itemSlug,
        List<MemoryStream>? diplomaPdfBuffersForMerge,
        bool useFlatMergedLinePath,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (diplomaPdfBuffersForMerge == null || diplomaPdfBuffersForMerge.Count < 1)
            return;

        try
        {
            foreach (var s in diplomaPdfBuffersForMerge)
                s.Position = 0;

            using var merged = new MemoryStream();
            SupportingDocumentsPdfSharpHelper.MergePdfStreams(diplomaPdfBuffersForMerge, merged);
            merged.Position = 0;

            string rel = useFlatMergedLinePath
                ? ZipEntryPath(zipInnerRoot, $"Diplomas/MergedByLine/{itemSlug}_AllDiplomas_merged.pdf")
                : ZipEntryPath(zipInnerRoot, $"Diplomas/{itemSlug}/Merged/_AllDiplomas_merged.pdf");
            rel = ReserveZipEntryPath(reservedZipPaths, rel);
            var entry = archive.CreateEntry(rel, CompressionLevel.Fastest);
            await using var es = entry.Open();
            await merged.CopyToAsync(es, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Merged diploma PDF skipped for item folder {ItemSlug}.", itemSlug);
        }
        finally
        {
            if (diplomaPdfBuffersForMerge != null)
            {
                foreach (var s in diplomaPdfBuffersForMerge)
                    await s.DisposeAsync().ConfigureAwait(false);
                diplomaPdfBuffersForMerge.Clear();
            }
        }
    }

    private static async Task AppendDiplomasAsync(
        IObjectSpace os,
        PdfGenerationBatch batch,
        ZipArchive archive,
        HashSet<string> reservedZipPaths,
        string? zipInnerRoot,
        ApplicationItem item,
        string itemSlug,
        bool emitIndividualZipEntries,
        List<MemoryStream>? mergeOut,
        List<MemoryStream>? batchDiplomaPdfMergeSlices,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var personId = item.Person.ID;
        IQueryable<Education> query = os.GetObjectsQuery<Education>()
            .Where(e => e.Person.ID == personId && !e.IsDeleted);

        if (batch.DiplomaScope == PdfBatchDiplomaScope.CurrentEducationOnly)
        {
            if (item.CurrentEducation == null)
                return;
            var curId = item.CurrentEducation.ID;
            query = query.Where(e => e.ID == curId);
        }

        var educations = await query
            .Include(e => e.EducationInstitution)
            .OrderByDescending(e => e.GraduationYear)
            .ThenBy(e => e.EducationInstitution != null ? e.EducationInstitution.Name : "")
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        int eduIndex = 0;
        foreach (var edu in educations)
        {
            eduIndex++;
            var docs = await os.GetObjectsQuery<EducationDocument>()
                .Where(d => d.Education.ID == edu.ID)
                .OrderBy(d => d.ID)
                .Include(d => d.File)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            int docIdx = 0;
            string inst = SanitizeSegment(edu.EducationInstitution?.Name ?? "Edu", 20);
            string year = edu.GraduationYear?.ToString(CultureInfo.InvariantCulture) ?? "Year";
            // Mirror Passport layout: Diplomas/{itemSlug}/{slot}/docNN.ext (one folder per education row).
            string eduSlot = SanitizeSegment($"E{eduIndex:00}_{year}_{inst}", 72);
            foreach (var doc in docs)
            {
                docIdx++;
                string ext = GetExtension(doc?.File);
                if (emitIndividualZipEntries)
                {
                    string rel = ZipEntryPath(zipInnerRoot, $"Diplomas/{itemSlug}/{eduSlot}/doc{docIdx:00}{ext}");
                    rel = ReserveZipEntryPath(reservedZipPaths, rel);
                    await TryWriteDocumentAsync(os, archive, rel, doc, logger, cancellationToken).ConfigureAwait(false);
                }

                byte[]? mergeContent = await GetDocumentFileContentAsync(os, doc, cancellationToken).ConfigureAwait(false);
                if (mergeContent is not { Length: > 0 })
                    continue;

                bool wantBatch = batchDiplomaPdfMergeSlices != null;
                bool wantItem = batch.IncludeMergedDiplomaPdf && mergeOut != null;
                if (!wantBatch && !wantItem)
                    continue;

                if (!TryCreateMergeSlicePdfStream(mergeContent, ext, "DiplomaMerge", logger, out var slice))
                    continue;

                if (wantItem)
                    mergeOut!.Add(slice);

                if (wantBatch)
                {
                    if (wantItem)
                        batchDiplomaPdfMergeSlices!.Add(CloneMemoryStream(slice));
                    else
                        batchDiplomaPdfMergeSlices!.Add(slice);
                }
            }
        }
    }

    /// <summary>
    /// Writes a batch-wide merged PDF at <paramref name="relativeFilePath"/>; disposes <paramref name="sources"/>.
    /// </summary>
    private static async Task WriteMergedBatchPdfSlicesAsync(
        ZipArchive archive,
        HashSet<string> reservedZipPaths,
        string? zipInnerRoot,
        string relativeFilePath,
        string logLabel,
        string successLogSuffix,
        List<MemoryStream> sources,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (sources == null || sources.Count < 1)
            return;

        try
        {
            foreach (var s in sources)
                s.Position = 0;

            using var merged = new MemoryStream();
            SupportingDocumentsPdfSharpHelper.MergePdfStreams(sources, merged);
            merged.Position = 0;

            string rel = ZipEntryPath(zipInnerRoot, relativeFilePath);
            rel = ReserveZipEntryPath(reservedZipPaths, rel);
            var entry = archive.CreateEntry(rel, CompressionLevel.Fastest);
            await using var es = entry.Open();
            await merged.CopyToAsync(es, cancellationToken).ConfigureAwait(false);

            logger.LogInformation(
                "ZIP packer: {LogLabel} merge wrote {Rel} from {Count} source file(s){Suffix}",
                logLabel,
                rel,
                sources.Count,
                successLogSuffix);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ZIP packer: {LogLabel} merge skipped.", logLabel);
        }
        finally
        {
            foreach (var s in sources)
                await s.DisposeAsync().ConfigureAwait(false);
            sources.Clear();
        }
    }

    /// <summary>
    /// Writes <see cref="MergedCurrentPassportsZipRelativePath"/> once per ZIP after all items are packed.
    /// <paramref name="sources"/> are disposed here (same pattern as merged diplomas).
    /// </summary>
    public static Task WriteMergedCurrentPassportsPdfIfNeededAsync(
        ZipArchive archive,
        HashSet<string> reservedZipPaths,
        string? zipInnerRoot,
        List<MemoryStream> sources,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (sources == null || sources.Count < 1)
            return Task.CompletedTask;

        return WriteMergedBatchPdfSlicesAsync(
            archive,
            reservedZipPaths,
            zipInnerRoot,
            MergedCurrentPassportsZipRelativePath,
            "CurrentPassports",
            " (batch item order, Current passport only).",
            sources,
            logger,
            cancellationToken);
    }

    /// <summary>
    /// Writes <see cref="MergedCurrentVisasZipRelativePath"/> once per ZIP after all items are packed.
    /// </summary>
    public static Task WriteMergedCurrentVisasPdfIfNeededAsync(
        ZipArchive archive,
        HashSet<string> reservedZipPaths,
        string? zipInnerRoot,
        List<MemoryStream> sources,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (sources == null || sources.Count < 1)
            return Task.CompletedTask;

        return WriteMergedBatchPdfSlicesAsync(
            archive,
            reservedZipPaths,
            zipInnerRoot,
            MergedCurrentVisasZipRelativePath,
            "CurrentVisas",
            " (batch item order, Current visa only).",
            sources,
            logger,
            cancellationToken);
    }

    /// <summary>
    /// Writes <see cref="MergedBatchDiplomasZipRelativePath"/> once per ZIP after all items are packed.
    /// </summary>
    public static Task WriteMergedAllDiplomasPdfIfNeededAsync(
        ZipArchive archive,
        HashSet<string> reservedZipPaths,
        string? zipInnerRoot,
        List<MemoryStream> sources,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (sources == null || sources.Count < 1)
            return Task.CompletedTask;

        return WriteMergedBatchPdfSlicesAsync(
            archive,
            reservedZipPaths,
            zipInnerRoot,
            MergedBatchDiplomasZipRelativePath,
            "AllDiplomas",
            " (batch item order, all packed diploma files).",
            sources,
            logger,
            cancellationToken);
    }

    /// <summary>
    /// Writes <see cref="MergedCurrentWorkPermitsZipRelativePath"/> once per ZIP after all items are packed.
    /// </summary>
    public static Task WriteMergedCurrentWorkPermitsPdfIfNeededAsync(
        ZipArchive archive,
        HashSet<string> reservedZipPaths,
        string? zipInnerRoot,
        List<MemoryStream> sources,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (sources == null || sources.Count < 1)
            return Task.CompletedTask;

        return WriteMergedBatchPdfSlicesAsync(
            archive,
            reservedZipPaths,
            zipInnerRoot,
            MergedCurrentWorkPermitsZipRelativePath,
            "CurrentWorkPermits",
            " (batch item order, Current work permit only).",
            sources,
            logger,
            cancellationToken);
    }

    private static async Task AppendPassportAsync(
        IObjectSpace os,
        ZipArchive archive,
        HashSet<string> reservedZipPaths,
        string? zipInnerRoot,
        ApplicationItem item,
        string itemSlug,
        bool emitIndividualZipEntries,
        List<MemoryStream>? currentPassportPdfMergeSlices,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        // Passport copies are line-scoped only: ApplicationItem.CurrentPassport / PreviousPassport.
        // Do not fall back to Person.CurrentPassport — it can change after the application is filed.
        var cur = item.CurrentPassport;
        var prev = item.PreviousPassport;
        bool duplicate = cur != null && prev != null && cur.ID == prev.ID;
        if (duplicate)
            logger.LogWarning(
                "ZIP packer: ApplicationItem {ItemId} has same Passport for Current and Previous; packing Current only.",
                item.ID);

        if (cur == null && prev == null)
        {
            logger.LogInformation(
                "ZIP packer: Passport section skipped for item folder {ItemSlug} (ApplicationItem {ItemId}): no CurrentPassport or PreviousPassport on the line.",
                itemSlug,
                item.ID);
            return;
        }

        if (cur != null)
            await WritePassportSlotAsync(os, archive, reservedZipPaths, zipInnerRoot, itemSlug, cur, "Current", emitIndividualZipEntries, currentPassportPdfMergeSlices, logger, cancellationToken).ConfigureAwait(false);

        if (prev != null && !duplicate)
            await WritePassportSlotAsync(os, archive, reservedZipPaths, zipInnerRoot, itemSlug, prev, "Previous", emitIndividualZipEntries, null, logger, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WritePassportSlotAsync(
        IObjectSpace os,
        ZipArchive archive,
        HashSet<string> reservedZipPaths,
        string? zipInnerRoot,
        string itemSlug,
        Passport passport,
        string currentOrPrevious,
        bool emitIndividualZipEntries,
        List<MemoryStream>? currentPassportPdfMergeSlices,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        // Rows are the same as Passport.Documents in the UI (entity type PassportDocument, FileData per row).
        var docs = await os.GetObjectsQuery<PassportDocument>()
            .Where(d => d.Passport.ID == passport.ID)
            .OrderBy(d => d.ID)
            .Include(d => d.File)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (docs.Count == 0)
        {
            logger.LogInformation(
                "ZIP packer: no PassportDocument rows for passport {PassportId} ({Slot}); UI Documents tab would be empty for that passport.",
                passport.ID,
                currentOrPrevious);
            return;
        }

        var writtenPaths = new List<string>(docs.Count);
        int i = 0;
        foreach (var doc in docs)
        {
            i++;
            string ext = GetExtension(doc?.File);
            string rel = ZipEntryPath(zipInnerRoot, $"Passport/{itemSlug}/{currentOrPrevious}/doc{i:00}{ext}");
            if (emitIndividualZipEntries)
                rel = ReserveZipEntryPath(reservedZipPaths, rel);

            byte[]? content = await GetDocumentFileContentAsync(os, doc, cancellationToken).ConfigureAwait(false);
            if (content == null || content.Length == 0)
            {
                logger.LogWarning(
                    "ZIP packer: skip empty FileData for passport document row {Index} passport {PassportId} ({Slot}); file id {FileId}, reported size {Size}.",
                    i,
                    passport.ID,
                    currentOrPrevious,
                    doc?.File?.ID,
                    doc?.File?.Size);
                continue;
            }

            if (emitIndividualZipEntries
                && await TryWriteRawBytesAsync(archive, rel, content, logger, cancellationToken).ConfigureAwait(false))
                writtenPaths.Add(rel);

            if (currentPassportPdfMergeSlices != null
                && currentOrPrevious.Equals("Current", StringComparison.OrdinalIgnoreCase)
                && TryCreateMergeSlicePdfStream(content, ext, "CurrentPassports", logger, out var slice))
            {
                currentPassportPdfMergeSlices.Add(slice);
            }
        }

        logger.LogInformation(
            "ZIP packer: Passport {Slot} itemFolder={ItemSlug} passportId={PassportId}: {Written} of {Total} document row(s) written to ZIP. Paths={Paths}",
            currentOrPrevious,
            itemSlug,
            passport.ID,
            writtenPaths.Count,
            docs.Count,
            writtenPaths.Count == 0
                ? "(none — check ZIP packer skip/failed warnings above)"
                : JoinPathsForLog(writtenPaths, MaxPassportPathsLogChars));
    }

    private static async Task AppendVisaAsync(
        IObjectSpace os,
        ZipArchive archive,
        HashSet<string> reservedZipPaths,
        string? zipInnerRoot,
        ApplicationItem item,
        string itemSlug,
        bool emitIndividualZipEntries,
        List<MemoryStream>? currentVisaPdfMergeSlices,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var cur = item.CurrentVisa;
        var prev = item.PreviousVisa;
        bool duplicate = cur != null && prev != null && cur.ID == prev.ID;
        if (duplicate)
            logger.LogWarning(
                "ZIP packer: ApplicationItem {ItemId} has same Visa for Current and Previous; packing Current only.",
                item.ID);

        if (cur != null)
            await WriteVisaSlotAsync(os, archive, reservedZipPaths, zipInnerRoot, itemSlug, cur, "Current", emitIndividualZipEntries, currentVisaPdfMergeSlices, logger, cancellationToken).ConfigureAwait(false);

        if (prev != null && !duplicate)
            await WriteVisaSlotAsync(os, archive, reservedZipPaths, zipInnerRoot, itemSlug, prev, "Previous", emitIndividualZipEntries, null, logger, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteVisaSlotAsync(
        IObjectSpace os,
        ZipArchive archive,
        HashSet<string> reservedZipPaths,
        string? zipInnerRoot,
        string itemSlug,
        Visa visa,
        string currentOrPrevious,
        bool emitIndividualZipEntries,
        List<MemoryStream>? currentVisaPdfMergeSlices,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var docs = await os.GetObjectsQuery<VisaDocument>()
            .Where(d => d.Visa.ID == visa.ID)
            .OrderBy(d => d.ID)
            .Include(d => d.File)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        if (docs.Count == 0)
        {
            logger.LogInformation(
                "ZIP packer: no VisaDocument rows for visa {VisaId} ({Slot}).",
                visa.ID,
                currentOrPrevious);
            return;
        }

        var writtenPaths = new List<string>(docs.Count);
        int i = 0;
        foreach (var doc in docs)
        {
            i++;
            string ext = GetExtension(doc?.File);
            string rel = ZipEntryPath(zipInnerRoot, $"Visa/{itemSlug}/{currentOrPrevious}/doc{i:00}{ext}");
            if (emitIndividualZipEntries)
                rel = ReserveZipEntryPath(reservedZipPaths, rel);

            byte[]? content = await GetDocumentFileContentAsync(os, doc, cancellationToken).ConfigureAwait(false);
            if (content == null || content.Length == 0)
            {
                logger.LogWarning(
                    "ZIP packer: skip empty FileData for visa document row {Index} visa {VisaId} ({Slot}); file id {FileId}, reported size {Size}.",
                    i,
                    visa.ID,
                    currentOrPrevious,
                    doc?.File?.ID,
                    doc?.File?.Size);
                continue;
            }

            if (emitIndividualZipEntries
                && await TryWriteRawBytesAsync(archive, rel, content, logger, cancellationToken).ConfigureAwait(false))
                writtenPaths.Add(rel);

            if (currentVisaPdfMergeSlices != null
                && currentOrPrevious.Equals("Current", StringComparison.OrdinalIgnoreCase)
                && TryCreateMergeSlicePdfStream(content, ext, "CurrentVisas", logger, out var slice))
            {
                currentVisaPdfMergeSlices.Add(slice);
            }
        }

        logger.LogInformation(
            "ZIP packer: Visa {Slot} itemFolder={ItemSlug} visaId={VisaId}: {Written} of {Total} document row(s) written to ZIP. Paths={Paths}",
            currentOrPrevious,
            itemSlug,
            visa.ID,
            writtenPaths.Count,
            docs.Count,
            writtenPaths.Count == 0
                ? "(none — check ZIP packer skip/failed warnings above)"
                : JoinPathsForLog(writtenPaths, MaxPassportPathsLogChars));
    }

    private static async Task AppendMedicalRecordAsync(
        IObjectSpace os,
        ZipArchive archive,
        HashSet<string> reservedZipPaths,
        string? zipInnerRoot,
        ApplicationItem item,
        string itemSlug,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        // Line-scoped only: ApplicationItem.CurrentMedicalRecord — no PreviousMedicalRecord on the item.
        var mr = item.CurrentMedicalRecord;
        if (mr == null)
            return;

        var docs = await os.GetObjectsQuery<MedicalRecordDocument>()
            .Where(d => d.MedicalRecord.ID == mr.ID)
            .OrderBy(d => d.ID)
            .Include(d => d.File)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        if (docs.Count == 0)
        {
            logger.LogInformation(
                "ZIP packer: no MedicalRecordDocument rows for medical record {MedicalRecordId} (itemFolder={ItemSlug}); UI Documents tab would be empty for that record.",
                mr.ID,
                itemSlug);
            return;
        }

        var writtenPaths = new List<string>(docs.Count);
        int i = 0;
        foreach (var doc in docs)
        {
            i++;
            string ext = GetExtension(doc?.File);
            string rel = ZipEntryPath(zipInnerRoot, $"MedicalRecord/{itemSlug}/Current/doc{i:00}{ext}");
            rel = ReserveZipEntryPath(reservedZipPaths, rel);

            byte[]? content = await GetDocumentFileContentAsync(os, doc, cancellationToken).ConfigureAwait(false);
            if (content == null || content.Length == 0)
            {
                logger.LogWarning(
                    "ZIP packer: skip empty FileData for medical record document row {Index} medicalRecord {MedicalRecordId}; file id {FileId}, reported size {Size}.",
                    i,
                    mr.ID,
                    doc?.File?.ID,
                    doc?.File?.Size);
                continue;
            }

            if (await TryWriteRawBytesAsync(archive, rel, content, logger, cancellationToken).ConfigureAwait(false))
                writtenPaths.Add(rel);
        }

        logger.LogInformation(
            "ZIP packer: MedicalRecord Current itemFolder={ItemSlug} medicalRecordId={MedicalRecordId}: {Written} of {Total} document row(s) written to ZIP. Paths={Paths}",
            itemSlug,
            mr.ID,
            writtenPaths.Count,
            docs.Count,
            writtenPaths.Count == 0
                ? "(none — check ZIP packer skip/failed warnings above)"
                : JoinPathsForLog(writtenPaths, MaxPassportPathsLogChars));
    }

    private static async Task AppendAddressOfResidenceAsync(
        IObjectSpace os,
        ZipArchive archive,
        HashSet<string> reservedZipPaths,
        string? zipInnerRoot,
        ApplicationItem item,
        string itemSlug,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        // Line-scoped only: ApplicationItem.CurrentAddressOfResidence — no Previous on the item.
        var addr = item.CurrentAddressOfResidence;
        if (addr == null)
            return;

        var addrDocs = await os.GetObjectsQuery<AddressOfResidenceDocument>()
            .Where(d => d.AddressOfResidence.ID == addr.ID)
            .OrderBy(d => d.ID)
            .Include(d => d.File)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        if (addrDocs.Count == 0)
        {
            logger.LogInformation(
                "ZIP packer: no AddressOfResidenceDocument rows for addressOfResidence {AddressId} (itemFolder={ItemSlug}).",
                addr.ID,
                itemSlug);
        }

        var writtenPaths = new List<string>(addrDocs.Count + 4);
        int i = 0;
        foreach (var doc in addrDocs)
        {
            i++;
            string ext = GetExtension(doc?.File);
            string rel = ZipEntryPath(zipInnerRoot, $"AddressOfResidence/{itemSlug}/Current/doc{i:00}{ext}");
            rel = ReserveZipEntryPath(reservedZipPaths, rel);

            byte[]? content = await GetDocumentFileContentAsync(os, doc, cancellationToken).ConfigureAwait(false);
            if (content == null || content.Length == 0)
            {
                logger.LogWarning(
                    "ZIP packer: skip empty FileData for address-of-residence document row {Index} addressOfResidence {AddressId}; file id {FileId}, reported size {Size}.",
                    i,
                    addr.ID,
                    doc?.File?.ID,
                    doc?.File?.Size);
                continue;
            }

            if (await TryWriteRawBytesAsync(archive, rel, content, logger, cancellationToken).ConfigureAwait(false))
                writtenPaths.Add(rel);
        }

        int lodgingRows = 0;
        if (addr.Type == ResidenceType.Lodging && addr.Lodging != null)
        {
            var lodgingId = addr.Lodging.ID;
            var lodDocs = await os.GetObjectsQuery<LodgingDocument>()
                .Where(d => d.Lodging.ID == lodgingId)
                .OrderBy(d => d.ID)
                .Include(d => d.File)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            lodgingRows = lodDocs.Count;
            if (lodgingRows == 0)
            {
                logger.LogInformation(
                    "ZIP packer: no LodgingDocument rows for lodging {LodgingId} linked from addressOfResidence {AddressId} (itemFolder={ItemSlug}).",
                    lodgingId,
                    addr.ID,
                    itemSlug);
            }

            int j = 0;
            foreach (var doc in lodDocs)
            {
                j++;
                string ext = GetExtension(doc?.File);
                string rel = ZipEntryPath(zipInnerRoot, $"AddressOfResidence/{itemSlug}/Current/Lodging/doc{j:00}{ext}");
                rel = ReserveZipEntryPath(reservedZipPaths, rel);

                byte[]? content = await GetDocumentFileContentAsync(os, doc, cancellationToken).ConfigureAwait(false);
                if (content == null || content.Length == 0)
                {
                    logger.LogWarning(
                        "ZIP packer: skip empty FileData for lodging document row {Index} lodging {LodgingId}; file id {FileId}, reported size {Size}.",
                        j,
                        lodgingId,
                        doc?.File?.ID,
                        doc?.File?.Size);
                    continue;
                }

                if (await TryWriteRawBytesAsync(archive, rel, content, logger, cancellationToken).ConfigureAwait(false))
                    writtenPaths.Add(rel);
            }
        }

        int totalRows = addrDocs.Count + lodgingRows;
        logger.LogInformation(
            "ZIP packer: AddressOfResidence Current itemFolder={ItemSlug} addressOfResidenceId={AddressId}: {Written} of {Total} document row(s) written to ZIP (address rows={AddrRows}, lodging rows={LodgingRows}). Paths={Paths}",
            itemSlug,
            addr.ID,
            writtenPaths.Count,
            totalRows,
            addrDocs.Count,
            lodgingRows,
            writtenPaths.Count == 0
                ? "(none — check ZIP packer skip/failed warnings above)"
                : JoinPathsForLog(writtenPaths, MaxPassportPathsLogChars));
    }

    private static async Task AppendWorkPermitAsync(
        IObjectSpace os,
        ZipArchive archive,
        HashSet<string> reservedZipPaths,
        string? zipInnerRoot,
        ApplicationItem item,
        string itemSlug,
        bool emitIndividualZipEntries,
        List<MemoryStream>? currentWorkPermitPdfMergeSlices,
        HashSet<Guid>? workPermitIdsContributedToCurrentBatchMerge,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var curWpi = item.CurrentWorkPermitItem;
        var prevWpi = item.PreviousWorkPermitItem;
        var curWp = curWpi?.WorkPermit;
        var prevWp = prevWpi?.WorkPermit;
        bool duplicateWp = curWp != null && prevWp != null && curWp.ID == prevWp.ID;
        if (duplicateWp)
            logger.LogWarning(
                "ZIP packer: ApplicationItem {ItemId} has same WorkPermit on Current and Previous work permit items; packing Current slot only.",
                item.ID);

        if (curWp != null)
            await WriteWorkPermitSlotAsync(os, archive, reservedZipPaths, zipInnerRoot, itemSlug, curWp, "Current", emitIndividualZipEntries, currentWorkPermitPdfMergeSlices, workPermitIdsContributedToCurrentBatchMerge, logger, cancellationToken).ConfigureAwait(false);

        if (prevWp != null && !duplicateWp)
            await WriteWorkPermitSlotAsync(os, archive, reservedZipPaths, zipInnerRoot, itemSlug, prevWp, "Previous", emitIndividualZipEntries, null, null, logger, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteWorkPermitSlotAsync(
        IObjectSpace os,
        ZipArchive archive,
        HashSet<string> reservedZipPaths,
        string? zipInnerRoot,
        string itemSlug,
        WorkPermit workPermit,
        string currentOrPrevious,
        bool emitIndividualZipEntries,
        List<MemoryStream>? currentWorkPermitPdfMergeSlices,
        HashSet<Guid>? workPermitIdsContributedToCurrentBatchMerge,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var docs = await os.GetObjectsQuery<WorkPermitDocument>()
            .Where(d => d.WorkPermit.ID == workPermit.ID)
            .OrderBy(d => d.ID)
            .Include(d => d.File)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        if (docs.Count == 0)
        {
            logger.LogInformation(
                "ZIP packer: no WorkPermitDocument rows for work permit {WorkPermitId} ({Slot}); itemFolder={ItemSlug}.",
                workPermit.ID,
                currentOrPrevious,
                itemSlug);
            return;
        }

        bool addMergeSlices = currentWorkPermitPdfMergeSlices != null
            && currentOrPrevious.Equals("Current", StringComparison.OrdinalIgnoreCase);

        // Reserve a work permit in the dedupe set only after at least one merge slice succeeds (see loop below).
        // If we used HashSet.Add before attempting slices, the first application line sharing this permit could "claim"
        // the ID with zero mergeable pages (e.g. non-PDF first row), and later lines would skip — producing no CurrentWorkPermits.pdf.
        if (addMergeSlices && workPermitIdsContributedToCurrentBatchMerge != null
            && workPermitIdsContributedToCurrentBatchMerge.Contains(workPermit.ID))
        {
            addMergeSlices = false;
            logger.LogInformation(
                "ZIP packer: WorkPermit Current itemFolder={ItemSlug} workPermitId={WorkPermitId}: skipping batch merge slices for CurrentWorkPermits.pdf (same work permit already merged earlier in this batch).",
                itemSlug,
                workPermit.ID);
        }

        var writtenPaths = new List<string>(docs.Count);
        int i = 0;
        foreach (var doc in docs)
        {
            i++;
            string ext = GetExtension(doc?.File);
            string rel = ZipEntryPath(zipInnerRoot, $"WorkPermit/{itemSlug}/{currentOrPrevious}/doc{i:00}{ext}");
            if (emitIndividualZipEntries)
                rel = ReserveZipEntryPath(reservedZipPaths, rel);

            byte[]? content = await GetDocumentFileContentAsync(os, doc, cancellationToken).ConfigureAwait(false);
            if (content == null || content.Length == 0)
            {
                logger.LogWarning(
                    "ZIP packer: skip empty FileData for work permit document row {Index} workPermit {WorkPermitId} ({Slot}); file id {FileId}, reported size {Size}.",
                    i,
                    workPermit.ID,
                    currentOrPrevious,
                    doc?.File?.ID,
                    doc?.File?.Size);
                continue;
            }

            if (emitIndividualZipEntries
                && await TryWriteRawBytesAsync(archive, rel, content, logger, cancellationToken).ConfigureAwait(false))
                writtenPaths.Add(rel);

            if (addMergeSlices
                && TryCreateMergeSlicePdfStream(content, ext, "CurrentWorkPermits", logger, out var slice))
            {
                currentWorkPermitPdfMergeSlices!.Add(slice);
                workPermitIdsContributedToCurrentBatchMerge?.Add(workPermit.ID);
            }
        }

        logger.LogInformation(
            "ZIP packer: WorkPermit {Slot} itemFolder={ItemSlug} workPermitId={WorkPermitId}: {Written} of {Total} document row(s) written to ZIP. Paths={Paths}",
            currentOrPrevious,
            itemSlug,
            workPermit.ID,
            writtenPaths.Count,
            docs.Count,
            writtenPaths.Count == 0
                ? "(none — check ZIP packer skip/failed warnings above)"
                : JoinPathsForLog(writtenPaths, MaxPassportPathsLogChars));
    }

    private static async Task AppendInvitationAsync(
        IObjectSpace os,
        ZipArchive archive,
        HashSet<string> reservedZipPaths,
        string? zipInnerRoot,
        ApplicationItem item,
        string itemSlug,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        // Line-scoped: ApplicationItem.CurrentInvitationItem only (no PreviousInvitationItem on the item).
        var inv = item.CurrentInvitationItem?.Invitation;
        if (inv == null)
            return;

        await WriteInvitationSlotAsync(os, archive, reservedZipPaths, zipInnerRoot, itemSlug, inv, "Current", logger, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteInvitationSlotAsync(
        IObjectSpace os,
        ZipArchive archive,
        HashSet<string> reservedZipPaths,
        string? zipInnerRoot,
        string itemSlug,
        Invitation invitation,
        string currentOrPrevious,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var docs = await os.GetObjectsQuery<InvitationDocument>()
            .Where(d => d.Invitation.ID == invitation.ID)
            .OrderBy(d => d.ID)
            .Include(d => d.File)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        if (docs.Count == 0)
        {
            logger.LogInformation(
                "ZIP packer: no InvitationDocument rows for invitation {InvitationId} ({Slot}); itemFolder={ItemSlug}.",
                invitation.ID,
                currentOrPrevious,
                itemSlug);
            return;
        }

        var writtenPaths = new List<string>(docs.Count);
        int i = 0;
        foreach (var doc in docs)
        {
            i++;
            string ext = GetExtension(doc?.File);
            string rel = ZipEntryPath(zipInnerRoot, $"Invitation/{itemSlug}/{currentOrPrevious}/doc{i:00}{ext}");
            rel = ReserveZipEntryPath(reservedZipPaths, rel);

            byte[]? content = await GetDocumentFileContentAsync(os, doc, cancellationToken).ConfigureAwait(false);
            if (content == null || content.Length == 0)
            {
                logger.LogWarning(
                    "ZIP packer: skip empty FileData for invitation document row {Index} invitation {InvitationId} ({Slot}); file id {FileId}, reported size {Size}.",
                    i,
                    invitation.ID,
                    currentOrPrevious,
                    doc?.File?.ID,
                    doc?.File?.Size);
                continue;
            }

            if (await TryWriteRawBytesAsync(archive, rel, content, logger, cancellationToken).ConfigureAwait(false))
                writtenPaths.Add(rel);
        }

        logger.LogInformation(
            "ZIP packer: Invitation {Slot} itemFolder={ItemSlug} invitationId={InvitationId}: {Written} of {Total} document row(s) written to ZIP. Paths={Paths}",
            currentOrPrevious,
            itemSlug,
            invitation.ID,
            writtenPaths.Count,
            docs.Count,
            writtenPaths.Count == 0
                ? "(none — check ZIP packer skip/failed warnings above)"
                : JoinPathsForLog(writtenPaths, MaxPassportPathsLogChars));
    }

    private static async Task AppendFamilyRelationshipAsync(
        IObjectSpace os,
        ZipArchive archive,
        HashSet<string> reservedZipPaths,
        string? zipInnerRoot,
        ApplicationItem item,
        string itemSlug,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        // Person-scoped PersonDocument rows (family relationship package; caller only invokes for non-employees).
        var personId = item.Person.ID;
        var docs = await os.GetObjectsQuery<PersonDocument>()
            .Where(d => d.Person.ID == personId)
            .OrderBy(d => d.ID)
            .Include(d => d.File)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        if (docs.Count == 0)
        {
            logger.LogInformation(
                "ZIP packer: no PersonDocument rows for person {PersonId} (itemFolder={ItemSlug}); family-relationship ZIP section empty.",
                personId,
                itemSlug);
            return;
        }

        var writtenPaths = new List<string>(docs.Count);
        int i = 0;
        foreach (var doc in docs)
        {
            i++;
            string ext = GetExtension(doc?.File);
            string rel = ZipEntryPath(zipInnerRoot, $"FamilyRelationship/{itemSlug}/Current/doc{i:00}{ext}");
            rel = ReserveZipEntryPath(reservedZipPaths, rel);

            byte[]? content = await GetDocumentFileContentAsync(os, doc, cancellationToken).ConfigureAwait(false);
            if (content == null || content.Length == 0)
            {
                logger.LogWarning(
                    "ZIP packer: skip empty FileData for person document row {Index} person {PersonId} (family relationship); file id {FileId}, reported size {Size}.",
                    i,
                    personId,
                    doc?.File?.ID,
                    doc?.File?.Size);
                continue;
            }

            if (await TryWriteRawBytesAsync(archive, rel, content, logger, cancellationToken).ConfigureAwait(false))
                writtenPaths.Add(rel);
        }

        logger.LogInformation(
            "ZIP packer: FamilyRelationship Current itemFolder={ItemSlug} personId={PersonId}: {Written} of {Total} document row(s) written to ZIP. Paths={Paths}",
            itemSlug,
            personId,
            writtenPaths.Count,
            docs.Count,
            writtenPaths.Count == 0
                ? "(none — check ZIP packer skip/failed warnings above)"
                : JoinPathsForLog(writtenPaths, MaxPassportPathsLogChars));
    }

    private static bool IsLikelyPdfBytes(byte[] content) =>
        content is { Length: >= 5 }
        && content[0] == (byte)'%'
        && content[1] == (byte)'P'
        && content[2] == (byte)'D'
        && content[3] == (byte)'F';

    /// <summary>
    /// Builds one PDF stream per attachment for batch merges (PDF passthrough; raster images → one page each).
    /// </summary>
    private static bool TryCreateMergeSlicePdfStream(
        byte[] content,
        string ext,
        string mergeKind,
        ILogger logger,
        out MemoryStream pdfStream)
    {
        pdfStream = null!;
        if (content == null || content.Length == 0)
            return false;

        if (IsLikelyPdfBytes(content))
        {
            var copy = new MemoryStream(content.Length);
            copy.Write(content, 0, content.Length);
            copy.Position = 0;
            pdfStream = copy;
            return true;
        }

        if (IsPdfExtension(ext))
        {
            logger.LogWarning(
                "ZIP packer: file has extension {Ext} but payload is not a PDF signature; trying image decode for {MergeKind} merge.",
                ext,
                mergeKind);
        }

        try
        {
            var outMs = new MemoryStream();
            if (!SupportingDocumentsPdfSharpHelper.TryWriteSinglePagePdfFromRasterBytes(content, outMs, logger))
                return false;
            outMs.Position = 0;
            pdfStream = outMs;
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "ZIP packer: could not rasterize attachment ({Ext}) for {MergeKind} merge; skipping that file.",
                ext,
                mergeKind);
            return false;
        }
    }

    private static MemoryStream CloneMemoryStream(MemoryStream source)
    {
        source.Position = 0;
        var copy = new MemoryStream((int)Math.Max(source.Length, 0));
        source.CopyTo(copy);
        copy.Position = 0;
        source.Position = 0;
        return copy;
    }

    private static async Task<byte[]?> GetDocumentFileContentAsync(
        IObjectSpace objectSpace,
        DocumentBase doc,
        CancellationToken cancellationToken)
    {
        if (doc?.File == null)
            return null;

        byte[] content = doc.File.Content;
        if ((content == null || content.Length == 0) && doc.File.Size > 0)
        {
            var fileId = doc.File.ID;
            var fd = await objectSpace.GetObjectsQuery<FileData>()
                .Where(f => f.ID == fileId)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            content = fd?.Content;
        }

        return content is { Length: > 0 } ? content : null;
    }

    /// <returns><see langword="true"/> if a non-empty ZIP entry was written.</returns>
    private static async Task<bool> TryWriteDocumentAsync(
        IObjectSpace objectSpace,
        ZipArchive archive,
        string relativePath,
        DocumentBase doc,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        byte[]? content = await GetDocumentFileContentAsync(objectSpace, doc, cancellationToken).ConfigureAwait(false);
        if (content == null)
        {
            if (doc?.File != null)
            {
                logger.LogWarning(
                    "ZIP packer: skip empty FileData for document {DocType} id {DocId}; file id {FileId}, reported size {Size}.",
                    doc.GetType().Name,
                    osGetKey(doc),
                    doc.File.ID,
                    doc.File.Size);
            }

            return false;
        }

        return await TryWriteRawBytesAsync(archive, relativePath, content, logger, cancellationToken).ConfigureAwait(false);
    }

    /// <returns><see langword="true"/> if bytes were written to a new ZIP entry.</returns>
    private static async Task<bool> TryWriteRawBytesAsync(
        ZipArchive archive,
        string relativePath,
        byte[] content,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (content == null || content.Length == 0)
            return false;

        try
        {
            var entry = archive.CreateEntry(relativePath, CompressionLevel.Fastest);
            await using (var es = entry.Open())
            {
                await es.WriteAsync(content, cancellationToken).ConfigureAwait(false);
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ZIP packer: failed to write entry {Path}.", relativePath);
            return false;
        }
    }

    private static string JoinPathsForLog(IReadOnlyList<string> paths, int maxChars)
    {
        if (paths == null || paths.Count == 0)
            return "(none)";

        var sb = new StringBuilder();
        for (int i = 0; i < paths.Count; i++)
        {
            if (i > 0)
                sb.Append("; ");
            string p = paths[i].Replace('\\', '/');
            if (sb.Length + p.Length > maxChars)
            {
                sb.Append("… (+").Append(paths.Count - i).Append(" more)");
                break;
            }

            sb.Append(p);
        }

        return sb.ToString();
    }

    private static string osGetKey(DocumentBase doc) => doc is DevExpress.Persistent.BaseImpl.EF.BaseObject bo ? bo.ID.ToString() : "?";

    private static string GetExtension(FileData file)
    {
        if (file == null)
            return ".bin";
        string name = file.FileName;
        if (string.IsNullOrWhiteSpace(name))
            return ".bin";
        var ext = Path.GetExtension(name);
        return string.IsNullOrEmpty(ext) ? ".bin" : SanitizeSegment(ext, 10);
    }

    private static bool IsPdfExtension(string ext) =>
        string.Equals(ext, ".pdf", StringComparison.OrdinalIgnoreCase);

    /// <summary>Reserves a unique path inside the ZIP (call before <see cref="ZipArchive.CreateEntry"/>).</summary>
    public static string ReserveZipEntryPath(HashSet<string> reserved, string path)
    {
        string p = path.Replace('\\', '/');
        if (p.Length > MaxPathLength)
            p = TrimPathLength(p);

        if (reserved.Add(p))
            return p;

        string dir = Path.GetDirectoryName(p)?.Replace('\\', '/') ?? "";
        string stem = Path.GetFileNameWithoutExtension(p);
        string ext = Path.GetExtension(p);
        for (int n = 2; n < 10_000; n++)
        {
            string cand = string.IsNullOrEmpty(dir)
                ? $"{stem}_{n}{ext}"
                : $"{dir}/{stem}_{n}{ext}";
            cand = cand.Replace('\\', '/');
            if (reserved.Add(cand))
                return cand.Length > MaxPathLength ? TrimPathLength(cand) : cand;
        }

        string fallback = $"{p}_dup_{Guid.NewGuid():N}";
        reserved.Add(fallback);
        return fallback;
    }

    private static string TrimPathLength(string path)
    {
        if (path.Length <= MaxPathLength)
            return path.Replace('\\', '/');

        string ext = Path.GetExtension(path);
        string withoutExt = path.Length > ext.Length ? path[..^ext.Length] : path;
        int take = MaxPathLength - ext.Length - 4;
        if (take < 8)
            take = 8;
        return withoutExt[..Math.Min(withoutExt.Length, take)] + "_tr" + ext;
    }

    private static string SanitizeSegment(string value, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "NA";

        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(Math.Min(value.Length, maxLen));
        foreach (var ch in value.Trim())
        {
            if (invalid.Contains(ch) || ch is '/' or '\\' or ':' or '?' or '*' or '"' or '<' or '>' or '|')
                sb.Append('_');
            else
                sb.Append(ch == ' ' ? '_' : ch);
            if (sb.Length >= maxLen)
                break;
        }

        string s = sb.ToString();
        if (string.IsNullOrEmpty(s))
            s = "NA";
        return Regex.Replace(s, @"_+", "_", RegexOptions.CultureInvariant);
    }
}
