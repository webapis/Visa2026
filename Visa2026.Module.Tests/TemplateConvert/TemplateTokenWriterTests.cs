using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ClosedXML.Excel;
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
    public void Word_overlapping_same_token_keeps_longest_span_silently()
    {
        var content = TemplateConvertFixtures.CreateWordDocument(
            new[] { new[] { "Mehmet ÇIRAK __ signed" } });

        var result = _writer.Apply(new TemplateTokenWriteRequest
        {
            SourceContent = content,
            Format = TemplateSourceFormat.Docx,
            Substitutions = new[]
            {
                new TokenSubstitution(new DocumentRegion.WordSpan("body/0", 0, 12), "ds.CHFN"),
                new TokenSubstitution(new DocumentRegion.WordSpan("body/0", 0, 15), "ds.CHFN"),
            },
        });

        Assert.Empty(result.Skipped);
        Assert.Single(result.AppliedSubstitutions);
        Assert.Equal(15, ((DocumentRegion.WordSpan)result.AppliedSubstitutions[0].Region).Length);
        Assert.Equal(
            "{{ds.CHFN}} signed",
            TemplateConvertFixtures.GetParagraphText(result.Content, "body/0"));
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
    public void Excel_loop_open_prepends_when_cell_already_has_row_token()
    {
        var source = TemplateConvertFixtures.CreateExcelRoster();
        var withRnum = ExcelTemplateTokenWriter.Write(
            source,
            new[] { new TokenSubstitution(new DocumentRegion.ExcelCell("Sanaw", "A2"), ".RNUM") },
            Array.Empty<LoopMarker>()).Content;

        var result = ExcelTemplateTokenWriter.Write(
            withRnum,
            Array.Empty<TokenSubstitution>(),
            new[]
            {
                new LoopMarker(
                    new DocumentRegion.ExcelCell("Sanaw", "A2"),
                    new DocumentRegion.ExcelCell("Sanaw", "A3"),
                    "ds.rows"),
            });

        Assert.Contains(result.AppliedLoops, static l => l.CollectionToken == "ds.rows");
        Assert.Equal("{{#ds.rows}}{{.RNUM}}", TemplateConvertFixtures.GetCellText(result.Content, "Sanaw", "A2"));
        Assert.Equal("{{/ds.rows}}", TemplateConvertFixtures.GetCellText(result.Content, "Sanaw", "A3"));
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
    
    [Fact]
    public void Word_yellow_highlight_is_cleared_when_token_is_written()
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            main.Document = new Document(new Body(
                new Paragraph(
                    new Run(new Text("Prefix ")),
                    new Run(
                        new RunProperties(new Highlight { Val = HighlightColorValues.Yellow }),
                        new Text("8/-015")),
                    new Run(new Text(" suffix")))));
            main.Document.Save();
        }

        var source = stream.ToArray();
        Assert.Contains(
            TemplateConvertFixtures.GetRuns(source, "body/0"),
            r => r.RunProperties?.Highlight != null);

        var result = _writer.Apply(new TemplateTokenWriteRequest
        {
            SourceContent = source,
            Format = TemplateSourceFormat.Docx,
            Substitutions = new[]
            {
                new TokenSubstitution(new DocumentRegion.WordSpan("body/0", 7, 6), "ds.AFNUM"),
            },
        });

        Assert.Empty(result.Skipped);
        Assert.Equal("Prefix {{ds.AFNUM}} suffix", TemplateConvertFixtures.GetParagraphText(result.Content, "body/0"));
        Assert.DoesNotContain(
            TemplateConvertFixtures.GetRuns(result.Content, "body/0"),
            r => r.RunProperties?.Highlight != null);
    }

    [Fact]
    public void Excel_yellow_fill_is_cleared_when_token_is_written()
    {
        using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("Sanaw");
            sheet.Cell("A1").Value = "8/-015";
            sheet.Cell("A1").Style.Fill.BackgroundColor = XLColor.Yellow;
            workbook.SaveAs(stream);
        }

        var result = _writer.Apply(new TemplateTokenWriteRequest
        {
            SourceContent = stream.ToArray(),
            Format = TemplateSourceFormat.Xlsx,
            Substitutions = new[]
            {
                new TokenSubstitution(new DocumentRegion.ExcelCell("Sanaw", "A1"), "ds.AFNUM"),
            },
        });

        Assert.Empty(result.Skipped);
        Assert.Equal("{{ds.AFNUM}}", TemplateConvertFixtures.GetCellText(result.Content, "Sanaw", "A1"));

        using var verify = new MemoryStream(result.Content, writable: false);
        using var wb = new XLWorkbook(verify);
        Assert.Equal(XLFillPatternValues.None, wb.Worksheet("Sanaw").Cell("A1").Style.Fill.PatternType);
    }

    [Fact]
    public void StripAllYellowMarkup_clears_unmapped_leftover_highlights()
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            main.Document = new Document(new Body(
                new Paragraph(
                    new Run(new Text("Prefix ")),
                    new Run(
                        new RunProperties(new Highlight { Val = HighlightColorValues.Yellow }),
                        new Text("{{ds.VCAT}}")),
                    new Run(new Text(" middle ")),
                    new Run(
                        new RunProperties(new Highlight { Val = HighlightColorValues.Yellow }),
                        new Text("6 (alty)")),
                    new Run(new Text(" end")))));
            main.Document.Save();
        }

        var cleaned = WordTemplateTokenWriter.StripAllYellowMarkup(stream.ToArray());
        Assert.DoesNotContain(
            TemplateConvertFixtures.GetRuns(cleaned, "body/0"),
            r => r.RunProperties?.Highlight != null);
        Assert.Contains("6 (alty)", TemplateConvertFixtures.GetParagraphText(cleaned, "body/0"), StringComparison.Ordinal);
    }

    [Fact]
    public void StripAllYellowFills_clears_unmapped_rgb_leftover()
    {
        using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("Sanaw");
            sheet.Cell("A5").Value = "{{.PLN}}";
            sheet.Cell("G5").Value = "U12345678";
            sheet.Cell("G5").Style.Fill.BackgroundColor = XLColor.Yellow;
            workbook.SaveAs(stream);
        }

        var cleaned = ExcelTemplateTokenWriter.StripAllYellowFills(stream.ToArray());
        using var verify = new MemoryStream(cleaned, writable: false);
        using var wb = new XLWorkbook(verify);
        Assert.Equal(XLFillPatternValues.None, wb.Worksheet("Sanaw").Cell("G5").Style.Fill.PatternType);
        Assert.Equal("U12345678", wb.Worksheet("Sanaw").Cell("G5").GetString());
    }

    [Fact]
    public void StripAllYellowFills_clears_indexed_yellow()
    {
        using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("Sanaw");
            sheet.Cell("G5").Value = "U12345678";
            sheet.Cell("G5").Style.Fill.BackgroundColor = XLColor.FromIndex(13);
            workbook.SaveAs(stream);
        }

        var cleaned = ExcelTemplateTokenWriter.StripAllYellowFills(stream.ToArray());
        using var verify = new MemoryStream(cleaned, writable: false);
        using var wb = new XLWorkbook(verify);
        Assert.Equal(XLFillPatternValues.None, wb.Worksheet("Sanaw").Cell("G5").Style.Fill.PatternType);
    }

    [Fact]
    public void StripAllYellowFills_clears_merged_non_anchor_yellow()
    {
        using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("Sanaw");
            sheet.Range("J5:K5").Merge();
            sheet.Range("J5:K5").Style.Fill.BackgroundColor = XLColor.Yellow;
            sheet.Cell("J5").Value = "elektrik-elektronika";
            workbook.SaveAs(stream);
        }

        var cleaned = ExcelTemplateTokenWriter.StripAllYellowFills(stream.ToArray());
        using var verify = new MemoryStream(cleaned, writable: false);
        using var wb = new XLWorkbook(verify);
        var worksheet = wb.Worksheet("Sanaw");
        Assert.Equal(XLFillPatternValues.None, worksheet.Cell("J5").Style.Fill.PatternType);
        Assert.Equal(XLFillPatternValues.None, worksheet.Cell("K5").Style.Fill.PatternType);
    }
}
