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
/// Console verification: search for <c>ZIP packer: Passport</c> after a batch run to confirm counts and entry paths.
/// When passport copies are included, a batch-wide merge is written as <c>Passport/CurrentPassports.pdf</c>
/// (log: <c>ZIP packer: CurrentPassports</c>) using PdfSharpCore, not Spire, so merged output has no Spire evaluation banner.
/// </summary>
public static class ApplicationSupportingDocumentsPacker
{
    private const int MaxPathLength = 220;
    private const int MaxPassportPathsLogChars = 900;

    /// <summary>Batch-wide merge of every application line's <i>current</i> passport document files (ZIP root).</summary>
    public const string MergedCurrentPassportsZipRelativePath = "Passport/CurrentPassports.pdf";

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
    public static async Task AppendSupportingDocumentsForItemAsync(
        IObjectSpace objectSpace,
        PdfGenerationBatch batch,
        ZipArchive archive,
        HashSet<string> reservedZipPaths,
        string? zipInnerRoot,
        ApplicationItem item,
        int itemIndex1Based,
        HashSet<Guid> emittedWorkPermitIds,
        HashSet<Guid> emittedInvitationIds,
        List<MemoryStream> diplomaPdfBuffersForMerge,
        List<MemoryStream>? currentPassportPdfMergeSlices,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (item?.Person == null)
            return;

        string itemSlug = BuildItemSlug(itemIndex1Based, item.Person);

        if (batch.IncludeDiplomaFiles)
            await AppendDiplomasAsync(objectSpace, batch, archive, reservedZipPaths, zipInnerRoot, item, itemSlug, diplomaPdfBuffersForMerge, logger, cancellationToken).ConfigureAwait(false);

        if (batch.IncludePassportCopies)
            await AppendPassportAsync(objectSpace, archive, reservedZipPaths, zipInnerRoot, item, itemSlug, currentPassportPdfMergeSlices, logger, cancellationToken).ConfigureAwait(false);

        if (batch.IncludeVisaCopies)
            await AppendVisaAsync(objectSpace, archive, reservedZipPaths, zipInnerRoot, item, itemSlug, logger, cancellationToken).ConfigureAwait(false);

        if (batch.IncludeMedicalRecordCopies)
            await AppendMedicalRecordAsync(objectSpace, archive, reservedZipPaths, zipInnerRoot, item, itemSlug, logger, cancellationToken).ConfigureAwait(false);

        if (batch.IncludeAddressOfResidenceCopies)
            await AppendAddressOfResidenceAsync(objectSpace, archive, reservedZipPaths, zipInnerRoot, item, itemSlug, logger, cancellationToken).ConfigureAwait(false);

        if (batch.IncludeWorkPermitCopies && item.Person.IsEmployee)
            await AppendWorkPermitSharedAsync(objectSpace, archive, reservedZipPaths, zipInnerRoot, item, emittedWorkPermitIds, logger, cancellationToken).ConfigureAwait(false);

        if (batch.IncludeInvitationCopies)
            await AppendInvitationSharedAsync(objectSpace, archive, reservedZipPaths, zipInnerRoot, item, emittedInvitationIds, logger, cancellationToken).ConfigureAwait(false);

        if (batch.IncludeFamilyRelationshipCopies && !item.Person.IsEmployee)
            await AppendFamilyRelationshipAsync(objectSpace, archive, reservedZipPaths, zipInnerRoot, item, itemSlug, logger, cancellationToken).ConfigureAwait(false);
    }

    /// <param name="zipInnerRoot">Same semantics as <see cref="AppendSupportingDocumentsForItemAsync"/> (null/empty = archive root).</param>
    public static async Task WriteMergedDiplomaPdfIfNeededAsync(
        IPdfFormFillerService pdfFiller,
        ZipArchive archive,
        HashSet<string> reservedZipPaths,
        string? zipInnerRoot,
        string itemSlug,
        List<MemoryStream> diplomaPdfBuffersForMerge,
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
            pdfFiller.MergePdfs(diplomaPdfBuffersForMerge.ToArray(), merged);
            merged.Position = 0;

            string rel = ZipEntryPath(zipInnerRoot, $"Diplomas/{itemSlug}/_AllDiplomas_merged.pdf");
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
        List<MemoryStream> mergeOut,
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
                .Include(d => d.File)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            int docIdx = 0;
            foreach (var doc in docs)
            {
                docIdx++;
                string inst = SanitizeSegment(edu.EducationInstitution?.Name ?? "Edu", 20);
                string year = edu.GraduationYear?.ToString(CultureInfo.InvariantCulture) ?? "Year";
                string ext = GetExtension(doc?.File);
                string fileStem = $"E{eduIndex:00}_{year}_{inst}_doc{docIdx:00}";
                string rel = ZipEntryPath(zipInnerRoot, $"Diplomas/{itemSlug}/{fileStem}{ext}");
                rel = ReserveZipEntryPath(reservedZipPaths, rel);
                await TryWriteDocumentAsync(os, archive, rel, doc, logger, cancellationToken).ConfigureAwait(false);

                if (batch.IncludeMergedDiplomaPdf && mergeOut != null && IsPdfExtension(ext) && doc?.File?.Content is { Length: > 0 } content)
                {
                    var copy = new MemoryStream();
                    await copy.WriteAsync(content, cancellationToken).ConfigureAwait(false);
                    copy.Position = 0;
                    mergeOut.Add(copy);
                }
            }
        }
    }

    /// <summary>
    /// Writes <see cref="MergedCurrentPassportsZipRelativePath"/> once per ZIP after all items are packed.
    /// <paramref name="sources"/> are disposed here (same pattern as merged diplomas).
    /// </summary>
    public static async Task WriteMergedCurrentPassportsPdfIfNeededAsync(
        ZipArchive archive,
        HashSet<string> reservedZipPaths,
        string? zipInnerRoot,
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
            CurrentPassportsPdfSharpHelper.MergePdfStreams(sources, merged);
            merged.Position = 0;

            string rel = ZipEntryPath(zipInnerRoot, MergedCurrentPassportsZipRelativePath);
            rel = ReserveZipEntryPath(reservedZipPaths, rel);
            var entry = archive.CreateEntry(rel, CompressionLevel.Fastest);
            await using var es = entry.Open();
            await merged.CopyToAsync(es, cancellationToken).ConfigureAwait(false);

            logger.LogInformation(
                "ZIP packer: CurrentPassports merge wrote {Rel} from {Count} source file(s) (batch item order, Current passport only).",
                rel,
                sources.Count);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ZIP packer: CurrentPassports.pdf merge skipped.");
        }
        finally
        {
            foreach (var s in sources)
                await s.DisposeAsync().ConfigureAwait(false);
            sources.Clear();
        }
    }

    private static async Task AppendPassportAsync(
        IObjectSpace os,
        ZipArchive archive,
        HashSet<string> reservedZipPaths,
        string? zipInnerRoot,
        ApplicationItem item,
        string itemSlug,
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
            await WritePassportSlotAsync(os, archive, reservedZipPaths, zipInnerRoot, itemSlug, cur, "Current", currentPassportPdfMergeSlices, logger, cancellationToken).ConfigureAwait(false);

        if (prev != null && !duplicate)
            await WritePassportSlotAsync(os, archive, reservedZipPaths, zipInnerRoot, itemSlug, prev, "Previous", null, logger, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WritePassportSlotAsync(
        IObjectSpace os,
        ZipArchive archive,
        HashSet<string> reservedZipPaths,
        string? zipInnerRoot,
        string itemSlug,
        Passport passport,
        string currentOrPrevious,
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

            if (await TryWriteRawBytesAsync(archive, rel, content, logger, cancellationToken).ConfigureAwait(false))
                writtenPaths.Add(rel);

            if (currentPassportPdfMergeSlices != null
                && currentOrPrevious.Equals("Current", StringComparison.OrdinalIgnoreCase)
                && TryCreatePassportMergePdfStream(content, ext, logger, out var slice))
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
            await WriteVisaSlotAsync(os, archive, reservedZipPaths, zipInnerRoot, itemSlug, cur, "Current", logger, cancellationToken).ConfigureAwait(false);

        if (prev != null && !duplicate)
            await WriteVisaSlotAsync(os, archive, reservedZipPaths, zipInnerRoot, itemSlug, prev, "Previous", logger, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteVisaSlotAsync(
        IObjectSpace os,
        ZipArchive archive,
        HashSet<string> reservedZipPaths,
        string? zipInnerRoot,
        string itemSlug,
        Visa visa,
        string currentOrPrevious,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var docs = await os.GetObjectsQuery<VisaDocument>()
            .Where(d => d.Visa.ID == visa.ID)
            .Include(d => d.File)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        int i = 0;
        foreach (var doc in docs)
        {
            i++;
            string ext = GetExtension(doc?.File);
            string rel = ZipEntryPath(zipInnerRoot, $"Visa/{itemSlug}/{currentOrPrevious}/doc{i:00}{ext}");
            rel = ReserveZipEntryPath(reservedZipPaths, rel);
            await TryWriteDocumentAsync(os, archive, rel, doc, logger, cancellationToken).ConfigureAwait(false);
        }
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
        if (item.CurrentMedicalRecord == null)
            return;

        var docs = await os.GetObjectsQuery<MedicalRecordDocument>()
            .Where(d => d.MedicalRecord.ID == item.CurrentMedicalRecord.ID)
            .Include(d => d.File)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        int i = 0;
        foreach (var doc in docs)
        {
            i++;
            string ext = GetExtension(doc?.File);
            string rel = ZipEntryPath(zipInnerRoot, $"MedicalRecord/{itemSlug}/doc{i:00}{ext}");
            rel = ReserveZipEntryPath(reservedZipPaths, rel);
            await TryWriteDocumentAsync(os, archive, rel, doc, logger, cancellationToken).ConfigureAwait(false);
        }
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
        if (item.CurrentAddressOfResidence == null)
            return;

        var addr = item.CurrentAddressOfResidence;
        var addrDocs = await os.GetObjectsQuery<AddressOfResidenceDocument>()
            .Where(d => d.AddressOfResidence.ID == addr.ID)
            .Include(d => d.File)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        int i = 0;
        foreach (var doc in addrDocs)
        {
            i++;
            string ext = GetExtension(doc?.File);
            string rel = ZipEntryPath(zipInnerRoot, $"AddressOfResidence/{itemSlug}/doc{i:00}{ext}");
            rel = ReserveZipEntryPath(reservedZipPaths, rel);
            await TryWriteDocumentAsync(os, archive, rel, doc, logger, cancellationToken).ConfigureAwait(false);
        }

        if (addr.Type == ResidenceType.Lodging && addr.Lodging != null)
        {
            var lodgingId = addr.Lodging.ID;
            var lodDocs = await os.GetObjectsQuery<LodgingDocument>()
                .Where(d => d.Lodging.ID == lodgingId)
                .Include(d => d.File)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            int j = 0;
            foreach (var doc in lodDocs)
            {
                j++;
                string ext = GetExtension(doc?.File);
                string rel = ZipEntryPath(zipInnerRoot, $"AddressOfResidence/{itemSlug}/Lodging/doc{j:00}{ext}");
                rel = ReserveZipEntryPath(reservedZipPaths, rel);
                await TryWriteDocumentAsync(os, archive, rel, doc, logger, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static async Task AppendWorkPermitSharedAsync(
        IObjectSpace os,
        ZipArchive archive,
        HashSet<string> reservedZipPaths,
        string? zipInnerRoot,
        ApplicationItem item,
        HashSet<Guid> emittedWorkPermitIds,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        foreach (var wpi in new[] { item.CurrentWorkPermitItem, item.PreviousWorkPermitItem })
        {
            var wp = wpi?.WorkPermit;
            if (wp == null)
                continue;
            if (!emittedWorkPermitIds.Add(wp.ID))
                continue;

            string num = SanitizeSegment(string.IsNullOrWhiteSpace(wp.WorkPermitNumber) ? "WP" : wp.WorkPermitNumber, 24);
            string shortKey = wp.ID.ToString("N", CultureInfo.InvariantCulture)[..8];
            string folder = ZipEntryPath(zipInnerRoot, $"WorkPermit/_Shared/WP_{num}_{shortKey}");

            var docs = await os.GetObjectsQuery<WorkPermitDocument>()
                .Where(d => d.WorkPermit.ID == wp.ID)
                .Include(d => d.File)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            int i = 0;
            foreach (var doc in docs)
            {
                i++;
                string ext = GetExtension(doc?.File);
                string rel = $"{folder}/doc{i:00}{ext}";
                rel = ReserveZipEntryPath(reservedZipPaths, rel);
                await TryWriteDocumentAsync(os, archive, rel, doc, logger, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static async Task AppendInvitationSharedAsync(
        IObjectSpace os,
        ZipArchive archive,
        HashSet<string> reservedZipPaths,
        string? zipInnerRoot,
        ApplicationItem item,
        HashSet<Guid> emittedInvitationIds,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var ii = item.CurrentInvitationItem;
        var inv = ii?.Invitation;
        if (inv == null)
            return;
        if (!emittedInvitationIds.Add(inv.ID))
            return;

        string num = SanitizeSegment(string.IsNullOrWhiteSpace(inv.InvitationNumber) ? "INV" : inv.InvitationNumber, 24);
        string shortKey = inv.ID.ToString("N", CultureInfo.InvariantCulture)[..8];
        string folder = ZipEntryPath(zipInnerRoot, $"Invitation/_Shared/INV_{num}_{shortKey}");

        var docs = await os.GetObjectsQuery<InvitationDocument>()
            .Where(d => d.Invitation.ID == inv.ID)
            .Include(d => d.File)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        int i = 0;
        foreach (var doc in docs)
        {
            i++;
            string ext = GetExtension(doc?.File);
            string rel = $"{folder}/doc{i:00}{ext}";
            rel = ReserveZipEntryPath(reservedZipPaths, rel);
            await TryWriteDocumentAsync(os, archive, rel, doc, logger, cancellationToken).ConfigureAwait(false);
        }
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
        var personId = item.Person.ID;
        var docs = await os.GetObjectsQuery<PersonDocument>()
            .Where(d => d.Person.ID == personId)
            .Include(d => d.File)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        int i = 0;
        foreach (var doc in docs)
        {
            i++;
            string ext = GetExtension(doc?.File);
            string rel = ZipEntryPath(zipInnerRoot, $"FamilyRelationship/{itemSlug}/doc{i:00}{ext}");
            rel = ReserveZipEntryPath(reservedZipPaths, rel);
            await TryWriteDocumentAsync(os, archive, rel, doc, logger, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool IsLikelyPdfBytes(byte[] content) =>
        content is { Length: >= 5 }
        && content[0] == (byte)'%'
        && content[1] == (byte)'P'
        && content[2] == (byte)'D'
        && content[3] == (byte)'F';

    /// <summary>
    /// Builds one PDF stream per passport file for <see cref="WriteMergedCurrentPassportsPdfIfNeededAsync"/> (PDF passthrough; raster images → one page each).
    /// </summary>
    private static bool TryCreatePassportMergePdfStream(
        byte[] content,
        string ext,
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
                "ZIP packer: file has extension {Ext} but payload is not a PDF signature; trying image decode for CurrentPassports merge.",
                ext);
        }

        try
        {
            var outMs = new MemoryStream();
            if (!CurrentPassportsPdfSharpHelper.TryWriteSinglePagePdfFromRasterBytes(content, outMs, logger))
                return false;
            outMs.Position = 0;
            pdfStream = outMs;
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "ZIP packer: could not rasterize passport attachment ({Ext}) for CurrentPassports merge; skipping that file.",
                ext);
            return false;
        }
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
