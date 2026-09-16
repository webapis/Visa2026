#nullable enable

using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanReviewFieldOrderTests
{
    [Fact]
    public void Order_numbers_word_spans_top_to_bottom()
    {
        var fields = new[]
        {
            Field("b", "Nepesowa", new DocumentRegion.WordSpan("body/8", 2, 10)),
            Field("a", "Hilmi", new DocumentRegion.WordSpan("body/3", 0, 5)),
            Field("c", "Mehmet", new DocumentRegion.WordSpan("body/8", 0, 6)),
        };

        var ordered = ScanReviewFieldOrder.Order(fields);

        Assert.Equal(new[] { "a", "c", "b" }, ordered.Select(o => o.FieldId).ToArray());
        Assert.Equal(new[] { 1, 2, 3 }, ordered.Select(o => o.Order).ToArray());
    }

    [Fact]
    public void Order_places_header_marks_before_body()
    {
        var fields = new[]
        {
            Field("city", "Mary etrabyndan", new DocumentRegion.WordSpan("body/2", 0, 15)),
            Field("date", "12.02.2026", new DocumentRegion.WordSpan("header0/0", 0, 10)),
        };

        var ordered = ScanReviewFieldOrder.Order(fields);
        Assert.Equal(new[] { "date", "city" }, ordered.Select(o => o.FieldId).ToArray());
        Assert.Equal(new[] { 1, 2 }, ordered.Select(o => o.Order).ToArray());
    }

    [Fact]
    public void ApplyReadingBoxes_numbers_left_to_right_on_the_same_line()
    {
        var marks = new[]
        {
            FieldMark("p10", 10, "Mary"),
            FieldMark("p11", 11, "Ahal"),
            FieldMark("p12", 12, "etrabyndan"),
            FieldMark("p14", 14, "etrabyna"),
        };
        var boxes = new Dictionary<string, ScanExcelPreviewMarkBox>
        {
            ["p10"] = new(10, 50, 8, 2),
            ["p12"] = new(28, 50.4, 8, 2),
            ["p11"] = new(36, 50.2, 8, 2),
            ["p14"] = new(48, 50.1, 8, 2),
        };

        var ordered = ScanReviewFieldOrder.ApplyReadingBoxes(marks, boxes);
        Assert.Equal(new[] { "p10", "p12", "p11", "p14" }, ordered.Select(o => o.FieldId).ToArray());
        Assert.Equal(new[] { "1", "2", "3", "4" }, ordered.Select(o => o.DisplayOrder).ToArray());
    }

    [Fact]
    public void ApplyVisualSequence_renumbers_to_on_page_order()
    {
        var marks = new[]
        {
            FieldMark("p10", 10, "Mary"),
            FieldMark("p12", 12, "etrabyndan"),
            FieldMark("p11", 11, "Ahal"),
            FieldMark("p14", 14, "etrabyna"),
        };

        var ordered = ScanReviewFieldOrder.ApplyVisualSequence(marks, ["p10", "p12", "p11", "p14"]);
        Assert.Equal(new[] { "p10", "p12", "p11", "p14" }, ordered.Select(o => o.FieldId).ToArray());
        Assert.Equal(new[] { "1", "2", "3", "4" }, ordered.Select(o => o.DisplayOrder).ToArray());
    }

    [Fact]
    public void ApplyVisualSequence_keeps_unplaced_marks_after_on_page_hits()
    {
        var marks = new[]
        {
            FieldMark("a", 1, "Mary"),
            FieldMark("b", 2, "etrabyndan"),
            FieldMark("c", 3, "Ahal"),
        };

        var ordered = ScanReviewFieldOrder.ApplyVisualSequence(marks, ["b", "a"]);
        Assert.Equal(new[] { "b", "a", "c" }, ordered.Select(o => o.FieldId).ToArray());
        Assert.Equal(new[] { "1", "2", "3" }, ordered.Select(o => o.DisplayOrder).ToArray());
    }

    [Fact]
    public void Order_puts_fields_without_region_after_located_marks()
    {
        var fields = new[]
        {
            Field("later", "Z", null),
            Field("first", "A", new DocumentRegion.WordSpan("body/0", 0, 1)),
        };

        var ordered = ScanReviewFieldOrder.Order(fields);
        Assert.Equal("first", ordered[0].FieldId);
        Assert.Equal(1, ordered[0].Order);
        Assert.Equal("later", ordered[1].FieldId);
        Assert.Equal(2, ordered[1].Order);
    }

    [Fact]
    public void Order_places_a_picture_slot_after_earlier_text_in_the_same_paragraph()
    {
        var fields = new[]
        {
            Field("name", "Hilmi", new DocumentRegion.WordSpan("body/0", 0, 5)),
            Field("photo", "Person photo", new DocumentRegion.WordDrawing("body/0", 0, 8)),
        };

        var ordered = ScanReviewFieldOrder.Order(fields);
        Assert.Equal(new[] { "name", "photo" }, ordered.Select(o => o.FieldId).ToArray());
    }

    private static ScanReviewOrderedField FieldMark(string id, int order, string label) =>
        new(
            order,
            id,
            label,
            "{{ds.BTFRG}}",
            new DocumentRegion.WordSpan("body/0", 0, 4),
            0,
            false,
            OrderLabel: order.ToString(),
            OverlayId: id);

    private static ScanDetectedField Field(string id, string label, DocumentRegion? region) =>
        new()
        {
            FieldId = id,
            Box = ScanBoundingBox.FullPage,
            PageIndex = 0,
            LabelText = label,
            ProposedToken = "{{ds.PFN}}",
            Confidence = ScanFieldConfidence.High,
            Scope = ScanFieldScope.Header,
            SourceRegion = region,
        };
}
