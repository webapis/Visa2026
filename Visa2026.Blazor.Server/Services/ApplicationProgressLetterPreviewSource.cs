namespace Visa2026.Blazor.Server.Services;

/// <summary>
/// Serves <see cref="Module.BusinessObjects.ApplicationProgress.MinistryLetterFile"/> content
/// for the global file preview drawer. Identified by source type "progress-letter".
/// The <paramref name="objectId"/> passed from JS is the <c>ApplicationProgress.ID</c>;
/// the parent <c>Application.ID</c> is resolved internally for security validation.
/// </summary>
public sealed class ApplicationProgressLetterPreviewSource : IFilePreviewSource
{
    public const string Key = "progress-letter";

    private readonly ApplicationProgressMinistryLetterFileAccess _fileAccess;

    public ApplicationProgressLetterPreviewSource(ApplicationProgressMinistryLetterFileAccess fileAccess) =>
        _fileAccess = fileAccess;

    public string SourceType => Key;

    public Task<FilePreviewResult?> TryLoadAsync(Guid progressId)
    {
        // ApplicationProgressMinistryLetterFileAccess requires both applicationId + progressId.
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
