using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.WordReports;

public static class ApplicationItemReportPackageValidation
{
    public static bool TryResolveApplication(
        IObjectSpace objectSpace,
        IReadOnlyList<Guid> applicationItemIds,
        out Application? application,
        out string? errorMessageKey)
    {
        application = null;
        errorMessageKey = null;

        if (applicationItemIds == null || applicationItemIds.Count == 0)
        {
            errorMessageKey = "ApplicationItemReportPackage.ErrorNoSelection";
            return false;
        }

        var items = objectSpace.GetObjectsQuery<ApplicationItem>()
            .Where(item => applicationItemIds.Contains(item.ID))
            .Select(item => new { item.ID, ApplicationId = item.Application != null ? item.Application.ID : Guid.Empty })
            .ToList();

        if (items.Count == 0)
        {
            errorMessageKey = "ApplicationItemReportPackage.ErrorNoSelection";
            return false;
        }

        var applicationIds = items
            .Select(item => item.ApplicationId)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (applicationIds.Count != 1)
        {
            errorMessageKey = "ApplicationItemReportPackage.ErrorMultipleApplications";
            return false;
        }

        application = objectSpace.GetObjectByKey<Application>(applicationIds[0]);
        if (application == null)
        {
            errorMessageKey = "WordReports.EnqueueErrorNoApplication";
            return false;
        }

        return true;
    }
}
