using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.UserReports;

/// <summary>Security checks for opening <see cref="UserReportTemplate"/> from Resminamalar.</summary>
public static class UserReportTemplateEditAccess
{
    public static bool CanEditTemplates() =>
        CanReadTemplates()
        && SecuritySystem.IsGranted(
            new PermissionRequest(typeof(UserReportTemplate), SecurityOperations.Write));

    public static bool CanReadTemplates() =>
        SecuritySystem.IsGranted(
            new PermissionRequest(typeof(UserReportTemplate), SecurityOperations.Read));

    public static bool CanOpenTemplateDetail(IObjectSpace objectSpace, Guid templateId)
    {
        if (templateId == Guid.Empty || !CanReadTemplates())
            return false;

        return objectSpace.GetObjectByKey<UserReportTemplate>(templateId) != null;
    }
}
