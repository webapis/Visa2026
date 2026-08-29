#nullable enable

using Visa2026.Module.Services.TemplateScan;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanLetterLayoutNormalizerTests
{
    [Fact]
    public void Apply_rebuilds_side_by_side_header_and_signature()
    {
        var input = new ScanDocxLayoutProposal
        {
            Blocks =
            [
                new ScanDocxBlock { Kind = "paragraph", Align = "left", Text = "№ {{ds.AFNUM}}" },
                new ScanDocxBlock { Kind = "paragraph", Align = "left", Text = "{{ds.ADAT}}" },
                new ScanDocxBlock { Kind = "paragraph", Align = "left", Text = "Türkmenistanyň Döwlet" },
                new ScanDocxBlock { Kind = "paragraph", Align = "left", Text = "migrasiýa gullugyna" },
                new ScanDocxBlock { Kind = "paragraph", Align = "left", Text = "{{ds.Urgency_NameTm}}" },
                new ScanDocxBlock { Kind = "paragraph", Align = "left", Text = "Uzun body paragrafy bilen {{ds.TPCNT}} ({{ds.TPCTX}}) sany we {{ds.VPER}} {{ds.VCAT}} möhlet. Bu setir bilen justification üçin ýeterlik uzynlyk bolmaly." },
                new ScanDocxBlock { Kind = "paragraph", Align = "left", Text = "Türkmenistandaky şahamçasynyň müdiri" },
                new ScanDocxBlock { Kind = "paragraph", Align = "left", Text = "Mehmet ÇIRAK" },
            ],
            Rationale = "ai-flat",
        };

        var result = ScanLetterLayoutNormalizer.Apply(input);

        var header = Assert.Single(result.Blocks, b => b.Kind == "twoColumn" && (b.Text?.Contains("AFNUM", StringComparison.Ordinal) ?? false));
        Assert.Contains("ADAT", header.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("gullugyna", header.RightText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ADAT", header.RightText, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("right", header.RightAlign);
        Assert.Contains(result.Blocks, b => b.Style == "italic" && (b.Text?.Contains("Urgency", StringComparison.Ordinal) ?? false));
        Assert.Contains(result.Blocks, b => b.Align == "justify");
        var signature = Assert.Single(result.Blocks, b => b.Kind == "twoColumn" && (b.RightText?.Contains("ÇIRAK", StringComparison.Ordinal) ?? false));
        Assert.Equal("bold", signature.Style);
        Assert.Equal("bold", signature.RightStyle);
    }

    [Fact]
    public void Apply_fixes_afnum_adat_as_wrong_twoColumn_using_ocr_addressee()
    {
        var input = new ScanDocxLayoutProposal
        {
            Blocks =
            [
                new ScanDocxBlock
                {
                    Kind = "twoColumn",
                    Text = "№ {{ds.AFNUM}}",
                    RightText = "{{ds.ADAT}}",
                    Align = "left",
                    RightAlign = "right",
                },
                new ScanDocxBlock { Kind = "paragraph", Text = "{{ds.Urgency_NameTm}}", Style = "italic" },
                new ScanDocxBlock { Kind = "paragraph", Align = "justify", Text = "Uzun body paragrafy bilen {{ds.TPCNT}} ({{ds.TPCTX}}) we {{ds.VPER}} {{ds.VCAT}} üçin ýeterlik uzynlyk bolmaly setir." },
                new ScanDocxBlock { Kind = "paragraph", Text = "Türkmenistandaky şahamçasynyň müdiri", Style = "bold" },
                new ScanDocxBlock { Kind = "paragraph", Text = "Mehmet ÇIRAK", Style = "bold" },
            ],
            Rationale = "ai-bad-header",
        };

        var ocr = new[]
        {
            new ScanOcrLine { PageIndex = 0, Text = "№ 4/-434" },
            new ScanOcrLine { PageIndex = 0, Text = "28.04.2026 ý." },
            new ScanOcrLine { PageIndex = 0, Text = "Türkmenistanyň Döwlet" },
            new ScanOcrLine { PageIndex = 0, Text = "migrasiýa gullugyna" },
            new ScanOcrLine { PageIndex = 0, Text = "Adaty tertipde!" },
        };

        var result = ScanLetterLayoutNormalizer.Apply(input, ocr);
        var header = Assert.Single(result.Blocks, b => b.Kind == "twoColumn" && (b.Text?.Contains("AFNUM", StringComparison.Ordinal) ?? false));
        Assert.Contains("ADAT", header.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("gullugyna", header.RightText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ADAT", header.RightText ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}