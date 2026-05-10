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
/// Rules: <c>docs/APPLICATION_DIPLOMA_PACKAGE_PLAN.md</c> (§1.3 eligibility, §4.8 FileData-only, §4.4 naming, §4.5–§4.7 dedupe).
/// </summary>
public static class ApplicationSupportingDocumentsPacker
{
    private const int MaxPathLength = 220;

    /// <summary>Per-ZIP set of normalized entry paths already reserved (case-insensitive).</summary>
    public static string BuildItemSlug(int itemIndex1Based, Person person)
    {
        string first = SanitizeSegment(person?.FirstName ?? "NA", 24);
        string last = SanitizeSegment(person?.LastName ?? "NA", 24);
        return $"{itemIndex1Based:00}_{first}_{last}";
    }

    public static async Task AppendSupportingDocumentsForItemAsync(
        IObjectSpace objectSpace,
        PdfGenerationBatch batch,
        ZipArchive archive,
        HashSet<string> reservedZipPaths,
        string zipInnerRoot,
        ApplicationItem item,
        int itemIndex1Based,
        HashSet<Guid> emittedWorkPermitIds,
        HashSet<Guid> emittedInvitationIds,
        List<MemoryStream> diplomaPdfBuffersForMerge,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (item?.Person == null)
            return;

        string itemSlug = BuildItemSlug(itemIndex1Based, item.Person);
        string root = zipInnerRoot.TrimEnd('/');

        if (batch.IncludeDiplomaFiles)
            await AppendDiplomasAsync(objectSpace, batch, archive, reservedZipPaths, root, item, itemSlug, diplomaPdfBuffersForMerge, logger, cancellationToken).ConfigureAwait(false);

        if (batch.IncludePassportCopies)
            await AppendPassportAsync(objectSpace, archive, reservedZipPaths, root, item, itemSlug, logger, cancellationToken).ConfigureAwait(false);

        if (batch.IncludeVisaCopies)
            await AppendVisaAsync(objectSpace, archive, reservedZipPaths, root, item, itemSlug, logger, cancellationToken).ConfigureAwait(false);

        if (batch.IncludeMedicalRecordCopies)
            await AppendMedicalRecordAsync(objectSpace, archive, reservedZipPaths, root, item, itemSlug, logger, cancellationToken).ConfigureAwait(false);

        if (batch.IncludeAddressOfResidenceCopies)
            await AppendAddressOfResidenceAsync(objectSpace, archive, reservedZipPaths, root, item, itemSlug, logger, cancellationToken).ConfigureAwait(false);

        if (batch.IncludeWorkPermitCopies && item.Person.IsEmployee)
            await AppendWorkPermitSharedAsync(objectSpace, archive, reservedZipPaths, root, item, emittedWorkPermitIds, logger, cancellationToken).ConfigureAwait(false);

        if (batch.IncludeInvitationCopies)
            await AppendInvitationSharedAsync(objectSpace, archive, reservedZipPaths, root, item, emittedInvitationIds, logger, cancellationToken).ConfigureAwait(false);

        if (batch.IncludeFamilyRelationshipCopies && !item.Person.IsEmployee)
            await AppendFamilyRelationshipAsync(objectSpace, archive, reservedZipPaths, root, item, itemSlug, logger, cancellationToken).ConfigureAwait(false);
    }

    public static async Task WriteMergedDiplomaPdfIfNeededAsync(
        IPdfFormFillerService pdfFiller,
        ZipArchive archive,
        HashSet<string> reservedZipPaths,
        string zipInnerRoot,
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

            string root = zipInnerRoot.TrimEnd('/');
            string rel = $"{root}/Diplomas/{itemSlug}/_AllDiplomas_merged.pdf";
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
        string zipInnerRoot,
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
                string rel = $"{zipInnerRoot}/Diplomas/{itemSlug}/{fileStem}{ext}";
                rel = ReserveZipEntryPath(reservedZipPaths, rel);
                await TryWriteDocumentAsync(archive, rel, doc, logger, cancellationToken).ConfigureAwait(false);

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

    private static async Task AppendPassportAsync(
        IObjectSpace os,
        ZipArchive archive,
        HashSet<string> reservedZipPaths,
        string zipInnerRoot,
        ApplicationItem item,
        string itemSlug,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var cur = item.CurrentPassport;
        var prev = item.PreviousPassport;
        bool duplicate = cur != null && prev != null && cur.ID == prev.ID;
        if (duplicate)
            logger.LogWarning(
                "ZIP packer: ApplicationItem {ItemId} has same Passport for Current and Previous; packing Current only.",
                item.ID);

        if (cur != null)
            await WritePassportSlotAsync(os, archive, reservedZipPaths, zipInnerRoot, itemSlug, cur, "Current", logger, cancellationToken).ConfigureAwait(false);

        if (prev != null && !duplicate)
            await WritePassportSlotAsync(os, archive, reservedZipPaths, zipInnerRoot, itemSlug, prev, "Previous", logger, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WritePassportSlotAsync(
        IObjectSpace os,
        ZipArchive archive,
        HashSet<string> reservedZipPaths,
        string zipInnerRoot,
        string itemSlug,
        Passport passport,
        string currentOrPrevious,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var docs = await os.GetObjectsQuery<PassportDocument>()
            .Where(d => d.Passport.ID == passport.ID)
            .Include(d => d.File)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        int i = 0;
        foreach (var doc in docs)
        {
            i++;
            string ext = GetExtension(doc?.File);
            string rel = $"{zipInnerRoot}/Passport/{itemSlug}/{currentOrPrevious}/doc{i:00}{ext}";
            rel = ReserveZipEntryPath(reservedZipPaths, rel);
            await TryWriteDocumentAsync(archive, rel, doc, logger, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task AppendVisaAsync(
        IObjectSpace os,
        ZipArchive archive,
        HashSet<string> reservedZipPaths,
        string zipInnerRoot,
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
        string zipInnerRoot,
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
            string rel = $"{zipInnerRoot}/Visa/{itemSlug}/{currentOrPrevious}/doc{i:00}{ext}";
            rel = ReserveZipEntryPath(reservedZipPaths, rel);
            await TryWriteDocumentAsync(archive, rel, doc, logger, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task AppendMedicalRecordAsync(
        IObjectSpace os,
        ZipArchive archive,
        HashSet<string> reservedZipPaths,
        string zipInnerRoot,
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
            string rel = $"{zipInnerRoot}/MedicalRecord/{itemSlug}/doc{i:00}{ext}";
            rel = ReserveZipEntryPath(reservedZipPaths, rel);
            await TryWriteDocumentAsync(archive, rel, doc, logger, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task AppendAddressOfResidenceAsync(
        IObjectSpace os,
        ZipArchive archive,
        HashSet<string> reservedZipPaths,
        string zipInnerRoot,
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
            string rel = $"{zipInnerRoot}/AddressOfResidence/{itemSlug}/doc{i:00}{ext}";
            rel = ReserveZipEntryPath(reservedZipPaths, rel);
            await TryWriteDocumentAsync(archive, rel, doc, logger, cancellationToken).ConfigureAwait(false);
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
                string rel = $"{zipInnerRoot}/AddressOfResidence/{itemSlug}/Lodging/doc{j:00}{ext}";
                rel = ReserveZipEntryPath(reservedZipPaths, rel);
                await TryWriteDocumentAsync(archive, rel, doc, logger, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static async Task AppendWorkPermitSharedAsync(
        IObjectSpace os,
        ZipArchive archive,
        HashSet<string> reservedZipPaths,
        string zipInnerRoot,
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
            string folder = $"{zipInnerRoot}/WorkPermit/_Shared/WP_{num}_{shortKey}";

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
                await TryWriteDocumentAsync(archive, rel, doc, logger, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static async Task AppendInvitationSharedAsync(
        IObjectSpace os,
        ZipArchive archive,
        HashSet<string> reservedZipPaths,
        string zipInnerRoot,
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
        string folder = $"{zipInnerRoot}/Invitation/_Shared/INV_{num}_{shortKey}";

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
            await TryWriteDocumentAsync(archive, rel, doc, logger, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task AppendFamilyRelationshipAsync(
        IObjectSpace os,
        ZipArchive archive,
        HashSet<string> reservedZipPaths,
        string zipInnerRoot,
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
            string rel = $"{zipInnerRoot}/FamilyRelationship/{itemSlug}/doc{i:00}{ext}";
            rel = ReserveZipEntryPath(reservedZipPaths, rel);
            await TryWriteDocumentAsync(archive, rel, doc, logger, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task TryWriteDocumentAsync(
        ZipArchive archive,
        string relativePath,
        DocumentBase doc,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (doc?.File == null)
            return;

        byte[] content = doc.File.Content;
        if (content == null || content.Length == 0)
        {
            logger.LogDebug("ZIP packer: skip empty FileData for document {DocType} key {DocId}.", doc.GetType().Name, osGetKey(doc));
            return;
        }

        try
        {
            var entry = archive.CreateEntry(relativePath, CompressionLevel.Fastest);
            await using var es = entry.Open();
            await es.WriteAsync(content, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ZIP packer: failed to write entry {Path}.", relativePath);
        }
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
