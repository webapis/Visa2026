#nullable enable

using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>Builds a yellow field-plan proposal from Office package marks (no vision).</summary>
public static class ScanOfficeFieldPlanBuilder
{
    public static ScanFieldPlanProposal Build(
        IReadOnlyList<ScanOfficeYellowSpan> yellows,
        ApplicationProfilePlaceholderSet placeholderSet,
        byte[]? officeBytes = null,
        ScanSourceKind sourceKind = ScanSourceKind.Word,
        IReadOnlyList<ValueCandidate>? valueCandidates = null)
    {
        ArgumentNullException.ThrowIfNull(yellows);
        ArgumentNullException.ThrowIfNull(placeholderSet);

        if (sourceKind == ScanSourceKind.Excel
            && officeBytes is { Length: > 0 })
        {
            var excelFields = ScanExcelYellowResolver.Resolve(officeBytes, yellows, placeholderSet)
                .SelectMany(draft => ScanCompoundYellowBinder.Upgrade([draft], placeholderSet))
                .ToList();
            return new ScanFieldPlanProposal
            {
                Fields = excelFields,
                Gaps = Array.Empty<ScanGapDraft>(),
                YellowHighlightCount = yellows.Count,
                Rationale = "excel-manual-inference",
                Source = "excel-manual-inference",
            };
        }

        var drafts = new List<ScanDetectedFieldDraft>();
        var usedHeaderCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var instanceCandidates = valueCandidates ?? Array.Empty<ValueCandidate>();
        var catalogExamples = ScanYellowValueHintResolver.CatalogExampleCandidates(placeholderSet);
        var nearbyByIndex = BuildNearbyLabels(officeBytes, sourceKind, yellows);

        for (var yellowIndex = 0; yellowIndex < yellows.Count; yellowIndex++)
        {
            var yellow = yellows[yellowIndex];
            nearbyByIndex.TryGetValue(yellowIndex, out var nearbyLabel);

            var resolved = ScanYellowHighlightTokenResolver.ResolveFromYellowText(
                yellow.Text,
                ScanBoundingBox.FullPage,
                yellow.PageIndex,
                placeholderSet,
                usedHeaderCodes,
                yellow.Region);

            if (resolved.Count == 0 && instanceCandidates.Count > 0)
            {
                resolved = ScanYellowValueHintResolver.Resolve(
                    yellow.Text,
                    yellow.PageIndex,
                    placeholderSet,
                    instanceCandidates,
                    usedHeaderCodes,
                    yellow.Region,
                    preferHeaderToken: true);
            }

            if (resolved.Count == 0
                && catalogExamples.Count > 0
                && !ScanCompoundYellowParts.IsCommaCombination(yellow.Text))
            {
                resolved = ScanYellowValueHintResolver.Resolve(
                    yellow.Text,
                    yellow.PageIndex,
                    placeholderSet,
                    catalogExamples,
                    usedHeaderCodes,
                    yellow.Region,
                    preferHeaderToken: true);
            }

            if (resolved.Count == 0)
            {
                resolved = TryCloneDuplicateLabel(yellow, drafts);
            }

            if (resolved.Count == 0)
            {
                resolved = ScanSurroundPlaceholderPattern.TryDraft(
                    yellow.Text,
                    yellow.PageIndex,
                    yellow.Region,
                    nearbyLabel,
                    columnHeader: null,
                    placeholderSet,
                    usedHeaderCodes,
                    ScanSurroundPlaceholderPattern.MinScore(nearbyLabel, null));
            }

            if (resolved.Count == 0)
                resolved = TryCloneDuplicateLabel(yellow, drafts);

            if (resolved.Count > 0)
            {
                drafts.AddRange(ScanCompoundYellowBinder.Upgrade(
                    ScanCompanyRegistrationDateGuard.RewriteDrafts(
                        ScanRepresentativeNameGuard.RewriteDrafts(
                            AttachNearby(resolved, nearbyLabel),
                            placeholderSet,
                            instanceCandidates,
                            usedHeaderCodes),
                        placeholderSet,
                        usedHeaderCodes),
                    placeholderSet));
                continue;
            }

            drafts.AddRange(ScanCompoundYellowBinder.Upgrade(
                [
                    new ScanDetectedFieldDraft
                    {
                        FieldId = Guid.NewGuid().ToString("N"),
                        PageIndex = yellow.PageIndex,
                        LabelText = yellow.Text,
                        ProposedToken = null,
                        Confidence = ScanFieldConfidence.Medium,
                        Scope = ScanFieldScope.Header,
                        Box = ScanBoundingBox.FullPage,
                        SourceRegion = yellow.Region,
                        NearbyLabel = nearbyLabel,
                    },
                ],
                placeholderSet));
        }

        drafts.AddRange(BuildPersonPhotoDrafts(officeBytes, sourceKind, placeholderSet, drafts));

        return new ScanFieldPlanProposal
        {
            Fields = drafts,
            Gaps = Array.Empty<ScanGapDraft>(),
            YellowHighlightCount = yellows.Count,
            Rationale = "office-yellow",
            Source = "office-yellow",
        };
    }

    /// <summary>
    /// Reopens Review on a saved merge template: each library token cluster is already mapped.
    /// Does not re-guess from yellow sample text.
    /// </summary>
    public static ScanFieldPlanProposal BuildFromLibraryTokens(
        IReadOnlyList<ScanOfficeYellowSpan> tokenSpans,
        ApplicationProfilePlaceholderSet placeholderSet)
    {
        ArgumentNullException.ThrowIfNull(tokenSpans);
        ArgumentNullException.ThrowIfNull(placeholderSet);

        var drafts = new List<ScanDetectedFieldDraft>(tokenSpans.Count);
        foreach (var span in tokenSpans)
        {
            var codes = TemplateTokenSyntax.GetShortCodes(span.Text)
                .Where(placeholderSet.Contains)
                .ToList();
            if (codes.Count == 0)
                continue;

            drafts.Add(new ScanDetectedFieldDraft
            {
                FieldId = Guid.NewGuid().ToString("N"),
                PageIndex = span.PageIndex,
                LabelText = span.Text.Trim(),
                ProposedToken = span.Text.Trim(),
                Confidence = ScanFieldConfidence.High,
                Scope = ScopeFromCodes(codes, placeholderSet),
                Box = ScanBoundingBox.FullPage,
                SourceRegion = span.Region,
            });
        }

        return new ScanFieldPlanProposal
        {
            Fields = drafts,
            Gaps = Array.Empty<ScanGapDraft>(),
            YellowHighlightCount = 0,
            Rationale = ScanOfficeLibraryTokenExtractor.FieldPlanSource,
            Source = ScanOfficeLibraryTokenExtractor.FieldPlanSource,
        };
    }

    internal static IReadOnlyList<ScanDetectedFieldDraft> BuildPersonPhotoDrafts(
        byte[]? officeBytes,
        ScanSourceKind sourceKind,
        ApplicationProfilePlaceholderSet placeholderSet,
        IReadOnlyList<ScanDetectedFieldDraft> existing)
    {
        if (sourceKind != ScanSourceKind.Word || officeBytes is not { Length: > 64 })
            return Array.Empty<ScanDetectedFieldDraft>();

        var photo = placeholderSet.Allowed.FirstOrDefault(static e =>
            e.IsImage
            && (string.Equals(e.ShortCode, "PPH", StringComparison.OrdinalIgnoreCase)
                || string.Equals(e.CanonicalPath, "Person_Photo", StringComparison.OrdinalIgnoreCase)));
        if (photo == null)
            return Array.Empty<ScanDetectedFieldDraft>();

        var slots = ScanOfficePictureExtractor.Extract(officeBytes);
        if (slots.Count == 0)
            return Array.Empty<ScanDetectedFieldDraft>();

        var token = photo.BuildWordToken(UserReportPlaceholderScope.Row);
        var label = string.IsNullOrWhiteSpace(photo.LabelEn) ? "Person photo" : photo.LabelEn;
        var drafts = new List<ScanDetectedFieldDraft>(slots.Count);
        foreach (var slot in slots)
        {
            if (existing.Any(d => d.SourceRegion is DocumentRegion.WordDrawing drawing
                && string.Equals(drawing.ParagraphAddress, slot.ParagraphAddress, StringComparison.Ordinal)
                && drawing.DrawingIndex == slot.DrawingIndex))
                continue;

            drafts.Add(new ScanDetectedFieldDraft
            {
                FieldId = Guid.NewGuid().ToString("N"),
                PageIndex = 0,
                LabelText = label,
                ProposedToken = token,
                Confidence = ScanFieldConfidence.High,
                Scope = ScanFieldScope.Row,
                Box = ScanBoundingBox.FullPage,
                SourceRegion = slot,
            });
        }

        return drafts;
    }

    private static ScanFieldScope ScopeFromCodes(
        IReadOnlyList<string> codes,
        ApplicationProfilePlaceholderSet placeholderSet)
    {
        foreach (var code in codes)
        {
            var entry = placeholderSet.Allowed.FirstOrDefault(e =>
                string.Equals(e.ShortCode, code, StringComparison.OrdinalIgnoreCase));
            if (entry?.Scope == UserReportPlaceholderScope.Row)
                return ScanFieldScope.Row;
        }

        return ScanFieldScope.Header;
    }

    private static Dictionary<int, string?> BuildNearbyLabels(
        byte[]? officeBytes,
        ScanSourceKind sourceKind,
        IReadOnlyList<ScanOfficeYellowSpan> yellows)
    {
        var map = new Dictionary<int, string?>();
        if (officeBytes is not { Length: > 64 } || yellows.Count == 0)
            return map;

        var probes = new List<ScanDetectedFieldDraft>(yellows.Count);
        for (var i = 0; i < yellows.Count; i++)
        {
            probes.Add(new ScanDetectedFieldDraft
            {
                FieldId = "ctx" + i.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Box = ScanBoundingBox.FullPage,
                PageIndex = yellows[i].PageIndex,
                LabelText = yellows[i].Text,
                SourceRegion = yellows[i].Region,
            });
        }

        var contexts = ScanYellowMarkContextBuilder.Build(officeBytes, sourceKind, probes);
        for (var i = 0; i < probes.Count; i++)
        {
            if (!contexts.TryGetValue(probes[i].FieldId, out var context))
                continue;
            var nearby = JoinNearby(context.PrintedLabel, context.FollowingCaption);
            if (!string.IsNullOrWhiteSpace(nearby))
                map[i] = nearby;
        }

        return map;
    }

    private static string? JoinNearby(string? printedLabel, string? followingCaption)
    {
        if (string.IsNullOrWhiteSpace(printedLabel))
            return string.IsNullOrWhiteSpace(followingCaption) ? null : followingCaption.Trim();
        if (string.IsNullOrWhiteSpace(followingCaption)
            || followingCaption.Contains(printedLabel, StringComparison.Ordinal))
            return printedLabel;
        return printedLabel.Trim() + " " + followingCaption.Trim();
    }

    private static IReadOnlyList<ScanDetectedFieldDraft> AttachNearby(
        IReadOnlyList<ScanDetectedFieldDraft> drafts,
        string? nearbyLabel)
    {
        if (string.IsNullOrWhiteSpace(nearbyLabel) || drafts.Count == 0)
            return drafts;

        return drafts.Select(d => new ScanDetectedFieldDraft
        {
            FieldId = d.FieldId,
            PageIndex = d.PageIndex,
            LabelText = d.LabelText,
            ProposedToken = d.ProposedToken,
            Confidence = d.Confidence,
            Scope = d.Scope,
            Box = d.Box,
            SourceRegion = d.SourceRegion,
            ColumnHeader = d.ColumnHeader,
            NearbyLabel = nearbyLabel,
            Alternatives = d.Alternatives,
        }).ToList();
    }

    private static IReadOnlyList<ScanDetectedFieldDraft> TryCloneDuplicateLabel(
        ScanOfficeYellowSpan yellow,
        IReadOnlyList<ScanDetectedFieldDraft> drafts)
    {
        var key = TemplateTextNormalizer.NormalizeIdentifier(yellow.Text);
        if (key.Length < TemplateTextNormalizer.MinimumMatchLength)
            return Array.Empty<ScanDetectedFieldDraft>();

        var prior = drafts.FirstOrDefault(d =>
            !string.IsNullOrWhiteSpace(d.ProposedToken)
            && string.Equals(
                TemplateTextNormalizer.NormalizeIdentifier(d.LabelText),
                key,
                StringComparison.Ordinal));
        if (prior == null)
            return Array.Empty<ScanDetectedFieldDraft>();

        return
        [
            new ScanDetectedFieldDraft
            {
                FieldId = Guid.NewGuid().ToString("N"),
                PageIndex = yellow.PageIndex,
                LabelText = yellow.Text,
                ProposedToken = prior.ProposedToken,
                Confidence = prior.Confidence,
                Scope = prior.Scope,
                Box = ScanBoundingBox.FullPage,
                SourceRegion = yellow.Region,
                Alternatives = prior.Alternatives,
            },
        ];
    }
}
