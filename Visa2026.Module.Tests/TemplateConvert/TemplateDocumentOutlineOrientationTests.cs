#nullable enable

using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Visa2026.Module.Services.TemplateConvert;
using Xunit;

namespace Visa2026.Module.Tests.TemplateConvert;

public class TemplateDocumentOutlineOrientationTests
{
    [Fact]
    public void Word_default_section_is_portrait()
    {
        var bytes = WordWithPageSize(11906, 16838, orient: null);
        var outline = new TemplateDocumentOutlineReader().Read(bytes, TemplateSourceFormat.Docx);
        Assert.Equal(TemplatePageOrientation.Portrait, outline.PageOrientation);
        Assert.False(outline.IsLandscape);
    }

    [Fact]
    public void Word_landscape_orient_flag_is_landscape()
    {
        var bytes = WordWithPageSize(16838, 11906, PageOrientationValues.Landscape);
        var outline = new TemplateDocumentOutlineReader().Read(bytes, TemplateSourceFormat.Docx);
        Assert.Equal(TemplatePageOrientation.Landscape, outline.PageOrientation);
        Assert.True(outline.IsLandscape);
    }

    [Fact]
    public void Word_width_greater_than_height_is_landscape()
    {
        var bytes = WordWithPageSize(16838, 11906, orient: null);
        var outline = new TemplateDocumentOutlineReader().Read(bytes, TemplateSourceFormat.Docx);
        Assert.True(outline.IsLandscape);
    }

    [Fact]
    public void Word_landscape_orient_on_portrait_paper_size_is_landscape()
    {
        var bytes = WordWithPageSize(11906, 16838, PageOrientationValues.Landscape);
        var outline = new TemplateDocumentOutlineReader().Read(bytes, TemplateSourceFormat.Docx);
        Assert.True(outline.IsLandscape);
    }

    [Fact]
    public void Excel_default_page_setup_is_portrait()
    {
        using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            workbook.AddWorksheet("Sanaw").Cell(1, 1).Value = "A";
            workbook.SaveAs(stream);
        }

        var outline = new TemplateDocumentOutlineReader().Read(stream.ToArray(), TemplateSourceFormat.Xlsx);
        Assert.False(outline.IsLandscape);
    }

    [Fact]
    public void Excel_page_setup_landscape_is_landscape()
    {
        using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("Sanaw");
            sheet.Cell(1, 1).Value = "A";
            sheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            workbook.SaveAs(stream);
        }

        var outline = new TemplateDocumentOutlineReader().Read(stream.ToArray(), TemplateSourceFormat.Xlsx);
        Assert.True(outline.IsLandscape);
    }

    private static byte[] WordWithPageSize(uint width, uint height, PageOrientationValues? orient)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            var size = new PageSize { Width = width, Height = height };
            if (orient != null)
                size.Orient = orient.Value;

            main.Document = new Document(new Body(
                new Paragraph(new Run(new Text("letter"))),
                new SectionProperties(size)));
            main.Document.Save();
        }

        return stream.ToArray();
    }
}