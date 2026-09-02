using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

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

    /// <summary>1×1 PNG used as a sample portrait byte payload (extents, not pixels, decide photo vs icon).</summary>
    public static byte[] TinyPng() => Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    public static byte[] CreateWordWithInlinePicture(
        string? beforeText,
        long widthEmu,
        long heightEmu,
        string? afterText = null)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = document.AddMainDocumentPart();
            var imagePart = mainPart.AddImagePart(ImagePartType.Png);
            using (var imageStream = new MemoryStream(TinyPng(), writable: false))
                imagePart.FeedData(imageStream);

            var relationshipId = mainPart.GetIdOfPart(imagePart);
            var paragraph = new Paragraph();
            if (!string.IsNullOrEmpty(beforeText))
            {
                paragraph.AppendChild(new Run(new Text(beforeText)
                {
                    Space = SpaceProcessingModeValues.Preserve,
                }));
            }

            paragraph.AppendChild(new Run(BuildInlineDrawing(relationshipId, 1U, widthEmu, heightEmu)));

            if (!string.IsNullOrEmpty(afterText))
            {
                paragraph.AppendChild(new Run(new Text(afterText)
                {
                    Space = SpaceProcessingModeValues.Preserve,
                }));
            }

            mainPart.Document = new Document(new Body(paragraph, new SectionProperties()));
            mainPart.Document.Save();
        }

        return stream.ToArray();
    }

    public static byte[] AppendInlinePicture(byte[] source, long widthEmu, long heightEmu)
    {
        using var buffer = new MemoryStream();
        buffer.Write(source, 0, source.Length);
        buffer.Position = 0;

        using (var document = WordprocessingDocument.Open(buffer, true))
        {
            var mainPart = document.MainDocumentPart
                ?? throw new InvalidOperationException("Word document has no main document part.");
            var imagePart = mainPart.AddImagePart(ImagePartType.Png);
            using (var imageStream = new MemoryStream(TinyPng(), writable: false))
                imagePart.FeedData(imageStream);

            var relationshipId = mainPart.GetIdOfPart(imagePart);
            var paragraph = new Paragraph(new Run(BuildInlineDrawing(relationshipId, 8U, widthEmu, heightEmu)));
            var body = mainPart.Document.Body
                ?? throw new InvalidOperationException("Word document has no body.");
            var section = body.Elements<SectionProperties>().LastOrDefault();
            if (section != null)
                body.InsertBefore(paragraph, section);
            else
                body.AppendChild(paragraph);

            mainPart.Document.Save();
            document.Save();
        }

        return buffer.ToArray();
    }

    private static Drawing BuildInlineDrawing(string relationshipId, uint drawingId, long widthEmu, long heightEmu)
    {
        var graphicData = new A.GraphicData(
            new PIC.Picture(
                new PIC.NonVisualPictureProperties(
                    new PIC.NonVisualDrawingProperties { Id = 0U, Name = $"Photo {drawingId}.png" },
                    new PIC.NonVisualPictureDrawingProperties()),
                new PIC.BlipFill(
                    new A.Blip { Embed = relationshipId },
                    new A.Stretch(new A.FillRectangle())),
                new PIC.ShapeProperties(
                    new A.Transform2D(
                        new A.Offset { X = 0L, Y = 0L },
                        new A.Extents { Cx = widthEmu, Cy = heightEmu }),
                    new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle })))
        {
            Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture",
        };

        return new Drawing(
            new DW.Inline(
                new DW.Extent { Cx = widthEmu, Cy = heightEmu },
                new DW.EffectExtent
                {
                    LeftEdge = 0L,
                    TopEdge = 0L,
                    RightEdge = 0L,
                    BottomEdge = 0L,
                },
                new DW.DocProperties { Id = drawingId, Name = $"Picture {drawingId}" },
                new DW.NonVisualGraphicFrameDrawingProperties(new A.GraphicFrameLocks { NoChangeAspect = true }),
                new A.Graphic(graphicData))
            {
                DistanceFromTop = 0U,
                DistanceFromBottom = 0U,
                DistanceFromLeft = 0U,
                DistanceFromRight = 0U,
            });
    }
}
