using System.Text.RegularExpressions;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.Options;

#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

/// <summary>
/// Decides whether an uploaded document can become a template, and marks the spans that would be
/// replaced (L7). Runs entirely locally — no AI involved (E-D1).
/// </summary>
public interface ITemplateCandidateAnalyzer
{
    TemplateCandidateReport Analyze(TemplateCandidateRequest request);
}

/// <inheritdoc cref="ITemplateCandidateAnalyzer"/>
public sealed class TemplateCandidateAnalyzer : ITemplateCandidateAnalyzer
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    private static readonly Regex DateLikePattern =
        new(@"\b\d{1,2}[.\-/]\d{1,2}[.\-/]\d{2,4}\b", RegexOptions.CultureInvariant, RegexTimeout);

    private static readonly Regex IdentifierLikePattern =
        new(@"\b[A-Za-z]{0,2}\d{6,}\b", RegexOptions.CultureInvariant, RegexTimeout);

    private static readonly Regex ExistingTokenPattern =
        new(@"\{\{[^{}]+\}\}", RegexOptions.CultureInvariant, RegexTimeout);

    private readonly TemplateSuitabilityOptions _options;

    public TemplateCandidateAnalyzer(IOptions<TemplateSuitabilityOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public TemplateCandidateReport Analyze(TemplateCandidateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Content);
        ArgumentNullException.ThrowIfNull(request.ValueMap);

        List<Segment> segments;
        try
        {
            segments = ReadSegments(request.Content, request.Format);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            // Officer-supplied file: anything malformed is a Fail with an explanation, not a crash.
            return Failed(new SuitabilityReason(
                SuitabilityReasonCode.Unreadable,
                $"The file could not be read as {(request.Format == TemplateSourceFormat.Docx ? "Word" : "Excel")}: {ex.Message}"));
        }

        if (segments.Count == 0)
        {
            return Failed(new SuitabilityReason(
                SuitabilityReasonCode.NoExtractableText,
                "No readable text was found. A scanned image cannot be converted."));
        }

        var highlights = new List<HighlightRegion>();
        var alreadyTokenized = false;

        foreach (var segment in segments)
        {
            alreadyTokenized |= ExistingTokenPattern.IsMatch(segment.Text);

            var matches = MatchSegment(segment, request.ValueMap.Candidates);
            highlights.AddRange(matches.Select(m => m.Highlight));
            highlights.AddRange(FindGaps(segment, matches));
        }

        return Score(highlights, alreadyTokenized);
    }

    private TemplateCandidateReport Score(List<HighlightRegion> highlights, bool alreadyTokenized)
    {
        var matches = highlights.Where(static h => h.Kind == HighlightKind.Match).ToList();
        var gapCount = highlights.Count - matches.Count;

        var headerMatches = matches
            .Where(static m => m.RowIndex == null)
            .Select(static m => m.ShortCode!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var rowMatches = matches
            .Where(static m => m.RowIndex != null)
            .Select(static m => m.ShortCode!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        // One roster row is indistinguishable from a one-off mention; two or more imply a repeating table.
        var rosterLoop = matches
            .Where(static m => m.RowIndex != null)
            .Select(static m => m.RowIndex!.Value)
            .Distinct()
            .Count() >= 2;

        var reasons = new List<SuitabilityReason>();
        SuitabilityLevel level;

        if (matches.Count == 0)
        {
            level = SuitabilityLevel.Fail;
            reasons.Add(new SuitabilityReason(
                SuitabilityReasonCode.NoInstanceMatches,
                "None of the document text matches data from the selected case."));
        }
        else if (headerMatches < _options.MinHeaderMatchesToProceed && !rosterLoop)
        {
            level = SuitabilityLevel.Fail;
            reasons.Add(new SuitabilityReason(
                SuitabilityReasonCode.TooFewHeaderMatches,
                $"Only {headerMatches} case field(s) were recognised and no repeating people table was found. "
                + $"At least {_options.MinHeaderMatchesToProceed} are needed."));
        }
        else if (headerMatches >= _options.MinHeaderMatchesForPass)
        {
            level = SuitabilityLevel.Pass;
            reasons.Add(new SuitabilityReason(
                SuitabilityReasonCode.StrongHeaderCoverage,
                $"{headerMatches} case fields were recognised in the document."));
        }
        else if (rosterLoop && headerMatches >= _options.MinHeaderMatchesWithRosterLoop)
        {
            level = SuitabilityLevel.Pass;
            reasons.Add(new SuitabilityReason(
                SuitabilityReasonCode.RosterLoopDetected,
                $"A repeating people table was found, plus {headerMatches} case field(s)."));
        }
        else
        {
            level = SuitabilityLevel.Warn;
            reasons.Add(new SuitabilityReason(
                SuitabilityReasonCode.HeaderMatchesBelowPass,
                $"{headerMatches} case field(s) were recognised. Conversion will work but may leave gaps."));
        }

        if (rosterLoop && level != SuitabilityLevel.Fail
            && reasons.All(static r => r.Code != SuitabilityReasonCode.RosterLoopDetected))
        {
            reasons.Add(new SuitabilityReason(
                SuitabilityReasonCode.RosterLoopDetected,
                "A repeating people table was found and will become a roster loop."));
        }

        if (alreadyTokenized && level == SuitabilityLevel.Pass)
        {
            level = SuitabilityLevel.Warn;
        }

        if (alreadyTokenized)
        {
            reasons.Add(new SuitabilityReason(
                SuitabilityReasonCode.AlreadyTokenized,
                "The file already contains {{…}} placeholders. Converting it again may duplicate them."));
        }

        if (gapCount > 0)
        {
            reasons.Add(new SuitabilityReason(
                SuitabilityReasonCode.GapsPresent,
                $"{gapCount} value(s) look like case data but have no matching field. Use Needs help to report them."));
        }

        return new TemplateCandidateReport
        {
            Level = level,
            Reasons = reasons,
            Highlights = highlights,
            DistinctHeaderMatches = headerMatches,
            DistinctRowMatches = rowMatches,
            GapCount = gapCount,
            RosterLoopDetected = rosterLoop,
        };
    }

    private static TemplateCandidateReport Failed(SuitabilityReason reason) =>
        new()
        {
            Level = SuitabilityLevel.Fail,
            Reasons = [reason],
            Highlights = [],
            DistinctHeaderMatches = 0,
            DistinctRowMatches = 0,
            GapCount = 0,
            RosterLoopDetected = false,
        };

    private static List<SegmentMatch> MatchSegment(Segment segment, IReadOnlyList<ValueCandidate> candidates)
    {
        var folded = TemplateTextIndex.CreateFolded(segment.Text);
        var identifier = TemplateTextIndex.CreateIdentifier(segment.Text);
        if (folded.IsEmpty)
            return [];

        var found = new List<SegmentMatch>();
        foreach (var candidate in candidates)
        {
            foreach (var key in candidate.MatchKeys)
            {
                // A key is normalized either way, so both views are searched rather than guessing
                // which normalization produced it.
                foreach (var (start, length) in folded.FindAll(key).Concat(identifier.FindAll(key)))
                    found.Add(new SegmentMatch(start, length, candidate, segment));
            }
        }

        return ResolveOverlaps(found, segment);
    }

    /// <summary>
    /// Longest match wins: the full name "Dowletmyrat Amanov" must beat the surname "Amanov" that
    /// sits inside it, and the writer skips overlapping spans anyway.
    /// </summary>
    private static List<SegmentMatch> ResolveOverlaps(List<SegmentMatch> found, Segment segment)
    {
        var ordered = found
            .OrderByDescending(static m => m.Length)
            .ThenBy(static m => m.Start)
            .ThenBy(static m => m.Candidate.ShortCode, StringComparer.Ordinal)
            .ToList();

        var kept = new List<SegmentMatch>();
        foreach (var match in ordered)
        {
            if (kept.Any(k => k.Start < match.Start + match.Length && match.Start < k.Start + k.Length))
                continue;

            kept.Add(match);

            // An Excel cell is replaced as a whole, so it can only carry one token.
            if (segment.WholeSegment)
                break;
        }

        return [.. kept.OrderBy(static m => m.Start)];
    }

    /// <summary>
    /// Conservative gap detection: only dates and long digit runs that no candidate claimed. Anything
    /// looser would mark ordinary prose as missing data.
    /// </summary>
    private static IEnumerable<HighlightRegion> FindGaps(Segment segment, List<SegmentMatch> matches)
    {
        var gaps = new List<HighlightRegion>();
        var seen = new List<(int Start, int Length)>();

        foreach (Match found in DateLikePattern.Matches(segment.Text).Concat(IdentifierLikePattern.Matches(segment.Text)))
        {
            if (matches.Any(m => m.Start < found.Index + found.Length && found.Index < m.Start + m.Length))
                continue;

            if (seen.Any(s => s.Start < found.Index + found.Length && found.Index < s.Start + s.Length))
                continue;

            seen.Add((found.Index, found.Length));
            gaps.Add(new HighlightRegion(
                segment.ToRegion(found.Index, found.Length),
                HighlightKind.Gap,
                found.Value,
                Token: null,
                ShortCode: null,
                RowIndex: null));
        }

        return gaps;
    }

    private static List<Segment> ReadSegments(byte[] content, TemplateSourceFormat format) =>
        format switch
        {
            TemplateSourceFormat.Docx => ReadWordSegments(content),
            TemplateSourceFormat.Xlsx => ReadExcelSegments(content),
            _ => throw new NotSupportedException($"Unsupported template format '{format}'."),
        };

    private static List<Segment> ReadWordSegments(byte[] content)
    {
        var segments = new List<Segment>();
        using var stream = new MemoryStream(content, writable: false);
        using var document = WordprocessingDocument.Open(stream, false);

        foreach (var paragraph in WordTemplateAddressing.EnumerateParagraphs(document))
        {
            var text = WordTemplateAddressing.GetParagraphText(paragraph.Paragraph);
            if (string.IsNullOrWhiteSpace(text))
                continue;

            var address = paragraph.Address;
            segments.Add(new Segment(
                text,
                (start, length) => new DocumentRegion.WordSpan(address, start, length),
                WholeSegment: false));
        }

        return segments;
    }

    private static List<Segment> ReadExcelSegments(byte[] content)
    {
        var segments = new List<Segment>();
        using var stream = new MemoryStream(content, writable: false);
        using var workbook = new XLWorkbook(stream);

        foreach (var worksheet in workbook.Worksheets)
        {
            var sheetName = worksheet.Name;
            foreach (var cell in worksheet.CellsUsed())
            {
                var text = cell.GetFormattedString();
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                var reference = cell.Address.ToStringRelative();
                segments.Add(new Segment(
                    text,
                    (_, _) => new DocumentRegion.ExcelCell(sheetName, reference),
                    WholeSegment: true));
            }
        }

        return segments;
    }

    private sealed record Segment(string Text, Func<int, int, DocumentRegion> ToRegion, bool WholeSegment);

    private sealed record SegmentMatch(int Start, int Length, ValueCandidate Candidate, Segment Segment)
    {
        public HighlightRegion Highlight => new(
            Segment.ToRegion(Start, Length),
            HighlightKind.Match,
            Segment.Text.Substring(Start, Length),
            Candidate.Token,
            Candidate.ShortCode,
            Candidate.RowIndex);
    }
}
