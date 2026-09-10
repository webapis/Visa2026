#nullable enable

using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Rebuilds the Review list from an approved snapshot (or mapped-file tokens) without
/// re-guessing yellow sample text. Unmapped leftover yellows stay empty until Remap unmarked.
/// </summary>
public static class ScanApprovedReviewRestorer
{
    public static ScanFieldPlan Restore(
        ApplicationProfilePlaceholderSet placeholderSet,
        IReadOnlyList<ScanOfficeYellowSpan> yellows,
        ScanApprovedReviewSnapshot? snapshot,
        IReadOnlyList<ScanOfficeYellowSpan>? mappedTokenSpans = null,
        byte[]? officeBytes = null,
        ScanSourceKind sourceKind = ScanSourceKind.Word)
    {
        ArgumentNullException.ThrowIfNull(placeholderSet);
        yellows ??= Array.Empty<ScanOfficeYellowSpan>();

        var liveYellows = ScanOfficePictureExtractor.MergeInto(yellows, officeBytes, sourceKind);
        var fields = snapshot is { Fields.Count: > 0 }
            ? RestoreFromSnapshot(placeholderSet, liveYellows, snapshot)
            : OverlayMappedTokens(placeholderSet, liveYellows, mappedTokenSpans);
        fields = ReanchorPhotos(placeholderSet, fields, liveYellows);

        return new ScanFieldPlan
        {
            PlaceholderSet = placeholderSet,
            ScanKind = ScanKind.FilledSample,
            Fields = fields,
            StaticRegions = Array.Empty<ScanStaticRegion>(),
            Gaps = Array.Empty<ScanGap>(),
            PendingQuestions = Array.Empty<ScanClarificationPrompt>(),
            Rationale = ScanApprovedReviewSnapshot.FieldPlanSource,
            Source = ScanApprovedReviewSnapshot.FieldPlanSource,
            YellowHighlightCount = yellows.Count(static y => y.Region is not DocumentRegion.WordDrawing),
        };
    }

    private static IReadOnlyList<ScanDetectedField> RestoreFromSnapshot(
        ApplicationProfilePlaceholderSet placeholderSet,
        IReadOnlyList<ScanOfficeYellowSpan> yellows,
        ScanApprovedReviewSnapshot snapshot)
    {
        var pins = new Dictionary<string, ScanApprovedReviewFieldDto>(StringComparer.Ordinal);
        foreach (var dto in snapshot.Fields)
        {
            var key = ScanDocumentRegionKey.ForRegion(dto.Region?.ToRegion())
                ?? TextKey(dto.PageIndex, dto.LabelText);
            pins.TryAdd(key, dto);
        }

        var usedPinKeys = new HashSet<string>(StringComparer.Ordinal);
        var matched = new List<ScanDetectedField>(yellows.Count);
        var unmatchedYellows = new List<ScanOfficeYellowSpan>();
        foreach (var yellow in yellows)
        {
            var key = ScanDocumentRegionKey.ForRegion(yellow.Region)
                ?? TextKey(yellow.PageIndex, yellow.Text);
            if (pins.TryGetValue(key, out var dto))
            {
                usedPinKeys.Add(key);
                matched.Add(ToField(placeholderSet, dto, yellow));
                continue;
            }

            unmatchedYellows.Add(yellow);
        }

        var unused = snapshot.Fields
            .Where(dto =>
            {
                var key = ScanDocumentRegionKey.ForRegion(dto.Region?.ToRegion())
                    ?? TextKey(dto.PageIndex, dto.LabelText);
                return usedPinKeys.Add(key);
            })
            .ToList();

        var unusedByGroup = unused
            .GroupBy(dto => ScanDocumentRegionKey.AlignGroupKey(dto.Region?.ToRegion()), StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(d => ScanDocumentRegionKey.OrderInGroup(d.Region?.ToRegion())).ToList(),
                StringComparer.Ordinal);
        var groupOrdinal = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var yellow in unmatchedYellows)
        {
            var group = ScanDocumentRegionKey.AlignGroupKey(yellow.Region);
            if (unusedByGroup.TryGetValue(group, out var bucket))
            {
                groupOrdinal.TryGetValue(group, out var ordinal);
                if (ordinal < bucket.Count)
                {
                    groupOrdinal[group] = ordinal + 1;
                    matched.Add(ToField(placeholderSet, bucket[ordinal], yellow));
                    continue;
                }
            }

            matched.Add(UnmappedYellow(yellow));
        }

        foreach (var pair in unusedByGroup)
        {
            groupOrdinal.TryGetValue(pair.Key, out var usedCount);
            for (var i = usedCount; i < pair.Value.Count; i++)
            {
                var dto = pair.Value[i];
                if (dto.Region?.ToRegion() is DocumentRegion.WordDrawing)
                    matched.Add(ToField(placeholderSet, dto, yellow: null));
            }
        }

        return SplitCompoundsOntoFollowingUnmapped(placeholderSet, matched);
    }

    /// <summary>
    /// Photos are drawings, not yellow text. Snapshot / mapped overlay often stores
    /// <c>{{IMAGE:PPH}}</c> as a WordSpan (or a stale drawing address). Re-pin those
    /// tokens onto live body portraits so Review and Generate keep the mapping.
    /// </summary>
    private static IReadOnlyList<ScanDetectedField> ReanchorPhotos(
        ApplicationProfilePlaceholderSet placeholderSet,
        IReadOnlyList<ScanDetectedField> fields,
        IReadOnlyList<ScanOfficeYellowSpan> yellows)
    {
        var drawings = yellows
            .Where(static y => y.Region is DocumentRegion.WordDrawing)
            .OrderBy(static y => ((DocumentRegion.WordDrawing)y.Region).ParagraphAddress, StringComparer.Ordinal)
            .ThenBy(static y => ((DocumentRegion.WordDrawing)y.Region).DrawingIndex)
            .ToList();

        if (drawings.Count == 0)
        {
            return fields
                .Where(static f => f.SourceRegion is not DocumentRegion.WordDrawing)
                .ToList();
        }

        var liveKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var drawing in drawings)
        {
            var key = ScanDocumentRegionKey.ForRegion(drawing.Region);
            if (key != null)
                liveKeys.Add(key);
        }

        var kept = new List<ScanDetectedField>(fields.Count + drawings.Count);
        var pendingImages = new Queue<ScanDetectedField>();
        foreach (var field in fields)
        {
            if (field.SourceRegion is DocumentRegion.WordDrawing drawing)
            {
                var key = ScanDocumentRegionKey.ForRegion(drawing);
                if (key != null && liveKeys.Contains(key))
                {
                    kept.Add(field);
                    continue;
                }

                if (ScanOfficePictureExtractor.IsPersonPhotoToken(field.ProposedToken))
                    pendingImages.Enqueue(field);
                continue;
            }

            if (ScanOfficePictureExtractor.IsPersonPhotoToken(field.ProposedToken))
            {
                pendingImages.Enqueue(field);
                if (!LooksLikeTokenLabel(field.LabelText))
                    kept.Add(WithoutToken(field));
                continue;
            }

            kept.Add(field);
        }

        TryGetPersonPhotoToken(placeholderSet, out var defaultToken);
        for (var i = 0; i < kept.Count; i++)
        {
            if (kept[i].SourceRegion is not DocumentRegion.WordDrawing)
                continue;
            if (ScanOfficePictureExtractor.IsPersonPhotoToken(kept[i].ProposedToken))
                continue;

            var source = pendingImages.Count > 0 ? pendingImages.Dequeue() : null;
            var token = source?.ProposedToken ?? defaultToken;
            if (string.IsNullOrWhiteSpace(token))
                continue;

            var slot = drawings.First(d =>
                string.Equals(
                    ScanDocumentRegionKey.ForRegion(d.Region),
                    ScanDocumentRegionKey.ForRegion(kept[i].SourceRegion),
                    StringComparison.Ordinal));
            kept[i] = PhotoField(placeholderSet, source, slot, token);
        }

        var claimed = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in kept)
        {
            var key = ScanDocumentRegionKey.ForRegion(field.SourceRegion);
            if (field.SourceRegion is DocumentRegion.WordDrawing && key != null)
                claimed.Add(key);
        }

        foreach (var drawing in drawings)
        {
            var key = ScanDocumentRegionKey.ForRegion(drawing.Region);
            if (key != null && !claimed.Add(key))
                continue;

            var source = pendingImages.Count > 0 ? pendingImages.Dequeue() : null;
            var token = source?.ProposedToken ?? defaultToken;
            if (string.IsNullOrWhiteSpace(token))
                continue;

            kept.Add(PhotoField(placeholderSet, source, drawing, token));
        }

        return kept;
    }

    private static ScanDetectedField PhotoField(
        ApplicationProfilePlaceholderSet placeholderSet,
        ScanDetectedField? source,
        ScanOfficeYellowSpan drawing,
        string token)
    {
        var rewritten = ScanLibraryTokenRewriter.Rewrite(token, placeholderSet);
        return new ScanDetectedField
        {
            FieldId = string.IsNullOrWhiteSpace(source?.FieldId)
                ? Guid.NewGuid().ToString("N")
                : source!.FieldId,
            Box = ScanBoundingBox.FullPage,
            PageIndex = drawing.PageIndex,
            LabelText = drawing.Text,
            ProposedToken = rewritten,
            Confidence = ScanFieldConfidence.High,
            Scope = ScanFieldScope.Row,
            SourceRegion = drawing.Region,
            HiddenPartIndexes = Array.Empty<int>(),
            IsLocked = true,
        };
    }

    private static ScanDetectedField WithoutToken(ScanDetectedField field) =>
        new()
        {
            FieldId = Guid.NewGuid().ToString("N"),
            Box = field.Box,
            PageIndex = field.PageIndex,
            LabelText = field.LabelText,
            ProposedToken = null,
            Confidence = ScanFieldConfidence.Medium,
            Scope = field.Scope,
            SourceRegion = field.SourceRegion,
            HiddenPartIndexes = Array.Empty<int>(),
            IsLocked = false,
        };

    private static bool LooksLikeTokenLabel(string? label) =>
        !string.IsNullOrWhiteSpace(label)
        && label.Contains("{{", StringComparison.Ordinal);

    private static bool TryGetPersonPhotoToken(
        ApplicationProfilePlaceholderSet placeholderSet,
        out string token)
    {
        token = string.Empty;
        var photo = placeholderSet.Allowed.FirstOrDefault(static e =>
            e.IsImage
            && (string.Equals(e.ShortCode, "PPH", StringComparison.OrdinalIgnoreCase)
                || string.Equals(e.CanonicalPath, "Person_Photo", StringComparison.OrdinalIgnoreCase)));
        if (photo == null)
            return false;

        token = photo.BuildWordToken(UserReportPlaceholderScope.Row);
        return true;
    }

    private static IReadOnlyList<ScanDetectedField> OverlayMappedTokens(
        ApplicationProfilePlaceholderSet placeholderSet,
        IReadOnlyList<ScanOfficeYellowSpan> yellows,
        IReadOnlyList<ScanOfficeYellowSpan>? mappedTokenSpans)
    {
        var tokens = ExpandMappedTokens(mappedTokenSpans ?? Array.Empty<ScanOfficeYellowSpan>(), placeholderSet);
        if (yellows.Count == 0)
        {
            return tokens
                .Select(span => FromTokenSpan(placeholderSet, span))
                .ToList();
        }

        var queues = tokens
            .Select((span, index) => (span, index))
            .GroupBy(x => ScanDocumentRegionKey.AlignGroupKey(x.span.Region), StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => new Queue<ScanOfficeYellowSpan>(
                    g.OrderBy(x => ScanDocumentRegionKey.OrderInGroup(x.span.Region))
                        .ThenBy(x => x.index)
                        .Select(x => x.span)),
                StringComparer.Ordinal);

        var yellowCountByGroup = yellows
            .GroupBy(static y => ScanDocumentRegionKey.AlignGroupKey(y.Region), StringComparer.Ordinal)
            .ToDictionary(static g => g.Key, static g => g.Count(), StringComparer.Ordinal);
        var assignedInGroup = new Dictionary<string, int>(StringComparer.Ordinal);
        var fields = new List<ScanDetectedField>(yellows.Count);
        foreach (var yellow in yellows)
        {
            var group = ScanDocumentRegionKey.AlignGroupKey(yellow.Region);
            assignedInGroup.TryGetValue(group, out var assigned);
            assignedInGroup[group] = assigned + 1;
            if (!queues.TryGetValue(group, out var queue) || queue.Count == 0)
            {
                fields.Add(UnmappedYellow(yellow));
                continue;
            }

            yellowCountByGroup.TryGetValue(group, out var yellowCount);
            var remainingYellows = yellowCount - assigned;
            if (remainingYellows == 1 && queue.Count > 1)
            {
                var joined = string.Join(", ", queue.Select(static s => s.Text.Trim()));
                queue.Clear();
                fields.Add(FromOverlay(placeholderSet, yellow, joined));
                continue;
            }

            fields.Add(FromOverlay(placeholderSet, yellow, queue.Dequeue().Text));
        }

        return fields;
    }

    private static List<ScanDetectedField> SplitCompoundsOntoFollowingUnmapped(
        ApplicationProfilePlaceholderSet placeholderSet,
        List<ScanDetectedField> fields)
    {
        for (var i = 0; i < fields.Count; i++)
        {
            var field = fields[i];
            var codes = TemplateTokenSyntax.GetShortCodes(field.ProposedToken);
            if (codes.Count <= 1)
                continue;

            var group = ScanDocumentRegionKey.AlignGroupKey(field.SourceRegion);
            var followers = new List<int>();
            for (var j = i + 1; j < fields.Count; j++)
            {
                if (!string.Equals(
                        ScanDocumentRegionKey.AlignGroupKey(fields[j].SourceRegion),
                        group,
                        StringComparison.Ordinal))
                    break;
                if (!string.IsNullOrWhiteSpace(fields[j].ProposedToken))
                    break;
                followers.Add(j);
            }

            var take = Math.Min(codes.Count, followers.Count + 1);
            if (take <= 1)
                continue;

            fields[i] = CopyMapped(placeholderSet, field, WrapCode(placeholderSet, codes[0]), keepId: true);
            for (var k = 1; k < take; k++)
            {
                var dest = fields[followers[k - 1]];
                fields[followers[k - 1]] = CopyMapped(
                    placeholderSet,
                    dest,
                    WrapCode(placeholderSet, codes[k]),
                    keepId: false,
                    locked: field.IsLocked);
            }
        }

        return fields;
    }

    private static ScanDetectedField CopyMapped(
        ApplicationProfilePlaceholderSet placeholderSet,
        ScanDetectedField template,
        string tokenText,
        bool keepId,
        bool? locked = null)
    {
        var token = ScanLibraryTokenRewriter.Rewrite(tokenText.Trim(), placeholderSet);
        var codes = TemplateTokenSyntax.GetShortCodes(token);
        return new ScanDetectedField
        {
            FieldId = keepId ? template.FieldId : Guid.NewGuid().ToString("N"),
            Box = template.Box,
            PageIndex = template.PageIndex,
            LabelText = template.LabelText,
            ProposedToken = token,
            Confidence = ScanFieldConfidence.High,
            Scope = ScopeFromCodes(codes, placeholderSet),
            SourceRegion = template.SourceRegion,
            HiddenPartIndexes = Array.Empty<int>(),
            IsLocked = locked ?? template.IsLocked,
        };
    }

    private static IReadOnlyList<ScanOfficeYellowSpan> ExpandMappedTokens(
        IReadOnlyList<ScanOfficeYellowSpan> mappedSpans,
        ApplicationProfilePlaceholderSet placeholderSet)
    {
        var expanded = new List<ScanOfficeYellowSpan>();
        foreach (var span in mappedSpans)
        {
            var codes = TemplateTokenSyntax.GetShortCodes(span.Text);
            if (codes.Count <= 1)
            {
                expanded.Add(span);
                continue;
            }

            for (var i = 0; i < codes.Count; i++)
            {
                DocumentRegion region = span.Region is DocumentRegion.WordSpan word
                    ? word with { Start = word.Start + i, Length = Math.Max(1, word.Length / codes.Count) }
                    : span.Region;
                expanded.Add(new ScanOfficeYellowSpan
                {
                    Text = WrapCode(placeholderSet, codes[i]),
                    Region = region,
                    PageIndex = span.PageIndex,
                });
            }
        }

        return expanded;
    }

    private static string WrapCode(ApplicationProfilePlaceholderSet placeholderSet, string code)
    {
        var entry = placeholderSet.Allowed.FirstOrDefault(e =>
            string.Equals(e.ShortCode, code, StringComparison.OrdinalIgnoreCase));
        if (entry == null)
            return "{{." + code + "}}";

        var usage = entry.Scope == UserReportPlaceholderScope.Row
            ? UserReportPlaceholderScope.Row
            : UserReportPlaceholderScope.Header;
        return entry.BuildWordToken(usage);
    }

    private static ScanDetectedField ToField(
        ApplicationProfilePlaceholderSet placeholderSet,
        ScanApprovedReviewFieldDto dto,
        ScanOfficeYellowSpan? yellow)
    {
        var token = string.IsNullOrWhiteSpace(dto.ProposedToken)
            ? null
            : ScanLibraryTokenRewriter.Rewrite(dto.ProposedToken, placeholderSet);
        var mapped = !string.IsNullOrWhiteSpace(token);
        return new ScanDetectedField
        {
            FieldId = string.IsNullOrWhiteSpace(dto.FieldId) ? Guid.NewGuid().ToString("N") : dto.FieldId,
            Box = ScanBoundingBox.FullPage,
            PageIndex = yellow?.PageIndex ?? dto.PageIndex,
            LabelText = yellow?.Text ?? dto.LabelText,
            ProposedToken = token,
            Confidence = mapped ? dto.Confidence : ScanFieldConfidence.Medium,
            Scope = dto.Scope,
            SourceRegion = yellow?.Region ?? dto.Region?.ToRegion(),
            HiddenPartIndexes = dto.HiddenPartIndexes ?? Array.Empty<int>(),
            IsLocked = mapped && dto.IsLocked,
        };
    }

    private static ScanDetectedField FromOverlay(
        ApplicationProfilePlaceholderSet placeholderSet,
        ScanOfficeYellowSpan yellow,
        string tokenText)
    {
        var token = ScanLibraryTokenRewriter.Rewrite(tokenText.Trim(), placeholderSet);
        var codes = TemplateTokenSyntax.GetShortCodes(token);
        return new ScanDetectedField
        {
            FieldId = Guid.NewGuid().ToString("N"),
            Box = ScanBoundingBox.FullPage,
            PageIndex = yellow.PageIndex,
            LabelText = yellow.Text,
            ProposedToken = token,
            Confidence = ScanFieldConfidence.High,
            Scope = ScopeFromCodes(codes, placeholderSet),
            SourceRegion = yellow.Region,
            IsLocked = true,
        };
    }

    private static ScanDetectedField FromTokenSpan(
        ApplicationProfilePlaceholderSet placeholderSet,
        ScanOfficeYellowSpan span)
    {
        var token = ScanLibraryTokenRewriter.Rewrite(span.Text.Trim(), placeholderSet);
        var codes = TemplateTokenSyntax.GetShortCodes(token);
        return new ScanDetectedField
        {
            FieldId = Guid.NewGuid().ToString("N"),
            Box = ScanBoundingBox.FullPage,
            PageIndex = span.PageIndex,
            LabelText = span.Text.Trim(),
            ProposedToken = token,
            Confidence = ScanFieldConfidence.High,
            Scope = ScopeFromCodes(codes, placeholderSet),
            SourceRegion = span.Region,
            IsLocked = true,
        };
    }

    private static ScanDetectedField UnmappedYellow(ScanOfficeYellowSpan yellow) =>
        new()
        {
            FieldId = Guid.NewGuid().ToString("N"),
            Box = ScanBoundingBox.FullPage,
            PageIndex = yellow.PageIndex,
            LabelText = yellow.Text,
            ProposedToken = null,
            Confidence = ScanFieldConfidence.Medium,
            Scope = ScanFieldScope.Header,
            SourceRegion = yellow.Region,
            IsLocked = false,
        };

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

    private static string TextKey(int pageIndex, string? label) =>
        "t|" + pageIndex.ToString(System.Globalization.CultureInfo.InvariantCulture)
        + "|" + TemplateTextNormalizer.NormalizeIdentifier(label ?? string.Empty);
}