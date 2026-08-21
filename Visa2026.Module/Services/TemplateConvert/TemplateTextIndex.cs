using System.Text;

#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

/// <summary>
/// A normalized view of a document segment that can map a match back to the exact original span.
/// </summary>
/// <remarks>
/// Matching has to run on normalized text — folded diacritics, collapsed whitespace, invariant
/// lowercase — but the token writer needs offsets into the original text. Normalizing changes
/// lengths, so every normalized character keeps the source range it came from.
/// </remarks>
public sealed class TemplateTextIndex
{
    private readonly int[] _sourceStart;
    private readonly int[] _sourceEnd;

    private TemplateTextIndex(string normalized, int[] sourceStart, int[] sourceEnd)
    {
        Normalized = normalized;
        _sourceStart = sourceStart;
        _sourceEnd = sourceEnd;
    }

    public string Normalized { get; }

    public bool IsEmpty => Normalized.Length == 0;

    /// <summary>Mirrors <see cref="TemplateTextNormalizer.NormalizeFolded"/>.</summary>
    public static TemplateTextIndex CreateFolded(string? original) => Create(original, stripSeparators: false);

    /// <summary>Mirrors <see cref="TemplateTextNormalizer.NormalizeIdentifier"/>.</summary>
    public static TemplateTextIndex CreateIdentifier(string? original) => Create(original, stripSeparators: true);

    private static TemplateTextIndex Create(string? original, bool stripSeparators)
    {
        var text = original ?? string.Empty;
        var builder = new StringBuilder(text.Length);
        var starts = new List<int>(text.Length);
        var ends = new List<int>(text.Length);

        var whitespaceRunStart = -1;

        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];

            if (char.IsWhiteSpace(ch))
            {
                if (whitespaceRunStart < 0)
                    whitespaceRunStart = i;
                continue;
            }

            // A run of whitespace collapses to one space, which owns the whole run.
            if (whitespaceRunStart >= 0)
            {
                if (builder.Length > 0 && !stripSeparators)
                {
                    builder.Append(' ');
                    starts.Add(whitespaceRunStart);
                    ends.Add(i);
                }

                whitespaceRunStart = -1;
            }

            var folded = TemplateTextNormalizer.Fold(char.ToLowerInvariant(ch));
            if (stripSeparators && TemplateTextNormalizer.IsIdentifierSeparator(folded))
                continue;

            builder.Append(folded);
            starts.Add(i);
            ends.Add(i + 1);
        }

        return new TemplateTextIndex(builder.ToString(), [.. starts], [.. ends]);
    }

    /// <summary>Maps a span of <see cref="Normalized"/> back to a span of the original text.</summary>
    public bool TryMapSpan(int normalizedStart, int normalizedLength, out int start, out int length)
    {
        start = 0;
        length = 0;

        if (normalizedLength <= 0
            || normalizedStart < 0
            || normalizedStart + normalizedLength > _sourceStart.Length)
        {
            return false;
        }

        start = _sourceStart[normalizedStart];
        var end = _sourceEnd[normalizedStart + normalizedLength - 1];
        length = end - start;
        return length > 0;
    }

    /// <summary>Every occurrence of <paramref name="needle"/>, as original-text spans.</summary>
    public IEnumerable<(int Start, int Length)> FindAll(string needle)
    {
        if (string.IsNullOrEmpty(needle) || IsEmpty)
            yield break;

        var searchFrom = 0;
        while (searchFrom <= Normalized.Length - needle.Length)
        {
            var hit = Normalized.IndexOf(needle, searchFrom, StringComparison.Ordinal);
            if (hit < 0)
                yield break;

            if (TryMapSpan(hit, needle.Length, out var start, out var length))
                yield return (start, length);

            searchFrom = hit + 1;
        }
    }
}
