#nullable enable

using System.IO.Compression;
using DocumentFormat.OpenXml.Packaging;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class WordOpenXmlPackageTests
{
    [Fact]
    public void EnumerateParagraphs_MissingRelatedPart_ThrowsUntilRepaired()
    {
        var broken = WithMissingRelatedPart(
            ScanOfficeYellowExtractorTests.CreateWordFixture("№ 4/-434"));

        InvalidOperationException? thrown = null;
        try
        {
            using var stream = new MemoryStream(broken, writable: false);
            using var document = WordprocessingDocument.Open(stream, false);
            _ = WordTemplateAddressing.EnumerateParagraphs(document);
        }
        catch (InvalidOperationException ex)
        {
            thrown = ex;
        }

        Assert.NotNull(thrown);
        Assert.Contains("Specified part does not exist", thrown.Message, StringComparison.OrdinalIgnoreCase);

        var repaired = WordOpenXmlPackage.EnsureLoadable(broken);
        using var opened = WordOpenXmlPackage.OpenRead(repaired);
        var paragraphs = WordTemplateAddressing.EnumerateParagraphs(opened);
        Assert.Contains(paragraphs, p => WordTemplateAddressing.GetParagraphText(p.Paragraph)
            .Contains("4/-434", StringComparison.Ordinal));
    }

    [Fact]
    public void Ingest_WordWithMissingRelatedPart_StillFindsYellow()
    {
        var (_, _, ingest, _) = ScanTestServiceFactory.Create();
        var bytes = WithMissingRelatedPart(
            ScanOfficeYellowExtractorTests.CreateWordFixture("№ 4/-434"));

        var result = ingest.Ingest(new ScanNormalizeRequest
        {
            Content = bytes,
            FileName = "marked.docx",
        });

        Assert.True(result.Suitability.CanContinue);
        Assert.Contains(
            result.Ocr.Lines,
            line => line.Text.Contains("4/-434", StringComparison.Ordinal));

        var yellows = new ScanOfficeYellowExtractor().Extract(
            result.Input.OfficePackageBytes!,
            ScanSourceKind.Word);
        Assert.Contains(yellows, span => span.Text.Contains("4/-434", StringComparison.Ordinal));
    }

    internal static byte[] WithMissingRelatedPart(byte[] docx)
    {
        const string danglingRels = """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
              <Relationship Id="rIdMissingScan" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/image" Target="media/missing-scan.png"/>
            </Relationships>
            """;

        using var input = new MemoryStream(docx, writable: false);
        using var output = new MemoryStream();
        using (var reader = new ZipArchive(input, ZipArchiveMode.Read, leaveOpen: true))
        using (var writer = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            var wroteDocRels = false;
            foreach (var entry in reader.Entries)
            {
                var dest = writer.CreateEntry(entry.FullName);
                using var source = entry.Open();
                using var target = dest.Open();
                if (entry.FullName.Replace('\\', '/').EndsWith("word/_rels/document.xml.rels", StringComparison.OrdinalIgnoreCase))
                {
                    var xml = System.Xml.Linq.XDocument.Load(source);
                    System.Xml.Linq.XNamespace ns = "http://schemas.openxmlformats.org/package/2006/relationships";
                    xml.Root!.Add(new System.Xml.Linq.XElement(
                        ns + "Relationship",
                        new System.Xml.Linq.XAttribute("Id", "rIdMissingScan"),
                        new System.Xml.Linq.XAttribute(
                            "Type",
                            "http://schemas.openxmlformats.org/officeDocument/2006/relationships/image"),
                        new System.Xml.Linq.XAttribute("Target", "media/missing-scan.png")));
                    xml.Save(target);
                    wroteDocRels = true;
                }
                else
                {
                    source.CopyTo(target);
                }
            }

            if (!wroteDocRels)
            {
                var dest = writer.CreateEntry("word/_rels/document.xml.rels");
                using var target = dest.Open();
                using var sw = new StreamWriter(target, new System.Text.UTF8Encoding(false));
                sw.Write(danglingRels);
            }
        }

        return output.ToArray();
    }
}
