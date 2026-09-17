#nullable enable

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.Services.UserReports;

public class WordScanTableRowExpanderTests
{
    [Fact]
    public void Expand_clones_prototype_row_for_each_person()
    {
        var bytes = TokenTable();
        var rows = new List<IDictionary<string, object>>
        {
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["PLN"] = "Alkan",
                ["PFNM"] = "Cafer",
                ["PNAT"] = "TUR",
            },
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["PLN"] = "Erol",
                ["PFNM"] = "Hilmi",
                ["PNAT"] = "TUR",
            },
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["PLN"] = "Demirci",
                ["PFNM"] = "Omer",
                ["PNAT"] = "TUR",
            },
        };

        var expanded = WordScanTableRowExpander.ExpandPrototypeTableRow(bytes, rows);
        using var stream = new MemoryStream(expanded);
        using var document = WordprocessingDocument.Open(stream, false);
        var table = document.MainDocumentPart!.Document.Body!.Descendants<Table>().Single();
        var dataRows = table.Elements<TableRow>().Skip(1).ToList();
        Assert.Equal(3, dataRows.Count);
        Assert.Contains("Alkan", CellText(dataRows[0], 0), StringComparison.Ordinal);
        Assert.Contains("Erol", CellText(dataRows[1], 0), StringComparison.Ordinal);
        Assert.Contains("Demirci", CellText(dataRows[2], 0), StringComparison.Ordinal);
        Assert.DoesNotContain("{{.", document.MainDocumentPart.Document.Body.InnerText);
        Assert.Contains("{{ds.ACPOS}}", document.MainDocumentPart.Document.Body.InnerText, StringComparison.Ordinal);
    }

    [Fact]
    public void Expand_clones_seeded_table_row_loop()
    {
        var bytes = TokenTable(includeLoop: true);
        var rows = new List<IDictionary<string, object>>
        {
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { ["PLN"] = "Alkan" },
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { ["PLN"] = "Erol" },
        };

        var expanded = WordScanTableRowExpander.ExpandPrototypeTableRow(bytes, rows);
        using var stream = new MemoryStream(expanded);
        using var document = WordprocessingDocument.Open(stream, false);
        var table = document.MainDocumentPart!.Document.Body!.Descendants<Table>().Single();
        var dataRows = table.Elements<TableRow>().Skip(1).ToList();
        Assert.Equal(2, dataRows.Count);
        Assert.Contains("Alkan", CellText(dataRows[0], 0), StringComparison.Ordinal);
        Assert.Contains("Erol", CellText(dataRows[1], 0), StringComparison.Ordinal);
        Assert.DoesNotContain("{{#ds.rows}}", document.MainDocumentPart.Document.Body.InnerText, StringComparison.Ordinal);
        Assert.Contains("{{ds.ACPOS}}", document.MainDocumentPart.Document.Body.InnerText, StringComparison.Ordinal);
    }

    [Fact]
    public void Expand_leaves_block_loop_outside_table_alone()
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            var header = new TableRow();
            foreach (var caption in new[] { "Familiyasy", "Ady", "Rayatlygy" })
                header.AppendChild(new TableCell(new Paragraph(new Run(new Text(caption)))));

            var data = new TableRow();
            foreach (var text in new[] { "{{.PLN}}", "{{.PFNM}}", "{{.PNAT}}" })
                data.AppendChild(new TableCell(new Paragraph(new Run(new Text(text)))));

            main.Document = new Document(new Body(
                new Paragraph(new Run(new Text("{{#ds.rows}}"))),
                new Table(
                    new TableGrid(
                        new GridColumn { Width = "1440" },
                        new GridColumn { Width = "1440" },
                        new GridColumn { Width = "1440" }),
                    header,
                    data),
                new Paragraph(new Run(new Text("{{/ds.rows}}")))));
            main.Document.Save();
        }

        var rows = new List<IDictionary<string, object>>
        {
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { ["PLN"] = "Alkan" },
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { ["PLN"] = "Erol" },
        };

        var expanded = WordScanTableRowExpander.ExpandPrototypeTableRow(stream.ToArray(), rows);
        using var output = new MemoryStream(expanded);
        using var opened = WordprocessingDocument.Open(output, false);
        var table = opened.MainDocumentPart!.Document.Body!.Descendants<Table>().Single();
        Assert.Equal(2, table.Elements<TableRow>().Count());
        Assert.Contains("{{#ds.rows}}", opened.MainDocumentPart.Document.Body.InnerText, StringComparison.Ordinal);
        Assert.Contains("{{.PLN}}", opened.MainDocumentPart.Document.Body.InnerText, StringComparison.Ordinal);
    }

    private static string CellText(TableRow row, int index) =>
        WordTemplateAddressingText(row.Elements<TableCell>().ElementAt(index));

    private static string WordTemplateAddressingText(TableCell cell) =>
        string.Concat(cell.Descendants<Text>().Select(static t => t.Text ?? string.Empty));

    private static byte[] TokenTable(bool includeLoop = false)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            var header = new TableRow();
            foreach (var caption in new[] { "Familiyasy", "Ady", "Rayatlygy" })
                header.AppendChild(new TableCell(new Paragraph(new Run(new Text(caption)))));

            var first = includeLoop ? "{{#ds.rows}}{{.PLN}}" : "{{.PLN}}";
            var last = includeLoop ? "{{.PNAT}}{{/ds.rows}}" : "{{.PNAT}}";
            var data = new TableRow();
            foreach (var text in new[] { first, "{{.PFNM}}", last })
                data.AppendChild(new TableCell(new Paragraph(new Run(new Text(text)))));

            main.Document = new Document(new Body(
                new Table(
                    new TableGrid(
                        new GridColumn { Width = "1440" },
                        new GridColumn { Width = "1440" },
                        new GridColumn { Width = "1440" }),
                    header,
                    data),
                new Paragraph(new Run(new Text("{{ds.ACPOS}}")))));
            main.Document.Save();
        }

        return stream.ToArray();
    }
}