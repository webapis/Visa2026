#nullable enable

using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanFieldPlanLockMergeTests
{
    [Fact]
    public void Apply_keeps_locked_token_when_rebuild_guesses_differently()
    {
        var set = HeaderSet();
        var region = new DocumentRegion.WordSpan("body/0", 0, 10);
        var locked = Field("old", "{{ds.TRCK}}", region, locked: true);
        var rebuilt = Field("new", "{{ds.ADAT}}", region);
        var plan = Plan(set, rebuilt);

        var merged = ScanFieldPlanLockMerge.Apply(plan, [locked]);

        var field = Assert.Single(merged.Fields);
        Assert.Equal("{{ds.TRCK}}", field.ProposedToken);
        Assert.True(field.IsLocked);
        Assert.Equal("new", field.FieldId);
    }

    [Fact]
    public void Apply_strips_locked_header_code_from_unlocked_field()
    {
        var set = HeaderSet();
        var lockedRegion = new DocumentRegion.WordSpan("body/0", 0, 8);
        var otherRegion = new DocumentRegion.WordSpan("body/1", 0, 8);
        var locked = Field("a", "{{ds.AFNUM}}", lockedRegion, locked: true);
        var stolen = Field("b", "{{ds.AFNUM}}", otherRegion);
        var plan = Plan(set, stolen);

        var merged = ScanFieldPlanLockMerge.Apply(plan, [locked]);

        Assert.Contains(merged.Fields, f => f.IsLocked && f.ProposedToken == "{{ds.AFNUM}}");
        var unlocked = Assert.Single(merged.Fields, f => !f.IsLocked);
        Assert.True(string.IsNullOrWhiteSpace(unlocked.ProposedToken));
    }

    [Fact]
    public void Apply_keeps_locked_field_missing_from_rebuild()
    {
        var set = HeaderSet();
        var region = new DocumentRegion.WordSpan("body/9", 0, 4);
        var locked = Field("kept", "{{ds.ADAT}}", region, locked: true);
        var other = Field("other", "{{ds.AFNUM}}", new DocumentRegion.WordSpan("body/1", 0, 4));
        var plan = Plan(set, other);

        var merged = ScanFieldPlanLockMerge.Apply(plan, [locked]);

        Assert.Equal(2, merged.Fields.Count);
        Assert.Contains(merged.Fields, f => f.IsLocked && f.ProposedToken == "{{ds.ADAT}}");
    }

    [Fact]
    public async Task BuildAsync_remap_keeps_officer_locked_placeholder()
    {
        var (_, _, ingest, fieldPlan) = ScanTestServiceFactory.Create();
        var set = BothSet();
        var bytes = ScanOfficeYellowExtractorTests.CreateWordFixture("№ 4/-434", "02.02.2009");
        var ingested = ingest.Ingest(new ScanNormalizeRequest
        {
            Content = bytes,
            FileName = "letter.docx",
        });

        var first = await fieldPlan.BuildAsync(new ScanFieldPlanBuildRequest
        {
            Ingest = ingested,
            PlaceholderSet = set,
            ScanKind = ScanKind.FilledSample,
        });

        var number = Assert.Single(first.Fields, f =>
            TemplateTokenSyntax.TryGetShortCode(f.ProposedToken, out var code)
            && code.Equals("AFNUM", StringComparison.OrdinalIgnoreCase));
        var overridden = ScanFieldPlanOfficerOverride.ApplyToken(first, number.FieldId, "ACRDT");
        var lockedPlan = ScanFieldPlanOfficerOverride.SetLocked(overridden, number.FieldId, true);
        var locked = Assert.Single(lockedPlan.Fields, f => f.IsLocked);

        var remapped = await fieldPlan.BuildAsync(new ScanFieldPlanBuildRequest
        {
            Ingest = ingested,
            PlaceholderSet = set,
            ScanKind = ScanKind.FilledSample,
            LockedFields = [locked],
        });

        var kept = Assert.Single(remapped.Fields, f =>
            f.SourceRegion is DocumentRegion.WordSpan span
            && span.ParagraphAddress == ((DocumentRegion.WordSpan)locked.SourceRegion!).ParagraphAddress);
        Assert.True(kept.IsLocked);
        Assert.True(TemplateTokenSyntax.TryGetShortCode(kept.ProposedToken, out var keptCode));
        Assert.Equal("ACRDT", keptCode);
    }

    [Fact]
    public async Task BuildOffCircuit_returns_same_locked_plan_as_direct_build()
    {
        var (_, _, ingest, fieldPlan) = ScanTestServiceFactory.Create();
        var set = BothSet();
        var bytes = ScanOfficeYellowExtractorTests.CreateWordFixture("№ 4/-434", "02.02.2009");
        var ingested = ingest.Ingest(new ScanNormalizeRequest
        {
            Content = bytes,
            FileName = "letter.docx",
        });
        var request = new ScanFieldPlanBuildRequest
        {
            Ingest = ingested,
            PlaceholderSet = set,
            ScanKind = ScanKind.FilledSample,
            SkipAiRefinement = true,
        };

        var direct = await fieldPlan.BuildAsync(request);
        var offCircuit = await ScanFieldPlanBuildOffCircuit.RunAsync(fieldPlan, request);

        Assert.Equal(direct.Fields.Count, offCircuit.Fields.Count);
        Assert.True(offCircuit.HasMappedFields);
    }

    private static ApplicationProfilePlaceholderSet HeaderSet() =>
        Set(ApplicationProfileTemplateDataScope.ApplicationHeader);

    private static ApplicationProfilePlaceholderSet BothSet() =>
        Set(ApplicationProfileTemplateDataScope.Both);

    private static ApplicationProfilePlaceholderSet Set(ApplicationProfileTemplateDataScope dataScope) =>
        new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile(),
                DataScope = dataScope,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });

    private static ScanDetectedField Field(string id, string token, DocumentRegion region, bool locked = false) =>
        new()
        {
            FieldId = id,
            Box = ScanBoundingBox.FullPage,
            PageIndex = 0,
            LabelText = "sample",
            ProposedToken = token,
            Confidence = ScanFieldConfidence.High,
            Scope = ScanFieldScope.Header,
            SourceRegion = region,
            IsLocked = locked,
        };

    private static ScanFieldPlan Plan(ApplicationProfilePlaceholderSet set, params ScanDetectedField[] fields) =>
        new()
        {
            PlaceholderSet = set,
            ScanKind = ScanKind.FilledSample,
            Fields = fields,
            StaticRegions = Array.Empty<ScanStaticRegion>(),
            Gaps = Array.Empty<ScanGap>(),
            PendingQuestions = Array.Empty<ScanClarificationPrompt>(),
            Source = "test",
            YellowHighlightCount = fields.Length,
        };
}