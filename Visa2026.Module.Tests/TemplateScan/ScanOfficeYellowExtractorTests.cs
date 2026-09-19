#nullable enable

using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.IO;
using System.Linq;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanOfficeYellowExtractorTests
{
    [Fact]
    public void Extract_Word_FindsYellowHighlightRuns()
    {
        var bytes = CreateWordFixture("№ 4/-434", "literal text");
        var spans = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Word);
        Assert.Contains(spans, s => s.Text.Contains("4/-434", StringComparison.Ordinal));
        Assert.All(spans, s => Assert.IsType<DocumentRegion.WordSpan>(s.Region));
    }

    [Fact]
    public void Extract_Word_TableCellShading_CountsAsYellowWithoutRunHighlight()
    {
        var bytes = CreateWordShadedTable("TUR", "Yok");
        var spans = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Word);
        Assert.Contains(spans, s => s.Text == "TUR");
        Assert.Contains(spans, s => s.Text == "Yok");
        Assert.Equal(2, spans.Count);
    }

    [Fact]
    public void Extract_Word_SplitsDirectorTitleStuckToSignatoryName()
    {
        var bytes = CreateWordFixture("Turkmenistandaky sahamcasynyn mudiriMehmet Cirak");
        var spans = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Word);
        Assert.Contains(spans, s => s.Text.Contains("mudiri", StringComparison.OrdinalIgnoreCase)
            && !s.Text.Contains("Mehmet", StringComparison.Ordinal));
        Assert.Contains(spans, s => s.Text.Contains("Mehmet", StringComparison.Ordinal));
    }

    [Fact]
    public void Normalize_Docx_SetsOfficeSource()
    {
        var (normalizer, _, _, _) = ScanTestServiceFactory.Create();
        var bytes = CreateWordFixture("Adaty tertipde!", "body");
        var input = normalizer.Normalize(new ScanNormalizeRequest
        {
            Content = bytes,
            FileName = "letter.docx",
        });
        Assert.Equal(ScanSourceKind.Word, input.SourceKind);
        Assert.NotNull(input.OfficePackageBytes);
        Assert.True(input.IsOfficeSource);
    }

    [Fact]
    public async Task BuildAsync_WordYellow_MapsLibraryTokensWithoutVision()
    {
        var (_, _, ingest, fieldPlan) = ScanTestServiceFactory.Create();
        var set = new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile(),
                DataScope = ApplicationProfileTemplateDataScope.Both,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });

        var bytes = CreateWordFixture("№ 4/-434", "Adaty tertipde!");
        var ingested = ingest.Ingest(new ScanNormalizeRequest
        {
            Content = bytes,
            FileName = "letter.docx",
        });

        Assert.True(ingested.Suitability.CanContinue);

        var plan = await fieldPlan.BuildAsync(new ScanFieldPlanBuildRequest
        {
            Ingest = ingested,
            PlaceholderSet = set,
            ScanKind = ScanKind.FilledSample,
        });

        Assert.Equal("office-yellow", plan.Source);
        Assert.True(plan.YellowHighlightCount >= 1);
        Assert.Contains(plan.Fields, f => f.ProposedToken != null
            && TemplateTokenSyntax.TryGetShortCode(f.ProposedToken, out var c)
            && c.Equals("AFNUM", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(plan.Fields, f => f.SourceRegion is DocumentRegion.WordSpan);
    }

    [Fact]
    public void Extract_Excel_FindsYellowCells()
    {
        using var ms = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("Sheet1");
            ws.Cell("A1").Value = "header";
            ws.Cell("B2").Value = "köp gezeklik";
            ws.Cell("B2").Style.Fill.BackgroundColor = XLColor.FromArgb(255, 255, 235, 40);
            wb.SaveAs(ms);
        }

        var spans = new ScanOfficeYellowExtractor().Extract(ms.ToArray(), ScanSourceKind.Excel);
        Assert.Contains(spans, s => s.Text.Contains("köp gezeklik", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(spans, s => s.Region is DocumentRegion.ExcelCell);
    }

    [Fact]
    public void Extract_Excel_ignores_yellow_cells_on_sheets_after_the_first()
    {
        using var ms = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var first = wb.AddWorksheet("Sanaw");
            first.Cell("B5").Value = "Erol";
            first.Cell("B5").Style.Fill.BackgroundColor = XLColor.Yellow;

            var second = wb.AddWorksheet("Archive");
            second.Cell("A1").Value = "ignored";
            second.Cell("A1").Style.Fill.BackgroundColor = XLColor.Yellow;

            wb.SaveAs(ms);
        }

        var spans = new ScanOfficeYellowExtractor().Extract(ms.ToArray(), ScanSourceKind.Excel);
        Assert.Single(spans);
        Assert.Equal("Erol", spans[0].Text);
        Assert.Equal("Sanaw", ((DocumentRegion.ExcelCell)spans[0].Region).SheetName);
    }

    [Fact]
    public void Extract_Word_AddsDigitBeforeYellowCountWords()
    {
        var bytes = CreateWordCountPairParagraph();
        var spans = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Word);
        var texts = spans.Select(s => s.Text).ToList();
        Assert.Contains("1", texts);
        Assert.Contains("bir", texts);
        Assert.Contains("2", texts);
        Assert.Contains("iki", texts);
        Assert.True(texts.IndexOf("1") < texts.IndexOf("bir"));
        Assert.True(texts.IndexOf("2") < texts.IndexOf("iki"));
    }

    [Fact]
    public void Build_Word_MapsSyntheticLeadingCountDigits()
    {
        var bytes = CreateWordCountPairParagraph();
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Word);
        var set = new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile(),
                DataScope = ApplicationProfileTemplateDataScope.Both,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });

        var proposal = ScanOfficeFieldPlanBuilder.Build(yellows, set, bytes, ScanSourceKind.Word);
        var plan = new ScanFieldPlanMerger().Merge(new ScanFieldPlanMergeRequest
        {
            Proposal = proposal,
            PlaceholderSet = set,
            ScanKind = ScanKind.FilledSample,
        });

        var summary = string.Join(
            " | ",
            plan.Fields.Select(f => $"{f.LabelText}=>{f.ProposedToken}"));
        Assert.True(
            plan.Fields.Any(f => f.LabelText == "1" && f.ProposedToken == "{{ds.TPCNT}}"),
            summary);
        Assert.True(
            plan.Fields.Any(f => f.LabelText == "bir" && f.ProposedToken == "{{ds.TPCTX}}"),
            summary);
        Assert.True(
            plan.Fields.Any(f => f.LabelText == "2" && f.ProposedToken == "{{ds.BTDCNT}}"),
            summary);
        Assert.True(
            plan.Fields.Any(f => f.LabelText == "iki" && f.ProposedToken == "{{ds.BTDCTX}}"),
            summary);
    }

    [Fact]
    public void Build_Word_MapsYellowPersonThenDurationDigits()
    {
        var bytes = CreateWordCountPairParagraphYellowDigits();
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Word);
        var set = new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile(),
                DataScope = ApplicationProfileTemplateDataScope.Both,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });

        var proposal = ScanOfficeFieldPlanBuilder.Build(yellows, set, bytes, ScanSourceKind.Word);
        var plan = new ScanFieldPlanMerger().Merge(new ScanFieldPlanMergeRequest
        {
            Proposal = proposal,
            PlaceholderSet = set,
            ScanKind = ScanKind.FilledSample,
        });

        var summary = string.Join(
            " | ",
            plan.Fields.Select(f => $"{f.LabelText}=>{f.ProposedToken}"));
        Assert.True(
            plan.Fields.Any(f => f.LabelText == "1" && f.ProposedToken == "{{ds.TPCNT}}"),
            summary);
        Assert.True(
            plan.Fields.Any(f => f.LabelText == "bir" && f.ProposedToken == "{{ds.TPCTX}}"),
            summary);
        Assert.True(
            plan.Fields.Any(f => f.LabelText == "2" && f.ProposedToken == "{{ds.BTDCNT}}"),
            summary);
        Assert.True(
            plan.Fields.Any(f => f.LabelText == "iki" && f.ProposedToken == "{{ds.BTDCTX}}"),
            summary);
    }

    [Fact]
    public void Build_Word_MapsCountDigitsOnFullBusinessTripSentence()
    {
        var bytes = CreateBusinessTripLetterParagraph();
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Word);
        var set = new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile(),
                DataScope = ApplicationProfileTemplateDataScope.Both,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });

        var proposal = ScanOfficeFieldPlanBuilder.Build(yellows, set, bytes, ScanSourceKind.Word);
        var plan = new ScanFieldPlanMerger().Merge(new ScanFieldPlanMergeRequest
        {
            Proposal = proposal,
            PlaceholderSet = set,
            ScanKind = ScanKind.FilledSample,
        });

        var summary = "yellows: " + string.Join(", ", yellows.Select(y => y.Text))
            + " || fields: " + string.Join(" | ", plan.Fields.Select(f => $"{f.LabelText}=>{f.ProposedToken}"));
        Assert.True(
            plan.Fields.Any(f => f.LabelText == "15" && f.ProposedToken == "{{ds.TPCNT}}"),
            summary);
        Assert.True(
            plan.Fields.Any(f => f.LabelText == "on bäş" && f.ProposedToken == "{{ds.TPCTX}}"),
            summary);
        Assert.True(
            plan.Fields.Any(f => f.LabelText == "20" && f.ProposedToken == "{{ds.BTDCNT}}"),
            summary);
        Assert.True(
            plan.Fields.Any(f => f.LabelText == "ýigrimi" && f.ProposedToken == "{{ds.BTDCTX}}"),
            summary);
    }

    [Theory]
    [InlineData("on bäş", "ýigrimi")]
    [InlineData("on bäş)", "ýigrimi)")]
    [InlineData("(on bäş", "(ýigrimi")]
    [InlineData("15 (on bäş", "20 (ýigrimi")]
    [InlineData("15 (on bäş)", "20 (ýigrimi)")]
    [InlineData("15", "20")]
    public void Build_Word_MapsCountPairsForEveryHighlightBoundary(string personYellow, string durationYellow)
    {
        var bytes = CreateBusinessTripLetterParagraph(personYellow, durationYellow);
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Word);
        var set = new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile(),
                DataScope = ApplicationProfileTemplateDataScope.Both,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });

        var proposal = ScanOfficeFieldPlanBuilder.Build(yellows, set, bytes, ScanSourceKind.Word);
        var plan = new ScanFieldPlanMerger().Merge(new ScanFieldPlanMergeRequest
        {
            Proposal = proposal,
            PlaceholderSet = set,
            ScanKind = ScanKind.FilledSample,
        });

        var summary = "yellows: " + string.Join(", ", yellows.Select(y => $"[{y.Text}]"))
            + " || fields: " + string.Join(" | ", plan.Fields.Select(f => $"{f.LabelText}=>{f.ProposedToken}"));
        Assert.True(plan.Fields.Any(f => f.LabelText == "15" && f.ProposedToken == "{{ds.TPCNT}}"), summary);
        Assert.True(plan.Fields.Any(f => f.LabelText == "on bäş" && f.ProposedToken == "{{ds.TPCTX}}"), summary);
        Assert.True(plan.Fields.Any(f => f.LabelText == "20" && f.ProposedToken == "{{ds.BTDCNT}}"), summary);
        Assert.True(plan.Fields.Any(f => f.LabelText == "ýigrimi" && f.ProposedToken == "{{ds.BTDCTX}}"), summary);
    }

    [Fact]
    public void Build_Word_MapsCountDigitsWhenPairIsPrintedWithInnerSpaces()
    {
        var bytes = CreateBusinessTripLetterParagraphSpacedPair();
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Word);
        var set = new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile(),
                DataScope = ApplicationProfileTemplateDataScope.Both,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });

        var proposal = ScanOfficeFieldPlanBuilder.Build(yellows, set, bytes, ScanSourceKind.Word);
        var plan = new ScanFieldPlanMerger().Merge(new ScanFieldPlanMergeRequest
        {
            Proposal = proposal,
            PlaceholderSet = set,
            ScanKind = ScanKind.FilledSample,
        });

        var summary = "yellows: " + string.Join(", ", yellows.Select(y => $"[{y.Text}]"))
            + " || fields: " + string.Join(" | ", plan.Fields.Select(f => $"{f.LabelText}=>{f.ProposedToken}"));
        Assert.True(plan.Fields.Any(f => f.LabelText == "15" && f.ProposedToken == "{{ds.TPCNT}}"), summary);
        Assert.True(plan.Fields.Any(f => f.LabelText == "20" && f.ProposedToken == "{{ds.BTDCNT}}"), summary);
    }

    [Fact]
    public void Build_Word_MapsCountDigitsWhenBothDigitAndWordsAreYellow()
    {
        // Screenshot shape: yellow on "15" and "(on bäş)", and on "20" and "(ýigrimi)".
        var bytes = CreateBusinessTripLetterParagraphBothSidesYellow();
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Word);
        var set = new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile(),
                DataScope = ApplicationProfileTemplateDataScope.Both,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });

        var proposal = ScanOfficeFieldPlanBuilder.Build(yellows, set, bytes, ScanSourceKind.Word);
        var plan = new ScanFieldPlanMerger().Merge(new ScanFieldPlanMergeRequest
        {
            Proposal = proposal,
            PlaceholderSet = set,
            ScanKind = ScanKind.FilledSample,
        });

        var summary = "yellows: " + string.Join(", ", yellows.Select(y => $"[{y.Text}]"))
            + " || fields: " + string.Join(" | ", plan.Fields.Select(f => $"{f.LabelText}=>{f.ProposedToken}"));
        Assert.True(plan.Fields.Any(f => f.LabelText == "15" && f.ProposedToken == "{{ds.TPCNT}}"), summary);
        Assert.True(plan.Fields.Any(f => f.LabelText == "on bäş" && f.ProposedToken == "{{ds.TPCTX}}"), summary);
        Assert.True(plan.Fields.Any(f => f.LabelText == "20" && f.ProposedToken == "{{ds.BTDCNT}}"), summary);
        Assert.True(plan.Fields.Any(f => f.LabelText == "ýigrimi" && f.ProposedToken == "{{ds.BTDCTX}}"), summary);
    }

    [Fact]
    public void Extract_Word_LeavesUnhighlightedCountPairAlone()
    {
        var bytes = CreateBusinessTripLetterParagraph(personYellow: null, durationYellow: null);
        var spans = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Word);
        var texts = spans.Select(s => s.Text).ToList();
        Assert.DoesNotContain("15", texts);
        Assert.DoesNotContain("on bäş", texts);
        Assert.DoesNotContain("20", texts);
        Assert.DoesNotContain("ýigrimi", texts);
    }

    [Fact]
    public async Task BuildAsync_Word_MapsPersonThenDurationDigits()
    {
        var (_, _, ingest, fieldPlan) = ScanTestServiceFactory.Create();
        var set = new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile(),
                DataScope = ApplicationProfileTemplateDataScope.Both,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });

        var ingested = ingest.Ingest(new ScanNormalizeRequest
        {
            Content = CreateWordCountPairParagraph(),
            FileName = "letter.docx",
        });
        var plan = await fieldPlan.BuildAsync(new ScanFieldPlanBuildRequest
        {
            Ingest = ingested,
            PlaceholderSet = set,
            ScanKind = ScanKind.FilledSample,
            SkipAiRefinement = true,
        });

        var summary = string.Join(
            " | ",
            plan.Fields.Select(f => $"{f.LabelText}=>{f.ProposedToken}"));
        Assert.True(
            plan.Fields.Any(f => f.LabelText == "1" && f.ProposedToken == "{{ds.TPCNT}}"),
            summary);
        Assert.True(
            plan.Fields.Any(f => f.LabelText == "bir" && f.ProposedToken == "{{ds.TPCTX}}"),
            summary);
        Assert.True(
            plan.Fields.Any(f => f.LabelText == "2" && f.ProposedToken == "{{ds.BTDCNT}}"),
            summary);
        Assert.True(
            plan.Fields.Any(f => f.LabelText == "iki" && f.ProposedToken == "{{ds.BTDCTX}}"),
            summary);
    }

    public static byte[] CreateWordShadedTable(params string[] cellTexts)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            var row = new TableRow();
            foreach (var text in cellTexts)
            {
                row.AppendChild(new TableCell(
                    new TableCellProperties(
                        new Shading
                        {
                            Val = ShadingPatternValues.Clear,
                            Color = "auto",
                            Fill = "FFFF00",
                        }),
                    new Paragraph(new Run(new Text(text)))));
            }

            main.Document = new Document(new Body(new Table(row)));
            main.Document.Save();
        }

        return stream.ToArray();
    }

    [Fact]
    public void Extract_Word_FindsDashedAttachmentCountAndHeaderNumber()
    {
        var bytes = CreateWordGosundyAttachments();
        var spans = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Word);
        var texts = spans.Select(s => s.Text).ToList();
        Assert.Contains("1", texts);
        Assert.Contains("-1", texts);
        Assert.Contains(spans, s => s.Text.Contains("1/-2", StringComparison.Ordinal));
        Assert.Contains(spans, s => s.Region is DocumentRegion.WordSpan w
            && w.ParagraphAddress.StartsWith("header", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Build_Word_KeepsBothGosundyAttachmentCounts()
    {
        var bytes = CreateWordGosundyAttachments();
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Word);
        var set = new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile(),
                DataScope = ApplicationProfileTemplateDataScope.Both,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });

        var proposal = ScanOfficeFieldPlanBuilder.Build(yellows, set, bytes, ScanSourceKind.Word);
        var plan = new ScanFieldPlanMerger().Merge(new ScanFieldPlanMergeRequest
        {
            Proposal = proposal,
            PlaceholderSet = set,
            ScanKind = ScanKind.FilledSample,
        });

        var summary = string.Join(" | ", plan.Fields.Select(f => f.LabelText + "=>" + f.ProposedToken));
        var tpcnt = plan.Fields.Where(f => f.ProposedToken == "{{ds.TPCNT}}").ToList();
        Assert.True(tpcnt.Count == 1 && tpcnt[0].LabelText == "3", summary);
        Assert.Contains(plan.Fields, f => f.ProposedToken == "{{ds.AFNUM}}");
        Assert.DoesNotContain(
            plan.Fields,
            f => ScanOfficialLetterHints.LooksLikeIsolatedCountDigit(f.LabelText)
                && f.LabelText != "3"
                && f.ProposedToken == "{{ds.TPCNT}}");
        Assert.Contains(
            plan.Fields,
            f => f.LabelText == "1" && string.IsNullOrWhiteSpace(f.ProposedToken));
        Assert.Contains(
            plan.Fields,
            f => f.LabelText == "-1" && string.IsNullOrWhiteSpace(f.ProposedToken));
    }

    [Theory]
    [InlineData("Gosundy: 1. Pasport nusgalary")]
    [InlineData("pasport nusgalary – 1 sany")]
    [InlineData("rayatynyn maglumaty -1 sany")]
    public void Enclosure_lines_are_not_person_count(string text)
    {
        Assert.True(ScanOfficialLetterHints.LooksLikeEnclosureCount(text));
        Assert.False(ScanOfficialLetterHints.LooksLikePersonCountPhrase(text));
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("\u20131")]
    [InlineData("1")]
    public void IsolatedCountDigit_accepts_dashed_gosundy_counts(string text)
    {
        Assert.True(ScanOfficialLetterHints.LooksLikeIsolatedCountDigit(text));
    }

    public static byte[] CreateWordFixture(params string[] yellowPhrases)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            main.Document = new Document(new Body());
            var body = main.Document.Body!;

            foreach (var phrase in yellowPhrases)
            {
                var run = new Run(
                    new RunProperties(new Highlight { Val = HighlightColorValues.Yellow }),
                    new Text(phrase));
                body.AppendChild(new Paragraph(run));
            }

            body.AppendChild(new Paragraph(new Run(new Text("not highlighted"))));
            main.Document.Save();
        }

        return stream.ToArray();
    }

    /// <summary>Yellow sample, then the parenthetical field map on the next line (borçnama form).</summary>
    public static byte[] CreateWordWithYellowThenCaption(string leftLabel, string yellow, string caption)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            main.Document = new Document(new Body(
                new Paragraph(
                    new Run(new Text(leftLabel) { Space = SpaceProcessingModeValues.Preserve }),
                    new Run(
                        new RunProperties(new Highlight { Val = HighlightColorValues.Yellow }),
                        new Text(yellow))),
                new Paragraph(new Run(new Text(caption)))));
            main.Document.Save();
        }

        return stream.ToArray();
    }

    /// <summary>Plain caption paragraph, then a yellow sample on the next line (borçnama wekil slot).</summary>
    public static byte[] CreateWordWithCaptionThenYellow(string caption, string yellow)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            main.Document = new Document(new Body(
                new Paragraph(new Run(new Text(caption))),
                new Paragraph(
                    new Run(
                        new RunProperties(new Highlight { Val = HighlightColorValues.Yellow }),
                        new Text(yellow)))));
            main.Document.Save();
        }

        return stream.ToArray();
    }

    public static byte[] CreateWordGosundyAttachments()
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            var headerPart = main.AddNewPart<HeaderPart>();
            headerPart.Header = new Header(
                new Paragraph(
                    new Run(
                        new RunProperties(new Highlight { Val = HighlightColorValues.Yellow }),
                        new Text("№ 1/-2") { Space = SpaceProcessingModeValues.Preserve })));

            var yellow = () => new RunProperties(new Highlight { Val = HighlightColorValues.Yellow });
            var body = new Body(
                new Paragraph(
                    new Run(new Text("sanawdaky ")),
                    new Run(yellow(), new Text("3")),
                    new Run(new Text(" sany dasary yurt rayaty."))),
                new Paragraph(
                    new Run(new Text("Gosundy: 1. Pasport nusgalary – ")),
                    new Run(yellow(), new Text("1")),
                    new Run(new Text(" sany"))),
                new Paragraph(
                    new Run(new Text("2. Maglumaty ")),
                    new Run(yellow(), new Text("-1")),
                    new Run(new Text(" sany"))),
                new SectionProperties(
                    new HeaderReference
                    {
                        Id = main.GetIdOfPart(headerPart),
                        Type = HeaderFooterValues.Default,
                    }));
            main.Document = new Document(body);
            main.Document.Save();
        }

        return stream.ToArray();
    }

    /// <summary>
    /// Letter line with yellow count-words only — digits stay plain, as Word often stores them.
    /// </summary>
    public static byte[] CreateWordCountPairParagraph()
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            var yellow = new RunProperties(new Highlight { Val = HighlightColorValues.Yellow });
            main.Document = new Document(new Body(
                new Paragraph(
                    new Run(new Text("sanawdaky ") { Space = SpaceProcessingModeValues.Preserve }),
                    new Run(new Text("1")),
                    new Run(new Text(" (") { Space = SpaceProcessingModeValues.Preserve }),
                    new Run(yellow.CloneNode(true), new Text("bir")),
                    new Run(new Text(") sany daşary ýurt raýatynyň 12.02.2026-den 13.02.2026-ne çenli ") { Space = SpaceProcessingModeValues.Preserve }),
                    new Run(new Text("2")),
                    new Run(new Text(" (") { Space = SpaceProcessingModeValues.Preserve }),
                    new Run(new RunProperties(new Highlight { Val = HighlightColorValues.Yellow }), new Text("iki")),
                    new Run(new Text(") gün möhlet bilen") { Space = SpaceProcessingModeValues.Preserve }))));
            main.Document.Save();
        }

        return stream.ToArray();
    }

    /// <summary>
    /// The real İş saparyna letter sentence: officers yellow the count words, dates and
    /// region / city forms, while the count digits stay plain.
    /// </summary>
    public static byte[] CreateBusinessTripLetterParagraph(
        string? personYellow = "on bäş",
        string? durationYellow = "ýigrimi")
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            Run Plain(string text) => new(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
            Run Yellow(string text) => new(
                new RunProperties(new Highlight { Val = HighlightColorValues.Yellow }),
                new Text(text) { Space = SpaceProcessingModeValues.Preserve });

            // "15 (on bäş)" always prints the same; only the highlighted slice moves.
            IEnumerable<Run> Pair(string pair, string? yellow)
            {
                var cut = yellow == null ? -1 : pair.IndexOf(yellow, StringComparison.Ordinal);
                if (cut < 0)
                {
                    yield return Plain(pair);
                    yield break;
                }

                if (cut > 0)
                    yield return Plain(pair[..cut]);
                yield return Yellow(yellow!);
                var tail = pair[(cut + yellow!.Length)..];
                if (tail.Length > 0)
                    yield return Plain(tail);
            }

            var paragraph = new Paragraph(Plain("Hatymyzyň goşundysynda görkezilen sanawdaky "));
            foreach (var run in Pair("15 (on bäş)", personYellow))
                paragraph.AppendChild(run);
            paragraph.AppendChild(Plain(" sany daşary ýurt raýatynyň "));
            paragraph.AppendChild(Yellow("12.02.2026"));
            paragraph.AppendChild(Plain("-den "));
            paragraph.AppendChild(Yellow("30.02.2026"));
            paragraph.AppendChild(Plain("-ne çenli "));
            foreach (var run in Pair("20 (ýigrimi)", durationYellow))
                paragraph.AppendChild(run);
            paragraph.AppendChild(Plain(" gün möhlet bilen "));
            paragraph.AppendChild(Yellow("Mary welaýatynyň"));
            paragraph.AppendChild(Plain(" "));
            paragraph.AppendChild(Yellow("Mary etrabyndan"));
            paragraph.AppendChild(Plain(" "));
            paragraph.AppendChild(Yellow("Ahal welaýatynyň"));
            paragraph.AppendChild(Plain(" "));
            paragraph.AppendChild(Yellow("Akbugdaý etrabyna"));
            paragraph.AppendChild(Plain(" iş saparyna gidýändigini Size habar berýäris."));

            main.Document = new Document(new Body(paragraph));
            main.Document.Save();
        }

        return stream.ToArray();
    }

    /// <summary>Word letters often print the pair with inner spaces: <c>15 ( on bäş )</c>.</summary>
    public static byte[] CreateBusinessTripLetterParagraphSpacedPair()
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            Run Plain(string text) => new(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
            Run Yellow(string text) => new(
                new RunProperties(new Highlight { Val = HighlightColorValues.Yellow }),
                new Text(text) { Space = SpaceProcessingModeValues.Preserve });

            main.Document = new Document(new Body(
                new Paragraph(
                    Plain("Hatymyzyň goşundysynda görkezilen sanawdaky "),
                    Plain("15"),
                    Plain(" ( "),
                    Yellow("on bäş"),
                    Plain(" ) sany daşary ýurt raýatynyň "),
                    Yellow("12.02.2026"),
                    Plain("-den "),
                    Yellow("30.02.2026"),
                    Plain("-ne çenli "),
                    Plain("20"),
                    Plain(" ( "),
                    Yellow("ýigrimi"),
                    Plain(" ) gün möhlet bilen iş saparyna gidýändigini Size habar berýäris."))));
            main.Document.Save();
        }

        return stream.ToArray();
    }

    /// <summary>
    /// Officer yellow on both sides of each pair (digit + parenthetical words), as in the
    /// Yuztutma iş sapary Review screenshot.
    /// </summary>
    public static byte[] CreateBusinessTripLetterParagraphBothSidesYellow()
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            Run Plain(string text) => new(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
            Run Yellow(string text) => new(
                new RunProperties(new Highlight { Val = HighlightColorValues.Yellow }),
                new Text(text) { Space = SpaceProcessingModeValues.Preserve });

            main.Document = new Document(new Body(
                new Paragraph(
                    Plain("Hatymyzyň goşundysynda görkezilen sanawdaky "),
                    Yellow("15"),
                    Plain(" "),
                    Yellow("(on bäş)"),
                    Plain(" sany daşary ýurt raýatynyň "),
                    Yellow("12.02.2026"),
                    Plain("-den "),
                    Yellow("13.02.2026"),
                    Plain("-ne çenli "),
                    Yellow("20"),
                    Plain(" "),
                    Yellow("(ýigrimi)"),
                    Plain(" gün möhlet bilen iş saparyna gidýändigini Size habar berýäris."))));
            main.Document.Save();
        }

        return stream.ToArray();
    }

    /// <summary>Both the digit and the words are yellow, as officers often highlight.</summary>
    public static byte[] CreateWordCountPairParagraphYellowDigits()
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            main.Document = new Document(new Body(
                new Paragraph(
                    new Run(new Text("sanawdaky ") { Space = SpaceProcessingModeValues.Preserve }),
                    new Run(
                        new RunProperties(new Highlight { Val = HighlightColorValues.Yellow }),
                        new Text("1")),
                    new Run(new Text(" (") { Space = SpaceProcessingModeValues.Preserve }),
                    new Run(
                        new RunProperties(new Highlight { Val = HighlightColorValues.Yellow }),
                        new Text("bir")),
                    new Run(new Text(") sany daşary ýurt raýatynyň 12.02.2026-den 13.02.2026-ne çenli ") { Space = SpaceProcessingModeValues.Preserve }),
                    new Run(
                        new RunProperties(new Highlight { Val = HighlightColorValues.Yellow }),
                        new Text("2")),
                    new Run(new Text(" (") { Space = SpaceProcessingModeValues.Preserve }),
                    new Run(
                        new RunProperties(new Highlight { Val = HighlightColorValues.Yellow }),
                        new Text("iki")),
                    new Run(new Text(") gün möhlet bilen") { Space = SpaceProcessingModeValues.Preserve }))));
            main.Document.Save();
        }

        return stream.ToArray();
    }
}