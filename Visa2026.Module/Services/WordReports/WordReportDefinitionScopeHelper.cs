using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.WordReports;

public static class WordReportDefinitionScopeHelper
{
    public static bool MatchesUserTemplateScope(UserReportBoType rootBoType, WordReportPackageScope scope) =>
        scope switch
        {
            WordReportPackageScope.Application => rootBoType == UserReportBoType.Application,
            WordReportPackageScope.ApplicationItem => rootBoType is UserReportBoType.ApplicationItem or UserReportBoType.Person,
            _ => false
        };
}
