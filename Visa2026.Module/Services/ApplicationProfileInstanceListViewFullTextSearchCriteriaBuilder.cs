using System.Collections.Generic;
using System.Linq;
using System.Text;
using DevExpress.Data.Filtering;
using Visa2026.Module.Services.ReportDashboard;

namespace Visa2026.Module.Services;

/// <summary>
/// FullTextSearch extras for Application Profile Instance ListViews: linked people
/// first/last/middle names, and the document numbers printed on progress steps
/// (ministry result number, hand-in document number, CO / process number).
/// </summary>
public static class ApplicationProfileInstanceListViewFullTextSearchCriteriaBuilder
{
    /// <summary>
    /// Compare a compacted search key to a progress-step number (spaces ignored in the typed key).
    /// Same free-join shape as passport search so EF Core translates to EXISTS.
    /// </summary>
    public const string ResultNumberContainsCriteria =
        "Contains(Lower([ResultNumber]), ?)";

    public const string SubmissionDocumentNumberContainsCriteria =
        "Contains(Lower([SubmissionDocumentNumber]), ?)";

    public const string ProgressProcessNumberContainsCriteria =
        "Contains(Lower([ProcessNumber]), ?)";

    private const string StepNumberContainsCriteria =
        "(" + ResultNumberContainsCriteria
        + " Or " + SubmissionDocumentNumberContainsCriteria
        + " Or " + ProgressProcessNumberContainsCriteria + ")";

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
    /// Match folded tokens against the numbers shown under progress steps:
    /// ministry <see cref="BusinessObjects.ApplicationProfileInstanceProgress.ResultNumber"/>,
    /// hand-in <see cref="BusinessObjects.ApplicationProfileInstanceProgress.SubmissionDocumentNumber"/>,
    /// and the CO / process number on that progress row.
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

        // One association filter: every token must appear on the same progress row,
        // in any of the three step numbers (e.g. "4 / 4114" matches stored "4/4114").
        var inner = string.Join(" And ", needles.Select(_ => StepNumberContainsCriteria));
        var operands = new object[needles.Length * 3];
        for (var i = 0; i < needles.Length; i++)
        {
            operands[i * 3] = needles[i];
            operands[i * 3 + 1] = needles[i];
            operands[i * 3 + 2] = needles[i];
        }

        return CriteriaOperator.Parse($"[ProgressHistory][{inner}]", operands);
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