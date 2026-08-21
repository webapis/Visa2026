using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

public sealed record WordParagraphAddress(string Address, WordPart Part, Paragraph Paragraph)
{
    public bool IsInTable => Paragraph.Ancestors<TableCell>().Any();
}

/// <summary>
/// Deterministic paragraph addresses shared by the candidate analyser, the token writer, and the diff gate.
/// Addresses are ordinal (<c>body/12</c>, <c>header0/3</c>) rather than <c>w14:paraId</c>, which is optional
/// and absent from many real ministry documents.
/// </summary>
public static class WordTemplateAddressing
{
    public static IReadOnlyList<WordParagraphAddress> EnumerateParagraphs(WordprocessingDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var addresses = new List<WordParagraphAddress>();
        var mainPart = document.MainDocumentPart;
        if (mainPart == null)
            return addresses;

        if (mainPart.Document?.Body != null)
            AddRange(addresses, mainPart.Document.Body, WordPart.Body, "body");

        var headerIndex = 0;
        foreach (var headerPart in mainPart.HeaderParts)
        {
            if (headerPart.Header != null)
                AddRange(addresses, headerPart.Header, WordPart.Header, $"header{headerIndex}");
            headerIndex++;
        }

        var footerIndex = 0;
        foreach (var footerPart in mainPart.FooterParts)
        {
            if (footerPart.Footer != null)
                AddRange(addresses, footerPart.Footer, WordPart.Footer, $"footer{footerIndex}");
            footerIndex++;
        }

        return addresses;
    }

    /// <summary>Concatenated <c>w:t</c> text of a paragraph — the coordinate space for <see cref="DocumentRegion.WordSpan"/>.</summary>
    public static string GetParagraphText(Paragraph paragraph)
    {
        ArgumentNullException.ThrowIfNull(paragraph);
        return string.Concat(paragraph.Descendants<Text>().Select(static t => t.Text ?? string.Empty));
    }

    private static void AddRange(List<WordParagraphAddress> addresses, OpenXmlElement root, WordPart part, string prefix)
    {
        var index = 0;
        foreach (var paragraph in root.Descendants<Paragraph>())
        {
            addresses.Add(new WordParagraphAddress($"{prefix}/{index}", part, paragraph));
            index++;
        }
    }
}
