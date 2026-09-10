#nullable enable

using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Review lock: keep officer-reviewed placeholders while Remap unmarked re-guesses other yellows.
/// Identity is the OpenXML / Excel region, not FieldId.
/// </summary>
public static class ScanFieldPlanLockMerge
{
    public static Dictionary<string, ScanDetectedField> Index(IReadOnlyList<ScanDetectedField>? lockedFields)
    {
        var map = new Dictionary<string, ScanDetectedField>(StringComparer.Ordinal);
        if (lockedFields == null)
            return map;

        foreach (var field in lockedFields)
        {
            if (!field.IsLocked)
                continue;

            map[ScanDocumentRegionKey.ForField(field)] = field;
        }

        return map;
    }

    public static void RegisterUsedHeaderCodes(
        IReadOnlyList<ScanDetectedField>? lockedFields,
        ApplicationProfilePlaceholderSet placeholderSet,
        HashSet<string> usedHeaderCodes)
    {
        ArgumentNullException.ThrowIfNull(placeholderSet);
        ArgumentNullException.ThrowIfNull(usedHeaderCodes);
        if (lockedFields == null)
            return;

        foreach (var field in lockedFields)
        {
            if (!field.IsLocked)
                continue;

            foreach (var code in TemplateTokenSyntax.GetShortCodes(field.ProposedToken))
            {
                if (IsReservedHeaderCode(placeholderSet, field.Scope, code))
                    usedHeaderCodes.Add(code);
            }
        }
    }

    public static ScanDetectedFieldDraft ToDraft(ScanDetectedField pin, ScanOfficeYellowSpan? yellow = null)
    {
        ArgumentNullException.ThrowIfNull(pin);
        return new ScanDetectedFieldDraft
        {
            FieldId = Guid.NewGuid().ToString("N"),
            PageIndex = yellow?.PageIndex ?? pin.PageIndex,
            LabelText = yellow?.Text ?? pin.LabelText,
            ProposedToken = pin.ProposedToken,
            Confidence = pin.Confidence,
            Scope = pin.Scope,
            Box = pin.Box,
            SourceRegion = yellow?.Region ?? pin.SourceRegion,
            Alternatives = pin.Alternatives,
            IsLocked = true,
        };
    }

    public static ScanFieldPlan Apply(ScanFieldPlan rebuilt, IReadOnlyList<ScanDetectedField>? lockedFields)
    {
        ArgumentNullException.ThrowIfNull(rebuilt);
        var pins = Index(lockedFields);
        if (pins.Count == 0)
            return rebuilt;

        var lockedHeaderCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        RegisterUsedHeaderCodes(lockedFields, rebuilt.PlaceholderSet, lockedHeaderCodes);

        var usedKeys = new HashSet<string>(StringComparer.Ordinal);
        var fields = new List<ScanDetectedField>(rebuilt.Fields.Count + pins.Count);
        foreach (var field in rebuilt.Fields)
        {
            var key = ScanDocumentRegionKey.ForField(field);
            if (pins.TryGetValue(key, out var pin))
            {
                usedKeys.Add(key);
                fields.Add(Restore(field, pin));
                continue;
            }

            fields.Add(StripStolenHeaderCodes(field, lockedHeaderCodes, rebuilt.PlaceholderSet));
        }

        foreach (var pair in pins)
        {
            if (usedKeys.Contains(pair.Key))
                continue;

            fields.Add(pair.Value);
        }

        return new ScanFieldPlan
        {
            PlaceholderSet = rebuilt.PlaceholderSet,
            ScanKind = rebuilt.ScanKind,
            Fields = fields,
            StaticRegions = rebuilt.StaticRegions,
            Gaps = rebuilt.Gaps,
            PendingQuestions = rebuilt.PendingQuestions,
            Rationale = rebuilt.Rationale,
            Source = rebuilt.Source,
            YellowHighlightCount = rebuilt.YellowHighlightCount,
        };
    }

    private static bool IsReservedHeaderCode(
        ApplicationProfilePlaceholderSet set,
        ScanFieldScope fieldScope,
        string shortCode)
    {
        if (fieldScope == ScanFieldScope.Row)
            return false;

        var entry = set.Allowed.FirstOrDefault(e =>
            string.Equals(e.ShortCode, shortCode, StringComparison.OrdinalIgnoreCase));
        return entry != null && entry.Scope != UserReportPlaceholderScope.Row;
    }

    private static ScanDetectedField Restore(ScanDetectedField rebuilt, ScanDetectedField pin) =>
        new()
        {
            FieldId = rebuilt.FieldId,
            Box = rebuilt.Box,
            PageIndex = rebuilt.PageIndex,
            LabelText = rebuilt.LabelText,
            ProposedToken = pin.ProposedToken,
            Confidence = pin.Confidence,
            Scope = pin.Scope,
            SourceRegion = rebuilt.SourceRegion ?? pin.SourceRegion,
            Alternatives = pin.Alternatives,
            HiddenPartIndexes = pin.HiddenPartIndexes,
            IsLocked = true,
        };

    private static ScanDetectedField StripStolenHeaderCodes(
        ScanDetectedField field,
        HashSet<string> lockedHeaderCodes,
        ApplicationProfilePlaceholderSet set)
    {
        if (field.IsLocked || lockedHeaderCodes.Count == 0 || string.IsNullOrWhiteSpace(field.ProposedToken))
            return field;

        var codes = TemplateTokenSyntax.GetShortCodes(field.ProposedToken);
        var kept = codes
            .Where(c => !lockedHeaderCodes.Contains(c))
            .ToList();
        if (kept.Count == codes.Count)
            return field;

        string? token = null;
        if (kept.Count > 0)
        {
            var usage = field.Scope == ScanFieldScope.Row
                ? UserReportPlaceholderScope.Row
                : UserReportPlaceholderScope.Header;
            var parts = new List<string>();
            foreach (var code in kept)
            {
                var entry = set.Allowed.FirstOrDefault(e =>
                    string.Equals(e.ShortCode, code, StringComparison.OrdinalIgnoreCase));
                if (entry != null)
                    parts.Add(entry.BuildWordToken(usage));
            }

            token = parts.Count == 0
                ? null
                : ScanFieldPlanOfficerOverride.JoinLibraryTokens(field.LabelText, field.ProposedToken, parts);
        }

        return new ScanDetectedField
        {
            FieldId = field.FieldId,
            Box = field.Box,
            PageIndex = field.PageIndex,
            LabelText = field.LabelText,
            ProposedToken = token,
            Confidence = string.IsNullOrWhiteSpace(token) ? ScanFieldConfidence.Low : field.Confidence,
            Scope = field.Scope,
            SourceRegion = field.SourceRegion,
            Alternatives = field.Alternatives,
            HiddenPartIndexes = field.HiddenPartIndexes,
            IsLocked = false,
        };
    }
}