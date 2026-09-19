#nullable enable

using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanSurroundPlaceholderPatternTests
{
    private static ApplicationProfilePlaceholderSet Set() =>
        new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile(),
                DataScope = ApplicationProfileTemplateDataScope.Both,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });

    [Theory]
    [InlineData("Aynabat Meredowa", "Ise cagrylan adam: (ady, familiyasy, atasynyn ady, doglan senesi)", "PFN")]
    [InlineData("Gurban Annayew", "Ise cagrylan adam:", "PFN")]
    [InlineData("03.04.1991", "(ady, familiyasy, atasynyn ady, doglan senesi)", "PDBT")]
    [InlineData("U37109249", "pasporty: (pasportyn seriyasy we belgisi, nirede we hacan berildi, mohleti)", "PPN")]
    [InlineData("19.02.2024", "pasporty: (pasportyn seriyasy we belgisi, nirede we hacan berildi, mohleti)", "PPED")]
    [InlineData("19.02.2034", "yolbascy pasporty: (pasportyn seriyasy we belgisi, nirede we hacan berildi, mohleti)", "CHPE")]
    public void Immediate_surround_ranks_the_matching_placeholder(
        string yellow,
        string nearby,
        string expectedCode)
    {
        var ranked = ScanSurroundPlaceholderPattern.Rank(
            yellow,
            nearby,
            null,
            Set(),
            UserReportPlaceholderScope.Header);

        Assert.NotEmpty(ranked);
        Assert.Equal(expectedCode, ranked[0].ShortCode);
        Assert.True(ranked[0].ScorePercent >= ScanSurroundPlaceholderPattern.NearbyMinScore);
    }

    [Fact]
    public void Turkmenistan_residence_column_ranks_ADRS_not_company_address()
    {
        var ranked = ScanSurroundPlaceholderPattern.Rank(
            "Aşgabat şäheriniň 11-nji (Bagtyýarlyk) etrap, I.Gandyýew köçesi",
            "Türkmenistandaky salgysy",
            "Türkmenistandaky salgysy",
            Set(),
            UserReportPlaceholderScope.Row);

        Assert.Equal("ADRS", ranked[0].ShortCode);
        Assert.True(ranked[0].ScorePercent >= ScanSurroundPlaceholderPattern.NearbyMinScore);
        Assert.DoesNotContain(
            ranked.Take(2),
            a => a.ShortCode.Equals("ACADR", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Company_legal_address_caption_still_ranks_ACADR()
    {
        var ranked = ScanSurroundPlaceholderPattern.Rank(
            "Aşgabat ş., Bitarap Türkmenistan şaýoly 538",
            "Karhana (hasaba alnan belgisi, senesi, yuridiki salgysy, telefon belgisi)",
            null,
            Set(),
            UserReportPlaceholderScope.Header);

        Assert.Contains(
            ranked.Take(3),
            a => a.ShortCode.Equals("ACADR", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Surround_pattern_does_not_map_a_hired_person_name_to_company_address()
    {
        var ranked = ScanSurroundPlaceholderPattern.Rank(
            "Meret Hydyrow",
            "Ise cagrylan adam: (ady, familiyasy, atasynyn ady, doglan senesi)",
            null,
            Set(),
            UserReportPlaceholderScope.Row);

        Assert.Equal("PFN", ranked[0].ShortCode);
        Assert.DoesNotContain(ranked.Take(2), a => a.ShortCode.Equals("ACADR", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Rank_on_name_and_dob_comma_line_prefers_person_not_company_address()
    {
        var ranked = ScanSurroundPlaceholderPattern.Rank(
            "Aynabat Meredowa, 03.04.1991",
            "Ise cagrylan adam: (ady, familiyasy, atasynyn ady, doglan senesi)",
            null,
            Set(),
            UserReportPlaceholderScope.Row);

        Assert.DoesNotContain(
            ranked.Take(3),
            a => a.ShortCode.Equals("ACADR", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            ranked.Take(3),
            a => a.ShortCode.Equals("PFN", StringComparison.OrdinalIgnoreCase)
                || a.ShortCode.Equals("PDBT", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Cover_letter_addressee_ranks_MSRV_not_residence()
    {
        var ranked = ScanSurroundPlaceholderPattern.Rank(
            "Türkmenistanyň Döwlet migrasiýa gullugynyň Aşgabat şäheri boýunça müdirliginiň müdirine",
            null,
            null,
            Set(),
            UserReportPlaceholderScope.Header);

        Assert.Equal("MSRV", ranked[0].ShortCode);
        Assert.True(ranked[0].ScorePercent >= ScanSurroundPlaceholderPattern.NearbyMinScore);
        Assert.DoesNotContain(
            ranked.Take(2),
            a => a.ShortCode.Equals("ADRS", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Cover_letter_branch_director_title_ranks_ACPOS()
    {
        var ranked = ScanSurroundPlaceholderPattern.Rank(
            "Türkmenistandaky şahamçasynyň müdiri",
            null,
            null,
            Set(),
            UserReportPlaceholderScope.Header);

        Assert.Equal("ACPOS", ranked[0].ShortCode);
        Assert.DoesNotContain(
            ranked.Take(2),
            a => a.ShortCode.Equals("ACADR", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Cover_letter_signatory_name_after_title_ranks_CHFN_not_roster()
    {
        var ranked = ScanSurroundPlaceholderPattern.Rank(
            "Mehmet Çırak",
            "Türkmenistandaky şahamçasynyň müdiri",
            null,
            Set(),
            UserReportPlaceholderScope.Header);

        Assert.Contains(ranked[0].ShortCode, new[] { "CHFN", "ACFNM" }, StringComparer.OrdinalIgnoreCase);
        Assert.NotEqual("ACPOS", ranked[0].ShortCode);
        Assert.NotEqual("PFN", ranked[0].ShortCode);
    }

    [Fact]
    public void Attachment_count_digit_does_not_steal_ACPOS_from_director_title_nearby()
    {
        var ranked = ScanSurroundPlaceholderPattern.Rank(
            "1",
            "Türkmenistandaky şahamçasynyň müdiri",
            null,
            Set(),
            UserReportPlaceholderScope.Header);

        Assert.DoesNotContain(
            ranked,
            a => a.ShortCode.Equals("ACPOS", StringComparison.OrdinalIgnoreCase)
                && a.ScorePercent >= ScanSurroundPlaceholderPattern.NearbyMinScore);
    }

    [Fact]
    public void Cover_letter_footer_maps_count_title_and_name_without_shifting_ACPOS()
    {
        var bytes = CreateGoshundyThenSignatoryWord();
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Word);
        Assert.True(yellows.Count >= 3);

        var plan = ScanOfficeFieldPlanBuilder.Build(yellows, Set(), bytes, ScanSourceKind.Word);
        var byLabel = plan.Fields.ToDictionary(
            f => f.LabelText?.Trim() ?? string.Empty,
            f => TemplateTokenSyntax.TryGetShortCode(f.ProposedToken ?? string.Empty, out var code)
                ? code
                : null,
            StringComparer.Ordinal);

        Assert.True(byLabel.ContainsKey("1"));
        Assert.NotEqual("ACPOS", byLabel["1"]);
        Assert.Equal("ACPOS", byLabel["Türkmenistandaky şahamçasynyň müdiri"]);
        Assert.Contains(byLabel["Mehmet Çırak"], new[] { "CHFN", "ACFNM" }, StringComparer.OrdinalIgnoreCase);
    }

    private static byte[] CreateGoshundyThenSignatoryWord()
    {
        using var stream = new MemoryStream();
        using (var document = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Create(
                   stream,
                   DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            var yellow = new DocumentFormat.OpenXml.Wordprocessing.Highlight
            {
                Val = DocumentFormat.OpenXml.Wordprocessing.HighlightColorValues.Yellow,
            };
            main.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(
                new DocumentFormat.OpenXml.Wordprocessing.Body(
                    new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
                        new DocumentFormat.OpenXml.Wordprocessing.Run(
                            new DocumentFormat.OpenXml.Wordprocessing.Text("Passportlaryň göçürmeleri – ")),
                        new DocumentFormat.OpenXml.Wordprocessing.Run(
                            new DocumentFormat.OpenXml.Wordprocessing.RunProperties(
                                (DocumentFormat.OpenXml.Wordprocessing.Highlight)yellow.CloneNode(true)),
                            new DocumentFormat.OpenXml.Wordprocessing.Text("1")),
                        new DocumentFormat.OpenXml.Wordprocessing.Run(
                            new DocumentFormat.OpenXml.Wordprocessing.Text(" (bir) sah."))),
                    new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
                        new DocumentFormat.OpenXml.Wordprocessing.Run(
                            new DocumentFormat.OpenXml.Wordprocessing.RunProperties(
                                (DocumentFormat.OpenXml.Wordprocessing.Highlight)yellow.CloneNode(true)),
                            new DocumentFormat.OpenXml.Wordprocessing.Text(
                                "Türkmenistandaky şahamçasynyň müdiri"))),
                    new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
                        new DocumentFormat.OpenXml.Wordprocessing.Run(
                            new DocumentFormat.OpenXml.Wordprocessing.RunProperties(
                                (DocumentFormat.OpenXml.Wordprocessing.Highlight)yellow.CloneNode(true)),
                            new DocumentFormat.OpenXml.Wordprocessing.Text("Mehmet Çırak")))));
            main.Document.Save();
        }

        return stream.ToArray();
    }
}