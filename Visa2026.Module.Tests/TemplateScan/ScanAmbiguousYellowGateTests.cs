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
        bool locked = false,
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
            IsLocked = locked,
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
            alternatives:
            [
                new ScanTokenAlternative("{{.PLN}}", "PLN", 65, "header"),
                new ScanTokenAlternative("{{.PFNM}}", "PFNM", 60, "shape"),
            ]);

        Assert.True(ScanAmbiguousYellowGate.NeedsAiRefinement(draft, Options()));
    }

    [Fact]
    public void NeedsAiRefinement_true_when_close_candidates()
    {
        var draft = Draft(
            "{{.PLN}}",
            ScanFieldConfidence.High,
            alternatives:
            [
                new ScanTokenAlternative("{{.PLN}}", "PLN", 85, "header"),
                new ScanTokenAlternative("{{.PFNM}}", "PFNM", 78, "shape"),
            ]);

        Assert.True(ScanAmbiguousYellowGate.NeedsAiRefinement(draft, Options()));
    }

    [Fact]
    public void NeedsAiRefinement_false_when_high_confident_winner()
    {
        var draft = Draft(
            "{{.PLN}}",
            ScanFieldConfidence.High,
            alternatives:
            [
                new ScanTokenAlternative("{{.PLN}}", "PLN", 92, "header"),
                new ScanTokenAlternative("{{.PFNM}}", "PFNM", 40, "shape"),
            ]);

        Assert.False(ScanAmbiguousYellowGate.NeedsAiRefinement(draft, Options()));
    }

    [Fact]
    public void NeedsAiRefinement_false_when_column_header_BTAD_despite_close_ADRS()
    {
        var draft = Draft(
            "{{.BTAD}}",
            ScanFieldConfidence.High,
            alternatives:
            [
                new ScanTokenAlternative("{{.BTAD}}", "BTAD", 100, "Column header"),
                new ScanTokenAlternative("{{.ADRS}}", "ADRS", 91, "Left field label"),
                new ScanTokenAlternative("{{.PFAC}}", "PFAC", 91, "Three-letter country code + column header + surround"),
            ]);

        Assert.False(ScanAmbiguousYellowGate.NeedsAiRefinement(draft, Options()));
    }

    [Fact]
    public void NeedsAiRefinement_false_when_locked()
    {
        Assert.False(ScanAmbiguousYellowGate.NeedsAiRefinement(
            Draft(null, ScanFieldConfidence.Low, locked: true),
            Options()));
    }

    [Fact]
    public void NeedsAiRefinement_true_when_incorrect_hint_even_if_high()
    {
        var draft = Draft(
            "{{.PLN}}",
            ScanFieldConfidence.High,
            alternatives:
            [
                new ScanTokenAlternative("{{.PLN}}", "PLN", 92, "header"),
                new ScanTokenAlternative("{{.PFNM}}", "PFNM", 40, "shape"),
            ]);

        Assert.True(ScanAmbiguousYellowGate.NeedsAiRefinement(
            draft,
            Options(),
            new ScanRemapOfficerHints(false, true)));
    }

    [Fact]
    public void NeedsAiRefinement_false_when_locked_despite_incorrect_hint()
    {
        Assert.False(ScanAmbiguousYellowGate.NeedsAiRefinement(
            Draft("{{.PLN}}", ScanFieldConfidence.Low, locked: true),
            Options(),
            new ScanRemapOfficerHints(true, true)));
    }

    [Fact]
    public void SelectForRefinement_unidentified_puts_unmapped_first()
    {
        var mapped = Draft(
            "{{.PLN}}",
            ScanFieldConfidence.Medium,
            alternatives: [new ScanTokenAlternative("{{.PLN}}", "PLN", 60, "header")]);
        mapped = new ScanDetectedFieldDraft
        {
            FieldId = "mapped",
            Box = mapped.Box,
            PageIndex = 0,
            LabelText = "Erol",
            ProposedToken = mapped.ProposedToken,
            Confidence = mapped.Confidence,
            Scope = mapped.Scope,
            Alternatives = mapped.Alternatives,
        };
        var unmapped = new ScanDetectedFieldDraft
        {
            FieldId = "gap",
            Box = ScanBoundingBox.FullPage,
            PageIndex = 0,
            LabelText = "leftover yellow",
            ProposedToken = null,
            Confidence = ScanFieldConfidence.High,
            Scope = ScanFieldScope.Header,
        };

        var selected = ScanAmbiguousYellowGate.SelectForRefinement(
            [mapped, unmapped],
            Options(),
            new ScanRemapOfficerHints(true, false));

        Assert.Equal("gap", selected[0].FieldId);
    }
}