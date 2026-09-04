using Microsoft.Extensions.Options;
using Visa2026.Module.Services.TemplateConvert;
using Xunit;

namespace Visa2026.Module.Tests.TemplateConvert;

public class TemplateCandidateAnalyzerTests
{
    private static ITemplateCandidateAnalyzer Analyzer(TemplateSuitabilityOptions? options = null) =>
        new TemplateCandidateAnalyzer(Options.Create(options ?? new TemplateSuitabilityOptions()));

    private static ApplicationProfileInstanceValueMap Map(params (string ShortCode, string Value, int? RowIndex)[] values)
    {
        var candidates = values
            .Select(v =>
            {
                var kind = TemplateValueMatchKeys.Classify(v.ShortCode, v.Value);
                var keys = TemplateValueMatchKeys.Build(v.Value, kind);
                var token = v.RowIndex == null ? $"{{{{ds.{v.ShortCode}}}}}" : $"{{{{.{v.ShortCode}}}}}";
                return new ValueCandidate(v.ShortCode, token, v.Value, keys[0], kind, v.RowIndex, keys);
            })
            .ToList();

        return new ApplicationProfileInstanceValueMap
        {
            ApplicationProfileInstanceId = Guid.NewGuid(),
            Header = new Dictionary<string, string?>(),
            Rows = [],
            Candidates = candidates,
            Rejected = [],
        };
    }

    private static TemplateCandidateReport AnalyzeWord(
        byte[] content,
        ApplicationProfileInstanceValueMap map,
        TemplateSuitabilityOptions? options = null) =>
        Analyzer(options).Analyze(new TemplateCandidateRequest
        {
            Content = content,
            Format = TemplateSourceFormat.Docx,
            ValueMap = map,
        });

    /// <summary>Six header fields, enough to pass outright under E-D6.</summary>
    private static (byte[] Content, ApplicationProfileInstanceValueMap Map) PassingLetter()
    {
        var content = TemplateConvertFixtures.CreateWordDocument(
            ["Arza belgisi: TRM-2026-120"],
            ["Senesi: 20.01.2026"],
            ["Kärhana: Çalyk Enerji"],
            ["Gol çekiji: Mehmet Cirak"],
            ["Wiza möhleti: alty aý"],
            ["Wiza kategoriýasy: kop gezeklik"]);

        var map = Map(
            ("AFNUM", "TRM-2026-120", null),
            ("ADAT", "20.01.2026", null),
            ("ACNAM", "Çalyk Enerji", null),
            ("ACFNM", "Mehmet Cirak", null),
            ("VPER", "alty aý", null),
            ("VCAT", "kop gezeklik", null));

        return (content, map);
    }

    [Fact]
    public void A_matched_value_is_highlighted_with_its_token()
    {
        var content = TemplateConvertFixtures.CreateWordDocument(["Işgär: Dowletmyrat Amanov"]);
        var report = AnalyzeWord(content, Map(("PFN", "Dowletmyrat Amanov", 0)));

        var highlight = Assert.Single(report.Highlights.Where(h => h.Kind == HighlightKind.Match));
        Assert.Equal("{{.PFN}}", highlight.Token);
        Assert.Equal("Dowletmyrat Amanov", highlight.MatchedText);
    }

    /// <summary>The span must address the original text, which the token writer then edits.</summary>
    [Fact]
    public void Highlight_spans_survive_whitespace_collapsing()
    {
        const string paragraph = "Işgär:    Dowletmyrat  Amanov  (pasport)";
        var content = TemplateConvertFixtures.CreateWordDocument([paragraph]);

        var report = AnalyzeWord(content, Map(("PFN", "Dowletmyrat Amanov", 0)));

        var span = Assert.IsType<DocumentRegion.WordSpan>(
            report.Highlights.Single(h => h.Kind == HighlightKind.Match).Region);
        Assert.Equal("Dowletmyrat  Amanov", paragraph.Substring(span.Start, span.Length));
    }

    [Fact]
    public void Matching_ignores_turkmen_diacritics()
    {
        var content = TemplateConvertFixtures.CreateWordDocument(["Işgär: Aynabat Sirmedowa"]);

        var report = AnalyzeWord(content, Map(("PFN", "Aýnabat Şirmedowa", 0)));

        Assert.Contains(report.Highlights, h => h.ShortCode == "PFN");
    }

    [Fact]
    public void Identifiers_match_across_separator_differences()
    {
        var content = TemplateConvertFixtures.CreateWordDocument(["Pasport: U 3655-6957"]);

        var report = AnalyzeWord(content, Map(("PPN", "U36556957", 0)));

        Assert.Contains(report.Highlights, h => h.ShortCode == "PPN" && h.Kind == HighlightKind.Match);
    }

    /// <summary>A surname sits inside the full name; the longer match has to win.</summary>
    [Fact]
    public void Overlapping_matches_resolve_to_the_longest()
    {
        var content = TemplateConvertFixtures.CreateWordDocument(["Işgär: Dowletmyrat Amanov"]);

        var report = AnalyzeWord(content, Map(
            ("PFN", "Dowletmyrat Amanov", 0),
            ("PLN", "Amanov", 0)));

        var match = Assert.Single(report.Highlights.Where(h => h.Kind == HighlightKind.Match));
        Assert.Equal("PFN", match.ShortCode);
    }

    [Fact]
    public void A_document_with_no_case_data_fails()
    {
        var content = TemplateConvertFixtures.CreateWordDocument(["Umumy düzgünler we şertler."]);

        var report = AnalyzeWord(content, Map(("PFN", "Dowletmyrat Amanov", 0)));

        Assert.Equal(SuitabilityLevel.Fail, report.Level);
        Assert.False(report.CanConvert);
        Assert.Contains(report.Reasons, r => r.Code == SuitabilityReasonCode.NoInstanceMatches);
    }

    [Fact]
    public void Too_few_header_matches_without_a_roster_fails()
    {
        var content = TemplateConvertFixtures.CreateWordDocument(
            ["Arza belgisi: TRM-2026-120"],
            ["Senesi: 20.01.2026"]);

        var report = AnalyzeWord(content, Map(
            ("AFNUM", "TRM-2026-120", null),
            ("ADAT", "20.01.2026", null)));

        Assert.Equal(SuitabilityLevel.Fail, report.Level);
        Assert.Contains(report.Reasons, r => r.Code == SuitabilityReasonCode.TooFewHeaderMatches);
    }

    [Fact]
    public void Three_to_five_header_matches_warn()
    {
        var content = TemplateConvertFixtures.CreateWordDocument(
            ["Arza belgisi: TRM-2026-120"],
            ["Senesi: 20.01.2026"],
            ["Kärhana: Çalyk Enerji"]);

        var report = AnalyzeWord(content, Map(
            ("AFNUM", "TRM-2026-120", null),
            ("ADAT", "20.01.2026", null),
            ("ACNAM", "Çalyk Enerji", null)));

        Assert.Equal(SuitabilityLevel.Warn, report.Level);
        Assert.True(report.CanConvert);
        Assert.True(report.RequiresWarningAcknowledgement);
        Assert.Equal(3, report.DistinctHeaderMatches);
    }

    [Fact]
    public void Six_header_matches_pass()
    {
        var (content, map) = PassingLetter();

        var report = AnalyzeWord(content, map);

        Assert.Equal(SuitabilityLevel.Pass, report.Level);
        Assert.False(report.RequiresWarningAcknowledgement);
        Assert.Equal(6, report.DistinctHeaderMatches);
    }

    /// <summary>Values repeating over two roster rows imply a table that can carry a loop.</summary>
    [Fact]
    public void Two_matched_roster_rows_plus_two_header_fields_pass()
    {
        var content = TemplateConvertFixtures.CreateWordDocument(
            ["Arza belgisi: TRM-2026-120"],
            ["Senesi: 20.01.2026"],
            ["1. Dowletmyrat Amanov"],
            ["2. Aynabat Meredowa"]);

        var report = AnalyzeWord(content, Map(
            ("AFNUM", "TRM-2026-120", null),
            ("ADAT", "20.01.2026", null),
            ("PFN", "Dowletmyrat Amanov", 0),
            ("PFN", "Aynabat Meredowa", 1)));

        Assert.True(report.RosterLoopDetected);
        Assert.Equal(SuitabilityLevel.Pass, report.Level);
        Assert.Contains(report.Reasons, r => r.Code == SuitabilityReasonCode.RosterLoopDetected);
    }

    [Fact]
    public void One_matched_roster_row_is_not_a_loop()
    {
        var content = TemplateConvertFixtures.CreateWordDocument(["1. Dowletmyrat Amanov"]);

        var report = AnalyzeWord(content, Map(("PFN", "Dowletmyrat Amanov", 0)));

        Assert.False(report.RosterLoopDetected);
        Assert.Equal(SuitabilityLevel.Fail, report.Level);
    }

    [Fact]
    public void Thresholds_come_from_configuration()
    {
        var content = TemplateConvertFixtures.CreateWordDocument(
            ["Arza belgisi: TRM-2026-120"],
            ["Senesi: 20.01.2026"]);

        var map = Map(("AFNUM", "TRM-2026-120", null), ("ADAT", "20.01.2026", null));

        Assert.Equal(SuitabilityLevel.Fail, AnalyzeWord(content, map).Level);

        var relaxed = new TemplateSuitabilityOptions
        {
            MinHeaderMatchesToProceed = 2,
            MinHeaderMatchesForPass = 2,
        };

        Assert.Equal(SuitabilityLevel.Pass, AnalyzeWord(content, map, relaxed).Level);
    }

    [Fact]
    public void An_unreadable_file_fails_with_an_explanation()
    {
        var report = AnalyzeWord([1, 2, 3, 4], Map(("PFN", "Dowletmyrat Amanov", 0)));

        Assert.Equal(SuitabilityLevel.Fail, report.Level);
        Assert.Contains(report.Reasons, r => r.Code == SuitabilityReasonCode.Unreadable);
    }

    [Fact]
    public void A_document_with_no_text_fails()
    {
        var report = AnalyzeWord(TemplateConvertFixtures.CreateWordDocument([" "]), Map(("PFN", "Amanov Dowlet", 0)));

        Assert.Equal(SuitabilityLevel.Fail, report.Level);
        Assert.Contains(report.Reasons, r => r.Code == SuitabilityReasonCode.NoExtractableText);
    }

    /// <summary>An already-tokenized file converts, but the officer is warned about duplicates.</summary>
    [Fact]
    public void An_already_tokenized_file_is_demoted_to_warn()
    {
        var (content, map) = PassingLetter();
        Assert.Equal(SuitabilityLevel.Pass, AnalyzeWord(content, map).Level);

        var tokenized = TemplateConvertFixtures.CreateWordDocument(
            ["Arza belgisi: TRM-2026-120"],
            ["Senesi: 20.01.2026"],
            ["Kärhana: Çalyk Enerji"],
            ["Gol çekiji: Mehmet Cirak"],
            ["Wiza möhleti: alty aý"],
            ["Wiza kategoriýasy: kop gezeklik"],
            ["Işgär: {{.PFN}}"]);

        var report = AnalyzeWord(tokenized, map);

        Assert.Equal(SuitabilityLevel.Warn, report.Level);
        Assert.Contains(report.Reasons, r => r.Code == SuitabilityReasonCode.AlreadyTokenized);
    }

    [Fact]
    public void Unmatched_dates_are_reported_as_gaps()
    {
        var (_, map) = PassingLetter();

        var content = TemplateConvertFixtures.CreateWordDocument(
            ["Arza belgisi: TRM-2026-120"],
            ["Senesi: 20.01.2026"],
            ["Kärhana: Çalyk Enerji"],
            ["Gol çekiji: Mehmet Cirak"],
            ["Wiza möhleti: alty aý"],
            ["Wiza kategoriýasy: kop gezeklik"],
            ["Şertnama gutarýan senesi: 31.12.2030"]);

        var report = AnalyzeWord(content, map);

        Assert.Equal(1, report.GapCount);
        Assert.Contains(report.Highlights, h => h.Kind == HighlightKind.Gap && h.MatchedText == "31.12.2030");
        Assert.Contains(report.Reasons, r => r.Code == SuitabilityReasonCode.GapsPresent);
    }

    [Fact]
    public void A_matched_date_is_not_also_a_gap()
    {
        var (content, map) = PassingLetter();

        var report = AnalyzeWord(content, map);

        Assert.Equal(0, report.GapCount);
    }

    [Fact]
    public void Excel_matches_address_the_whole_cell()
    {
        var content = TemplateConvertFixtures.CreateExcelSheet(
            "Sanaw",
            ("A1", "Familiýasy, ady"),
            ("A2", "Meredowa Aynabat"),
            ("B2", "T 1234567"));

        var report = Analyzer().Analyze(new TemplateCandidateRequest
        {
            Content = content,
            Format = TemplateSourceFormat.Xlsx,
            ValueMap = Map(("PFN", "Meredowa Aynabat", 0), ("PPN", "T1234567", 0)),
        });

        var cells = report.Highlights
            .Where(h => h.Kind == HighlightKind.Match)
            .Select(h => Assert.IsType<DocumentRegion.ExcelCell>(h.Region))
            .ToList();

        Assert.Equal(2, cells.Count);
        Assert.Contains(cells, c => c.SheetName == "Sanaw" && c.CellReference == "A2");
        Assert.Contains(cells, c => c.SheetName == "Sanaw" && c.CellReference == "B2");
    }
}
