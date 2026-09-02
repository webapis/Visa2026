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
