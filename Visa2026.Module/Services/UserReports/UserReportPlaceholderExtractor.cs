using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Visa2026.Module.Services.UserReports
{
    /// <inheritdoc cref="IUserReportPlaceholderExtractor"/>
    public class UserReportPlaceholderExtractor : IUserReportPlaceholderExtractor
    {
        // Matches {{placeholder}} or {{#collection}} {{/collection}} {{.property}}
        private static readonly Regex PlaceholderRegex = new Regex(
            @"\{\{([#/.]?[\w.]+)\}\}",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public async Task<IList<string>> ExtractPlaceholdersAsync(Stream docxStream)
        {
            var placeholders = new HashSet<string>();

            // Reset stream position if needed
            if (docxStream.CanSeek)
                docxStream.Position = 0;

            using (var doc = WordprocessingDocument.Open(docxStream, false))
            {
                if (doc.MainDocumentPart?.Document?.Body == null)
                    return new List<string>(placeholders);

                // Extract from main document body
                var body = doc.MainDocumentPart.Document.Body;
                ExtractFromElement(body, placeholders);

                // Extract from headers
                foreach (var headerPart in doc.MainDocumentPart.HeaderParts)
                {
                    if (headerPart.Header != null)
                        ExtractFromElement(headerPart.Header, placeholders);
                }

                // Extract from footers
                foreach (var footerPart in doc.MainDocumentPart.FooterParts)
                {
                    if (footerPart.Footer != null)
                        ExtractFromElement(footerPart.Footer, placeholders);
                }
            }

            return await Task.FromResult(new List<string>(placeholders));
        }

        private void ExtractFromElement(OpenXmlElement element, HashSet<string> placeholders)
        {
            // Get all text content from the element
            var textContent = element.InnerText;

            // Find all placeholders in the text
            var matches = PlaceholderRegex.Matches(textContent);
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    var placeholder = match.Groups[1].Value;
                    placeholders.Add(placeholder);
                }
            }
        }
    }
}
