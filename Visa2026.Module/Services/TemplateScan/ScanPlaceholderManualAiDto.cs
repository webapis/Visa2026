#nullable enable

using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Placeholder manual shape for Azure: tokens grouped by related business object.
/// Short codes stay the reply key.
/// </summary>
internal static class ScanPlaceholderManualAiDto
{
    public static object BuildAllowedTokensByBo(IEnumerable<UserReportPlaceholderCatalogEntry> allowed)
    {
        ArgumentNullException.ThrowIfNull(allowed);

        return UserReportPlaceholderRelatedBoCatalog.Group(allowed)
            .Select(static g => new
            {
                relatedBo = g.RelatedBo.ToString(),
                tokens = g.Entries.Select(static e => new
                {
                    e.ShortCode,
                    tokenHeader = e.BuildWordToken(UserReportPlaceholderScope.Header),
                    tokenRow = e.BuildWordToken(UserReportPlaceholderScope.Row),
                    e.LabelEn,
                    labelTk = e.LabelTk,
                    labelTr = e.LabelTr,
                    relatedBo = e.RelatedBo.ToString(),
                    pack = e.Pack.ToString(),
                    role = ScanPlaceholderRoleCatalog.Resolve(e.ShortCode).ToString(),
                    description = ScanPlaceholderRoleCatalog.Describe(e),
                    example = e.ExampleValue,
                    path = e.CanonicalPath,
                    scope = e.Scope.ToString(),
                }),
            })
            .ToList();
    }
}