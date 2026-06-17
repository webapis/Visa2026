using Visa2026.Module.Services.WordReports;

namespace Visa2026.Module.Services.PreviewSlot;

/// <summary>
/// Stable occupant keys for the global preview slot (last open wins).
/// </summary>
public static class VisaPreviewSlotOccupantKeys
{
    public static string ForResminamalar(ResminamalarSlotRequest request)
    {
        if (request.ApplicationId == Guid.Empty)
            return "resminamalar:empty";

        if (request.Scope == WordReportPackageScope.ApplicationItem)
        {
            var ids = request.ApplicationItemIds?
                .Where(id => id != Guid.Empty)
                .OrderBy(id => id)
                .ToArray() ?? Array.Empty<Guid>();
            return $"resminamalar:items:{request.ApplicationId:N}:{string.Join(',', ids.Select(id => id.ToString("N")))}";
        }

        return $"resminamalar:app:{request.ApplicationId:N}";
    }

    public static string ForFile(string sourceType, Guid objectId) =>
        $"file:{sourceType?.Trim()}:{objectId:N}";

    public static string ForDocumentCopies(IReadOnlyList<Guid> applicationItemIds)
    {
        var ids = applicationItemIds?
            .Where(id => id != Guid.Empty)
            .OrderBy(id => id)
            .ToArray() ?? Array.Empty<Guid>();

        if (ids.Length == 0)
            return "document-copies:empty";

        return $"document-copies:items:{string.Join(',', ids.Select(id => id.ToString("N")))}";
    }

    public static string ForDocumentCopies(DocumentCopiesSlotRequest request) =>
        ForDocumentCopies(request?.ApplicationItemIds ?? Array.Empty<Guid>());

    public static string ForProgressLetters(Guid applicationId) =>
        applicationId == Guid.Empty ? "progress-letters:empty" : $"progress-letters:app:{applicationId:N}";

    public static string ForProgressLetters(ProgressLettersSlotRequest request) =>
        ForProgressLetters(request?.ApplicationId ?? Guid.Empty);
}
