#nullable enable

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Officer rule: only yellow-highlighted spans may become placeholders.
/// Missing yellow, or yellow that maps to nothing in the library → Fail.
/// </summary>
public static class ScanYellowHighlightGate
{
    public static ScanSuitabilityReport Apply(
        ScanSuitabilityReport prior,
        int yellowHighlightCount,
        ScanFieldPlan fieldPlan)
    {
        ArgumentNullException.ThrowIfNull(prior);
        ArgumentNullException.ThrowIfNull(fieldPlan);

        var issues = prior.Issues.ToList();

        if (yellowHighlightCount <= 0)
        {
            if (string.Equals(fieldPlan.Source, ScanOfficeLibraryTokenExtractor.FieldPlanSource, StringComparison.Ordinal)
                && fieldPlan.HasMappedFields)
            {
                return prior;
            }

            issues.Add(new ScanSuitabilityIssue
            {
                Code = ScanSuitabilityIssueCode.NoYellowHighlights,
                Message =
                    "Highlight every value that should become a placeholder in yellow on the scan, then upload again. Non-highlighted text stays literal.",
            });

            return new ScanSuitabilityReport
            {
                Verdict = ScanSuitabilityVerdict.Fail,
                TextConfidence = prior.TextConfidence,
                Issues = issues,
            };
        }

        if (!fieldPlan.HasMappedFields)
        {
            issues.Add(new ScanSuitabilityIssue
            {
                Code = ScanSuitabilityIssueCode.YellowHighlightsUnmapped,
                Message =
                    "Yellow highlights were found, but none mapped to placeholders in the library. Adjust highlights or clarify labels, then try again.",
            });

            return new ScanSuitabilityReport
            {
                Verdict = ScanSuitabilityVerdict.Fail,
                TextConfidence = prior.TextConfidence,
                Issues = issues,
            };
        }

        if (fieldPlan.Gaps.Count > 0 && prior.Verdict == ScanSuitabilityVerdict.Pass)
        {
            // Warn only — do not use YellowHighlightsUnmapped (that code is Fail in suitability).
            issues.Add(new ScanSuitabilityIssue
            {
                Code = ScanSuitabilityIssueCode.TextConfidenceLow,
                Message =
                    $"{fieldPlan.Gaps.Count} yellow highlight(s) could not be matched to the placeholder library — review gaps before generate.",
            });

            return new ScanSuitabilityReport
            {
                Verdict = ScanSuitabilityVerdict.Warn,
                TextConfidence = prior.TextConfidence,
                Issues = issues,
            };
        }

        return prior.Issues.Count == issues.Count
            ? prior
            : new ScanSuitabilityReport
            {
                Verdict = prior.Verdict,
                TextConfidence = prior.TextConfidence,
                Issues = issues,
            };
    }
}