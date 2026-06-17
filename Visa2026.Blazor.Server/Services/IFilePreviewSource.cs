namespace Visa2026.Blazor.Server.Services;

/// <summary>
/// Resolved by source-type key; fetches file bytes for the global <see cref="VisaFilePreviewDrawer"/> component.
/// Add one implementation per document kind (progress letter, passport document, work permit document, etc.).
/// </summary>
public interface IFilePreviewSource
{
    /// <summary>Short identifier registered in DI and referenced from JS (e.g. "progress-letter").</summary>
    string SourceType { get; }

    /// <summary>
    /// Loads file bytes for <paramref name="objectId"/>.
    /// Returns null if the object is not found, has no file, or access is denied.
    /// </summary>
    Task<FilePreviewResult?> TryLoadAsync(Guid objectId);
}

public sealed class FilePreviewResult
{
    public required byte[] Content { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
}
