#nullable enable

using System.Globalization;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Officer remap on Review: keep the yellow span, swap library token(s).
/// </summary>
public static class ScanFieldPlanOfficerOverride
{
    public static ScanFieldPlan ApplyToken(ScanFieldPlan plan, string fieldId, string? shortCode)
    {
        var requested = (shortCode ?? string.Empty).Trim();
        return ApplyTokens(
            plan,
            fieldId,
            requested.Length == 0 ? Array.Empty<string>() : [requested]);
    }

    /// <summary>
    /// One yellow span can hold several library tokens (passport line, name+date, count+period).
    /// </summary>
    public static ScanFieldPlan ApplyTokens(ScanFieldPlan plan, string fieldId, IReadOnlyList<string>? shortCodes)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (string.IsNullOrWhiteSpace(fieldId))
            return plan;

        var resolvedId = ScanReviewFieldOrder.ParentFieldId(fieldId);
        var index = -1;
        for (var i = 0; i < plan.Fields.Count; i++)
        {
            if (string.Equals(plan.Fields[i].FieldId, resolvedId, StringComparison.Ordinal))
            {
                index = i;
                break;
            }
        }

        if (index < 0)
            return PromoteGap(plan, resolvedId, shortCodes);

        var field = plan.Fields[index];
        var requested = (shortCodes ?? Array.Empty<string>())
            .Select(static c => (c ?? string.Empty).Trim())
            .Where(static c => c.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var usage = field.Scope == ScanFieldScope.Row
            ? UserReportPlaceholderScope.Row
            : UserReportPlaceholderScope.Header;

        var parts = new List<string>();
        foreach (var code in requested)
        {
            var entry = plan.PlaceholderSet.Allowed.FirstOrDefault(e =>
                string.Equals(e.ShortCode, code, StringComparison.OrdinalIgnoreCase));
            if (entry == null)
                continue;

            parts.Add(entry.BuildWordToken(usage));
        }

        if (requested.Count > 0 && parts.Count == 0)
            return plan;

        string? token = parts.Count == 0
            ? null
            : JoinLibraryTokens(field.LabelText, field.ProposedToken, parts);

        if (string.Equals(field.ProposedToken, token, StringComparison.Ordinal))
            return plan;

        var fields = plan.Fields.ToList();
        fields[index] = CopyField(field, token, string.IsNullOrWhiteSpace(token)
            ? ScanFieldConfidence.Low
            : ScanFieldConfidence.High);

        return WithFields(plan, fields, plan.Gaps);
    }

    /// <summary>Lock or unlock a yellow span so Remap unmarked will skip or re-guess it.</summary>
    public static ScanFieldPlan SetLocked(ScanFieldPlan plan, string fieldId, bool locked)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var resolvedId = ScanReviewFieldOrder.ParentFieldId(fieldId);
        if (string.IsNullOrWhiteSpace(resolvedId))
            return plan;

        var index = -1;
        for (var i = 0; i < plan.Fields.Count; i++)
        {
            if (string.Equals(plan.Fields[i].FieldId, resolvedId, StringComparison.Ordinal))
            {
                index = i;
                break;
            }
        }

        if (index < 0 || plan.Fields[index].IsLocked == locked)
            return plan;

        var fields = plan.Fields.ToList();
        var field = fields[index];
        fields[index] = CopyField(field, field.ProposedToken, field.Confidence, field.HiddenPartIndexes, locked);
        return WithFields(plan, fields, plan.Gaps);
    }

    /// <summary>Lock every mapped yellow so Remap unmarked only targets gaps and unlocked rows.</summary>
    public static ScanFieldPlan LockMapped(ScanFieldPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var changed = false;
        var fields = new List<ScanDetectedField>(plan.Fields.Count);
        foreach (var field in plan.Fields)
        {
            if (!field.IsLocked && !string.IsNullOrWhiteSpace(field.ProposedToken))
            {
                changed = true;
                fields.Add(CopyField(field, field.ProposedToken, field.Confidence, field.HiddenPartIndexes, locked: true));
                continue;
            }

            fields.Add(field);
        }

        return changed ? WithFields(plan, fields, plan.Gaps) : plan;
    }

    /// <summary>
    /// Remap one Review sub-row (5.1) without dropping sibling tokens on the same yellow span.
    /// Empty sibling slots stay as positional holes so Lock/Review keep the code on the chosen part.
    /// </summary>
    public static ScanFieldPlan ApplyPartCodes(
        ScanFieldPlan plan,
        string rowKey,
        IReadOnlyList<string>? partShortCodes)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (string.IsNullOrWhiteSpace(rowKey))
            return plan;

        var parentId = ScanReviewFieldOrder.ParentFieldId(rowKey);
        var partIndex = OverlayPartIndex(rowKey);
        if (partIndex <= 0)
            return ApplyTokens(plan, parentId, partShortCodes);

        var field = plan.Fields.FirstOrDefault(f =>
            string.Equals(f.FieldId, parentId, StringComparison.Ordinal));
        if (field == null)
            return plan;

        var parts = ScanCompoundYellowParts.Split(field.LabelText, field.ProposedToken);
        if (parts.Count <= 1)
            return ApplyTokens(plan, parentId, partShortCodes);

        var hidden = field.HiddenPartIndexes ?? Array.Empty<int>();
        var requested = (partShortCodes ?? Array.Empty<string>())
            .Select(static c => (c ?? string.Empty).Trim())
            .Where(static c => c.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var token = BuildPositionalCompoundToken(
            field,
            plan.PlaceholderSet,
            parts,
            hidden,
            remapPartIndex: partIndex,
            remapCodes: requested);

        if (string.Equals(field.ProposedToken, token, StringComparison.Ordinal))
            return plan;

        var index = -1;
        for (var i = 0; i < plan.Fields.Count; i++)
        {
            if (string.Equals(plan.Fields[i].FieldId, parentId, StringComparison.Ordinal))
            {
                index = i;
                break;
            }
        }

        if (index < 0)
            return plan;

        var fields = plan.Fields.ToList();
        fields[index] = CopyField(
            field,
            token,
            string.IsNullOrWhiteSpace(token) ? ScanFieldConfidence.Low : ScanFieldConfidence.High,
            field.HiddenPartIndexes,
            field.IsLocked);
        return WithFields(plan, fields, plan.Gaps);
    }

    /// <summary>
    /// Officer Add placeholder on 12.1 / 12.2 appends a sibling (12.3) on the same yellow span.
    /// Empty unmapped parts still use <see cref="ApplyPartCodes"/> to fill that slot.
    /// </summary>
    public static ScanFieldPlan AppendShortCodes(
        ScanFieldPlan plan,
        string rowKey,
        IReadOnlyList<string>? extraShortCodes)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (string.IsNullOrWhiteSpace(rowKey))
            return plan;

        var parentId = ScanReviewFieldOrder.ParentFieldId(rowKey);
        var field = plan.Fields.FirstOrDefault(f =>
            string.Equals(f.FieldId, parentId, StringComparison.Ordinal));
        if (field == null)
            return plan;

        var extra = (extraShortCodes ?? Array.Empty<string>())
            .Select(static c => (c ?? string.Empty).Trim())
            .Where(static c => c.Length > 0)
            .ToList();
        if (extra.Count == 0)
            return plan;

        var allCodes = TemplateTokenSyntax.GetShortCodes(field.ProposedToken).ToList();
        foreach (var code in extra)
        {
            if (!allCodes.Contains(code, StringComparer.OrdinalIgnoreCase))
                allCodes.Add(code);
        }

        return ApplyTokens(plan, parentId, allCodes);
    }

    /// <summary>
    /// Dismiss a Detected fields row. A compound part (10.2) is hidden; the yellow span keeps remaining tokens.
    /// A whole mark (or the last remaining part) is removed so Generate leaves the printed text.
    /// </summary>
    public static ScanFieldPlan RemoveReviewRow(ScanFieldPlan plan, string rowKey)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (string.IsNullOrWhiteSpace(rowKey))
            return plan;

        var parentId = ScanReviewFieldOrder.ParentFieldId(rowKey);
        var gapIndex = -1;
        for (var i = 0; i < plan.Gaps.Count; i++)
        {
            if (string.Equals(plan.Gaps[i].FieldId, parentId, StringComparison.Ordinal))
            {
                gapIndex = i;
                break;
            }
        }

        if (gapIndex >= 0)
        {
            var gaps = plan.Gaps.ToList();
            gaps.RemoveAt(gapIndex);
            return WithFields(plan, plan.Fields.ToList(), gaps);
        }

        var index = -1;
        for (var i = 0; i < plan.Fields.Count; i++)
        {
            if (string.Equals(plan.Fields[i].FieldId, parentId, StringComparison.Ordinal))
            {
                index = i;
                break;
            }
        }

        if (index < 0)
            return plan;

        var field = plan.Fields[index];
        var parts = ScanCompoundYellowParts.Split(field.LabelText, field.ProposedToken);
        var partIndex = OverlayPartIndex(rowKey);
        var hidden = field.HiddenPartIndexes.ToList();

        if (parts.Count <= 1 || partIndex <= 0)
            return DropField(plan, index);

        if (hidden.Contains(partIndex))
            return plan;

        hidden.Add(partIndex);
        var visible = parts.Count(p => !hidden.Contains(p.Index));
        if (visible <= 0)
            return DropField(plan, index);

        var token = BuildPositionalCompoundToken(field, plan.PlaceholderSet, parts, hidden);

        var fields = plan.Fields.ToList();
        fields[index] = CopyField(
            field,
            token,
            string.IsNullOrWhiteSpace(token) ? ScanFieldConfidence.Low : field.Confidence,
            hidden);

        return WithFields(plan, fields, plan.Gaps.ToList());
    }

    private static ScanFieldPlan PromoteGap(
        ScanFieldPlan plan,
        string fieldId,
        IReadOnlyList<string>? shortCodes)
    {
        var gapIndex = -1;
        for (var i = 0; i < plan.Gaps.Count; i++)
        {
            if (string.Equals(plan.Gaps[i].FieldId, fieldId, StringComparison.Ordinal))
            {
                gapIndex = i;
                break;
            }
        }

        if (gapIndex < 0)
            return plan;

        var gap = plan.Gaps[gapIndex];
        var gaps = plan.Gaps.ToList();
        gaps.RemoveAt(gapIndex);

        var fields = plan.Fields.ToList();
        fields.Add(new ScanDetectedField
        {
            FieldId = gap.FieldId,
            Box = ScanBoundingBox.FullPage,
            PageIndex = gap.PageIndex,
            LabelText = gap.LabelText,
            ProposedToken = null,
            Confidence = ScanFieldConfidence.Low,
            Scope = ScanFieldScope.Header,
            SourceRegion = gap.SourceRegion,
        });

        return ApplyTokens(WithFields(plan, fields, gaps), fieldId, shortCodes);
    }

    private static ScanFieldPlan DropField(ScanFieldPlan plan, int index)
    {
        var fields = plan.Fields.ToList();
        fields.RemoveAt(index);
        return WithFields(plan, fields, plan.Gaps.ToList());
    }

    private static ScanFieldPlan WithFields(
        ScanFieldPlan plan,
        IReadOnlyList<ScanDetectedField> fields,
        IReadOnlyList<ScanGap> gaps) =>
        new()
        {
            PlaceholderSet = plan.PlaceholderSet,
            ScanKind = plan.ScanKind,
            Fields = fields,
            StaticRegions = plan.StaticRegions,
            Gaps = gaps,
            PendingQuestions = plan.PendingQuestions,
            Rationale = plan.Rationale,
            Source = "officer",
            YellowHighlightCount = plan.YellowHighlightCount,
        };

    private static ScanDetectedField CopyField(
        ScanDetectedField field,
        string? token,
        ScanFieldConfidence confidence,
        IReadOnlyList<int>? hiddenPartIndexes = null,
        bool? locked = null) =>
        new()
        {
            FieldId = field.FieldId,
            Box = field.Box,
            PageIndex = field.PageIndex,
            LabelText = field.LabelText,
            ProposedToken = token,
            Confidence = confidence,
            Scope = field.Scope,
            SourceRegion = field.SourceRegion,
            Alternatives = field.Alternatives,
            HiddenPartIndexes = hiddenPartIndexes ?? field.HiddenPartIndexes,
            IsLocked = locked ?? field.IsLocked,
        };

    private static int OverlayPartIndex(string rowKey)
    {
        var key = rowKey.Trim();
        var colon = key.LastIndexOf(':');
        if (colon <= 0 || colon + 1 >= key.Length)
            return 0;

        return int.TryParse(key[(colon + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var part)
            ? part
            : 0;
    }

    public static string FormatOfficerHint(ScanFieldPlan plan, string fieldId, int order) =>
        FormatOfficerHint(plan, fieldId, order.ToString(System.Globalization.CultureInfo.InvariantCulture));

    public static string FormatOfficerHint(ScanFieldPlan plan, string fieldId, string displayOrder)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var (label, current) = Describe(plan, ScanReviewFieldOrder.ParentFieldId(fieldId));
        return $"Mark #{displayOrder} is selected (“{label}”). Current placeholder: {current}. Add one or more from the Short list, or tell me which library fields fit this yellow mark.";
    }

    public static string FormatChatContext(ScanFieldPlan plan, string fieldId, int order) =>
        FormatChatContext(plan, fieldId, order.ToString(System.Globalization.CultureInfo.InvariantCulture));

    public static string FormatChatContext(ScanFieldPlan plan, string fieldId, string displayOrder)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var resolvedId = ScanReviewFieldOrder.ParentFieldId(fieldId);
        var field = plan.Fields.FirstOrDefault(f =>
            string.Equals(f.FieldId, resolvedId, StringComparison.Ordinal));
        if (field == null)
        {
            var gap = plan.Gaps.FirstOrDefault(g =>
                string.Equals(g.FieldId, resolvedId, StringComparison.Ordinal));
            if (gap != null)
                return $"Focused yellow mark #{displayOrder}. Printed text: \"{gap.LabelText}\". Currently unmapped. If the officer is talking about this highlight, suggest placeholders for it. If they name a different Review # or printed form line, change those fieldIds instead.";

            return $"Focused yellow mark #{displayOrder}. If the officer names a different Review # or form line, remap those yellows, not this mark.";
        }

        var codes = TemplateTokenSyntax.GetShortCodes(field.ProposedToken);
        var current = codes.Count == 0
            ? "unmapped"
            : string.Join(" + ", codes) + " (" + field.ProposedToken + ")";
        var names = new List<string>();
        foreach (var code in codes)
        {
            var entry = plan.PlaceholderSet.Allowed.FirstOrDefault(e =>
                string.Equals(e.ShortCode, code, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(entry?.CanonicalPath))
                names.Add(entry.CanonicalPath);
        }

        var nameBit = names.Count == 0 ? string.Empty : $" Full name: {string.Join(" · ", names)}.";
        return $"Focused yellow mark #{displayOrder}. Printed text: \"{field.LabelText}\". Current placeholder: {current}.{nameBit} Remap this fieldId only when the officer means this printed text. If they name another Review # or form line (for example 12. Wizanyň berilen senesi), change those fieldIds and leave this mark unchanged.";
    }

    /// <summary>
    /// One token per comma segment, including <see cref="ScanCompoundYellowParts.EmptyPartToken"/>
    /// for hidden and unmapped slots, so later Add/Lock still land on the officer's part (11.2).
    /// </summary>
    private static string? BuildPositionalCompoundToken(
        ScanDetectedField field,
        ApplicationProfilePlaceholderSet placeholderSet,
        IReadOnlyList<ScanCompoundPart> parts,
        IReadOnlyList<int> hidden,
        int remapPartIndex = 0,
        IReadOnlyList<string>? remapCodes = null)
    {
        var usage = field.Scope == ScanFieldScope.Row
            ? UserReportPlaceholderScope.Row
            : UserReportPlaceholderScope.Header;
        var pieces = new List<string>(parts.Count);
        foreach (var part in parts)
        {
            if (hidden.Contains(part.Index))
            {
                pieces.Add(ScanCompoundYellowParts.EmptyPartToken);
                continue;
            }

            IReadOnlyList<string> codesForPart = remapPartIndex == part.Index
                ? remapCodes ?? Array.Empty<string>()
                : string.IsNullOrWhiteSpace(part.ShortCode)
                    ? Array.Empty<string>()
                    : [part.ShortCode];

            if (codesForPart.Count == 0)
            {
                pieces.Add(ScanCompoundYellowParts.EmptyPartToken);
                continue;
            }

            var built = new List<string>();
            foreach (var code in codesForPart)
            {
                var entry = placeholderSet.Allowed.FirstOrDefault(e =>
                    string.Equals(e.ShortCode, code, StringComparison.OrdinalIgnoreCase));
                if (entry != null)
                    built.Add(entry.BuildWordToken(usage));
            }

            pieces.Add(built.Count == 0
                ? ScanCompoundYellowParts.EmptyPartToken
                : built.Count == 1
                    ? built[0]
                    : string.Join(" ", built));
        }

        if (pieces.Count == 0 || pieces.All(ScanCompoundYellowParts.IsEmptyPartToken))
            return null;

        return string.Join(InferSeparator(field.LabelText, field.ProposedToken), pieces);
    }

    internal static string JoinLibraryTokens(
        string labelText,
        string? previousToken,
        IReadOnlyList<string> tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        if (tokens.Count <= 1)
            return tokens.Count == 0 ? string.Empty : tokens[0];

        return string.Join(InferSeparator(labelText, previousToken), tokens);
    }

    private static string InferSeparator(string labelText, string? previousToken)
    {
        if (!string.IsNullOrWhiteSpace(previousToken))
        {
            var firstEnd = previousToken.IndexOf("}}", StringComparison.Ordinal);
            var nextStart = firstEnd >= 0
                ? previousToken.IndexOf("{{", firstEnd + 2, StringComparison.Ordinal)
                : -1;
            if (firstEnd >= 0 && nextStart > firstEnd)
                return previousToken[(firstEnd + 2)..nextStart];
        }

        var label = labelText ?? string.Empty;
        if (label.Contains(',', StringComparison.Ordinal))
            return ", ";
        if (label.Contains('/', StringComparison.Ordinal))
            return " / ";
        if (label.Contains(" - ", StringComparison.Ordinal)
            || label.Contains(" – ", StringComparison.Ordinal)
            || label.Contains(" — ", StringComparison.Ordinal))
            return " - ";
        return " ";
    }

    private static (string Label, string Current) Describe(ScanFieldPlan plan, string fieldId)
    {
        var field = plan.Fields.FirstOrDefault(f =>
            string.Equals(f.FieldId, fieldId, StringComparison.Ordinal));
        if (field != null)
        {
            var codes = TemplateTokenSyntax.GetShortCodes(field.ProposedToken);
            var current = codes.Count == 0 ? "unmapped" : string.Join(" + ", codes);
            return (field.LabelText, current);
        }

        var gap = plan.Gaps.FirstOrDefault(g =>
            string.Equals(g.FieldId, fieldId, StringComparison.Ordinal));
        return (gap?.LabelText ?? "this mark", "unmapped");
    }
}