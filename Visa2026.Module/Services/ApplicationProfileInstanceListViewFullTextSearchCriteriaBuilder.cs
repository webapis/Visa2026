using System.Collections.Generic;
using System.Linq;
using System.Text;
using DevExpress.Data.Filtering;
using Visa2026.Module.Services.ReportDashboard;

namespace Visa2026.Module.Services;

/// <summary>
/// FullTextSearch extras for Application Profile Instance ListViews: linked people
/// first/last/middle names, and ministry <c>ResultNumber</c> on progress history.
/// </summary>
public static class ApplicationProfileInstanceListViewFullTextSearchCriteriaBuilder
{
    /// <summary>
    /// Compare a compacted search key to <c>ResultNumber</c> (spaces ignored in the typed key).
    /// Same free-join shape as passport search so EF Core translates to EXISTS.
    /// </summary>
    public const string ResultNumberContainsCriteria =
        "Contains(Lower([ResultNumber]), ?)";

    public static CriteriaOperator? BuildLinkedPeopleIdentityCriteria(string searchText)
    {
        var tokens = ReportDashboardCatalog.PersonSearchTokens(searchText);
        if (tokens.Length == 0)
            return null;

        var innerParts = new List<string>();
        var operands = new List<object>();
        foreach (var token in tokens)
        {
            innerParts.Add(
                $"({PersonSearchTextNormalizer.FoldedLowerContainsCriteria("[FirstName]")} Or {PersonSearchTextNormalizer.FoldedLowerContainsCriteria("[LastName]")} Or {PersonSearchTextNormalizer.FoldedLowerContainsCriteria("[MiddleName]")})");
            operands.Add(token);
            operands.Add(token);
            operands.Add(token);
        }

        return CriteriaOperator.Parse($"[People][{string.Join(" And ", innerParts)}]", operands.ToArray());
    }

    /// <summary>
    /// Match folded tokens against <see cref="BusinessObjects.ApplicationProfileInstanceProgress.ResultNumber"/>
    /// on any progress row (Netije belgisi on ministry Approved/Unapproved steps).
    /// </summary>
    public static CriteriaOperator? BuildProgressResultNumberCriteria(string searchText)
    {
        var needles = ReportDashboardCatalog.PersonSearchTokens(searchText)
            .Select(CompactResultNumberSearchKey)
            .Where(static n => n.Length > 0)
            .Select(static n => n.ToLowerInvariant())
            .ToArray();
        if (needles.Length == 0)
            return null;

        // One association filter: every token must appear in the same ResultNumber (e.g. "4"+"4114" → "4/4114").
        var inner = string.Join(" And ", needles.Select(_ => ResultNumberContainsCriteria));
        return CriteriaOperator.Parse(
            $"[ProgressHistory][{inner}]",
            needles.Cast<object>().ToArray());
    }

    /// <summary>Drop spaces so typed <c>4 / 4114</c> matches stored <c>4/4114</c>.</summary>
    public static string CompactResultNumberSearchKey(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return string.Empty;

        var builder = new StringBuilder(token.Length);
        foreach (var ch in token)
        {
            if (char.IsWhiteSpace(ch))
                continue;
            builder.Append(ch);
        }

        return builder.ToString();
    }
}