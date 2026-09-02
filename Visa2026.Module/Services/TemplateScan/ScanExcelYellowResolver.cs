#nullable enable

using ClosedXML.Excel;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Maps yellow Excel cells to placeholders using column headers + manual + content shape.
/// Sample literals (Erol, Hilmi) are never matched against case instance values.
/// </summary>
public static class ScanExcelYellowResolver
{
    public static IReadOnlyList<ScanDetectedFieldDraft> Resolve(
        byte[] workbookBytes,
        IReadOnlyList<ScanOfficeYellowSpan> yellows,
        ApplicationProfilePlaceholderSet placeholderSet)
    {
        ArgumentNullException.ThrowIfNull(workbookBytes);
        ArgumentNullException.ThrowIfNull(yellows);
        ArgumentNullException.ThrowIfNull(placeholderSet);

        if (yellows.Count == 0)
            return Array.Empty<ScanDetectedFieldDraft>();

        using var stream = new MemoryStream(workbookBytes, writable: false);
        using var workbook = new XLWorkbook(stream);
        var catalog = ScanPlaceholderCatalogIndex.Build(placeholderSet);
        var usedHeaderCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var drafts = new List<ScanDetectedFieldDraft>();

        foreach (var yellow in yellows)
        {
            if (yellow.Region is not DocumentRegion.ExcelCell excelCell)
                continue;

            var sheet = workbook.Worksheets.FirstOrDefault(w =>
                string.Equals(w.Name, excelCell.SheetName, StringComparison.OrdinalIgnoreCase));
            if (sheet == null)
                continue;

            IXLCell cell;
            try
            {
                cell = sheet.Cell(excelCell.CellReference);
            }
            catch (ArgumentException)
            {
                continue;
            }

            var header = ScanExcelWorkbookHelper.GetColumnHeader(sheet, cell.Address.ColumnNumber, cell.Address.RowNumber);
            var profile = ScanExcelColumnProfiles.Match(header);
            var headerScores = catalog.ScoreHeader(header);

            var inference = InferCell(
                yellow.Text,
                header,
                profile,
                headerScores,
                placeholderSet,
                cell.Address.RowNumber);

            if (inference.ProposedToken == null)
            {
                drafts.Add(new ScanDetectedFieldDraft
                {
                    FieldId = Guid.NewGuid().ToString("N"),
                    PageIndex = yellow.PageIndex,
                    LabelText = yellow.Text,
                    ProposedToken = null,
                    Confidence = ScanFieldConfidence.Medium,
                    Scope = ScanFieldScope.Row,
                    Box = ScanBoundingBox.FullPage,
                    SourceRegion = yellow.Region,
                    Alternatives = inference.Alternatives,
                    ColumnHeader = header,
                });
                continue;
            }

            if (TemplateTokenSyntax.TryGetShortCode(inference.ProposedToken, out var code)
                && !usedHeaderCodes.Add(code)
                && inference.Scope == ScanFieldScope.Header)
            {
                // Header token already used — keep as row if ambiguous.
            }

            drafts.Add(new ScanDetectedFieldDraft
            {
                FieldId = Guid.NewGuid().ToString("N"),
                PageIndex = yellow.PageIndex,
                LabelText = yellow.Text,
                ProposedToken = inference.ProposedToken,
                Confidence = inference.Confidence,
                Scope = inference.Scope,
                Box = ScanBoundingBox.FullPage,
                SourceRegion = yellow.Region,
                Alternatives = inference.Alternatives,
                ColumnHeader = header,
            });
        }

        return drafts;
    }

    private sealed record CellInference(
        string? ProposedToken,
        ScanFieldConfidence Confidence,
        ScanFieldScope Scope,
        IReadOnlyList<ScanTokenAlternative> Alternatives);

    private static CellInference InferCell(
        string cellText,
        string? header,
        ScanExcelColumnProfiles.Profile? profile,
        IReadOnlyList<(UserReportPlaceholderCatalogEntry Entry, int Score)> headerScores,
        ApplicationProfilePlaceholderSet placeholderSet,
        int dataRow)
    {
        var scope = DetermineScope(profile, headerScores, dataRow);
        var usageScope = scope == ScanFieldScope.Row
            ? UserReportPlaceholderScope.Row
            : UserReportPlaceholderScope.Header;

        if (profile is { IsCompound: true })
            return InferCompoundCell(cellText, header, profile, headerScores, placeholderSet, usageScope, scope);

        var preferCodes = profile?.ShortCodes
            ?? headerScores.Select(static h => h.Entry.ShortCode).Take(3).ToArray();

        var shapeScores = ScanShapeTokenMatcher.ScoreSnippet(
            cellText,
            placeholderSet,
            usageScope,
            preferCodes);

        var merged = MergeScores(shapeScores, headerScores, preferCodes, placeholderSet, usageScope)
            .ToDictionary(static a => a.ShortCode, StringComparer.OrdinalIgnoreCase);
        foreach (var surround in ScanSurroundPlaceholderPattern.Rank(
                     cellText,
                     header,
                     null,
                     placeholderSet,
                     usageScope))
        {
            if (!merged.TryGetValue(surround.ShortCode, out var existing)
                || surround.ScorePercent > existing.ScorePercent)
                merged[surround.ShortCode] = surround;
            else
            {
                merged[surround.ShortCode] = existing with
                {
                    ScorePercent = Math.Min(100, existing.ScorePercent + 8),
                    Reason = existing.Reason + " + surround",
                };
            }
        }

        var rankedMerged = merged.Values
            .OrderByDescending(static a => a.ScorePercent)
            .ThenBy(static a => a.ShortCode, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (rankedMerged.Count == 0)
            return new CellInference(null, ScanFieldConfidence.Medium, scope, Array.Empty<ScanTokenAlternative>());

        var top = rankedMerged[0];
        var confidence = top.ScorePercent >= 80
            ? ScanFieldConfidence.High
            : top.ScorePercent >= 55 ? ScanFieldConfidence.Medium : ScanFieldConfidence.Low;

        return new CellInference(top.Token, confidence, scope, rankedMerged.Take(5).ToList());
    }

    private static CellInference InferCompoundCell(
        string cellText,
        string? header,
        ScanExcelColumnProfiles.Profile profile,
        IReadOnlyList<(UserReportPlaceholderCatalogEntry Entry, int Score)> headerScores,
        ApplicationProfilePlaceholderSet placeholderSet,
        UserReportPlaceholderScope usageScope,
        ScanFieldScope scope)
    {
        var working = cellText.Trim();
        if (!string.IsNullOrWhiteSpace(profile.LiteralPrefix))
        {
            var prefixFolded = TemplateTextNormalizer.NormalizeFolded(profile.LiteralPrefix);
            var textFolded = TemplateTextNormalizer.NormalizeFolded(working);
            if (textFolded.StartsWith(prefixFolded, StringComparison.Ordinal))
            {
                var idx = working.Length - textFolded.Length + prefixFolded.Length;
                working = idx >= 0 && idx <= working.Length ? working[idx..].TrimStart() : working;
            }
        }

        var segments = SplitCompoundSegments(working, profile);
        var tokenParts = new List<string>();
        var allAlternatives = new List<ScanTokenAlternative>();

        for (var i = 0; i < profile.ShortCodes.Length; i++)
        {
            var expectedCode = profile.ShortCodes[i];
            var segment = i < segments.Count ? segments[i].Trim() : string.Empty;

            if (segment.Length == 0)
                continue;

            if (!placeholderSet.Contains(expectedCode))
                continue;

            var entry = placeholderSet.Allowed.First(e =>
                string.Equals(e.ShortCode, expectedCode, StringComparison.OrdinalIgnoreCase));

            var shapeScores = ScanShapeTokenMatcher.ScoreSnippet(
                segment,
                placeholderSet,
                usageScope,
                [expectedCode]);

            var headerBoost = headerScores.FirstOrDefault(h =>
                string.Equals(h.Entry.ShortCode, expectedCode, StringComparison.OrdinalIgnoreCase));

            var surroundHit = ScanSurroundPlaceholderPattern.Rank(
                    segment,
                    header,
                    null,
                    placeholderSet,
                    usageScope)
                .FirstOrDefault(s =>
                    string.Equals(s.ShortCode, expectedCode, StringComparison.OrdinalIgnoreCase));

            var score = Math.Max(
                Math.Max(
                    shapeScores.FirstOrDefault(s =>
                        string.Equals(s.ShortCode, expectedCode, StringComparison.OrdinalIgnoreCase))
                        ?.ScorePercent ?? 0,
                    headerBoost.Entry != null ? headerBoost.Score : 0),
                surroundHit?.ScorePercent ?? 0);

            if (score < 40)
                score = 70;

            tokenParts.Add(entry.BuildWordToken(usageScope));
            allAlternatives.Add(new ScanTokenAlternative(
                entry.BuildWordToken(usageScope),
                expectedCode,
                Math.Min(100, score + 10),
                $"Column segment {i + 1}"));
        }

        if (tokenParts.Count == 0)
            return new CellInference(null, ScanFieldConfidence.Medium, scope, allAlternatives);

        var cellTemplate = BuildCellTemplate(cellText, segments, tokenParts, profile);
        var avgScore = allAlternatives.Count > 0
            ? (int)allAlternatives.Average(static a => a.ScorePercent)
            : 70;

        var confidence = avgScore >= 80
            ? ScanFieldConfidence.High
            : avgScore >= 55 ? ScanFieldConfidence.Medium : ScanFieldConfidence.Low;

        var wholeCellAlt = new ScanTokenAlternative(cellTemplate, "COMPOUND", avgScore, "Column header compound layout");
        var ranked = new List<ScanTokenAlternative> { wholeCellAlt };
        ranked.AddRange(allAlternatives.OrderByDescending(static a => a.ScorePercent));

        return new CellInference(cellTemplate, confidence, scope, ranked.Take(6).ToList());
    }

    private static List<string> SplitCompoundSegments(string text, ScanExcelColumnProfiles.Profile profile)
    {
        if (profile.ShortCodes.Length == 2 && text.Contains(',', StringComparison.Ordinal))
            return text.Split(',', 2).Select(static s => s.Trim()).ToList();

        if (profile.ShortCodes.Length >= 3 && text.Contains(',', StringComparison.Ordinal))
        {
            var commaParts = text.Split(',', 2).Select(static s => s.Trim()).ToList();
            if (commaParts.Count == 2 && commaParts[1].Contains(profile.InnerSeparator))
            {
                var inner = commaParts[1].Split(profile.InnerSeparator, 2).Select(static s => s.Trim()).ToList();
                return [commaParts[0], inner[0], inner.Count > 1 ? inner[1] : string.Empty];
            }

            return commaParts;
        }

        if (text.Contains(profile.InnerSeparator))
            return text.Split(profile.InnerSeparator).Select(static s => s.Trim()).ToList();

        if (text.Contains(',', StringComparison.Ordinal))
            return text.Split(',').Select(static s => s.Trim()).ToList();

        return [text];
    }

    private static string BuildCellTemplate(
        string original,
        IReadOnlyList<string> segments,
        IReadOnlyList<string> tokens,
        ScanExcelColumnProfiles.Profile profile)
    {
        if (tokens.Count == 1)
            return tokens[0];

        if (original.Contains(',', StringComparison.Ordinal) && tokens.Count >= 2)
        {
            if (profile.ShortCodes.Length >= 3 && original.Contains(profile.InnerSeparator))
                return $"{tokens[0]}, {tokens[1]}{profile.InnerSeparator}{tokens[2]}";

            return string.Join(", ", tokens);
        }

        return string.Join($"{profile.InnerSeparator}", tokens);
    }

    private static List<ScanTokenAlternative> MergeScores(
        IReadOnlyList<ScanTokenAlternative> shapeScores,
        IReadOnlyList<(UserReportPlaceholderCatalogEntry Entry, int Score)> headerScores,
        IReadOnlyList<string> preferCodes,
        ApplicationProfilePlaceholderSet placeholderSet,
        UserReportPlaceholderScope usageScope)
    {
        var merged = new Dictionary<string, ScanTokenAlternative>(StringComparer.OrdinalIgnoreCase);

        foreach (var shape in shapeScores)
            merged[shape.ShortCode] = shape;

        foreach (var (entry, score) in headerScores)
        {
            var token = entry.BuildWordToken(
                entry.Scope == UserReportPlaceholderScope.Header
                    ? UserReportPlaceholderScope.Header
                    : usageScope);

            var combined = score + (preferCodes.Contains(entry.ShortCode, StringComparer.OrdinalIgnoreCase) ? 15 : 0);
            if (merged.TryGetValue(entry.ShortCode, out var existing))
            {
                merged[entry.ShortCode] = existing with
                {
                    ScorePercent = Math.Min(100, existing.ScorePercent + score / 2),
                    Reason = existing.Reason + " + column header",
                };
            }
            else
            {
                merged[entry.ShortCode] = new ScanTokenAlternative(
                    token,
                    entry.ShortCode,
                    Math.Min(100, combined),
                    "Column header");
            }
        }

        // Sanaw "Raýatlygy" is the ISO code column (PNAT). A catalog exact-label
        // hit for the nationality *name* (PNTM) must not outrank the column profile.
        foreach (var prefer in preferCodes)
        {
            if (!merged.TryGetValue(prefer, out var existing))
                continue;

            merged[prefer] = existing with
            {
                ScorePercent = Math.Min(100, existing.ScorePercent + 20),
            };
        }

        return merged.Values
            .OrderByDescending(static c => c.ScorePercent)
            .ThenBy(static c => c.ShortCode, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static ScanFieldScope DetermineScope(
        ScanExcelColumnProfiles.Profile? profile,
        IReadOnlyList<(UserReportPlaceholderCatalogEntry Entry, int Score)> headerScores,
        int dataRow)
    {
        var codes = profile?.ShortCodes
            ?? headerScores.Select(static h => h.Entry.ShortCode).Take(1).ToArray();

        if (codes.Any(static c => c is "ACPOS" or "ACFNM" or "VPER" or "VCAT" or "ADAT" or "AFNUM"))
            return ScanFieldScope.Header;

        // Sanaw yellow sits on the sample data row under column headers. That row is often 5,
        // but some officer files put headers on row 3 and yellow on row 4 — do not use a
        // magic row number or those cells become {{ds.PLN}} (header) and Approve blocks.
        if (profile != null
            || headerScores.Any(static h =>
                h.Entry.Scope is UserReportPlaceholderScope.Row or UserReportPlaceholderScope.Both))
        {
            return ScanFieldScope.Row;
        }

        return dataRow >= 5 ? ScanFieldScope.Row : ScanFieldScope.Header;
    }
}

internal static class ScanExcelWorkbookHelper
{
    public static int? FindHeaderRow(IXLWorksheet sheet, int columnNumber, int dataRow)
    {
        for (var row = dataRow - 1; row >= Math.Max(1, dataRow - 20); row--)
        {
            if (!string.IsNullOrWhiteSpace(ReadCellText(sheet.Cell(row, columnNumber))))
                return row;
        }

        return null;
    }

    public static string? GetColumnHeader(IXLWorksheet sheet, int columnNumber, int dataRow)
    {
        var row = FindHeaderRow(sheet, columnNumber, dataRow);
        return row is int headerRow
            ? ReadCellText(sheet.Cell(headerRow, columnNumber))
            : null;
    }

    public static string ReadCellText(IXLCell cell)
    {
        if (cell.DataType == XLDataType.DateTime && cell.TryGetValue(out DateTime dateTime) && dateTime.Year > 1)
            return dateTime.ToString("dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture);

        var text = cell.GetFormattedString()?.Trim() ?? string.Empty;
        if (text.Length == 0)
            text = cell.GetString()?.Trim() ?? string.Empty;
        return text;
    }
}

internal static class ScanExcelColumnProfiles
{
    internal sealed record Profile(
        string[] HeaderKeys,
        string[] ShortCodes,
        bool IsCompound,
        string? LiteralPrefix = null,
        char InnerSeparator = '/');

    private static readonly Profile[] Known =
    [
        new(["№", "no", "setir belgisi"], ["RNUM"], false),
        new(["familiyasy", "familiya", "soyad"], ["PLN"], false),
        new(["ady", " ady"], ["PFNM"], false),
        new(["doglan senesi we yeri", "doglan senesi", "dogum"], ["PDBT", "PCBT", "PBPL"], true),
        new(["jynsy", "cinsiyet", "gender"], ["PGND"], false),
        new(["nikasy", "marital"], ["PMST"], false),
        new(["rayatlygy", "uyruk", "nationality"], ["PNAT"], false),
        new(["rayatlyk ady", "nationality name"], ["PNTM"], false),
        new(["pasport belgisi we mohleti", "pasport belgisi", "pasport"], ["PPN", "PPED"], true),
        new(["pasport gornusi", "pasport tipi", "passport type"], ["PPTP"], false),
        new(["pasport edarasy", "berlen edara", "authority"], ["PPAT"], false),
        new(["berlen yurt", "pasport yurdy", "issued country"], ["PPCC", "PPCT"], true),
        new(["bilimi we okan yeri", "bilimi", "egitim"], ["EGLV", "EGIY"], true),
        new(["okan yeri", "okuw jayy", "institution"], ["EGIN"], false),
        new(["bitiren yyl", "graduation year", "mezuniyet"], ["EGYR"], false),
        new(["bilimine gora hunari", "hunari", "specialty"], ["EGSP"], false),
        new(["wezipesi", "wezepe", "pozisyon", "position"], ["POSN"], false),
        new(["onki islan yerleri", "previous workplaces"], ["PWTM"], false),
        new(["wiza ucin masgala", "family members for visa", "visa application family"], ["PVFM"], false),
        new(["gelmeginin maksady", "gelmegin maksady", "purpose of arrival"], ["RGEL"], false),
        new(["cagyran tarap", "inviting party"], ["ACNAM"], false),
        new(["mohleti we gezekligi", "gezeklik", "wiza"], ["AVPRD", "AVCAT"], true, LiteralPrefix: "cakylyk "),
        new(["turkmenistandaky salgysy", "turkmenistandaki"], ["ADRS"], false),
        new(["dasary yurtdaky salgysy", "dasary yurt"], ["PFAC", "PFAD"], true),
        new(["barjak serhet yakasy", "serhet yaka", "border zone"], ["ABZLN"], false),
        new(["sahamcanyn mudiri", "gol cekiji wezipesi"], ["ACPOS"], false),
    ];

    public static Profile? Match(string? headerText)
    {
        var folded = TemplateTextNormalizer.NormalizeFolded(headerText);
        if (folded.Length == 0)
            return null;

        Profile? best = null;
        var bestScore = 0;

        foreach (var profile in Known)
        {
            foreach (var key in profile.HeaderKeys)
            {
                var score = 0;
                if (string.Equals(folded, key, StringComparison.Ordinal))
                    score = 100;
                else if (folded.Contains(key, StringComparison.Ordinal) || key.Contains(folded, StringComparison.Ordinal))
                    score = 75;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = profile;
                }
            }
        }

        return bestScore >= 55 ? best : null;
    }
}
