using System.Globalization;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.UserReports;

#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

/// <summary>
/// Reverse-mapping input: what the chosen instance actually holds, per allowed placeholder, so a
/// filled document's literals can be matched back to tokens without any template existing yet.
/// </summary>
public interface IApplicationProfileInstanceValueMapService
{
    ApplicationProfileInstanceValueMap Build(ApplicationProfileInstanceValueMapRequest request);
}

/// <inheritdoc cref="IApplicationProfileInstanceValueMapService"/>
public sealed class ApplicationProfileInstanceValueMapService : IApplicationProfileInstanceValueMapService
{
    public ApplicationProfileInstanceValueMap Build(ApplicationProfileInstanceValueMapRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Instance);
        ArgumentNullException.ThrowIfNull(request.PlaceholderSet);

        var allowed = request.PlaceholderSet.Allowed;
        var wantsHeader = request.DataScope != ApplicationProfileTemplateDataScope.PeopleM2M;
        var wantsRows = request.DataScope != ApplicationProfileTemplateDataScope.ApplicationHeader;

        var header = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var rows = new List<IReadOnlyDictionary<string, string?>>();
        var draft = new List<ValueCandidate>();
        var rejected = new List<RejectedValue>();

        if (wantsHeader)
        {
            foreach (var entry in allowed.Where(static e => !e.IsImage && e.Scope != UserReportPlaceholderScope.Row))
            {
                var raw = ReadValue(request.Instance, entry);
                header[entry.ShortCode] = raw;
                Collect(entry, raw, rowIndex: null, UserReportPlaceholderScope.Header, draft, rejected);
            }
        }

        if (wantsRows)
        {
            // Images carry no text to reverse-match, so they never enter the value map.
            var rowEntries = allowed.Where(static e => !e.IsImage && e.Scope != UserReportPlaceholderScope.Header).ToList();
            var lines = request.Rows ?? ResolveRows(request.Instance);

            for (var index = 0; index < lines.Count; index++)
            {
                var row = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in rowEntries)
                {
                    var raw = ReadValue(lines[index], entry);
                    row[entry.ShortCode] = raw;
                    Collect(entry, raw, index, UserReportPlaceholderScope.Row, draft, rejected);
                }

                rows.Add(row);
            }
        }

        var candidates = request.RetainAmbiguousLiterals
            ? draft
            : RejectAmbiguous(draft, rejected);

        return new ApplicationProfileInstanceValueMap
        {
            ApplicationProfileInstanceId = request.Instance.ID,
            Header = header,
            Rows = rows,
            Candidates = candidates,
            Rejected = rejected
                .OrderBy(static r => r.ShortCode, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static r => r.RowIndex ?? -1)
                .ToList(),
        };
    }

    private static IReadOnlyList<ApplicationRosterMergeLine> ResolveRows(ApplicationProfileInstance instance) =>
        [.. UserReportMergeDataHelper.GetActiveApplicationItems(instance)];

    private static string? ReadValue(object source, UserReportPlaceholderCatalogEntry entry)
    {
        var value = UserReportMergeDataHelper.GetPropertyValue(source, entry.CanonicalPath);
        return value switch
        {
            null => null,
            string text => text,
            DateTime date => date.Year <= 1 ? null : date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
            byte[] => null,
            bool flag => flag ? "true" : "false",
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => null,
        };
    }

    private static void Collect(
        UserReportPlaceholderCatalogEntry entry,
        string? raw,
        int? rowIndex,
        UserReportPlaceholderScope usageScope,
        List<ValueCandidate> draft,
        List<RejectedValue> rejected)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return;

        var kind = TemplateValueMatchKeys.Classify(entry.ShortCode, raw);

        // An unset date renders as 01.01.0001. That is missing data, not a value to match, so it is
        // absent from the map rather than recorded as rejected.
        if (kind == ValueKind.Date && TemplateValueMatchKeys.IsSentinelDate(raw))
            return;

        if (kind == ValueKind.Number && TemplateValueMatchKeys.CountDigits(raw) <= 2)
        {
            rejected.Add(new RejectedValue(entry.ShortCode, raw, kind, rowIndex, ValueRejectionReason.SmallNumber));
            return;
        }

        var keys = TemplateValueMatchKeys.Build(raw, kind);
        if (keys.Count == 0)
        {
            rejected.Add(new RejectedValue(entry.ShortCode, raw, kind, rowIndex, ValueRejectionReason.TooShort));
            return;
        }

        draft.Add(new ValueCandidate(
            entry.ShortCode,
            entry.BuildWordToken(usageScope),
            raw,
            keys[0],
            kind,
            rowIndex,
            keys));
    }

    /// <summary>
    /// A literal that resolves to more than one token cannot be attributed, so it is dropped and
    /// recorded. Aliases collide here by design — <c>Travel_PurposeOfTravelTm</c> returns
    /// <c>Position_PositionTm</c>, and highlighting one over the other would be a coin flip.
    /// </summary>
    private static List<ValueCandidate> RejectAmbiguous(
        List<ValueCandidate> draft,
        List<RejectedValue> rejected)
    {
        var codesByKey = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var candidate in draft)
        {
            foreach (var key in candidate.MatchKeys)
            {
                if (!codesByKey.TryGetValue(key, out var codes))
                    codesByKey[key] = codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                codes.Add(candidate.ShortCode);
            }
        }

        var kept = new List<ValueCandidate>(draft.Count);
        foreach (var candidate in draft)
        {
            var ambiguous = candidate.MatchKeys.Any(key => codesByKey[key].Count > 1);
            if (ambiguous)
            {
                rejected.Add(new RejectedValue(
                    candidate.ShortCode,
                    candidate.RawValue,
                    candidate.Kind,
                    candidate.RowIndex,
                    ValueRejectionReason.Ambiguous));
                continue;
            }

            kept.Add(candidate);
        }

        return kept;
    }
}
