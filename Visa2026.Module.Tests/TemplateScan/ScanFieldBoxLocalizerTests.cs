#nullable enable

using System.Drawing;
using System.Drawing.Imaging;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanFieldBoxLocalizerTests
{
    [Fact]
    public void Detect_finds_yellow_highlighter_regions()
    {
        using var bmp = new Bitmap(200, 200);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.White);
            using var yellow = new SolidBrush(Color.FromArgb(255, 255, 230, 0));
            g.FillRectangle(yellow, 20, 20, 60, 18);
            g.FillRectangle(yellow, 20, 80, 80, 20);
        }

        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        var boxes = ScanYellowRegionDetector.Detect(ms.ToArray());
        Assert.True(boxes.Count >= 2, $"expected >=2 yellow boxes, got {boxes.Count}");
        Assert.True(boxes[0].Top < boxes[1].Top);
    }

    [Fact]
    public void Detect_accepts_pale_highlighter_and_rejects_sparse_noise()
    {
        using var bmp = new Bitmap(300, 300);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.White);
            using var pale = new SolidBrush(Color.FromArgb(255, 255, 245, 157));
            g.FillRectangle(pale, 40, 180, 100, 22);

            using var speck = new SolidBrush(Color.FromArgb(255, 240, 220, 140));
            for (var i = 0; i < 8; i++)
                g.FillRectangle(speck, 50 + i * 18, 110, 3, 3);
        }

        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        var boxes = ScanYellowRegionDetector.Detect(ms.ToArray());
        Assert.True(boxes.Count >= 1, "pale yellow should be detected");
        Assert.True(boxes.All(b => b.Top > 0.45), "sparse gap speckles must not become boxes");
        Assert.True(boxes.Count <= 2, $"unexpected extra boxes: {boxes.Count}");
    }

    [Fact]
    public void Detect_rejects_small_warm_text_fragments()
    {
        using var bmp = new Bitmap(400, 400);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.White);
            using var yellow = new SolidBrush(Color.FromArgb(255, 255, 235, 40));
            g.FillRectangle(yellow, 40, 80, 110, 22); // real urgency-sized highlight

            // Warm fragments like anti-aliased "de" / "sa" / "sany"
            using var frag = new SolidBrush(Color.FromArgb(255, 220, 200, 90));
            g.FillRectangle(frag, 60, 160, 10, 8);
            g.FillRectangle(frag, 140, 165, 12, 8);
            g.FillRectangle(frag, 220, 200, 22, 9);
        }

        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        var boxes = ScanYellowRegionDetector.Detect(ms.ToArray());
        Assert.Single(boxes);
        Assert.True(boxes[0].Top < 0.35);
    }

    [Fact]
    public void Apply_does_not_park_urgency_on_text_fragment()
    {
        using var bmp = new Bitmap(400, 400);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.White);
            using var yellow = new SolidBrush(Color.FromArgb(255, 255, 235, 40));
            g.FillRectangle(yellow, 30, 90, 120, 22);  // urgency
            g.FillRectangle(yellow, 40, 260, 130, 24); // body
            using var frag = new SolidBrush(Color.FromArgb(255, 230, 210, 100));
            g.FillRectangle(frag, 180, 150, 14, 9);
        }

        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);

        var plan = new ScanFieldPlan
        {
            PlaceholderSet = PlaceholderSet(),
            ScanKind = ScanKind.FilledSample,
            Fields =
            [
                new ScanDetectedField
                {
                    FieldId = "u",
                    PageIndex = 0,
                    LabelText = "Adaty tertipde!",
                    ProposedToken = "{{ds.Urgency_NameTm}}",
                    Confidence = ScanFieldConfidence.High,
                    Scope = ScanFieldScope.Header,
                    Box = new ScanBoundingBox(0.05, 0.2, 0.4, 0.28),
                },
                new ScanDetectedField
                {
                    FieldId = "t",
                    PageIndex = 0,
                    LabelText = "18",
                    ProposedToken = "{{ds.TPCNT}}",
                    Confidence = ScanFieldConfidence.High,
                    Scope = ScanFieldScope.Header,
                    Box = new ScanBoundingBox(0.08, 0.35, 0.35, 0.42),
                },
            ],
            StaticRegions = Array.Empty<ScanStaticRegion>(),
            Gaps = Array.Empty<ScanGap>(),
            PendingQuestions = Array.Empty<ScanClarificationPrompt>(),
            Source = "test",
            YellowHighlightCount = 2,
        };

        var localized = ScanFieldBoxLocalizer.Apply(
            plan,
            [new ScanPageImage { PageIndex = 0, PngBytes = ms.ToArray(), WidthPx = 400, HeightPx = 400 }]);

        var urgency = Assert.Single(localized.Fields, f => f.FieldId == "u");
        Assert.True(urgency.Box.Top < 0.35, $"urgency parked wrong: {urgency.Box}");
        Assert.True(urgency.Box.Bottom < 0.4, $"urgency too tall/low: {urgency.Box}");
        Assert.True(urgency.Box.Right - urgency.Box.Left > 0.15, "urgency should use real yellow width");
    }

    [Fact]
    public void Apply_snaps_upward_shifted_ai_boxes_onto_body_yellow()
    {
        using var bmp = new Bitmap(400, 400);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.White);
            using var yellow = new SolidBrush(Color.FromArgb(255, 255, 235, 40));
            g.FillRectangle(yellow, 40, 260, 120, 24);
            g.FillRectangle(yellow, 200, 260, 90, 24);
            using var speck = new SolidBrush(Color.FromArgb(255, 235, 210, 120));
            for (var i = 0; i < 6; i++)
                g.FillRectangle(speck, 50 + i * 20, 160, 4, 4);
        }

        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);

        var plan = new ScanFieldPlan
        {
            PlaceholderSet = PlaceholderSet(),
            ScanKind = ScanKind.FilledSample,
            Fields =
            [
                new ScanDetectedField
                {
                    FieldId = "a",
                    PageIndex = 0,
                    LabelText = "18",
                    ProposedToken = "{{ds.TPCNT}}",
                    Confidence = ScanFieldConfidence.High,
                    Scope = ScanFieldScope.Header,
                    Box = new ScanBoundingBox(0.08, 0.35, 0.35, 0.42),
                },
                new ScanDetectedField
                {
                    FieldId = "b",
                    PageIndex = 0,
                    LabelText = "6 (alty) aý",
                    ProposedToken = "{{ds.VPER}}",
                    Confidence = ScanFieldConfidence.High,
                    Scope = ScanFieldScope.Header,
                    Box = new ScanBoundingBox(0.45, 0.36, 0.7, 0.43),
                },
            ],
            StaticRegions = Array.Empty<ScanStaticRegion>(),
            Gaps = Array.Empty<ScanGap>(),
            PendingQuestions = Array.Empty<ScanClarificationPrompt>(),
            Source = "test",
            YellowHighlightCount = 2,
        };

        var localized = ScanFieldBoxLocalizer.Apply(
            plan,
            [new ScanPageImage { PageIndex = 0, PngBytes = ms.ToArray(), WidthPx = 400, HeightPx = 400 }]);

        Assert.All(localized.Fields, f =>
        {
            Assert.True(f.Box.Top > 0.55, $"field {f.LabelText} still floating above yellow: {f.Box}");
            Assert.True(f.Box.Bottom > 0.6, $"field {f.LabelText} bottom too high: {f.Box}");
        });
    }

    [Fact]
    public void Apply_snaps_wrong_ai_boxes_onto_yellow_regions()
    {
        using var bmp = new Bitmap(300, 300);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.White);
            using var yellow = new SolidBrush(Color.FromArgb(255, 255, 235, 40));
            g.FillRectangle(yellow, 30, 40, 70, 22);
            g.FillRectangle(yellow, 30, 120, 90, 24);
        }

        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);

        var plan = new ScanFieldPlan
        {
            PlaceholderSet = PlaceholderSet(),
            ScanKind = ScanKind.FilledSample,
            Fields =
            [
                new ScanDetectedField
                {
                    FieldId = "1",
                    PageIndex = 0,
                    LabelText = "№ 4/-434",
                    ProposedToken = "{{ds.AFNUM}}",
                    Confidence = ScanFieldConfidence.High,
                    Scope = ScanFieldScope.Header,
                    Box = new ScanBoundingBox(0.7, 0.7, 0.95, 0.85),
                },
                new ScanDetectedField
                {
                    FieldId = "2",
                    PageIndex = 0,
                    LabelText = "Adaty tertipde!",
                    ProposedToken = "{{ds.Urgency_NameTm}}",
                    Confidence = ScanFieldConfidence.High,
                    Scope = ScanFieldScope.Header,
                    Box = new ScanBoundingBox(0.6, 0.5, 0.9, 0.65),
                },
            ],
            StaticRegions = Array.Empty<ScanStaticRegion>(),
            Gaps = Array.Empty<ScanGap>(),
            PendingQuestions = Array.Empty<ScanClarificationPrompt>(),
            Source = "test",
            YellowHighlightCount = 2,
        };

        var localized = ScanFieldBoxLocalizer.Apply(
            plan,
            [new ScanPageImage { PageIndex = 0, PngBytes = ms.ToArray(), WidthPx = 300, HeightPx = 300 }]);

        Assert.All(localized.Fields, f =>
        {
            Assert.True(f.Box.Top < 0.55, $"field {f.LabelText} still too low: {f.Box}");
            Assert.True(f.Box.Left < 0.5, $"field {f.LabelText} still too far right: {f.Box}");
        });
        Assert.Contains("box-localize", localized.Rationale);
    }

    private static ApplicationProfilePlaceholderSet PlaceholderSet() =>
        new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile(),
                DataScope = ApplicationProfileTemplateDataScope.Both,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });
}