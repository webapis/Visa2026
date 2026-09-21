using System.Collections.Generic;
using System.Linq;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ReportDashboard;

/// <summary>
/// EF-translatable Application Profile filters for Report Dashboard instance queries.
/// Profile-first; deprecated ApplicationType only when ApplicationProfile is null (dual-read).
/// </summary>
internal static class ReportDashboardProfileInstanceQuery
{
    public static IQueryable<ApplicationProfileInstance> WhereProgressRoute(
        this IQueryable<ApplicationProfileInstance> query,
        ApplicationProfileInstanceProgressRouteKind route) =>
        query.Where(a =>
            (a.ApplicationProfile != null && a.ApplicationProfile.ProgressRoute == route)
            || (a.ApplicationProfile == null
                && a.ApplicationType != null
                && a.ApplicationType.ApplicationProfileInstanceProgressRoute == route));

    public static IQueryable<ApplicationProfileInstance> WhereProducesInvitation(
        this IQueryable<ApplicationProfileInstance> query) =>
        query.Where(a =>
            (a.ApplicationProfile != null && a.ApplicationProfile.ProduceInvitation)
            || (a.ApplicationProfile == null
                && a.ApplicationType != null
                && a.ApplicationType.CanIssueInvitation));

    public static IQueryable<ApplicationProfileInstance> WhereRegistrationFamily(
        this IQueryable<ApplicationProfileInstance> query,
        IReadOnlyCollection<string> typeNameFallback) =>
        query.Where(a =>
            (a.ApplicationProfile != null
                && a.ApplicationProfile.ActionFamily == ApplicationProfileActionFamily.Registration)
            || (a.ApplicationProfile == null
                && a.ApplicationType != null
                && a.ApplicationType.Name != null
                && typeNameFallback.Contains(a.ApplicationType.Name)));

    public static string ProfileLabelOrMissing(ApplicationProfileInstance application, string missing)
    {
        var profile = application.ApplicationProfile;
        if (profile != null)
        {
            var name = profile.Name?.Trim();
            if (!string.IsNullOrEmpty(name))
                return name;
            var code = profile.Code?.Trim();
            if (!string.IsNullOrEmpty(code))
                return code;
        }

        var type = application.ApplicationType;
        if (type != null)
        {
            var typeName = type.NameTm?.Trim();
            if (!string.IsNullOrEmpty(typeName))
                return typeName;
            typeName = type.Name?.Trim();
            if (!string.IsNullOrEmpty(typeName))
                return typeName;
        }

        return missing;
    }
}