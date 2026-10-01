#nullable enable

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanWordTableHeaderTests
{
    [Fact]
    public void MapForYellows_reads_header_above_data_cell_not_previous_cell()
    {
        var bytes = SanawTable();
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Word);
        var ozer = Assert.Single(yellows, y => y.Text == "Ozer");
        var index = yellows.ToList().FindIndex(y => y.Text == "Ozer");
        var map = ScanWordTableHeader.MapForYellows(bytes, yellows);
        Assert.True(map.TryGetValue(index, out var header));
        Assert.Contains("Familiyasy", header, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual(ozer.Text, header);
    }

    [Fact]
    public void Build_maps_word_sanaw_names_and_birth_from_column_headers()
    {
        var bytes = SanawTable();
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Word);
        var set = PlaceholderSet();
        var proposal = ScanOfficeFieldPlanBuilder.Build(yellows, set, bytes, ScanSourceKind.Word);

        var ozer = Assert.Single(proposal.Fields, f => f.LabelText == "Ozer");
        Assert.Contains("PLN", ozer.ProposedToken, StringComparison.Ordinal);
        Assert.Equal(ScanFieldScope.Row, ozer.Scope);

        var arita = Assert.Single(proposal.Fields, f => f.LabelText == "Arita");
        Assert.Contains("PFNM", arita.ProposedToken, StringComparison.Ordinal);

        var birth = Assert.Single(proposal.Fields, f => f.LabelText.Contains("27.06.1981", StringComparison.Ordinal));
        var codes = TemplateTokenSyntax.GetShortCodes(birth.ProposedToken);
        Assert.Equal(["PDBT", "PCBT", "PBPL"], codes);
    }

    [Fact]
    public void Build_numbers_comma_split_roster_cell_as_one_group()
    {
        var bytes = PassportAndNameTable();
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Word);
        var set = PlaceholderSet();
        var proposal = ScanOfficeFieldPlanBuilder.Build(yellows, set, bytes, ScanSourceKind.Word);

        var passport = Assert.Single(proposal.Fields, f =>
            f.LabelText.Contains("S36133641", StringComparison.Ordinal));
        Assert.Contains(',', passport.LabelText);
        var codes = TemplateTokenSyntax.GetShortCodes(passport.ProposedToken);
        Assert.Equal(["PPN", "PPED"], codes);

        var ordered = ScanReviewFieldOrder.Order(proposal.Fields.Select(ToDetected).ToList());
        var parts = ordered.Where(o => o.LabelText.Contains("S36133641", StringComparison.Ordinal)
            || o.LabelText.Contains("15.11.2023", StringComparison.Ordinal)).ToList();
        Assert.Equal(["1.1", "1.2"], parts.Select(o => o.DisplayOrder).ToArray());
        Assert.Equal(["S36133641", "15.11.2023"], parts.Select(o => o.LabelText).ToArray());
        Assert.Equal(
            ["PPN", "PPED"],
            parts.Select(o => TemplateTokenSyntax.GetShortCodes(o.ProposedToken).Single()).ToArray());

        var name = Assert.Single(ordered, o => o.LabelText == "Ozer");
        Assert.Equal("2", name.DisplayOrder);
        Assert.DoesNotContain('.', name.DisplayOrder);
    }

    private static ApplicationProfilePlaceholderSet PlaceholderSet() =>
        new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile
                {
                    RequirePersonPassport = true,
                    RequirePersonEducation = true,
                    RequirePersonPosition = true,
                    RequirePersonAddressOfResidence = true,
                },
                DataScope = ApplicationProfileTemplateDataScope.Both,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });

    private static byte[] SanawTable()
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            var table = new Table(
                new TableGrid(
                    new GridColumn { Width = "1440" },
                    new GridColumn { Width = "1440" },
                    new GridColumn { Width = "2400" }),
                HeaderRow("Familiyasy", "Ady", "Doglan senesi we yeri"),
                DataRow("Ozer", "Arita", "27.06.1981, Turkiye, Iskenderun"));
            main.Document = new Document(new Body(table));
            main.Document.Save();
        }

        return stream.ToArray();
    }

    private static TableRow HeaderRow(params string[] cells)
    {
        var row = new TableRow();
        foreach (var text in cells)
            row.AppendChild(new TableCell(new Paragraph(new Run(new Text(text)))));
        return row;
    }

    private static ScanDetectedField ToDetected(ScanDetectedFieldDraft draft) =>
        new()
        {
            FieldId = draft.FieldId,
            Box = draft.Box,
            PageIndex = draft.PageIndex,
            LabelText = draft.LabelText,
            ProposedToken = draft.ProposedToken,
            Confidence = draft.Confidence,
            Scope = draft.Scope,
            SourceRegion = draft.SourceRegion,
            Alternatives = draft.Alternatives,
        };

    private static byte[] PassportAndNameTable()
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            var passport = new Paragraph(
                new Run(
                    new RunProperties(new Highlight { Val = HighlightColorValues.Yellow }),
                    new Text("S36133641") { Space = SpaceProcessingModeValues.Preserve }),
                new Run(new Text(", ") { Space = SpaceProcessingModeValues.Preserve }),
                new Run(
                    new RunProperties(new Highlight { Val = HighlightColorValues.Yellow }),
                    new Text("15.11.2023") { Space = SpaceProcessingModeValues.Preserve }));
            var name = new Paragraph(new Run(
                new RunProperties(new Highlight { Val = HighlightColorValues.Yellow }),
                new Text("Ozer")));
            var table = new Table(
                new TableRow(
                    new TableCell(new Paragraph(new Run(new Text("Pasport belgisi we möhleti")))),
                    new TableCell(new Paragraph(new Run(new Text("Familiýasy"))))),
                new TableRow(
                    new TableCell(passport),
                    new TableCell(name)));
            main.Document = new Document(new Body(table));
            main.Document.Save();
        }

        return stream.ToArray();
    }

    private static TableRow DataRow(params string[] cells)
    {
        var row = new TableRow();
        foreach (var text in cells)
        {
            row.AppendChild(new TableCell(new Paragraph(
                new Run(
                    new RunProperties(new Highlight { Val = HighlightColorValues.Yellow }),
                    new Text(text)))));
        }

        return row;
    }
}