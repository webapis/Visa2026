#nullable enable

using Visa2026.Module.Services.TemplateScan;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanAmbiguousYellowGateTests
{
    private static TemplateAiScanOptions Options() => new()
    {
        AmbiguousYellowMinConfidencePercent = 80,
        AmbiguousYellowScoreGapPercent = 15,
    };

    private static ScanDetectedFieldDraft Draft(
        string? token,
        ScanFieldConfidence confidence,
        params ScanTokenAlternative[] alternatives) =>
        new()
        {
            FieldId = "f1",
            Box = ScanBoundingBox.FullPage,
            PageIndex = 0,
            LabelText = "sample",
            ProposedToken = token,
            Confidence = confidence,
            Scope = ScanFieldScope.Row,
            Alternatives = alternatives,
        };

    [Fact]
    public void NeedsAiRefinement_true_when_unmapped()
    {
        Assert.True(ScanAmbiguousYellowGate.NeedsAiRefinement(
            Draft(null, ScanFieldConfidence.Medium),
            Options()));
    }

    [Fact]
    public void NeedsAiRefinement_true_when_low_top_score()
    {
        var draft = Draft(
            "{{.PLN}}",
            ScanFieldConfidence.Medium,
            new ScanTokenAlternative("{{.PLN}}", "PLN", 65, "header"),
            new ScanTokenAlternative("{{.PFNM}}", "PFNM", 60, "shape"));

        Assert.True(ScanAmbiguousYellowGate.NeedsAiRefinement(draft, Options()));
    }

    [Fact]
    public void NeedsAiRefinement_true_when_close_candidates()
    {
        var draft = Draft(
            "{{.PLN}}",
            ScanFieldConfidence.High,
            new ScanTokenAlternative("{{.PLN}}", "PLN", 85, "header"),
            new ScanTokenAlternative("{{.PFNM}}", "PFNM", 78, "shape"));

        Assert.True(ScanAmbiguousYellowGate.NeedsAiRefinement(draft, Options()));
    }

    [Fact]
    public void NeedsAiRefinement_false_when_high_confident_winner()
    {
        var draft = Draft(
            "{{.PLN}}",
            ScanFieldConfidence.High,
            new ScanTokenAlternative("{{.PLN}}", "PLN", 92, "header"),
            new ScanTokenAlternative("{{.PFNM}}", "PFNM", 40, "shape"));

        Assert.False(ScanAmbiguousYellowGate.NeedsAiRefinement(draft, Options()));
    }
}
