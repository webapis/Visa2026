using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Visa2026.Module.Services.PersonLinkedDocuments;

namespace Visa2026.Blazor.Server.Services;

public sealed class PersonDocumentFileAccess
{
    private readonly INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory;
    private readonly PersonDocumentCopyPdfMerger pdfMerger;

    public PersonDocumentFileAccess(
        INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory,
        PersonDocumentCopyPdfMerger pdfMerger)
    {
        this.nonSecuredObjectSpaceFactory = nonSecuredObjectSpaceFactory;
        this.pdfMerger = pdfMerger;
    }

    public bool TryGetMergedRecordPdf(
        Guid personId,
        string recordKey,
        out ApplicationItemDocumentFileResult? result)
    {
        result = null;
        if (personId == Guid.Empty || string.IsNullOrWhiteSpace(recordKey))
            return false;

        using var objectSpace = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<Person>();
        var person = objectSpace.GetObjectByKey<Person>(personId);
        if (person == null)
            return false;

        var snapshot = PersonLinkedDocumentsResolver.Resolve(objectSpace, person);
        var record = snapshot.FindRecord(recordKey);
        if (record == null)
            return false;

        if (!pdfMerger.TryBuildMergedPdf(personId, recordKey, record.RecordLabel, out var content, out var fileName)
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
            ContentType = "application/pdf"
        };
        return true;
    }

    public bool TryGetFile(Guid personId, string recordKey, Guid fileDataId, out ApplicationItemDocumentFileResult? result)
    {
        result = null;
        if (personId == Guid.Empty || fileDataId == Guid.Empty || string.IsNullOrWhiteSpace(recordKey))
            return false;

        using var objectSpace = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<Person>();
        var person = objectSpace.GetObjectByKey<Person>(personId);
        if (person == null)
            return false;

        var snapshot = PersonLinkedDocumentsResolver.Resolve(objectSpace, person);
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
            ContentType = DocumentFileContentTypes.GetContentType(fileName)
        };
        return true;
    }
}
