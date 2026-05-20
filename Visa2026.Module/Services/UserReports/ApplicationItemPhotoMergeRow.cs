using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.UserReports;

/// <summary>
/// DocxTemplater row model for <c>{{#ds.ApplicationItems}}</c> when templates use <c>{{IMAGE:Person_Photo}}</c> (injected after merge via <see cref="WordUserReportImageInjector"/>).
/// Supplies non-null <see cref="byte[]"/> (empty when no photo) so the injector clears the placeholder without leaving literal text.
/// </summary>
internal sealed class ApplicationItemPhotoMergeRow
{
    public string Person_FullName { get; init; } = string.Empty;

    public byte[] Person_Photo { get; init; } = Array.Empty<byte>();

    public static ApplicationItemPhotoMergeRow From(ApplicationItem item)
    {
        var photo = item.Person?.Photo;
        return new ApplicationItemPhotoMergeRow
        {
            Person_FullName = item.Person_FullName ?? string.Empty,
            Person_Photo = photo is { Length: > 0 } ? photo : Array.Empty<byte>(),
        };
    }
}
