using Visa2026.Module.Services.TemplateConvert;
using Xunit;

namespace Visa2026.Module.Tests.TemplateConvert;

public class TemplateResidualValueScannerTests
{
    private readonly ITemplateTokenWriter _writer = new TemplateTokenWriter();
    private readonly ITemplateResidualValueScanner _scanner = new TemplateResidualValueScanner();

    [Fact]
    public void Word_leftover_person_name_is_reported()
    {
        var content = TemplateConvertFixtures.CreateWordDocument(
            new[] { new[] { "Hormatly Aýnabat Meredowa" } });

        var result = _scanner.Scan(
            content,
            TemplateSourceFormat.Docx,
            new[] { new ResidualValueProbe("Aýnabat Meredowa", "Person_FullName") });

        Assert.False(result.IsClean);
        Assert.Equal("Person_FullName", result.Hits[0].Label);
        Assert.Equal("body/0", result.Hits[0].LocationHint);
    }

    [Fact]
    public void Word_document_is_clean_once_the_value_became_a_token()
    {
        var written = _writer.Apply(new TemplateTokenWriteRequest
        {
            SourceContent = TemplateConvertFixtures.CreateWordDocument(new[] { new[] { "Hormatly Aýnabat Meredowa" } }),
            Format = TemplateSourceFormat.Docx,
            Substitutions = new[]
            {
                new TokenSubstitution(new DocumentRegion.WordSpan("body/0", 9, 16), "ds.Person_FullName"),
            },
        });

        var result = _scanner.Scan(
            written.Content,
            TemplateSourceFormat.Docx,
            new[] { new ResidualValueProbe("Aýnabat Meredowa", "Person_FullName") });

        Assert.True(result.IsClean);
    }

    [Fact]
    public void Diacritics_do_not_hide_a_leftover_value()
    {
        var content = TemplateConvertFixtures.CreateWordDocument(
            new[] { new[] { "Aynabat Meredowa" } });

        var result = _scanner.Scan(
            content,
            TemplateSourceFormat.Docx,
            new[] { new ResidualValueProbe("Aýnabat Meredowa", "Person_FullName") });

        Assert.False(result.IsClean);
    }

    [Fact]
    public void Identifier_probes_ignore_separators_and_spacing()
    {
        var content = TemplateConvertFixtures.CreateWordDocument(
            new[] { new[] { "Pasport: T 1234567" } });

        var result = _scanner.Scan(
            content,
            TemplateSourceFormat.Docx,
            new[] { new ResidualValueProbe("T-1234567", "Passport_Number", ResidualProbeKind.Identifier) });

        Assert.False(result.IsClean);
    }

    [Fact]
    public void Values_shorter_than_the_minimum_are_not_matched()
    {
        var content = TemplateConvertFixtures.CreateWordDocument(new[] { new[] { "Otag 12" } });

        var result = _scanner.Scan(
            content,
            TemplateSourceFormat.Docx,
            new[] { new ResidualValueProbe("12", "Room") });

        Assert.True(result.IsClean);
    }

    [Fact]
    public void Excel_leftover_cell_value_reports_its_sheet_and_address()
    {
        var result = _scanner.Scan(
            TemplateConvertFixtures.CreateExcelRoster(),
            TemplateSourceFormat.Xlsx,
            new[] { new ResidualValueProbe("Meredowa Aýnabat", "Person_FullName") });

        Assert.False(result.IsClean);
        Assert.Equal("Sanaw!A2", result.Hits[0].LocationHint);
    }

    [Fact]
    public void No_probes_means_nothing_to_report()
    {
        var result = _scanner.Scan(
            TemplateConvertFixtures.CreateExcelRoster(),
            TemplateSourceFormat.Xlsx,
            Array.Empty<ResidualValueProbe>());

        Assert.True(result.IsClean);
    }
}
