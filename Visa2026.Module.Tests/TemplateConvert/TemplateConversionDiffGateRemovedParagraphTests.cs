#nullable enable

using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Visa2026.Module.Services.TemplateConvert;
using Xunit;

namespace Visa2026.Module.Tests.TemplateConvert;

/// <summary>
/// Extra green sample people lines are removed after token write; the diff gate must
/// ignore those original paragraph addresses or Continue/Approve fails falsely.
/// </summary>
public class TemplateConversionDiffGateRemovedParagraphTests
{
    private readonly ITemplateTokenWriter _writer = new TemplateTokenWriter();
    private readonly ITemplateConversionDiffGate _gate = new TemplateConversionDiffGate();

    [Fact]
    public void Word_passes_when_removed_green_sample_paragraph_is_declared()
    {
        var original = TemplateConvertFixtures.CreateWordDocument(
            new[] { "20.01.2026" },
            new[] { "1. Hayati Uyan" },
            new[] { "2. Hong Huang" });

        var result = _writer.Apply(new TemplateTokenWriteRequest
        {
            SourceContent = original,
            Format = TemplateSourceFormat.Docx,
            Substitutions =
            [
                new TokenSubstitution(new DocumentRegion.WordSpan("body/0", 0, 10), "{{ds.ADAT}}"),
                new TokenSubstitution(new DocumentRegion.WordSpan("body/1", 0, 15), "{{#ds.rows}}{{.RNUM}}. {{.PFN}}{{/ds.rows}}"),
            ],
        });

        var converted = DropBodyParagraph(result.Content, paragraphIndex: 2);

        var withoutHint = _gate.Verify(new TemplateDiffGateRequest
        {
            OriginalContent = original,
            ConvertedContent = converted,
            Format = TemplateSourceFormat.Docx,
            Substitutions = result.AppliedSubstitutions,
            Loops = result.AppliedLoops,
        });
        Assert.False(withoutHint.Passed);
        Assert.Contains(withoutHint.Violations, v => v.Contains("Paragraph count", StringComparison.Ordinal));

        var withHint = _gate.Verify(new TemplateDiffGateRequest
        {
            OriginalContent = original,
            ConvertedContent = converted,
            Format = TemplateSourceFormat.Docx,
            Substitutions = result.AppliedSubstitutions,
            Loops = result.AppliedLoops,
            RemovedParagraphAddresses = ["body/2"],
        });
        Assert.True(withHint.Passed, string.Join(" | ", withHint.Violations));
    }

    private static byte[] DropBodyParagraph(byte[] content, int paragraphIndex)
    {
        using var buffer = new MemoryStream();
        buffer.Write(content, 0, content.Length);
        buffer.Position = 0;
        using (var document = WordprocessingDocument.Open(buffer, true))
        {
            var body = document.MainDocumentPart!.Document!.Body!;
            var paragraphs = body.Elements<Paragraph>().ToList();
            paragraphs[paragraphIndex].Remove();
            document.MainDocumentPart.Document.Save();
            document.Save();
        }

        return buffer.ToArray();
    }
}
