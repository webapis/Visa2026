#nullable enable

using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Cover letters with only <c>{{ds.*}}</c> header tokens must save as
/// <see cref="ApplicationProfileTemplateDataScope.ApplicationHeader"/> so Resminamalar
/// merges one application document. Scan upload defaults to Both, which maps to
/// ApplicationItem and fills per person — leaving AFNUM/ADAT/MSRV/TPCNT blank.
/// </summary>
public static class ScanTemplateDataScope
{
    public static ApplicationProfileTemplateDataScope FromProposedTokens(
        IEnumerable<string?> tokens,
        ApplicationProfileTemplateDataScope fallback)
    {
        var hasRow = false;
        var hasHeader = false;
        foreach (var raw in tokens ?? Array.Empty<string?>())
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;
            if (LooksLikeImageToken(raw))
                continue;
            if (LooksLikeRowToken(raw))
                hasRow = true;
            else
                hasHeader = true;
        }

        if (!hasRow && !hasHeader)
            return fallback;
        if (hasRow && hasHeader)
            return ApplicationProfileTemplateDataScope.Both;
        return hasRow
            ? ApplicationProfileTemplateDataScope.PeopleM2M
            : ApplicationProfileTemplateDataScope.ApplicationHeader;
    }

    public static ApplicationProfileTemplateDataScope FromFieldPlan(
        ScanFieldPlan? plan,
        ApplicationProfileTemplateDataScope fallback) =>
        FromProposedTokens(plan?.Fields.Select(static f => f.ProposedToken), fallback);

    private static bool LooksLikeImageToken(string token)
    {
        var trimmed = token.Trim();
        return trimmed.Contains("IMAGE:", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeRowToken(string token)
    {
        var trimmed = token.Trim();
        if (trimmed.Contains("{{.", StringComparison.Ordinal))
            return true;
        if (trimmed.StartsWith("{{", StringComparison.Ordinal))
            return false;
        return trimmed.StartsWith(".", StringComparison.Ordinal);
    }
}