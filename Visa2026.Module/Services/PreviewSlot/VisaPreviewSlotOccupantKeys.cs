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
}
