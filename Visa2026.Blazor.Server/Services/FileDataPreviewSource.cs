using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;

namespace Visa2026.Blazor.Server.Services;

/// <summary>Preview a <see cref="FileData"/> row (person document scans) in #visa-preview-slot.</summary>
public sealed class FileDataPreviewSource : IFilePreviewSource
{
    public const string Key = "file-data";

    private readonly INonSecuredObjectSpaceFactory _objectSpaceFactory;

    public FileDataPreviewSource(INonSecuredObjectSpaceFactory objectSpaceFactory) =>
        _objectSpaceFactory = objectSpaceFactory;

    public string SourceType => Key;

    public Task<FilePreviewResult?> TryLoadAsync(Guid objectId)
    {
        if (objectId == Guid.Empty)
            return Task.FromResult<FilePreviewResult?>(null);

        using var objectSpace = _objectSpaceFactory.CreateNonSecuredObjectSpace<Person>();
        var file = objectSpace.GetObjectByKey<FileData>(objectId);
        if (file == null || file.Size <= 0)
            return Task.FromResult<FilePreviewResult?>(null);

        var content = file.Content;
        if (content == null || content.Length == 0)
        {
            content = objectSpace.GetObjectsQuery<FileData>()
                .Where(f => f.ID == objectId)
                .Select(f => f.Content)
                .FirstOrDefault();
        }

        if (content == null || content.Length == 0)
            return Task.FromResult<FilePreviewResult?>(null);

        var fileName = string.IsNullOrWhiteSpace(file.FileName) ? "document" : file.FileName;
        return Task.FromResult<FilePreviewResult?>(new FilePreviewResult
        {
            Content = content,
            FileName = fileName,
            ContentType = DocumentFileContentTypes.GetContentType(fileName),
        });
    }
}