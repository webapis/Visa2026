#nullable enable

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Visa2026.Module.Services.UserReports;
using Visa2026.Module.Tests.TemplateConvert;
using Xunit;

namespace Visa2026.Module.Tests.Services.UserReports;

public class WordUserReportImageInjectorTests
{
    [Fact]
    public void Inject_replaces_token_split_by_a_newline_in_the_same_paragraph()
    {
        var content = CreateParagraphDocument(paragraph =>
        {
            paragraph.AppendChild(new Run(new Text("{{IMAGE:Person_Phot")));
            paragraph.AppendChild(new Run(new Text("\no}}") { Space = SpaceProcessingModeValues.Preserve }));
        });

        var injected = Inject(content);
        Assert.DoesNotContain("{{IMAGE:", GetBodyText(injected), StringComparison.OrdinalIgnoreCase);
        Assert.True(HasDrawing(injected));
    }

    [Fact]
    public void Inject_replaces_token_split_across_two_paragraphs_in_a_table_cell()
    {
        var content = CreateBodyDocument(body =>
        {
            body.AppendChild(new Table(
                new TableRow(
                    new TableCell(
                        new Paragraph(new Run(new Text("{{IMAGE:Person_Phot"))),
                        new Paragraph(new Run(new Text("o}}")))))));
        });

        var injected = Inject(content);
        Assert.DoesNotContain("{{IMAGE:", GetBodyText(injected), StringComparison.OrdinalIgnoreCase);
        Assert.True(HasDrawing(injected));
    }

    [Fact]
    public void Inject_replaces_short_pph_token()
    {
        var content = CreateParagraphDocument(paragraph =>
            paragraph.AppendChild(new Run(new Text("{{IMAGE:PPH}}"))));

        var injected = Inject(content);
        Assert.DoesNotContain("{{IMAGE:", GetBodyText(injected), StringComparison.OrdinalIgnoreCase);
        Assert.True(HasDrawing(injected));
    }

    private static byte[] Inject(byte[] source)
    {
        using var input = new MemoryStream();
        input.Write(source, 0, source.Length);
        input.Position = 0;
        using var output = new MemoryStream();
        WordUserReportImageInjector.Inject(
            input,
            output,
            new Dictionary<string, IReadOnlyList<byte[]>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Person_Photo"] = [TemplateConvertFixtures.TinyPng()],
            });
        return output.ToArray();
    }

    private static byte[] CreateParagraphDocument(Action<Paragraph> configureParagraph)
    {
        return CreateBodyDocument(body =>
        {
            var paragraph = new Paragraph();
            configureParagraph(paragraph);
            body.AppendChild(paragraph);
        });
    }

    private static byte[] CreateBodyDocument(Action<Body> configureBody)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            var body = new Body();
            configureBody(body);
            body.AppendChild(new SectionProperties());
            main.Document = new Document(body);
            main.Document.Save();
        }

        return stream.ToArray();
    }

    private static string GetBodyText(byte[] content)
    {
        using var stream = new MemoryStream(content, writable: false);
        using var document = WordprocessingDocument.Open(stream, false);
        return document.MainDocumentPart?.Document?.InnerText ?? string.Empty;
    }

    private static bool HasDrawing(byte[] content)
    {
        using var stream = new MemoryStream(content, writable: false);
        using var document = WordprocessingDocument.Open(stream, false);
        return document.MainDocumentPart?.Document?.Descendants<Drawing>().Any() == true;
    }
}
