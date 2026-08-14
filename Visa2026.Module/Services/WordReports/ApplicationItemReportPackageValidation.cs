using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.Services.WordReports;

public static class ApplicationItemReportPackageValidation
{
    public static bool TryResolveApplication(
        IObjectSpace objectSpace,
        IReadOnlyList<Guid> personIds,
        out ApplicationProfileInstance? application,
        out string? errorMessageKey)
    {
        application = null;
        errorMessageKey = null;

        if (personIds == null || personIds.Count == 0)
        {
            errorMessageKey = "ApplicationItemReportPackage.ErrorNoSelection";
            return false;
        }

        if (!ApplicationRosterHelper.TryLoadSharedApplicationPeople(
                objectSpace,
                personIds,
                applicationId: Guid.Empty,
                out application,
                out var people)
            || application == null
            || people.Count == 0)
        {
            errorMessageKey = people.Count == 0
                ? "ApplicationItemReportPackage.ErrorNoSelection"
                : "ApplicationItemReportPackage.ErrorMultipleApplications";
            return false;
        }

        return true;
    }
}
