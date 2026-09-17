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