namespace Visa2026.Blazor.Server.Services;

/// <summary>
/// Serves <see cref="Module.BusinessObjects.ApplicationProgress.MinistryLetterFile"/> content
/// for the global file preview drawer. Identified by source type "progress-letter".
/// The <paramref name="objectId"/> passed from JS is the <c>ApplicationProfileInstanceProgress.ID</c>;
/// the parent <c>Application.ID</c> is resolved internally for security validation.
/// </summary>
public sealed class ApplicationProfileInstanceProgressLetterPreviewSource : IFilePreviewSource
{
    public const string Key = "progress-letter";

    private readonly ApplicationProfileInstanceProgressMinistryLetterFileAccess _fileAccess;

    public ApplicationProfileInstanceProgressLetterPreviewSource(ApplicationProfileInstanceProgressMinistryLetterFileAccess fileAccess) =>
        _fileAccess = fileAccess;

    public string SourceType => Key;

    public Task<FilePreviewResult?> TryLoadAsync(Guid progressId)
    {
        // ApplicationProfileInstanceProgressMinistryLetterFileAccess requires both applicationId + progressId.
        // For the drawer we only have progressId — resolve parent application id first.
        if (!_fileAccess.TryGetFileByProgressId(progressId, out var result) || result == null)
            return Task.FromResult<FilePreviewResult?>(null);

        return Task.FromResult<FilePreviewResult?>(new FilePreviewResult
        {
            Content = result.Content,
            FileName = result.FileName,
            ContentType = result.ContentType,
        });
    }
}
