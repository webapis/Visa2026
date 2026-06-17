using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Services;

public sealed class ApplicationProgressMinistryLetterFileResult
{
    public required byte[] Content { get; init; }

    public required string FileName { get; init; }

    public required string ContentType { get; init; }
}

/// <summary>
/// Secure read access for <see cref="ApplicationProgress.MinistryLetterFile"/> on a parent <see cref="Application"/>.
/// </summary>
public sealed class ApplicationProgressMinistryLetterFileAccess
{
    private readonly INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory;

    public ApplicationProgressMinistryLetterFileAccess(INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory) =>
        this.nonSecuredObjectSpaceFactory = nonSecuredObjectSpaceFactory;

    /// <summary>Loads the ministry letter file for a progress row without requiring the parent application ID (used by the global preview drawer).</summary>
    public bool TryGetApplicationIdForProgress(Guid progressId, out Guid applicationId)
    {
        applicationId = Guid.Empty;
        if (progressId == Guid.Empty)
            return false;

        using var objectSpace = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<ApplicationProgress>();
        applicationId = objectSpace.GetObjectsQuery<ApplicationProgress>()
            .Where(p => p.ID == progressId && p.Application != null)
            .Select(p => p.Application!.ID)
            .FirstOrDefault();

        return applicationId != Guid.Empty;
    }

    public bool TryGetFileByProgressId(Guid progressId, out ApplicationProgressMinistryLetterFileResult? result)
    {
        result = null;
        if (progressId == Guid.Empty)
            return false;

        using var objectSpace = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<ApplicationProgress>();
        var progress = objectSpace.GetObjectsQuery<ApplicationProgress>()
            .Include(p => p.MinistryLetterFile)
            .FirstOrDefault(p => p.ID == progressId);

        if (progress == null)
            return false;

        var file = progress.MinistryLetterFile;
        if (file == null || file.Size <= 0)
            return false;

        var content = file.Content;
        if (content == null || content.Length == 0)
        {
            content = objectSpace.GetObjectsQuery<FileData>()
                .Where(f => f.ID == file.ID)
                .Select(f => f.Content)
                .FirstOrDefault();
        }

        if (content == null || content.Length == 0)
            return false;

        var fileName = string.IsNullOrWhiteSpace(file.FileName) ? "ministry-letter.pdf" : file.FileName;
        result = new ApplicationProgressMinistryLetterFileResult
        {
            Content = content,
            FileName = fileName,
            ContentType = DocumentFileContentTypes.GetContentType(fileName)
        };
        return true;
    }

    public bool TryGetFile(Guid applicationId, Guid progressId, out ApplicationProgressMinistryLetterFileResult? result)
    {
        result = null;
        if (applicationId == Guid.Empty || progressId == Guid.Empty)
            return false;

        using var objectSpace = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<ApplicationProgress>();
        var progress = objectSpace.GetObjectsQuery<ApplicationProgress>()
            .Include(p => p.Application)
            .Include(p => p.MinistryLetterFile)
            .FirstOrDefault(p => p.ID == progressId);

        if (progress?.Application?.ID != applicationId)
            return false;

        var file = progress.MinistryLetterFile;
        if (file == null || file.Size <= 0)
            return false;

        var content = file.Content;
        if (content == null || content.Length == 0)
        {
            content = objectSpace.GetObjectsQuery<FileData>()
                .Where(f => f.ID == file.ID)
                .Select(f => f.Content)
                .FirstOrDefault();
        }

        if (content == null || content.Length == 0)
            return false;

        var fileName = string.IsNullOrWhiteSpace(file.FileName) ? "ministry-letter.pdf" : file.FileName;
        result = new ApplicationProgressMinistryLetterFileResult
        {
            Content = content,
            FileName = fileName,
            ContentType = DocumentFileContentTypes.GetContentType(fileName)
        };
        return true;
    }
}
