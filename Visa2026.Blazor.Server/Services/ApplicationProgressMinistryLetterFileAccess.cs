using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Visa2026.Module.Services.PreviewSlot;

namespace Visa2026.Blazor.Server.Services;

public sealed class ApplicationProfileInstanceProgressMinistryLetterFileResult
{
    public required byte[] Content { get; init; }

    public required string FileName { get; init; }

    public required string ContentType { get; init; }
}

/// <summary>
/// Secure read access for <see cref="ApplicationProfileInstanceProgress.MinistryLetterFile"/> on a parent <see cref="Application"/>.
/// </summary>
public sealed class ApplicationProfileInstanceProgressMinistryLetterFileAccess
{
    private readonly INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory;

    public ApplicationProfileInstanceProgressMinistryLetterFileAccess(INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory) =>
        this.nonSecuredObjectSpaceFactory = nonSecuredObjectSpaceFactory;

    /// <summary>Loads the ministry letter file for a progress row without requiring the parent application ID (used by the global preview drawer).</summary>
    public bool TryGetApplicationProfileInstanceIdForProgress(Guid progressId, out Guid applicationId)
    {
        applicationId = Guid.Empty;
        if (progressId == Guid.Empty)
            return false;

        using var objectSpace = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<ApplicationProfileInstanceProgress>();
        applicationId = objectSpace.GetObjectsQuery<ApplicationProfileInstanceProgress>()
            .Where(p => p.ID == progressId && p.ApplicationProfileInstance != null)
            .Select(p => p.ApplicationProfileInstance!.ID)
            .FirstOrDefault();

        return applicationId != Guid.Empty;
    }

    public bool TryGetFileByProgressId(Guid progressId, out ApplicationProfileInstanceProgressMinistryLetterFileResult? result) =>
        TryGetPreview(Guid.Empty, progressId, null, previewAll: false, requireApplication: false, out result);

    public bool TryGetFile(Guid applicationId, Guid progressId, out ApplicationProfileInstanceProgressMinistryLetterFileResult? result) =>
        TryGetPreview(applicationId, progressId, null, previewAll: false, requireApplication: true, out result);

    public bool TryGetPreview(
        Guid applicationId,
        Guid progressId,
        Guid? fileId,
        bool previewAll,
        out ApplicationProfileInstanceProgressMinistryLetterFileResult? result) =>
        TryGetPreview(applicationId, progressId, fileId, previewAll, requireApplication: true, out result);

    private bool TryGetPreview(
        Guid applicationId,
        Guid progressId,
        Guid? fileId,
        bool previewAll,
        bool requireApplication,
        out ApplicationProfileInstanceProgressMinistryLetterFileResult? result)
    {
        result = null;
        if (progressId == Guid.Empty)
            return false;
        if (requireApplication && applicationId == Guid.Empty)
            return false;

        using var objectSpace = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<ApplicationProfileInstanceProgress>();
        var progress = LoadProgress(objectSpace, progressId);
        if (progress == null)
            return false;
        if (requireApplication && progress.ApplicationProfileInstance?.ID != applicationId)
            return false;

        if (previewAll)
            return TryBuildCombined(objectSpace, progress, out result);

        var file = fileId is Guid id && id != Guid.Empty
            ? FindFile(progress, id)
            : progress.MinistryLetterFile ?? progress.SubmissionDocumentFile;
        return TryReadFile(objectSpace, file, out result);
    }

    private static bool TryBuildCombined(
        IObjectSpace objectSpace,
        ApplicationProfileInstanceProgress progress,
        out ApplicationProfileInstanceProgressMinistryLetterFileResult? result)
    {
        result = null;
        var pairs = new List<(string Name, byte[] Content)>();
        foreach (var stored in ApplicationProfileInstanceProgressFiles.List(progress, ApplicationProfileInstanceProgressFiles.MinistryLetter)
                     .Concat(ApplicationProfileInstanceProgressFiles.List(progress, ApplicationProfileInstanceProgressFiles.SubmissionDocument)))
        {
            var file = FindFile(progress, stored.FileId);
            if (!TryReadBytes(objectSpace, file, out var content))
                continue;
            pairs.Add((stored.FileName, content));
        }

        if (!ApplicationProfileInstanceProgressFilePreview.TryBuild(pairs, out var built, out _) || built == null)
            return false;

        result = new ApplicationProfileInstanceProgressMinistryLetterFileResult
        {
            Content = built.Content,
            FileName = built.FileName,
            ContentType = built.ContentType,
        };
        return true;
    }

    private static FileData? FindFile(ApplicationProfileInstanceProgress progress, Guid fileId)
    {
        if (progress.MinistryLetterFile?.ID == fileId)
            return progress.MinistryLetterFile;
        if (progress.SubmissionDocumentFile?.ID == fileId)
            return progress.SubmissionDocumentFile;
        return progress.Files?.FirstOrDefault(item => item.File != null && item.File.ID == fileId)?.File;
    }

    private static bool TryReadFile(
        IObjectSpace objectSpace,
        FileData? file,
        out ApplicationProfileInstanceProgressMinistryLetterFileResult? result)
    {
        result = null;
        if (!TryReadBytes(objectSpace, file, out var content) || file == null)
            return false;

        var fileName = string.IsNullOrWhiteSpace(file.FileName) ? "file" : file.FileName;
        result = new ApplicationProfileInstanceProgressMinistryLetterFileResult
        {
            Content = content,
            FileName = fileName,
            ContentType = ApplicationProfileInstanceProgressFilePreview.ContentType(fileName),
        };
        return true;
    }

    private static bool TryReadBytes(IObjectSpace objectSpace, FileData? file, out byte[] content)
    {
        content = Array.Empty<byte>();
        if (file == null || file.Size <= 0)
            return false;

        var bytes = file.Content;
        if (bytes == null || bytes.Length == 0)
        {
            bytes = objectSpace.GetObjectsQuery<FileData>()
                .Where(f => f.ID == file.ID)
                .Select(f => f.Content)
                .FirstOrDefault();
        }

        if (bytes == null || bytes.Length == 0)
            return false;

        content = bytes;
        return true;
    }

    private static ApplicationProfileInstanceProgress? LoadProgress(IObjectSpace objectSpace, Guid progressId) =>
        objectSpace.GetObjectsQuery<ApplicationProfileInstanceProgress>()
            .Include(p => p.ApplicationProfileInstance)
            .Include(p => p.MinistryLetterFile)
            .Include(p => p.SubmissionDocumentFile)
            .Include(p => p.Files)
                .ThenInclude(f => f.File)
            .FirstOrDefault(p => p.ID == progressId);
}
