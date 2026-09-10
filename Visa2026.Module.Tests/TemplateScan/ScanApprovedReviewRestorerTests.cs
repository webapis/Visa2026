#nullable enable

using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Visa2026.Module.Services.UserReports;
using Visa2026.Module.Tests.TemplateConvert;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanApprovedReviewRestorerTests
{
    [Fact]
    public void Snapshot_roundtrip_keeps_token_lock_and_region()
    {
        var set = PlaceholderSet();
        var region = new DocumentRegion.WordSpan("p1", 4, 8);
        var plan = Plan(set, new ScanDetectedField
        {
            FieldId = "f1",
            Box = ScanBoundingBox.FullPage,
            PageIndex = 0,
            LabelText = "19.02.2034",
            ProposedToken = "{{ds.CHPE}}",
            Confidence = ScanFieldConfidence.High,
            Scope = ScanFieldScope.Header,
            SourceRegion = region,
            IsLocked = true,
        });

        var json = ScanApprovedReviewSnapshot.Serialize(plan);
        var snapshot = ScanApprovedReviewSnapshot.Deserialize(json);
        Assert.NotNull(snapshot);
        Assert.Single(snapshot!.Fields);
        Assert.Equal("{{ds.CHPE}}", snapshot.Fields[0].ProposedToken);
        Assert.True(snapshot.Fields[0].IsLocked);
        Assert.Equal("WordSpan", snapshot.Fields[0].Region?.Kind);
        Assert.Equal("p1", snapshot.Fields[0].Region?.ParagraphAddress);
    }

    [Fact]
    public void Restore_uses_snapshot_tokens_and_does_not_guess_leftover_yellow()
    {
        var set = PlaceholderSet();
        var mapped = new DocumentRegion.WordSpan("p1", 0, 6);
        var leftover = new DocumentRegion.WordSpan("p2", 0, 4);
        var plan = Plan(set, new ScanDetectedField
        {
            FieldId = "kept",
            Box = ScanBoundingBox.FullPage,
            PageIndex = 0,
            LabelText = "Emre",
            ProposedToken = "{{.PFN}}",
            Confidence = ScanFieldConfidence.High,
            Scope = ScanFieldScope.Row,
            SourceRegion = mapped,
            IsLocked = true,
        });
        var snapshot = ScanApprovedReviewSnapshot.Deserialize(ScanApprovedReviewSnapshot.Serialize(plan));

        var yellows = new[]
        {
            new ScanOfficeYellowSpan { Text = "Emre", Region = mapped },
            new ScanOfficeYellowSpan { Text = "xxxx", Region = leftover },
        };

        var restored = ScanApprovedReviewRestorer.Restore(set, yellows, snapshot);
        Assert.Equal(ScanApprovedReviewSnapshot.FieldPlanSource, restored.Source);
        Assert.Equal(2, restored.Fields.Count);

        var kept = restored.Fields.Single(f => f.SourceRegion is DocumentRegion.WordSpan w && w.ParagraphAddress == "p1");
        Assert.Contains("PFN", kept.ProposedToken, StringComparison.OrdinalIgnoreCase);
        Assert.True(kept.IsLocked);

        var gap = restored.Fields.Single(f => f.SourceRegion is DocumentRegion.WordSpan w && w.ParagraphAddress == "p2");
        Assert.True(string.IsNullOrWhiteSpace(gap.ProposedToken));
        Assert.False(gap.IsLocked);
    }

    [Fact]
    public void Restore_without_snapshot_overlays_mapped_tokens_by_paragraph_order()
    {
        var set = PlaceholderSet();
        var yellows = new[]
        {
            new ScanOfficeYellowSpan { Text = "Emre", Region = new DocumentRegion.WordSpan("p1", 0, 4) },
        };
        var mapped = new[]
        {
            new ScanOfficeYellowSpan { Text = "{{.PFN}}", Region = new DocumentRegion.WordSpan("p1", 0, 8) },
        };

        var restored = ScanApprovedReviewRestorer.Restore(set, yellows, snapshot: null, mapped);
        var field = Assert.Single(restored.Fields);
        Assert.Contains("PFN", field.ProposedToken, StringComparison.OrdinalIgnoreCase);
        Assert.True(field.IsLocked);
    }

    [Fact]
    public void Overlay_splits_mapped_cluster_one_token_per_yellow_and_keeps_duplicate_PFN()
    {
        var set = PlaceholderSet();
        var yellows = new[]
        {
            new ScanOfficeYellowSpan { Text = "Yerkin Didem", Region = new DocumentRegion.WordSpan("p1", 0, 12) },
            new ScanOfficeYellowSpan { Text = "kop gezeklik", Region = new DocumentRegion.WordSpan("p10", 0, 12) },
            new ScanOfficeYellowSpan { Text = "Yerkin Didem", Region = new DocumentRegion.WordSpan("p10", 14, 12) },
            new ScanOfficeYellowSpan { Text = "U3655957", Region = new DocumentRegion.WordSpan("p10", 28, 8) },
        };
        var mapped = new[]
        {
            new ScanOfficeYellowSpan { Text = "{{.PFN}}", Region = new DocumentRegion.WordSpan("p1", 0, 8) },
            new ScanOfficeYellowSpan
            {
                Text = "{{.AVCAT}}, {{.PFN}}, {{.PPN}}",
                Region = new DocumentRegion.WordSpan("p10", 0, 30),
            },
        };

        var restored = ScanApprovedReviewRestorer.Restore(set, yellows, snapshot: null, mapped);
        Assert.Equal(4, restored.Fields.Count);
        Assert.All(restored.Fields, static f => Assert.True(f.IsLocked));
        Assert.All(restored.Fields, static f =>
            Assert.Equal(1, TemplateTokenSyntax.GetShortCodes(f.ProposedToken).Count));
        Assert.Contains("PFN", restored.Fields[0].ProposedToken, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("AVCAT", restored.Fields[1].ProposedToken, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PFN", restored.Fields[2].ProposedToken, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PPN", restored.Fields[3].ProposedToken, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Overlay_keeps_compound_when_the_mapped_cell_is_one_yellow()
    {
        var set = PlaceholderSet();
        var yellows = new[]
        {
            new ScanOfficeYellowSpan
            {
                Text = "kop gezeklik Yerkin Didem U3655957",
                Region = new DocumentRegion.WordSpan("p10", 0, 40),
            },
        };
        var mapped = new[]
        {
            new ScanOfficeYellowSpan
            {
                Text = "{{.AVCAT}}, {{.PFN}}, {{.PPN}}",
                Region = new DocumentRegion.WordSpan("p10", 0, 30),
            },
        };

        var restored = ScanApprovedReviewRestorer.Restore(set, yellows, snapshot: null, mapped);
        var field = Assert.Single(restored.Fields);
        Assert.Equal(3, TemplateTokenSyntax.GetShortCodes(field.ProposedToken).Count);
        Assert.Contains("AVCAT", field.ProposedToken, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PFN", field.ProposedToken, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PPN", field.ProposedToken, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Restore_zips_stale_snapshot_offsets_onto_live_yellows_in_the_same_paragraph()
    {
        var set = PlaceholderSet();
        var live = new DocumentRegion.WordSpan("p1", 2, 10);
        var stale = new DocumentRegion.WordSpan("p1", 0, 4);
        var plan = Plan(set, new ScanDetectedField
        {
            FieldId = "kept",
            Box = ScanBoundingBox.FullPage,
            PageIndex = 0,
            LabelText = "Emre",
            ProposedToken = "{{.PFN}}",
            Confidence = ScanFieldConfidence.High,
            Scope = ScanFieldScope.Row,
            SourceRegion = stale,
            IsLocked = true,
        });
        var snapshot = ScanApprovedReviewSnapshot.Deserialize(ScanApprovedReviewSnapshot.Serialize(plan));
        var yellows = new[]
        {
            new ScanOfficeYellowSpan { Text = "Yerkin Didem", Region = live },
        };

        var restored = ScanApprovedReviewRestorer.Restore(set, yellows, snapshot);
        var field = Assert.Single(restored.Fields);
        Assert.Contains("PFN", field.ProposedToken, StringComparison.OrdinalIgnoreCase);
        Assert.True(field.IsLocked);
        var region = Assert.IsType<DocumentRegion.WordSpan>(field.SourceRegion);
        Assert.Equal(2, region.Start);
        Assert.Equal(10, region.Length);
    }

    [Fact]
    public void Restore_splits_snapshot_compound_onto_sibling_yellows()
    {
        var set = PlaceholderSet();
        var plan = Plan(set, new ScanDetectedField
        {
            FieldId = "visa",
            Box = ScanBoundingBox.FullPage,
            PageIndex = 0,
            LabelText = "kop gezeklik Yerkin Didem U3655957",
            ProposedToken = "{{.AVCAT}}, {{.PFN}}, {{.PPN}}",
            Confidence = ScanFieldConfidence.High,
            Scope = ScanFieldScope.Row,
            SourceRegion = new DocumentRegion.WordSpan("p10", 0, 40),
            IsLocked = true,
        });
        var snapshot = ScanApprovedReviewSnapshot.Deserialize(ScanApprovedReviewSnapshot.Serialize(plan));
        var yellows = new[]
        {
            new ScanOfficeYellowSpan { Text = "kop gezeklik", Region = new DocumentRegion.WordSpan("p10", 0, 12) },
            new ScanOfficeYellowSpan { Text = "Yerkin Didem", Region = new DocumentRegion.WordSpan("p10", 14, 12) },
            new ScanOfficeYellowSpan { Text = "U3655957", Region = new DocumentRegion.WordSpan("p10", 28, 8) },
        };

        var restored = ScanApprovedReviewRestorer.Restore(set, yellows, snapshot);
        Assert.Equal(3, restored.Fields.Count);
        Assert.All(restored.Fields, static f => Assert.True(f.IsLocked));
        Assert.Contains("AVCAT", restored.Fields[0].ProposedToken, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PFN", restored.Fields[1].ProposedToken, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PPN", restored.Fields[2].ProposedToken, StringComparison.OrdinalIgnoreCase);
        Assert.All(restored.Fields, static f =>
            Assert.Equal(1, TemplateTokenSyntax.GetShortCodes(f.ProposedToken).Count));
    }

    [Fact]
    public void Restore_reattaches_image_wordspan_to_live_portrait()
    {
        var set = PlaceholderSet();
        var bytes = YellowWordWithPortrait();
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Word);
        var snapshot = SnapshotImageOnSpan(set);

        var restored = ScanApprovedReviewRestorer.Restore(
            set,
            yellows,
            snapshot,
            officeBytes: bytes,
            sourceKind: ScanSourceKind.Word);

        var photo = Assert.Single(restored.Fields, f => f.SourceRegion is DocumentRegion.WordDrawing);
        Assert.Equal("Person photo", photo.LabelText);
        Assert.Contains("IMAGE:PPH", photo.ProposedToken, StringComparison.OrdinalIgnoreCase);
        Assert.True(photo.IsLocked);
        Assert.Equal(yellows.Count, restored.YellowHighlightCount);
    }

    [Fact]
    public void Restore_maps_live_portrait_when_snapshot_has_no_photo_row()
    {
        var set = PlaceholderSet();
        var bytes = YellowWordWithPortrait();
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Word);
        var plan = Plan(set, new ScanDetectedField
        {
            FieldId = "name",
            Box = ScanBoundingBox.FullPage,
            PageIndex = 0,
            LabelText = "№ 4/-434",
            ProposedToken = "{{.AFNUM}}",
            Confidence = ScanFieldConfidence.High,
            Scope = ScanFieldScope.Header,
            SourceRegion = yellows[0].Region,
            IsLocked = true,
        });
        var snapshot = ScanApprovedReviewSnapshot.Deserialize(ScanApprovedReviewSnapshot.Serialize(plan));

        var restored = ScanApprovedReviewRestorer.Restore(
            set,
            yellows,
            snapshot,
            officeBytes: bytes,
            sourceKind: ScanSourceKind.Word);

        Assert.Contains(restored.Fields, f =>
            f.SourceRegion is DocumentRegion.WordDrawing
            && f.ProposedToken != null
            && f.ProposedToken.Contains("IMAGE:PPH", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Restore_stale_drawing_address_reattaches_to_live_portrait()
    {
        var set = PlaceholderSet();
        var bytes = YellowWordWithPortrait();
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Word);
        var live = Assert.Single(ScanOfficePictureExtractor.Extract(bytes));
        var stale = live with { ParagraphAddress = "body/99", DrawingIndex = 7 };
        var plan = Plan(set, new ScanDetectedField
        {
            FieldId = "photo",
            Box = ScanBoundingBox.FullPage,
            PageIndex = 0,
            LabelText = "Person photo",
            ProposedToken = "{{IMAGE:PPH}}",
            Confidence = ScanFieldConfidence.High,
            Scope = ScanFieldScope.Row,
            SourceRegion = stale,
            IsLocked = true,
        });
        var snapshot = ScanApprovedReviewSnapshot.Deserialize(ScanApprovedReviewSnapshot.Serialize(plan));

        var restored = ScanApprovedReviewRestorer.Restore(
            set,
            yellows,
            snapshot,
            officeBytes: bytes,
            sourceKind: ScanSourceKind.Word);

        var photo = Assert.Single(restored.Fields, f => f.SourceRegion is DocumentRegion.WordDrawing);
        var drawing = Assert.IsType<DocumentRegion.WordDrawing>(photo.SourceRegion);
        Assert.Equal(live.ParagraphAddress, drawing.ParagraphAddress);
        Assert.Equal(live.DrawingIndex, drawing.DrawingIndex);
        Assert.Contains("IMAGE:PPH", photo.ProposedToken, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Restore_yellow_without_picture_does_not_invent_photo()
    {
        var set = PlaceholderSet();
        var bytes = ScanOfficeYellowExtractorTests.CreateWordFixture("№ 4/-434");
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Word);
        var snapshot = SnapshotImageOnSpan(set);

        var restored = ScanApprovedReviewRestorer.Restore(
            set,
            yellows,
            snapshot,
            officeBytes: bytes,
            sourceKind: ScanSourceKind.Word);

        Assert.DoesNotContain(restored.Fields, f => f.SourceRegion is DocumentRegion.WordDrawing);
        Assert.DoesNotContain(restored.Fields, f =>
            ScanOfficePictureExtractor.IsPersonPhotoToken(f.ProposedToken));
    }

    [Fact]
    public void Serialize_locks_mapped_rows_so_reopen_is_protected_from_remap()
    {
        var set = PlaceholderSet();
        var plan = Plan(set, new ScanDetectedField
        {
            FieldId = "open",
            Box = ScanBoundingBox.FullPage,
            PageIndex = 0,
            LabelText = "Emre",
            ProposedToken = "{{.PFN}}",
            Confidence = ScanFieldConfidence.High,
            Scope = ScanFieldScope.Row,
            SourceRegion = new DocumentRegion.WordSpan("p1", 0, 4),
            IsLocked = false,
        });

        var snapshot = ScanApprovedReviewSnapshot.Deserialize(ScanApprovedReviewSnapshot.Serialize(plan));
        Assert.True(snapshot!.Fields[0].IsLocked);
    }

    private static ApplicationProfilePlaceholderSet PlaceholderSet() =>
        new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile(),
                DataScope = ApplicationProfileTemplateDataScope.Both,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });

    private static readonly long PortraitWidthEmu = 35L * WordInlinePictureLocator.EmuPerMillimetre;
    private static readonly long PortraitHeightEmu = 45L * WordInlinePictureLocator.EmuPerMillimetre;

    private static byte[] YellowWordWithPortrait() =>
        TemplateConvertFixtures.AppendInlinePicture(
            ScanOfficeYellowExtractorTests.CreateWordFixture("№ 4/-434"),
            PortraitWidthEmu,
            PortraitHeightEmu);

    private static ScanApprovedReviewSnapshot SnapshotImageOnSpan(ApplicationProfilePlaceholderSet set)
    {
        var plan = Plan(set, new ScanDetectedField
        {
            FieldId = "photo",
            Box = ScanBoundingBox.FullPage,
            PageIndex = 0,
            LabelText = "{{IMAGE:PPH}}",
            ProposedToken = "{{IMAGE:PPH}}",
            Confidence = ScanFieldConfidence.High,
            Scope = ScanFieldScope.Row,
            SourceRegion = new DocumentRegion.WordSpan("p10", 0, 12),
            IsLocked = true,
        });
        return ScanApprovedReviewSnapshot.Deserialize(ScanApprovedReviewSnapshot.Serialize(plan))!;
    }

    [Fact]
    public void Restore_overlay_mapped_image_token_onto_live_portrait()
    {
        var set = PlaceholderSet();
        var bytes = YellowWordWithPortrait();
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Word);
        var mapped = new[]
        {
            new ScanOfficeYellowSpan
            {
                Text = "{{IMAGE:PPH}}",
                Region = new DocumentRegion.WordSpan("p10", 0, 12),
            },
        };

        var restored = ScanApprovedReviewRestorer.Restore(
            set,
            yellows,
            snapshot: null,
            mappedTokenSpans: mapped,
            officeBytes: bytes,
            sourceKind: ScanSourceKind.Word);

        var photo = Assert.Single(restored.Fields, f => f.SourceRegion is DocumentRegion.WordDrawing);
        Assert.Contains("IMAGE:PPH", photo.ProposedToken, StringComparison.OrdinalIgnoreCase);
        Assert.True(photo.IsLocked);
    }

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