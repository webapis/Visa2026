#nullable enable

using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Visa2026.Module.Services.TemplateConvert;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// A header letter can list several sample people in green. Generate keeps the first line as
/// <c>{{#ds.rows}}…{{/ds.rows}}</c> and removes the following sample lines.
/// </summary>
internal static class ScanLetterRosterCollapse
{
    private static readonly Regex NumberedPerson = new(
        @"^\s*\d{1,3}\s*[\.\)]\s+\S",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    internal sealed record Result(
        IReadOnlyList<TokenSubstitution> Substitutions,
        IReadOnlyList<LoopMarker> ParagraphLoops,
        IReadOnlyList<string> RemovedParagraphAddresses);

    public static Result Apply(byte[] documentContent, IReadOnlyList<TokenSubstitution> substitutions)
    {
        ArgumentNullException.ThrowIfNull(substitutions);
        if (documentContent is not { Length: > 0 } || substitutions.Count == 0)
            return new Result(substitutions, [], []);

        byte[] loadable;
        try
        {
            loadable = WordOpenXmlPackage.EnsureLoadable(documentContent);
        }
        catch (InvalidDataException)
        {
            return new Result(substitutions, [], []);
        }
        catch (OpenXmlPackageException)
        {
            return new Result(substitutions, [], []);
        }

        using var input = new MemoryStream(loadable, writable: false);
        using var document = WordprocessingDocument.Open(input, false);
        var addressed = WordTemplateAddressing.EnumerateParagraphs(document);
        var byAddress = substitutions
            .Select(static s => (Sub: s, Span: s.Region as DocumentRegion.WordSpan))
            .Where(static x => x.Span != null)
            .GroupBy(static x => x.Span!.ParagraphAddress, StringComparer.Ordinal)
            .ToDictionary(static g => g.Key, static g => g.Select(static x => x.Sub).ToList(), StringComparer.Ordinal);

        var groups = new List<List<WordParagraphAddress>>();
        List<WordParagraphAddress>? current = null;
        foreach (var addr in addressed)
        {
            if (!byAddress.TryGetValue(addr.Address, out var subs) || !IsRosterLine(addr, subs))
            {
                if (current != null)
                {
                    groups.Add(current);
                    current = null;
                }

                continue;
            }

            current ??= [];
            current.Add(addr);
        }

        if (current != null)
            groups.Add(current);

        if (groups.Count == 0)
            return new Result(substitutions, [], []);

        var rosterAddresses = new HashSet<string>(StringComparer.Ordinal);
        var removed = new List<string>();
        var prototypeSubs = new List<TokenSubstitution>();
        var loops = new List<LoopMarker>();

        foreach (var group in groups)
        {
            var first = group[0];
            rosterAddresses.Add(first.Address);
            prototypeSubs.Add(CollapseParagraph(first.Address, byAddress[first.Address]));
            loops.Add(new LoopMarker(
                new DocumentRegion.WordSpan(first.Address, 0, 0),
                new DocumentRegion.WordSpan(first.Address, 0, 0),
                TemplateRosterLoopPlanner.RowsCollectionToken));

            for (var i = 1; i < group.Count; i++)
            {
                var sample = group[i];
                rosterAddresses.Add(sample.Address);
                if (sample.Paragraph.Parent is Body)
                    removed.Add(sample.Address);
                else
                    prototypeSubs.AddRange(byAddress[sample.Address]);
            }
        }

        var kept = new List<TokenSubstitution>();
        foreach (var substitution in substitutions)
        {
            if (substitution.Region is DocumentRegion.WordSpan span
                && rosterAddresses.Contains(span.ParagraphAddress))
            {
                continue;
            }

            kept.Add(substitution);
        }

        kept.AddRange(prototypeSubs);
        return new Result(kept, loops, removed);
    }

    public static byte[] RemoveParagraphs(byte[] content, IReadOnlyList<string> addresses)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (content.Length == 0 || addresses == null || addresses.Count == 0)
            return content;

        var drop = new HashSet<string>(addresses, StringComparer.Ordinal);
        byte[] loadable;
        try
        {
            loadable = WordOpenXmlPackage.EnsureLoadable(content);
        }
        catch (InvalidDataException)
        {
            return content;
        }
        catch (OpenXmlPackageException)
        {
            return content;
        }

        using var buffer = new MemoryStream();
        buffer.Write(loadable, 0, loadable.Length);
        buffer.Position = 0;

        using (var document = WordprocessingDocument.Open(buffer, true))
        {
            var doomed = WordTemplateAddressing.EnumerateParagraphs(document)
                .Where(addr => drop.Contains(addr.Address) && CanRemove(addr.Paragraph))
                .Select(static addr => addr.Paragraph)
                .ToList();
            foreach (var paragraph in doomed)
                paragraph.Remove();

            document.MainDocumentPart?.Document.Save();
            document.Save();
        }

        return buffer.ToArray();
    }

    private static bool CanRemove(Paragraph paragraph)
    {
        if (paragraph.Parent is not Body)
            return false;

        return true;
    }

    private static TokenSubstitution CollapseParagraph(string address, IReadOnlyList<TokenSubstitution> subs)
    {
        var spans = subs
            .Select(static s => (Sub: s, Span: s.Region as DocumentRegion.WordSpan))
            .Where(static x => x.Span != null)
            .OrderBy(static x => x.Span!.Start)
            .ToList();
        if (spans.Count == 0)
            return subs[0];
        if (spans.Count == 1)
            return spans[0].Sub;

        var start = spans[0].Span!.Start;
        var end = spans.Max(static x => x.Span!.Start + x.Span.Length);
        return new TokenSubstitution(
            new DocumentRegion.WordSpan(address, start, end - start),
            spans[0].Sub.Token);
    }

    private static bool IsRosterLine(WordParagraphAddress address, IReadOnlyList<TokenSubstitution> subs)
    {
        if (subs.Count == 0 || subs.Any(static s => !IsRowToken(s.Token)))
            return false;
        if (IsMultiCellTableRow(address.Paragraph))
            return false;

        var text = WordTemplateAddressing.GetParagraphText(address.Paragraph).Trim();
        return NumberedPerson.IsMatch(text) || ScanShapeTokenMatcher.LooksLikePersonFullName(text);
    }

    private static bool IsMultiCellTableRow(Paragraph paragraph)
    {
        var row = paragraph.Ancestors<TableRow>().FirstOrDefault();
        return row != null && row.Elements<TableCell>().Count() >= 2;
    }

    private static bool IsRowToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;
        if (token.Contains("ds.", StringComparison.OrdinalIgnoreCase))
            return false;

        return !TemplateTokenSyntax.TryGetShortCode(token, out var code)
            || ScanPlaceholderRoleCatalog.Resolve(code) != ScanPlaceholderRole.Signatory;
    }
}
