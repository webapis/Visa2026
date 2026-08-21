using Visa2026.Module.Services.TemplateConvert;
using Xunit;

namespace Visa2026.Module.Tests.TemplateConvert;

public class TemplateConversionDiffGateTests
{
    private readonly ITemplateTokenWriter _writer = new TemplateTokenWriter();
    private readonly ITemplateConversionDiffGate _gate = new TemplateConversionDiffGate();

    private static byte[] Letter() =>
        TemplateConvertFixtures.CreateWordDocument(
            new[]
            {
                new[] { "Hormatly ", "Aýnabat", " Meredowa", " hakynda" },
                new[] { "Iş rugsatnamasyny uzaltmak barada." },
            },
            boldRuns: new[] { (0, 1) },
            headerText: "TÜRKMENISTANYN MINISTRLIGI");

    private static readonly TokenSubstitution NameSubstitution =
        new(new DocumentRegion.WordSpan("body/0", 9, 16), "ds.Person_FullName");

    [Fact]
    public void Word_conversion_that_only_places_approved_tokens_passes()
    {
        var original = Letter();
        var result = _writer.Apply(new TemplateTokenWriteRequest
        {
            SourceContent = original,
            Format = TemplateSourceFormat.Docx,
            Substitutions = new[] { NameSubstitution },
        });

        var verdict = _gate.Verify(new TemplateDiffGateRequest
        {
            OriginalContent = original,
            ConvertedContent = result.Content,
            Format = TemplateSourceFormat.Docx,
            Substitutions = result.AppliedSubstitutions,
            Loops = result.AppliedLoops,
        });

        Assert.True(verdict.Passed, string.Join(" | ", verdict.Violations));
    }

    [Fact]
    public void Word_conversion_with_loops_passes()
    {
        var original = Letter();
        var result = _writer.Apply(new TemplateTokenWriteRequest
        {
            SourceContent = original,
            Format = TemplateSourceFormat.Docx,
            Substitutions = new[] { NameSubstitution },
            Loops = new[]
            {
                new LoopMarker(
                    new DocumentRegion.WordSpan("body/1", 0, 0),
                    new DocumentRegion.WordSpan("body/1", 0, 0),
                    "ds.rows"),
            },
        });

        var verdict = _gate.Verify(new TemplateDiffGateRequest
        {
            OriginalContent = original,
            ConvertedContent = result.Content,
            Format = TemplateSourceFormat.Docx,
            Substitutions = result.AppliedSubstitutions,
            Loops = result.AppliedLoops,
        });

        Assert.True(verdict.Passed, string.Join(" | ", verdict.Violations));
    }

    [Fact]
    public void Word_edit_outside_the_approved_spans_fails()
    {
        var original = Letter();
        var result = _writer.Apply(new TemplateTokenWriteRequest
        {
            SourceContent = original,
            Format = TemplateSourceFormat.Docx,
            Substitutions = new[] { NameSubstitution },
        });

        var tampered = TemplateConvertFixtures.TamperWordText(result.Content, "body/1", "Rugsatnama ýatyryldy.");

        var verdict = _gate.Verify(new TemplateDiffGateRequest
        {
            OriginalContent = original,
            ConvertedContent = tampered,
            Format = TemplateSourceFormat.Docx,
            Substitutions = result.AppliedSubstitutions,
            Loops = result.AppliedLoops,
        });

        Assert.False(verdict.Passed);
        Assert.Contains(verdict.Violations, v => v.Contains("body/1", StringComparison.Ordinal));
    }

    [Fact]
    public void Word_token_the_officer_never_approved_fails()
    {
        var original = Letter();
        var result = _writer.Apply(new TemplateTokenWriteRequest
        {
            SourceContent = original,
            Format = TemplateSourceFormat.Docx,
            Substitutions = new[]
            {
                NameSubstitution,
                new TokenSubstitution(new DocumentRegion.WordSpan("body/1", 0, 2), "ds.Rogue_Token"),
            },
        });

        var verdict = _gate.Verify(new TemplateDiffGateRequest
        {
            OriginalContent = original,
            ConvertedContent = result.Content,
            Format = TemplateSourceFormat.Docx,
            Substitutions = new[] { NameSubstitution },
        });

        Assert.False(verdict.Passed);
        Assert.Contains(verdict.Violations, v => v.Contains("body/1", StringComparison.Ordinal));
    }

    [Fact]
    public void Excel_conversion_preserves_formats_widths_and_merges()
    {
        var original = TemplateConvertFixtures.CreateExcelRoster();
        var result = _writer.Apply(new TemplateTokenWriteRequest
        {
            SourceContent = original,
            Format = TemplateSourceFormat.Xlsx,
            Substitutions = new[]
            {
                new TokenSubstitution(new DocumentRegion.ExcelCell("Sanaw", "A2"), ".Person_FullName"),
                new TokenSubstitution(new DocumentRegion.ExcelCell("Sanaw", "B2"), ".Passport_Number"),
            },
        });

        var verdict = _gate.Verify(new TemplateDiffGateRequest
        {
            OriginalContent = original,
            ConvertedContent = result.Content,
            Format = TemplateSourceFormat.Xlsx,
            Substitutions = result.AppliedSubstitutions,
        });

        Assert.True(verdict.Passed, string.Join(" | ", verdict.Violations));
    }

    [Fact]
    public void Excel_value_change_outside_the_approved_cells_fails()
    {
        var original = TemplateConvertFixtures.CreateExcelRoster();
        var result = _writer.Apply(new TemplateTokenWriteRequest
        {
            SourceContent = original,
            Format = TemplateSourceFormat.Xlsx,
            Substitutions = new[]
            {
                new TokenSubstitution(new DocumentRegion.ExcelCell("Sanaw", "A2"), ".Person_FullName"),
                new TokenSubstitution(new DocumentRegion.ExcelCell("Sanaw", "A1"), ".Column_Header"),
            },
        });

        var verdict = _gate.Verify(new TemplateDiffGateRequest
        {
            OriginalContent = original,
            ConvertedContent = result.Content,
            Format = TemplateSourceFormat.Xlsx,
            Substitutions = new[]
            {
                new TokenSubstitution(new DocumentRegion.ExcelCell("Sanaw", "A2"), ".Person_FullName"),
            },
        });

        Assert.False(verdict.Passed);
        Assert.Contains(verdict.Violations, v => v.Contains("A1", StringComparison.Ordinal));
    }
}
