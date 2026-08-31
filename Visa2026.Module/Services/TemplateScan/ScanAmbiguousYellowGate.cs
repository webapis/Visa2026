#nullable enable

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>Decides when local rules should escalate a yellow mark to Azure for placeholder guessing.</summary>
public static class ScanAmbiguousYellowGate
{
    public static bool NeedsAiRefinement(ScanDetectedFieldDraft draft, TemplateAiScanOptions options)
    {
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(options);

        if (draft.ProposedToken == null)
            return true;

        if (draft.Confidence == ScanFieldConfidence.Low)
            return true;

        var ranked = draft.Alternatives
            .OrderByDescending(static a => a.ScorePercent)
            .ToList();

        if (ranked.Count == 0)
            return draft.Confidence != ScanFieldConfidence.High;

        var top = ranked[0].ScorePercent;
        if (top < options.AmbiguousYellowMinConfidencePercent)
            return true;

        if (ranked.Count > 1
            && top - ranked[1].ScorePercent < options.AmbiguousYellowScoreGapPercent)
            return true;

        return false;
    }

    public static IReadOnlyList<ScanDetectedFieldDraft> SelectForRefinement(
        IReadOnlyList<ScanDetectedFieldDraft> fields,
        TemplateAiScanOptions options)
    {
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(options);

        return fields
            .Where(f => NeedsAiRefinement(f, options))
            .Take(Math.Clamp(options.AmbiguousYellowMaxMarksPerCall, 1, 40))
            .ToList();
    }
}
