using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.HeaderLinkedDocuments;
using Visa2026.Module.Services.WordReports;

namespace Visa2026.Module.Services.PreviewSlot;

/// <summary>
/// Stable occupant keys for the global preview slot (last open wins).
/// </summary>
public static class VisaPreviewSlotOccupantKeys
{
    public static string ForResminamalar(ResminamalarSlotRequest request)
    {
        if (request.ApplicationProfileInstanceId == Guid.Empty)
            return "resminamalar:empty";

        if (request.Scope == WordReportPackageScope.ApplicationRosterMergeLine)
        {
            var ids = request.ApplicationItemIds?
                .Where(id => id != Guid.Empty)
                .OrderBy(id => id)
                .ToArray() ?? Array.Empty<Guid>();
            return $"resminamalar:items:{request.ApplicationProfileInstanceId:N}:{string.Join(',', ids.Select(id => id.ToString("N")))}";
        }

        return $"resminamalar:app:{request.ApplicationProfileInstanceId:N}";
    }

    public static string ForFile(string sourceType, Guid objectId) =>
        $"file:{sourceType?.Trim()}:{objectId:N}";

    public static string ForDocumentCopies(DocumentCopiesSlotRequest? request) =>
        request == null
            ? ForDocumentCopiesRoster(Guid.Empty, Array.Empty<Guid>())
            : ForDocumentCopiesRoster(request.ApplicationProfileInstanceId, request.ApplicationProfileInstancePersonIds);

    public static string ForDocumentCopiesRoster(Guid applicationId, IReadOnlyList<Guid> applicationPersonIds)
    {
        var ids = applicationPersonIds?
            .Where(id => id != Guid.Empty)
            .OrderBy(id => id)
            .ToArray() ?? Array.Empty<Guid>();

        if (applicationId == Guid.Empty || ids.Length == 0)
            return "document-copies:roster:empty";

        return $"document-copies:roster:{applicationId:N}:{string.Join(',', ids.Select(id => id.ToString("N")))}";
    }

    public static string ForProgressLetters(Guid applicationId) =>
        applicationId == Guid.Empty ? "progress-letters:empty" : $"progress-letters:app:{applicationId:N}";

    public static string ForProgressLetters(ProgressLettersSlotRequest request)
    {
        var baseKey = ForProgressLetters(request?.ApplicationProfileInstanceId ?? Guid.Empty);
        if (request?.OpenPreviewOnly == true
            && request.FocusProgressId is Guid focus
            && focus != Guid.Empty)
        {
            return $"{baseKey}|preview:{focus:N}";
        }

        return baseKey;
    }

    public static string ForPersonDocumentCopies(IReadOnlyList<Guid> personIds)
    {
        var ids = personIds?
            .Where(id => id != Guid.Empty)
            .OrderBy(id => id)
            .ToArray() ?? Array.Empty<Guid>();

        if (ids.Length == 0)
            return "person-document-copies:empty";

        if (ids.Length == 1)
            return $"person-document-copies:person:{ids[0]:N}";

        return $"person-document-copies:persons:{string.Join(',', ids.Select(id => id.ToString("N")))}";
    }

    public static string ForPersonDocumentCopies(PersonDocumentCopiesSlotRequest request) =>
        ForPersonDocumentCopies(request?.PersonIds ?? Array.Empty<Guid>());

    public static string ForHeaderDocumentCopies(HeaderDocumentCopiesSlotRequest request)
    {
        if (request == null || request.ParentId == Guid.Empty)
            return "header-document-copies:empty";

        var prefix = request.Family switch
        {
            HeaderDocumentCopiesFamily.WorkPermit => "work-permit-document-copies:work-permit",
            HeaderDocumentCopiesFamily.Invitation => "invitation-document-copies:invitation",
            HeaderDocumentCopiesFamily.Rejection => "rejection-document-copies:rejection",
            HeaderDocumentCopiesFamily.BorderZone => "border-zone-document-copies:border-zone",
            _ => "header-document-copies:unknown",
        };

        return $"{prefix}:{request.ParentId:N}";
    }

    public static string ForPlaceholderManual(UserReportBoType? filterRootBoType) =>
        filterRootBoType is UserReportBoType root
            ? $"placeholder-manual:root:{root}"
            : "placeholder-manual:all";
}
