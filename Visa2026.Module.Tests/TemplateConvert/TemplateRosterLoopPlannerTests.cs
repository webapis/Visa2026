using ClosedXML.Excel;
using Visa2026.Module.Services.TemplateConvert;
using Xunit;

namespace Visa2026.Module.Tests.TemplateConvert;

public class TemplateRosterLoopPlannerTests
{
    private static TemplateCandidateReport Report(
        bool rosterLoop,
        params HighlightRegion[] highlights) =>
        new()
        {
            Level = SuitabilityLevel.Pass,
            Reasons = Array.Empty<SuitabilityReason>(),
            Highlights = highlights,
            RosterLoopDetected = rosterLoop,
            DistinctHeaderMatches = highlights.Count(h => h.Kind == HighlightKind.Match && h.RowIndex == null),
            DistinctRowMatches = highlights.Count(h => h.Kind == HighlightKind.Match && h.RowIndex != null),
            GapCount = highlights.Count(h => h.Kind == HighlightKind.Gap),
        };

    [Fact]
    public void Header_only_plan_has_no_loops()
    {
        var report = Report(
            rosterLoop: false,
            new HighlightRegion(
                new DocumentRegion.WordSpan("body/0", 0, 5),
                HighlightKind.Match,
                "TRM-1",
                "{{ds.AFNUM}}",
                "AFNUM",
                RowIndex: null));

        var plan = TemplateRosterLoopPlanner.Build(report, TemplateSourceFormat.Docx);

        Assert.Single(plan.Substitutions);
        Assert.Empty(plan.Loops);
    }

    [Fact]
    public void Word_roster_wraps_first_and_last_person_paragraphs()
    {
        var report = Report(
            rosterLoop: true,
            new HighlightRegion(
                new DocumentRegion.WordSpan("body/0", 0, 4),
                HighlightKind.Match,
                "TRM-1",
                "{{ds.AFNUM}}",
                "AFNUM",
                RowIndex: null),
            new HighlightRegion(
                new DocumentRegion.WordSpan("body/1", 0, 10),
                HighlightKind.Match,
                "Person One",
                "{{.PFN}}",
                "PFN",
                RowIndex: 0),
            new HighlightRegion(
                new DocumentRegion.WordSpan("body/2", 0, 10),
                HighlightKind.Match,
                "Person Two",
                "{{.PFN}}",
                "PFN",
                RowIndex: 1));

        var plan = TemplateRosterLoopPlanner.Build(report, TemplateSourceFormat.Docx);

        Assert.Equal(2, plan.Substitutions.Count);
        Assert.DoesNotContain(plan.Substitutions, s => s.Region is DocumentRegion.WordSpan { ParagraphAddress: "body/2" });
        var loop = Assert.Single(plan.Loops);
        Assert.Equal("ds.rows", loop.CollectionToken);
        Assert.Equal("body/1", Assert.IsType<DocumentRegion.WordSpan>(loop.Start).ParagraphAddress);
        Assert.Equal("body/2", Assert.IsType<DocumentRegion.WordSpan>(loop.End).ParagraphAddress);
    }

    [Fact]
    public void Excel_roster_places_markers_beside_the_prototype_row()
    {
        var report = Report(
            rosterLoop: true,
            new HighlightRegion(
                new DocumentRegion.ExcelCell("Sanaw", "A2"),
                HighlightKind.Match,
                "Person One",
                "{{.PFN}}",
                "PFN",
                RowIndex: 0),
            new HighlightRegion(
                new DocumentRegion.ExcelCell("Sanaw", "B2"),
                HighlightKind.Match,
                "AA111",
                "{{.PPN}}",
                "PPN",
                RowIndex: 0),
            new HighlightRegion(
                new DocumentRegion.ExcelCell("Sanaw", "A3"),
                HighlightKind.Match,
                "Person Two",
                "{{.PFN}}",
                "PFN",
                RowIndex: 1));

        var plan = TemplateRosterLoopPlanner.Build(report, TemplateSourceFormat.Xlsx);

        Assert.Equal(2, plan.Substitutions.Count);
        Assert.All(plan.Substitutions, s =>
            Assert.Equal("2", Assert.IsType<DocumentRegion.ExcelCell>(s.Region).CellReference[^1..]));

        var loop = Assert.Single(plan.Loops);
        var start = Assert.IsType<DocumentRegion.ExcelCell>(loop.Start);
        var end = Assert.IsType<DocumentRegion.ExcelCell>(loop.End);
        Assert.Equal("Sanaw", start.SheetName);
        Assert.Equal("A2", start.CellReference);
        Assert.Equal("A3", end.CellReference);
        Assert.Equal("{{#ds.rows}}", TemplateTokenSyntax.LoopOpen(loop.CollectionToken));
        Assert.Equal("{{/ds.rows}}", TemplateTokenSyntax.LoopClose(loop.CollectionToken));
    }

    [Fact]
    public void Excel_and_word_writers_accept_the_planned_loops()
    {
        var writer = new TemplateTokenWriter();

        var wordReport = Report(
            rosterLoop: true,
            new HighlightRegion(
                new DocumentRegion.WordSpan("body/0", 0, 10),
                HighlightKind.Match,
                "Person One",
                "{{.PFN}}",
                "PFN",
                RowIndex: 0),
            new HighlightRegion(
                new DocumentRegion.WordSpan("body/1", 0, 10),
                HighlightKind.Match,
                "Person Two",
                "{{.PFN}}",
                "PFN",
                RowIndex: 1));

        var wordContent = TemplateConvertFixtures.CreateWordDocument(
            new[] { "Person One" },
            new[] { "Person Two" });
        var wordPlan = TemplateRosterLoopPlanner.Build(wordReport, TemplateSourceFormat.Docx);
        var wordResult = writer.Apply(new TemplateTokenWriteRequest
        {
            SourceContent = wordContent,
            Format = TemplateSourceFormat.Docx,
            Substitutions = wordPlan.Substitutions,
            Loops = wordPlan.Loops,
        });

        Assert.Empty(wordResult.Skipped);
        Assert.Equal("{{#ds.rows}}{{.PFN}}", TemplateConvertFixtures.GetParagraphText(wordResult.Content, "body/0"));
        Assert.Equal("Person Two{{/ds.rows}}", TemplateConvertFixtures.GetParagraphText(wordResult.Content, "body/1"));

        var excelReport = Report(
            rosterLoop: true,
            new HighlightRegion(
                new DocumentRegion.ExcelCell("Sanaw", "A2"),
                HighlightKind.Match,
                "Person One",
                "{{.PFN}}",
                "PFN",
                RowIndex: 0),
            new HighlightRegion(
                new DocumentRegion.ExcelCell("Sanaw", "A3"),
                HighlightKind.Match,
                "Person Two",
                "{{.PFN}}",
                "PFN",
                RowIndex: 1));

        var excelContent = TemplateConvertFixtures.CreateExcelSheet(
            "Sanaw",
            ("A2", "Person One"),
            ("A3", "Person Two"));
        var excelPlan = TemplateRosterLoopPlanner.Build(excelReport, TemplateSourceFormat.Xlsx);
        var excelResult = writer.Apply(new TemplateTokenWriteRequest
        {
            SourceContent = excelContent,
            Format = TemplateSourceFormat.Xlsx,
            Substitutions = excelPlan.Substitutions,
            Loops = excelPlan.Loops,
        });

        Assert.Empty(excelResult.Skipped);
        Assert.Equal("{{#ds.rows}}{{.PFN}}", TemplateConvertFixtures.GetCellText(excelResult.Content, "Sanaw", "A2"));
        Assert.Equal("{{/ds.rows}}", TemplateConvertFixtures.GetCellText(excelResult.Content, "Sanaw", "B3"));
        Assert.Equal("Person Two", TemplateConvertFixtures.GetCellText(excelResult.Content, "Sanaw", "A3"));
    }

    [Fact]
    public void PlanExcelLoopsFromSubstitutions_places_rows_loop_for_scan_row_tokens()
    {
        var subs = new List<TokenSubstitution>
        {
            new(new DocumentRegion.ExcelCell("Sanaw", "B5"), "{{.PLN}}"),
            new(new DocumentRegion.ExcelCell("Sanaw", "C5"), "{{.PFNM}}"),
        };

        var loops = TemplateRosterLoopPlanner.PlanExcelLoopsFromSubstitutions(subs);
        var loop = Assert.Single(loops);
        Assert.Equal("A5", ((DocumentRegion.ExcelCell)loop.Start).CellReference);
        Assert.Equal("A6", ((DocumentRegion.ExcelCell)loop.End).CellReference);
        Assert.Equal("{{#ds.rows}}", TemplateTokenSyntax.LoopOpen(loop.CollectionToken));
    }

    [Fact]
    public void PlanExcelLoopsFromSubstitutions_keeps_loop_on_A_when_RNUM_occupies_A()
    {
        var subs = new List<TokenSubstitution>
        {
            new(new DocumentRegion.ExcelCell("Sanaw", "A5"), "{{.RNUM}}"),
            new(new DocumentRegion.ExcelCell("Sanaw", "B5"), "{{.PLN}}"),
            new(new DocumentRegion.ExcelCell("Sanaw", "C5"), "{{.PFNM}}"),
        };

        var loops = TemplateRosterLoopPlanner.PlanExcelLoopsFromSubstitutions(subs);
        var loop = Assert.Single(loops);
        Assert.Equal("A5", ((DocumentRegion.ExcelCell)loop.Start).CellReference);
        Assert.Equal("A6", ((DocumentRegion.ExcelCell)loop.End).CellReference);
    }

    [Fact]
    public void PlanExcelLoopsFromSubstitutions_skips_merged_cells_when_workbook_provided()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Sanaw");
        sheet.Cell("A5").Value = "x";
        sheet.Range("A5:A6").Merge();
        sheet.Range("J5:K5").Merge();
        sheet.Cell("J5").Value = "position";
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        var bytes = ms.ToArray();

        var subs = new List<TokenSubstitution>
        {
            new(new DocumentRegion.ExcelCell("Sanaw", "B5"), "{{.PLN}}"),
            new(new DocumentRegion.ExcelCell("Sanaw", "J5"), "{{.POSN}}"),
        };

        var loops = TemplateRosterLoopPlanner.PlanExcelLoopsFromSubstitutions(subs, bytes);
        var loop = Assert.Single(loops);
        // Column A merged → prepend onto leftmost occupied data column (B).
        Assert.Equal("B5", ((DocumentRegion.ExcelCell)loop.Start).CellReference);
        Assert.Equal("B6", ((DocumentRegion.ExcelCell)loop.End).CellReference);
    }
}