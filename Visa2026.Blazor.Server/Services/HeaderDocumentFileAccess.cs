using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Visa2026.Module.Services.HeaderLinkedDocuments;

namespace Visa2026.Blazor.Server.Services;

public sealed class HeaderDocumentFileAccess
{
    private readonly INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory;
    private readonly HeaderDocumentCopyPdfMerger pdfMerger;

    public HeaderDocumentFileAccess(
        INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory,
        HeaderDocumentCopyPdfMerger pdfMerger)
    {
        this.nonSecuredObjectSpaceFactory = nonSecuredObjectSpaceFactory;
        this.pdfMerger = pdfMerger;
    }

    public bool TryGetMergedRecordPdf(
        HeaderDocumentCopiesFamily family,
        Guid parentId,
        string recordKey,
        out ApplicationItemDocumentFileResult? result)
    {
        result = null;
        if (parentId == Guid.Empty || string.IsNullOrWhiteSpace(recordKey))
            return false;

        using var objectSpace = CreateObjectSpace(family);
        if (objectSpace == null)
            return false;

        var snapshot = HeaderLinkedDocumentsResolver.Resolve(objectSpace, family, parentId);
        var record = snapshot.FindRecord(recordKey);
        if (record == null)
            return false;

        if (!pdfMerger.TryBuildMergedPdf(family, parentId, recordKey, record.RecordLabel, out var content, out var fileName)
            || content == null
            || content.Length == 0
            || string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        result = new ApplicationItemDocumentFileResult
        {
            Content = content,
            FileName = fileName,
            ContentType = "application/pdf",
        };
        return true;
    }

    public bool TryGetFile(
        HeaderDocumentCopiesFamily family,
        Guid parentId,
        string recordKey,
        Guid fileDataId,
        out ApplicationItemDocumentFileResult? result)
    {
        result = null;
        if (parentId == Guid.Empty || fileDataId == Guid.Empty || string.IsNullOrWhiteSpace(recordKey))
            return false;

        using var objectSpace = CreateObjectSpace(family);
        if (objectSpace == null)
            return false;

        var snapshot = HeaderLinkedDocumentsResolver.Resolve(objectSpace, family, parentId);
        if (!snapshot.ContainsFile(recordKey, fileDataId))
            return false;

        var file = objectSpace.GetObjectByKey<FileData>(fileDataId);
        if (file == null || file.Size <= 0)
            return false;

        var content = file.Content;
        if (content == null || content.Length == 0)
        {
            content = objectSpace.GetObjectsQuery<FileData>()
                .Where(f => f.ID == fileDataId)
                .Select(f => f.Content)
                .FirstOrDefault();
        }

        if (content == null || content.Length == 0)
            return false;

        var fileName = string.IsNullOrWhiteSpace(file.FileName) ? "document" : file.FileName;
        result = new ApplicationItemDocumentFileResult
        {
            Content = content,
            FileName = fileName,
            ContentType = DocumentFileContentTypes.GetContentType(fileName),
        };
        return true;
    }

    private IObjectSpace? CreateObjectSpace(HeaderDocumentCopiesFamily family) =>
        family switch
        {
            HeaderDocumentCopiesFamily.WorkPermit => nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<WorkPermit>(),
            HeaderDocumentCopiesFamily.Invitation => nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<Invitation>(),
            HeaderDocumentCopiesFamily.Rejection => nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<Rejection>(),
            HeaderDocumentCopiesFamily.BorderZone => nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<BorderZone>(),
            HeaderDocumentCopiesFamily.Visa => nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<Visa>(),
            _ => null,
        };
}
