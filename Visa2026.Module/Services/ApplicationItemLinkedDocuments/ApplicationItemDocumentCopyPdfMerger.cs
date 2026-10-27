using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.ApplicationItemLinkedDocuments;

namespace Visa2026.Module.Services.ApplicationItemLinkedDocuments;

public sealed class ApplicationItemDocumentCopyPdfMerger
{
    private readonly INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory;
    private readonly ILogger<ApplicationItemDocumentCopyPdfMerger> logger;

    public ApplicationItemDocumentCopyPdfMerger(
        INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory,
        ILogger<ApplicationItemDocumentCopyPdfMerger> logger)
    {
        this.nonSecuredObjectSpaceFactory = nonSecuredObjectSpaceFactory;
        this.logger = logger;
    }

    /// <summary>
    /// Merge slot files for skip-navigation People on an application.
    /// <paramref name="applicationPersonIds"/> and <see cref="ApplicationItemLinkedDocumentFileEntry.ApplicationItemId"/>
    /// are Person ids (legacy property name).
    /// </summary>
    public bool TryBuildMergedPdfForRoster(
        IReadOnlyList<Guid> applicationPersonIds,
        string slotKey,
        string slotLabel,
        IReadOnlyList<ApplicationItemLinkedDocumentFileEntry> entries,
        out byte[]? content,
        out string? fileName,
        Guid applicationId = default,
        DocumentCopyPrintLayout? printLayout = null)
    {
        content = null;
        fileName = null;

        if (applicationPersonIds == null || applicationPersonIds.Count == 0 || string.IsNullOrWhiteSpace(slotKey))
            return false;

        if (entries == null || entries.Count == 0)
            return false;

        var allowedRowIds = applicationPersonIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToHashSet();

        if (allowedRowIds.Count == 0)
            return false;

        using var objectSpace = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<ApplicationProfileInstance>();
        if (!ApplicationRosterHelper.TryLoadSharedApplicationPeople(
                objectSpace,
                applicationPersonIds,
                applicationId,
                out var application,
                out var people)
            || application == null)
        {
            return false;
        }

        var snapshots = new Dictionary<Guid, ApplicationItemLinkedDocumentsSnapshot>();
        foreach (var person in people)
        {
            snapshots[person.ID] = ApplicationPersonLinkedDocumentsResolver.Resolve(objectSpace, application, person);
        }

        foreach (var entry in entries)
        {
            if (!allowedRowIds.Contains(entry.ApplicationItemId))
                return false;

            if (!snapshots.TryGetValue(entry.ApplicationItemId, out var snapshot))
                return false;

            if (!snapshot.ContainsFile(entry.File.FileDataId))
                return false;
        }

        var layout = printLayout ?? DocumentCopyPrintLayout.DefaultFor(slotKey);
        bool composeVisaWithPassport = DocumentCopyPrintLayout.IsVisaKey(slotKey);
        var passportBytesByVisa = new Dictionary<Guid, byte[]?>();
        var pdfStreams = new List<MemoryStream>();
        try
        {
            foreach (var entry in entries)
            {
                if (!TryLoadFileContent(objectSpace, entry.File.FileDataId, out var fileContent, out var fileNameForExt))
                    continue;

                if (composeVisaWithPassport
                    && snapshots.TryGetValue(entry.ApplicationItemId, out var snapshot)
                    && TryLoadLatestPassportForVisaFile(
                        objectSpace,
                        snapshot,
                        entry.File.FileDataId,
                        slotKey,
                        passportBytesByVisa,
                        out var passportBytes)
                    && TryCreateVisaBesidePassportStream(passportBytes, fileContent, layout, out var paired))
                {
                    pdfStreams.Add(paired);
                    continue;
                }

                if (!TryCreateMergeSlicePdfStream(fileContent, fileNameForExt, slotKey, layout, out var pdfStream))
                    continue;

                pdfStreams.Add(pdfStream);
            }

            if (pdfStreams.Count == 0)
                return false;

            using var merged = new MemoryStream();
            SupportingDocumentsPdfSharpHelper.MergePdfStreams(pdfStreams, merged);
            content = merged.ToArray();
            fileName = BuildMergedFileName(entries, slotLabel);
            return content.Length > 0;
        }
        finally
        {
            foreach (var stream in pdfStreams)
                stream.Dispose();
        }
    }

    /// <summary>
    /// Loads the same roster files the PDF merge would use, without converting them to PDF.
    /// </summary>
    public bool TryLoadValidatedSourceFiles(
        IReadOnlyList<Guid> applicationPersonIds,
        IReadOnlyList<ApplicationItemLinkedDocumentFileEntry> entries,
        Guid applicationId,
        out IReadOnlyList<ApplicationItemDocumentImageSource> sources)
    {
        sources = Array.Empty<ApplicationItemDocumentImageSource>();

        if (applicationPersonIds == null || applicationPersonIds.Count == 0)
            return false;

        if (entries == null || entries.Count == 0)
            return false;

        var allowedRowIds = applicationPersonIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToHashSet();

        if (allowedRowIds.Count == 0)
            return false;

        using var objectSpace = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<ApplicationProfileInstance>();
        if (!ApplicationRosterHelper.TryLoadSharedApplicationPeople(
                objectSpace,
                applicationPersonIds,
                applicationId,
                out var application,
                out var people)
            || application == null)
        {
            return false;
        }

        var snapshots = new Dictionary<Guid, ApplicationItemLinkedDocumentsSnapshot>();
        foreach (var person in people)
        {
            snapshots[person.ID] = ApplicationPersonLinkedDocumentsResolver.Resolve(objectSpace, application, person);
        }

        foreach (var entry in entries)
        {
            if (!allowedRowIds.Contains(entry.ApplicationItemId))
                return false;

            if (!snapshots.TryGetValue(entry.ApplicationItemId, out var snapshot))
                return false;

            if (!snapshot.ContainsFile(entry.File.FileDataId))
                return false;
        }

        var loaded = new List<ApplicationItemDocumentImageSource>();
        foreach (var entry in entries)
        {
            if (!TryLoadFileContent(objectSpace, entry.File.FileDataId, out var fileContent, out var fileNameForExt))
                continue;

            loaded.Add(new ApplicationItemDocumentImageSource
            {
                Content = fileContent,
                FileName = string.IsNullOrWhiteSpace(fileNameForExt) ? "document" : fileNameForExt
            });
        }

        if (loaded.Count == 0)
            return false;

        sources = loaded;
        return true;
    }

    /// <summary>
    /// Visa that owns this scan. A family preview uses slot key <c>Visa.</c>, so the id comes from
    /// the person's visa group. A single-record preview can also be read from <c>Visa.{id}</c>.
    /// </summary>
    internal static Guid? VisaIdForFile(
        ApplicationItemLinkedDocumentsSnapshot? snapshot,
        Guid fileDataId,
        string slotKey)
    {
        if (snapshot != null && fileDataId != Guid.Empty)
        {
            var group = snapshot.Groups.FirstOrDefault(candidate =>
                candidate.SourceObjectId is Guid sourceId
                && sourceId != Guid.Empty
                && candidate.SourceObjectType == typeof(Visa)
                && candidate.Files.Any(file => file.FileDataId == fileDataId));
            if (group?.SourceObjectId is Guid id)
                return id;
        }

        return TryParseSlotRecordId(slotKey, "Visa.", out Guid parsed) ? parsed : null;
    }

    internal static bool TryParseSlotRecordId(string slotKey, string prefix, out Guid id)
    {
        id = Guid.Empty;
        if (string.IsNullOrWhiteSpace(slotKey)
            || string.IsNullOrWhiteSpace(prefix)
            || !slotKey.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string tail = slotKey.Substring(prefix.Length);
        return Guid.TryParseExact(tail, "N", out id) || Guid.TryParse(tail, out id);
    }

    private bool TryLoadLatestPassportForVisaFile(
        IObjectSpace objectSpace,
        ApplicationItemLinkedDocumentsSnapshot snapshot,
        Guid visaFileId,
        string slotKey,
        Dictionary<Guid, byte[]?> cache,
        out byte[] passportBytes)
    {
        passportBytes = Array.Empty<byte>();
        Guid? visaId = VisaIdForFile(snapshot, visaFileId, slotKey);
        if (visaId is not Guid id || id == Guid.Empty)
            return false;

        if (cache.TryGetValue(id, out byte[]? cached))
        {
            if (cached == null || cached.Length == 0)
                return false;

            passportBytes = cached;
            return true;
        }

        byte[]? loaded = LoadLatestPassportScan(objectSpace, id);
        cache[id] = loaded;
        if (loaded == null || loaded.Length == 0)
            return false;

        passportBytes = loaded;
        return true;
    }

    /// <summary>
    /// Latest PassportDocument on the passport that owns the visa. Those rows have no created date;
    /// files are listed by id, and the last content file is the latest row.
    /// </summary>
    private byte[]? LoadLatestPassportScan(IObjectSpace objectSpace, Guid visaId)
    {
        Guid passportId = objectSpace.GetObjectsQuery<Visa>()
            .Where(visa => visa.ID == visaId && visa.Passport != null)
            .Select(visa => visa.Passport.ID)
            .FirstOrDefault();
        if (passportId == Guid.Empty)
            return null;

        ApplicationItemLinkedDocumentFile? latest = ApplicationItemLinkedDocumentsResolver
            .LoadDocumentFiles<PassportDocument>(objectSpace, document => document.Passport.ID == passportId)
            .LastOrDefault(file => file.HasContent && file.FileDataId != Guid.Empty);
        if (latest == null)
            return null;

        return TryLoadFileContent(objectSpace, latest.FileDataId, out byte[] content, out _)
            ? content
            : null;
    }

    private bool TryCreateVisaBesidePassportStream(
        byte[] passportBytes,
        byte[] visaBytes,
        DocumentCopyPrintLayout layout,
        out MemoryStream pdfStream)
    {
        pdfStream = null!;
        var outMs = new MemoryStream();
        if (!SupportingDocumentsPdfSharpHelper.TryWriteVisaBesidePassport(
                passportBytes,
                visaBytes,
                outMs,
                logger,
                layout)
            || outMs.Length == 0)
        {
            outMs.Dispose();
            return false;
        }

        outMs.Position = 0;
        pdfStream = outMs;
        return true;
    }

    private bool TryLoadFileContent(
        IObjectSpace objectSpace,
        Guid fileDataId,
        out byte[] content,
        out string fileName)
    {
        content = Array.Empty<byte>();
        fileName = "document";

        var file = objectSpace.GetObjectByKey<FileData>(fileDataId);
        if (file == null || file.Size <= 0)
            return false;

        content = file.Content;
        if (content == null || content.Length == 0)
        {
            content = objectSpace.GetObjectsQuery<FileData>()
                .Where(f => f.ID == fileDataId)
                .Select(f => f.Content)
                .FirstOrDefault() ?? Array.Empty<byte>();
        }

        if (content.Length == 0)
            return false;

        fileName = string.IsNullOrWhiteSpace(file.FileName) ? "document" : file.FileName;
        return true;
    }

    private bool TryCreateMergeSlicePdfStream(
        byte[] content,
        string sourceFileName,
        string slotKey,
        DocumentCopyPrintLayout layout,
        out MemoryStream pdfStream)
    {
        pdfStream = null!;
        if (content == null || content.Length == 0)
            return false;

        string ext = Path.GetExtension(sourceFileName ?? string.Empty);
        if (IsPdfExtension(ext) && !DocumentFileUploadConstraints.IsLikelyPdf(content))
        {
            logger.LogWarning(
                "Document copies merge: file {FileName} has PDF extension but payload is not a PDF signature; trying image decode for slot {SlotKey}.",
                sourceFileName,
                slotKey);
        }

        bool landscapePage = !layout.UsesDocumentSize
            && slotKey.StartsWith("Visa.", StringComparison.OrdinalIgnoreCase);
        var outMs = new MemoryStream();
        if (!SupportingDocumentsPdfSharpHelper.TryWriteSlice(content, outMs, logger, layout, landscapePage))
            return false;

        outMs.Position = 0;
        pdfStream = outMs;
        return true;
    }

    private static bool IsPdfExtension(string ext) =>
        ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase);

    private static string BuildMergedFileName(
        IReadOnlyList<ApplicationItemLinkedDocumentFileEntry> entries,
        string slotLabel)
    {
        if (entries.Count == 1)
        {
            var singleName = entries[0].File.FileName;
            if (!string.IsNullOrWhiteSpace(singleName))
            {
                string baseName = Path.GetFileNameWithoutExtension(singleName);
                if (!string.IsNullOrWhiteSpace(baseName))
                    return SanitizeFileName(baseName) + ".pdf";
            }
        }

        if (!string.IsNullOrWhiteSpace(slotLabel))
            return SanitizeFileName(slotLabel) + ".pdf";

        return "document-copies.pdf";
    }

    private static string SanitizeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "document-copies";

        var invalid = Path.GetInvalidFileNameChars();
        var chars = value
            .Select(ch => invalid.Contains(ch) ? '-' : ch)
            .ToArray();
        var sanitized = new string(chars).Trim('-', ' ');
        return string.IsNullOrWhiteSpace(sanitized) ? "document-copies" : sanitized;
    }
}
