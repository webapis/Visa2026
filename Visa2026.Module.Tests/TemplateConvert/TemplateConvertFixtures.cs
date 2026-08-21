using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Visa2026.Module.Tests.TemplateConvert;

/// <summary>
/// Builds documents in code rather than checking binaries into the repo, so the fixtures stay
/// reviewable and the run-splitting cases can be described exactly.
/// </summary>
internal static class TemplateConvertFixtures
{
    /// <summary>Each inner array is one paragraph; each string is one run.</summary>
    public static byte[] CreateWordDocument(params string[][] paragraphs) =>
        CreateWordDocument(paragraphs, boldRuns: null, headerText: null);

    public static byte[] CreateWordDocument(
        string[][] paragraphs,
        IReadOnlyCollection<(int Paragraph, int Run)>? boldRuns = null,
        string? headerText = null)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = document.AddMainDocumentPart();
            var body = new Body();
            mainPart.Document = new Document(body);

            for (var p = 0; p < paragraphs.Length; p++)
            {
                var paragraph = new Paragraph();
                for (var r = 0; r < paragraphs[p].Length; r++)
                {
                    var run = new Run();
                    if (boldRuns?.Contains((p, r)) == true)
                        run.RunProperties = new RunProperties(new Bold());

                    run.AppendChild(new Text(paragraphs[p][r]) { Space = SpaceProcessingModeValues.Preserve });
                    paragraph.AppendChild(run);
                }

                body.AppendChild(paragraph);
            }

            var sectionProperties = new SectionProperties();
            if (headerText != null)
            {
                var headerPart = mainPart.AddNewPart<HeaderPart>();
                headerPart.Header = new Header(
                    new Paragraph(new Run(new Text(headerText) { Space = SpaceProcessingModeValues.Preserve })));

                sectionProperties.AppendChild(new HeaderReference
                {
                    Id = mainPart.GetIdOfPart(headerPart),
                    Type = HeaderFooterValues.Default,
                });
            }

            body.AppendChild(sectionProperties);
            mainPart.Document.Save();
        }

        return stream.ToArray();
    }

    public static byte[] CreateExcelRoster()
    {
        using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("Sanaw");
            sheet.Cell("A1").Value = "Familiýasy, ady";
            sheet.Cell("B1").Value = "Pasport";
            sheet.Cell("A2").Value = "Meredowa Aýnabat";
            sheet.Cell("B2").Value = "T 1234567";
            sheet.Cell("C2").Value = 1500;
            sheet.Cell("C2").Style.NumberFormat.Format = "#,##0.00";
            sheet.Cell("D2").FormulaA1 = "=C2*2";
            sheet.Range("A4:B4").Merge();
            sheet.Cell("A4").Value = "Jemi";
            sheet.Column(1).Width = 32;

            workbook.SaveAs(stream);
        }

        return stream.ToArray();
    }

    public static byte[] CreateExcelSheet(string sheetName, params (string Cell, string Value)[] cells)
    {
        using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet(sheetName);
            foreach (var (cell, value) in cells)
                sheet.Cell(cell).Value = value;

            workbook.SaveAs(stream);
        }

        return stream.ToArray();
    }

    public static string GetParagraphText(byte[] content, string address)
    {
        using var stream = new MemoryStream(content, writable: false);
        using var document = WordprocessingDocument.Open(stream, false);
        var paragraph = Module.Services.TemplateConvert.WordTemplateAddressing
            .EnumerateParagraphs(document)
            .Single(a => a.Address == address);

        return Module.Services.TemplateConvert.WordTemplateAddressing.GetParagraphText(paragraph.Paragraph);
    }

    public static IReadOnlyList<Run> GetRuns(byte[] content, string address)
    {
        using var stream = new MemoryStream(content, writable: false);
        using var document = WordprocessingDocument.Open(stream, false);
        var paragraph = Module.Services.TemplateConvert.WordTemplateAddressing
            .EnumerateParagraphs(document)
            .Single(a => a.Address == address);

        return paragraph.Paragraph.Elements<Run>().Select(static r => (Run)r.CloneNode(true)).ToList();
    }

    public static string GetCellText(byte[] content, string sheetName, string cellReference)
    {
        using var stream = new MemoryStream(content, writable: false);
        using var workbook = new XLWorkbook(stream);
        return workbook.Worksheet(sheetName).Cell(cellReference).GetFormattedString();
    }

    /// <summary>Edits text the converter never touched, to prove the diff gate notices.</summary>
    public static byte[] TamperWordText(byte[] content, string address, string newText)
    {
        using var buffer = new MemoryStream();
        buffer.Write(content, 0, content.Length);
        buffer.Position = 0;

        using (var document = WordprocessingDocument.Open(buffer, true))
        {
            var paragraph = Module.Services.TemplateConvert.WordTemplateAddressing
                .EnumerateParagraphs(document)
                .Single(a => a.Address == address)
                .Paragraph;

            var first = paragraph.Descendants<Text>().First();
            first.Text = newText;
            document.MainDocumentPart!.Document.Save();
            document.Save();
        }

        return buffer.ToArray();
    }
}
