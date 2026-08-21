using DocumentFormat.OpenXml.Wordprocessing;
using Visa2026.Module.Services.TemplateConvert;
using Xunit;

namespace Visa2026.Module.Tests.TemplateConvert;

public class TemplateTokenWriterTests
{
    private readonly ITemplateTokenWriter _writer = new TemplateTokenWriter();

    // "Hormatly " = 9 chars, "Aýnabat" = 7, " Meredowa" = 9 — the name spans runs 1 and 2.
    private static byte[] LetterWithSplitName() =>
        TemplateConvertFixtures.CreateWordDocument(
            new[] { new[] { "Hormatly ", "Aýnabat", " Meredowa", " hakynda" } },
            boldRuns: new[] { (0, 1) });

    [Fact]
    public void Word_phrase_split_across_runs_is_replaced_by_a_single_token()
    {
        var result = _writer.Apply(new TemplateTokenWriteRequest
        {
            SourceContent = LetterWithSplitName(),
            Format = TemplateSourceFormat.Docx,
            Substitutions = new[]
            {
                new TokenSubstitution(new DocumentRegion.WordSpan("body/0", 9, 16), "ds.Person_FullName"),
            },
        });

        Assert.Empty(result.Skipped);
        Assert.Single(result.AppliedSubstitutions);
        Assert.Equal(
            "Hormatly {{ds.Person_FullName}} hakynda",
            TemplateConvertFixtures.GetParagraphText(result.Content, "body/0"));
    }

    [Fact]
    public void Word_run_count_and_formatting_survive_the_substitution()
    {
        var result = _writer.Apply(new TemplateTokenWriteRequest
        {
            SourceContent = LetterWithSplitName(),
            Format = TemplateSourceFormat.Docx,
            Substitutions = new[]
            {
                new TokenSubstitution(new DocumentRegion.WordSpan("body/0", 9, 16), "ds.Person_FullName"),
            },
        });

        var runs = TemplateConvertFixtures.GetRuns(result.Content, "body/0");
        Assert.Equal(4, runs.Count);

        var tokenRun = runs.Single(r => r.InnerText.Contains("{{ds.Person_FullName}}", StringComparison.Ordinal));
        Assert.NotNull(tokenRun.RunProperties?.GetFirstChild<Bold>());
    }

    [Fact]
    public void Word_multiple_tokens_in_one_paragraph_keep_their_offsets()
    {
        var content = TemplateConvertFixtures.CreateWordDocument(
            new[] { new[] { "Aýnabat", " geldi ", "2026-08-20" } });

        var result = _writer.Apply(new TemplateTokenWriteRequest
        {
            SourceContent = content,
            Format = TemplateSourceFormat.Docx,
            Substitutions = new[]
            {
                new TokenSubstitution(new DocumentRegion.WordSpan("body/0", 0, 7), "ds.Person_FirstName"),
                new TokenSubstitution(new DocumentRegion.WordSpan("body/0", 14, 10), "ds.Application_Date"),
            },
        });

        Assert.Empty(result.Skipped);
        Assert.Equal(
            "{{ds.Person_FirstName}} geldi {{ds.Application_Date}}",
            TemplateConvertFixtures.GetParagraphText(result.Content, "body/0"));
    }

    [Fact]
    public void Word_header_paragraphs_are_addressable()
    {
        var content = TemplateConvertFixtures.CreateWordDocument(
            new[] { new[] { "Esasy tekst" } },
            headerText: "TÜRKMENISTANYN MINISTRLIGI");

        var result = _writer.Apply(new TemplateTokenWriteRequest
        {
            SourceContent = content,
            Format = TemplateSourceFormat.Docx,
            Substitutions = new[]
            {
                new TokenSubstitution(new DocumentRegion.WordSpan("header0/0", 0, 26), "ds.Ministry_Name"),
            },
        });

        Assert.Empty(result.Skipped);
        Assert.Equal("{{ds.Ministry_Name}}", TemplateConvertFixtures.GetParagraphText(result.Content, "header0/0"));
    }

    [Fact]
    public void Word_span_outside_the_paragraph_is_skipped_and_text_is_untouched()
    {
        var result = _writer.Apply(new TemplateTokenWriteRequest
        {
            SourceContent = LetterWithSplitName(),
            Format = TemplateSourceFormat.Docx,
            Substitutions = new[]
            {
                new TokenSubstitution(new DocumentRegion.WordSpan("body/0", 30, 40), "ds.Person_FullName"),
            },
        });

        Assert.Empty(result.AppliedSubstitutions);
        Assert.Single(result.Skipped);
        Assert.Contains("outside paragraph text", result.Skipped[0].Reason, StringComparison.Ordinal);
        Assert.Equal(
            "Hormatly Aýnabat Meredowa hakynda",
            TemplateConvertFixtures.GetParagraphText(result.Content, "body/0"));
    }

    [Fact]
    public void Word_unknown_paragraph_address_is_skipped()
    {
        var result = _writer.Apply(new TemplateTokenWriteRequest
        {
            SourceContent = LetterWithSplitName(),
            Format = TemplateSourceFormat.Docx,
            Substitutions = new[]
            {
                new TokenSubstitution(new DocumentRegion.WordSpan("body/42", 0, 3), "ds.Person_FullName"),
            },
        });

        Assert.Single(result.Skipped);
        Assert.Contains("not found", result.Skipped[0].Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void Word_overlapping_spans_are_all_skipped()
    {
        var result = _writer.Apply(new TemplateTokenWriteRequest
        {
            SourceContent = LetterWithSplitName(),
            Format = TemplateSourceFormat.Docx,
            Substitutions = new[]
            {
                new TokenSubstitution(new DocumentRegion.WordSpan("body/0", 9, 16), "ds.Person_FullName"),
                new TokenSubstitution(new DocumentRegion.WordSpan("body/0", 9, 7), "ds.Person_FirstName"),
            },
        });

        Assert.Empty(result.AppliedSubstitutions);
        Assert.Equal(2, result.Skipped.Count);
        Assert.All(result.Skipped, s => Assert.Contains("Overlapping", s.Reason, StringComparison.Ordinal));
    }

    [Fact]
    public void Word_loop_markers_wrap_the_boundary_paragraphs()
    {
        var content = TemplateConvertFixtures.CreateWordDocument(
            new[] { "Setir başy" },
            new[] { "Setir soňy" });

        var result = _writer.Apply(new TemplateTokenWriteRequest
        {
            SourceContent = content,
            Format = TemplateSourceFormat.Docx,
            Loops = new[]
            {
                new LoopMarker(
                    new DocumentRegion.WordSpan("body/0", 0, 0),
                    new DocumentRegion.WordSpan("body/1", 0, 0),
                    "ds.rows"),
            },
        });

        Assert.Empty(result.Skipped);
        Assert.Equal("{{#ds.rows}}Setir başy", TemplateConvertFixtures.GetParagraphText(result.Content, "body/0"));
        Assert.Equal("Setir soňy{{/ds.rows}}", TemplateConvertFixtures.GetParagraphText(result.Content, "body/1"));
    }

    [Fact]
    public void Excel_cell_value_is_replaced_by_the_token()
    {
        var result = _writer.Apply(new TemplateTokenWriteRequest
        {
            SourceContent = TemplateConvertFixtures.CreateExcelRoster(),
            Format = TemplateSourceFormat.Xlsx,
            Substitutions = new[]
            {
                new TokenSubstitution(new DocumentRegion.ExcelCell("Sanaw", "A2"), ".Person_FullName"),
                new TokenSubstitution(new DocumentRegion.ExcelCell("Sanaw", "B2"), ".Passport_Number"),
            },
        });

        Assert.Empty(result.Skipped);
        Assert.Equal("{{.Person_FullName}}", TemplateConvertFixtures.GetCellText(result.Content, "Sanaw", "A2"));
        Assert.Equal("{{.Passport_Number}}", TemplateConvertFixtures.GetCellText(result.Content, "Sanaw", "B2"));
    }

    [Fact]
    public void Excel_formula_cells_are_never_overwritten()
    {
        var result = _writer.Apply(new TemplateTokenWriteRequest
        {
            SourceContent = TemplateConvertFixtures.CreateExcelRoster(),
            Format = TemplateSourceFormat.Xlsx,
            Substitutions = new[]
            {
                new TokenSubstitution(new DocumentRegion.ExcelCell("Sanaw", "D2"), ".Total"),
            },
        });

        Assert.Empty(result.AppliedSubstitutions);
        Assert.Single(result.Skipped);
        Assert.Contains("formula", result.Skipped[0].Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Excel_non_anchor_merged_cells_are_skipped()
    {
        var result = _writer.Apply(new TemplateTokenWriteRequest
        {
            SourceContent = TemplateConvertFixtures.CreateExcelRoster(),
            Format = TemplateSourceFormat.Xlsx,
            Substitutions = new[]
            {
                new TokenSubstitution(new DocumentRegion.ExcelCell("Sanaw", "B4"), ".Total"),
            },
        });

        Assert.Single(result.Skipped);
        Assert.Contains("merged", result.Skipped[0].Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Excel_unknown_sheet_is_skipped()
    {
        var result = _writer.Apply(new TemplateTokenWriteRequest
        {
            SourceContent = TemplateConvertFixtures.CreateExcelRoster(),
            Format = TemplateSourceFormat.Xlsx,
            Substitutions = new[]
            {
                new TokenSubstitution(new DocumentRegion.ExcelCell("Yok", "A1"), ".Person_FullName"),
            },
        });

        Assert.Single(result.Skipped);
        Assert.Contains("not found", result.Skipped[0].Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void Excel_loop_markers_use_generator_syntax()
    {
        var result = _writer.Apply(new TemplateTokenWriteRequest
        {
            SourceContent = TemplateConvertFixtures.CreateExcelRoster(),
            Format = TemplateSourceFormat.Xlsx,
            Loops = new[]
            {
                new LoopMarker(
                    new DocumentRegion.ExcelCell("Sanaw", "E2"),
                    new DocumentRegion.ExcelCell("Sanaw", "E3"),
                    "ds.rows"),
            },
        });

        Assert.Empty(result.Skipped);
        Assert.Equal("{{#ds.rows}}", TemplateConvertFixtures.GetCellText(result.Content, "Sanaw", "E2"));
        Assert.Equal("{{/ds.rows}}", TemplateConvertFixtures.GetCellText(result.Content, "Sanaw", "E3"));
    }
}
