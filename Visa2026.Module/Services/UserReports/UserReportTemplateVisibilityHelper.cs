using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.UserReports;

/// <summary>
/// Loads active user templates with applicability links; visibility is evaluated in memory (not translatable to SQL).
/// </summary>
internal static class UserReportTemplateVisibilityHelper
{
    public static List<UserReportTemplate> GetVisibleActiveTemplates(
        IObjectSpace objectSpace,
        IUserReportVisibilityService visibilityService,
        Application application)
    {
        return objectSpace.GetObjectsQuery<UserReportTemplate>()
            .Include(t => t.ApplicableTypeLinks)
                .ThenInclude(l => l.ApplicationType)
            .Include(t => t.ApplicableProjectContractLinks)
                .ThenInclude(l => l.ProjectContract)
            .Where(t => t.IsActive)
            .AsEnumerable()
            .Where(t => visibilityService.IsTemplateVisible(t, application))
            .ToList();
    }

    public static bool AnyVisibleActiveTemplate(
        IObjectSpace objectSpace,
        IUserReportVisibilityService visibilityService,
        Application application) =>
        objectSpace.GetObjectsQuery<UserReportTemplate>()
            .Include(t => t.ApplicableTypeLinks)
                .ThenInclude(l => l.ApplicationType)
            .Include(t => t.ApplicableProjectContractLinks)
                .ThenInclude(l => l.ProjectContract)
            .Where(t => t.IsActive)
            .AsEnumerable()
            .Any(t => visibilityService.IsTemplateVisible(t, application));
}
