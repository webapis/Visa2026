using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationProfilePicker;
using Visa2026.Module.Services.OfficerShell;

namespace Visa2026.Module.Services.ApplicationProfileCatalog;

public sealed class ApplicationProfileCatalogQueryService : IApplicationProfileCatalogQueryService
{
    public IReadOnlyList<ApplicationProfileCatalogRow> GetProfiles(IObjectSpace objectSpace)
    {
        if (objectSpace == null)
            return Array.Empty<ApplicationProfileCatalogRow>();

        var usage = objectSpace.GetObjectsQuery<Application>()
            .Where(a => a.ApplicationProfile != null)
            .Select(a => new
            {
                ProfileId = a.ApplicationProfile!.ID,
                a.ProcessNumber,
                a.LatestPrimaryStateCode,
            })
            .AsEnumerable()
            .GroupBy(a => a.ProfileId)
            .ToDictionary(
                g => g.Key,
                g => new UsageCounts(
                    g.Count(a => OfficerShellApplicationFilters.IsStagedState(a.ProcessNumber, a.LatestPrimaryStateCode)),
                    g.Count(a => OfficerShellApplicationFilters.IsInProcessState(a.ProcessNumber, a.LatestPrimaryStateCode))));

        return objectSpace.GetObjectsQuery<ApplicationProfile>()
            .AsEnumerable()
            .Select(p =>
            {
                usage.TryGetValue(p.ID, out var counts);
                counts ??= UsageCounts.Empty;
                var locked = ApplicationProfileLockHelper.IsProfileConfigLocked(p, objectSpace);
                return new ApplicationProfileCatalogRow
                {
                    ProfileId = p.ID,
                    Name = p.Name ?? string.Empty,
                    Code = p.Code ?? string.Empty,
                    SelectionCode = p.SelectionCode,
                    ActionFamily = p.ActionFamily,
                    ProgressRoute = p.ProgressRoute,
                    IsActive = p.IsActive,
                    IsConfigLocked = locked,
                    LinkedApplicationCount = counts.Staged + counts.InProcess,
                    StagedUses = counts.Staged,
                    InProcessUses = counts.InProcess,
                    TemplateFamilyKey = OfficerShellTemplateFamily.ResolveKey(p),
                    StatusKey = ResolveStatusKey(p.IsActive, locked),
                    ActionFamilyLabel = ApplicationProfilePickerDisplayHelper.FormatActionFamily(p.ActionFamily),
                    ProgressRouteLabel = ApplicationProfilePickerDisplayHelper.FormatProgressRoute(p.ProgressRoute),
                };
            })
            .OrderByDescending(r => r.IsActive)
            .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string ResolveStatusKey(bool isActive, bool isLocked)
    {
        if (!isActive)
            return "draft";
        return isLocked ? "locked" : "active";
    }

    private sealed record UsageCounts(int Staged, int InProcess)
    {
        public static readonly UsageCounts Empty = new(0, 0);
    }
}