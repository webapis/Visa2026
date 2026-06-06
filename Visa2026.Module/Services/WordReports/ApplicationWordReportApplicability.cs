using System;
using System.Linq;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.WordReports;

internal static class ApplicationWordReportApplicability
{
    public static bool IsDefinitionApplicable(IWordReportDefinition def, Application application)
    {
        if (def == null || application == null)
            return false;

        var names = def.ApplicableApplicationTypeNames;
        if (names != null && names.Length > 0)
        {
            var appTypeName = application.ApplicationType?.Name;
            if (appTypeName == null || !names.Contains(appTypeName, StringComparer.Ordinal))
                return false;
        }

        return def.IsApplicable(application);
    }
}
