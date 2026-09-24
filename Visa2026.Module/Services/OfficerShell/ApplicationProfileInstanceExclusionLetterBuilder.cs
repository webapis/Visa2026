using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using DevExpress.ExpressApp;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.OfficerShell;

public sealed record ApplicationProfileInstanceExclusionPlaceholder(string Key, string Label, string Example, bool IsRow)
{
    public string Token => IsRow ? "{{." + Key + "}}" : "{{ds." + Key + "}}";
}

public sealed record ApplicationProfileInstanceExclusionDocument(byte[] Content, string FileName, string ContentType);

public sealed record ApplicationProfileInstanceExclusionTemplateFile(byte[] Content, string FileName, bool IsCustom);

public sealed class ApplicationProfileInstanceExclusionTemplateValidation
{
    public List<string> Errors { get; } = new();
    public List<string> UnknownTokens { get; } = new();
    public bool IsValid => Errors.Count == 0 && UnknownTokens.Count == 0;
}

/// <summary>
/// Seretmezlik documents: the letter (Word) and the separate roster — Sanaw (Word or Excel).
/// Company-wide templates live in <see cref="ApplicationProfileInstanceExclusionTemplate"/>; with no upload the
/// built-in layout is used. Documents are regenerated from the current template each time.
/// Tokens: <c>{{ds.Key}}</c>; one repeated row per excluded person between <c>{{#ds.People}}</c> and <c>{{/ds.People}}</c>
/// with <c>{{.Field}}</c> inside.
/// </summary>
public static class ApplicationProfileInstanceExclusionLetterBuilder
{
    public const string LoopName = "People";
    public const string LoopOpenToken = "{{#ds.People}}";
    public const string LoopCloseToken = "{{/ds.People}}";
    public const string WordContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    public const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private const string FontName = "Times New Roman";
    private const string FontSizeHalfPts = "28";

    public static IReadOnlyList<ApplicationProfileInstanceExclusionPlaceholder> Placeholders { get; } =
    [
        new("LetterNumber", "Letter №", "01/-02", false),
        new("LetterDate", "Letter date", "15.01.2026", false),
        new("AddresseeName", "Addressee (as printed)", "Türkmenistanyň Döwlet migrasiýa gullugyna", false),
        new("Salutation", "Salutation", "Hormatly Abdulla Muhammetgeldiýewiç!", false),
        new("ReferenceMinistryName", "Refers to (ministry)", "Energetika ministrligi", false),
        new("ReferenceMinistryGenitive", "Refers to (ministry), genitive", "Energetika ministrliginiň", false),
        new("ReferenceLetterDate", "Ministry letter date", "15.01.2026", false),
        new("ReferenceLetterNumber", "Ministry letter №", "7/202", false),
        new("Subject", "Subject of the original application", "köp gezeklik wizalaryny we iş rugsatnamalaryny uzaltmak", false),
        new("OriginalRosterCount", "Original roster (people)", "13", false),
        new("ExcludedCount", "Excluded people (number)", "1", false),
        new("ExcludedCountText", "Excluded people (words)", "bir", false),
        new("ApplicationNumber", "Case application №", "01/-01", false),
        new("CompanyName", "Company name", "Çalyk Enerji", false),
        new("SignatoryName", "Signatory name", "Mehmet ÇIRAK", false),
        new("SignatoryPosition", "Signatory position", "Türkmenistandaky şahamçasynyň müdiri", false),
        new("Index", "Row №", "1", true),
        new("FullName", "Full name", "Enes Can Uzun", true),
        new("LastName", "Last name", "Uzun", true),
        new("FirstName", "First name", "Enes", true),
        new("MiddleName", "Middle name", "Can", true),
        new("PassportNumber", "Passport №", "U34537060", true),
        new("DateOfBirth", "Date of birth", "01.02.1990", true),
        new("BirthPlace", "Place of birth", "Ankara", true),
        new("Nationality", "Nationality", "Türkiýe", true),
    ];

    public static string KindLabel(ApplicationProfileInstanceExclusionTemplateKind kind) =>
        kind == ApplicationProfileInstanceExclusionTemplateKind.Roster ? "Roster (Sanaw)" : "Letter";

    public static ApplicationProfileInstanceExclusionDocument Generate(
        IObjectSpace objectSpace,
        ApplicationProfileInstanceExclusion exclusion,
        ApplicationProfileInstanceExclusionTemplateKind kind)
    {
        ArgumentNullException.ThrowIfNull(objectSpace);
        ArgumentNullException.ThrowIfNull(exclusion);

        var template = GetTemplate(objectSpace, kind);
        var data = BuildMergeData(exclusion, exclusion.ApplicationProfileInstance, objectSpace);
        var isExcel = IsExcel(template.FileName);
        var content = isExcel ? MergeExcel(template.Content, data) : Merge(template.Content, data);
        return new ApplicationProfileInstanceExclusionDocument(
            content,
            BuildFileName(exclusion, kind, isExcel ? ".xlsx" : ".docx"),
            isExcel ? ExcelContentType : WordContentType);
    }

    /// <summary>Uploaded company template for <paramref name="kind"/>, otherwise the built-in layout.</summary>
    public static ApplicationProfileInstanceExclusionTemplateFile GetTemplate(
        IObjectSpace objectSpace,
        ApplicationProfileInstanceExclusionTemplateKind kind)
    {
        var custom = FindTemplateRow(objectSpace, kind);
        if (custom?.TemplateFile?.Content is { Length: > 0 } bytes)
            return new ApplicationProfileInstanceExclusionTemplateFile(bytes, custom.TemplateFile.FileName, IsCustom: true);

        return GetBuiltInTemplate(kind);
    }

    public static ApplicationProfileInstanceExclusionTemplateFile GetBuiltInTemplate(ApplicationProfileInstanceExclusionTemplateKind kind) =>
        kind == ApplicationProfileInstanceExclusionTemplateKind.Roster
            ? new ApplicationProfileInstanceExclusionTemplateFile(BuildDefaultRosterTemplate(), "Seretmezlik_sanaw_template.docx", IsCustom: false)
            : new ApplicationProfileInstanceExclusionTemplateFile(BuildDefaultTemplate(), "Seretmezlik_haty_template.docx", IsCustom: false);

    public static ApplicationProfileInstanceExclusionTemplate? FindTemplateRow(
        IObjectSpace objectSpace,
        ApplicationProfileInstanceExclusionTemplateKind kind) =>
        objectSpace.GetObjectsQuery<ApplicationProfileInstanceExclusionTemplate>()
            .Include(t => t.TemplateFile)
            .Where(t => t.Kind == kind)
            .AsEnumerable()
            .OrderByDescending(t => t.UpdatedOnUtc)
            .FirstOrDefault();

    public static string BuildFileName(
        ApplicationProfileInstanceExclusion exclusion,
        ApplicationProfileInstanceExclusionTemplateKind kind = ApplicationProfileInstanceExclusionTemplateKind.Letter,
        string extension = ".docx")
    {
        var number = string.IsNullOrWhiteSpace(exclusion.LetterNumber) ? "draft" : exclusion.LetterNumber;
        var safe = new string(number.Select(c => Path.GetInvalidFileNameChars().Contains(c) || c == ' ' ? '_' : c).ToArray());
        var prefix = kind == ApplicationProfileInstanceExclusionTemplateKind.Roster ? "Seretmezlik_sanaw_" : "Seretmezlik_";
        return prefix + safe + extension;
    }

    public static bool IsExcel(string? fileName) =>
        (fileName ?? string.Empty).EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase);

    public static bool IsWord(string? fileName) =>
        (fileName ?? string.Empty).EndsWith(".docx", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Checks an upload: file type per kind, only Seretmezlik placeholders, roster repeats a row per person,
    /// and a trial merge with sample data succeeds.
    /// </summary>
    public static ApplicationProfileInstanceExclusionTemplateValidation ValidateTemplate(
        ApplicationProfileInstanceExclusionTemplateKind kind,
        string fileName,
        byte[] content)
    {
        var result = new ApplicationProfileInstanceExclusionTemplateValidation();
        if (content == null || content.Length == 0)
        {
            result.Errors.Add("The file is empty.");
            return result;
        }

        var isExcel = IsExcel(fileName);
        if (kind == ApplicationProfileInstanceExclusionTemplateKind.Letter && !IsWord(fileName))
        {
            result.Errors.Add("The letter template must be a Word file (.docx).");
            return result;
        }

        if (!isExcel && !IsWord(fileName))
        {
            result.Errors.Add("Only Word (.docx) or Excel (.xlsx) files are supported. Save older .doc or .xls files in the new format first.");
            return result;
        }

        IReadOnlyList<string> tokens;
        try
        {
            tokens = isExcel ? ExtractExcelTokens(content) : ExtractWordTokens(content);
        }
        catch (Exception ex)
        {
            result.Errors.Add("The file could not be read: " + ex.Message);
            return result;
        }

        var headerKeys = Placeholders.Where(p => !p.IsRow).Select(p => p.Key).ToHashSet(StringComparer.Ordinal);
        var rowKeys = Placeholders.Where(p => p.IsRow).Select(p => p.Key).ToHashSet(StringComparer.Ordinal);
        var hasLoopOpen = false;
        var hasLoopClose = false;
        foreach (var raw in tokens)
        {
            var token = raw.Trim();
            if (token == "#ds." + LoopName) { hasLoopOpen = true; continue; }
            if (token == "/ds." + LoopName) { hasLoopClose = true; continue; }
            if (token.StartsWith("ds.", StringComparison.Ordinal) && headerKeys.Contains(token[3..])) continue;
            if (token.StartsWith(".", StringComparison.Ordinal) && rowKeys.Contains(token[1..])) continue;
            var display = "{{" + token + "}}";
            if (!result.UnknownTokens.Contains(display))
                result.UnknownTokens.Add(display);
        }

        if (hasLoopOpen != hasLoopClose)
            result.Errors.Add($"The people row must start with {LoopOpenToken} and end with {LoopCloseToken}.");
        if (kind == ApplicationProfileInstanceExclusionTemplateKind.Roster && !hasLoopOpen)
            result.Errors.Add($"The roster must repeat one row per person: put {LoopOpenToken} in the first cell of the row and {LoopCloseToken} in the last.");
        if (!hasLoopOpen && tokens.Any(t => t.Trim().StartsWith(".", StringComparison.Ordinal)))
            result.Errors.Add($"Row fields such as {{{{.FullName}}}} must be inside the {LoopOpenToken} … {LoopCloseToken} row.");

        if (result.Errors.Count > 0 || result.UnknownTokens.Count > 0)
            return result;

        try
        {
            var sample = BuildSampleMergeData();
            _ = isExcel ? MergeExcel(content, sample) : Merge(content, sample);
        }
        catch (Exception ex)
        {
            result.Errors.Add("A test merge with sample data failed: " + ex.Message);
        }

        return result;
    }

    public static byte[] Merge(byte[] template, Dictionary<string, object> data)
    {
        using var templateStream = new MemoryStream(template, 0, template.Length, writable: false, publiclyVisible: true);
        var docx = DocxTemplateFactory.Open(templateStream);
        docx.BindModel("ds", data);
        using var merged = new MemoryStream();
        docx.Save(merged);
        return merged.ToArray();
    }

    /// <summary>Excel roster: header tokens anywhere; the row holding <c>{{#ds.People}}</c> is repeated once per person.</summary>
    public static byte[] MergeExcel(byte[] template, Dictionary<string, object> data)
    {
        using var templateStream = new MemoryStream(template, 0, template.Length, writable: false, publiclyVisible: true);
        using var workbook = new XLWorkbook(templateStream);
        var people = data.TryGetValue(LoopName, out var list) && list is List<Dictionary<string, object>> rows
            ? rows
            : new List<Dictionary<string, object>>();

        foreach (var worksheet in workbook.Worksheets)
        {
            var loopRows = worksheet.CellsUsed()
                .Where(c => c.GetFormattedString().Contains(LoopOpenToken, StringComparison.Ordinal))
                .Select(c => c.Address.RowNumber)
                .Distinct()
                .OrderByDescending(r => r)
                .ToList();
            var loopSet = loopRows.ToHashSet();

            foreach (var cell in worksheet.CellsUsed().ToList())
            {
                if (loopSet.Contains(cell.Address.RowNumber))
                    continue;
                var text = cell.GetFormattedString();
                if (text.Contains("{{", StringComparison.Ordinal))
                    cell.Value = ReplaceTokens(text, data, row: null);
            }

            var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 1;
            foreach (var rowNumber in loopRows)
            {
                var snapshot = new Dictionary<int, string>();
                for (var column = 1; column <= lastColumn; column++)
                {
                    var text = worksheet.Cell(rowNumber, column).GetFormattedString();
                    if (!string.IsNullOrEmpty(text))
                        snapshot[column] = text;
                }

                if (people.Count > 1)
                    worksheet.Row(rowNumber).InsertRowsBelow(people.Count - 1);

                if (people.Count == 0)
                {
                    foreach (var (column, text) in snapshot)
                        worksheet.Cell(rowNumber, column).Value = ReplaceTokens(text, data, new Dictionary<string, object>());
                    continue;
                }

                for (var i = 0; i < people.Count; i++)
                {
                    foreach (var (column, text) in snapshot)
                        worksheet.Cell(rowNumber + i, column).Value = ReplaceTokens(text, data, people[i]);
                }
            }
        }

        using var output = new MemoryStream();
        workbook.SaveAs(output);
        return output.ToArray();
    }

    public static Dictionary<string, object> BuildMergeData(
        ApplicationProfileInstanceExclusion exclusion,
        ApplicationProfileInstance? instance,
        IObjectSpace? objectSpace = null)
    {
        var people = (exclusion.People ?? new List<ApplicationProfileInstanceExclusionPerson>())
            .OrderBy(p => p.Sequence)
            .Select((p, i) => BuildRow(i + 1, p))
            .ToList();

        var companyName = string.Empty;
        var signatoryName = string.Empty;
        var signatoryPosition = string.Empty;
        if (instance != null)
        {
            signatoryName = instance.Application_CompanyHead_FullName;
            signatoryPosition = instance.Application_CompanyHead_PositionTm;
            companyName = ApplicationProfileInstanceOrganizationLetterheadHelper.Resolve(instance, objectSpace).CompanyName ?? string.Empty;
        }

        return new Dictionary<string, object>
        {
            ["LetterNumber"] = exclusion.LetterNumber ?? string.Empty,
            ["LetterDate"] = FormatDate(exclusion.LetterDate),
            ["AddresseeName"] = exclusion.AddresseeName ?? string.Empty,
            ["Salutation"] = exclusion.Salutation ?? string.Empty,
            ["ReferenceMinistryName"] = exclusion.ReferenceMinistryName ?? string.Empty,
            ["ReferenceMinistryGenitive"] = TurkmenGenitive(exclusion.ReferenceMinistryName),
            ["ReferenceLetterDate"] = FormatDate(exclusion.ReferenceLetterDate),
            ["ReferenceLetterNumber"] = exclusion.ReferenceLetterNumber ?? string.Empty,
            ["Subject"] = exclusion.Subject ?? string.Empty,
            ["OriginalRosterCount"] = exclusion.OriginalRosterCount.ToString(CultureInfo.InvariantCulture),
            ["ExcludedCount"] = people.Count.ToString(CultureInfo.InvariantCulture),
            ["ExcludedCountText"] = people.Count > 0 ? ApplicationProfileInstance.NumberToTurkmenWords(people.Count) : string.Empty,
            ["ApplicationNumber"] = instance?.FullApplicationNumber ?? string.Empty,
            ["CompanyName"] = companyName,
            ["SignatoryName"] = signatoryName ?? string.Empty,
            ["SignatoryPosition"] = signatoryPosition ?? string.Empty,
            [LoopName] = people,
        };
    }

    /// <summary>Merge data filled from the placeholder catalog examples (upload trial merge, template preview).</summary>
    public static Dictionary<string, object> BuildSampleMergeData()
    {
        var data = Placeholders.Where(p => !p.IsRow).ToDictionary(p => p.Key, p => (object)p.Example);
        var row = Placeholders.Where(p => p.IsRow).ToDictionary(p => p.Key, p => (object)p.Example);
        var second = new Dictionary<string, object>(row) { ["Index"] = "2" };
        data[LoopName] = new List<Dictionary<string, object>> { row, second };
        return data;
    }

    /// <summary>Dative for a recipient block: <c>… gullugy</c> → <c>… gullugyna</c>, <c>… ministrligi</c> → <c>… ministrligine</c>.</summary>
    public static string TurkmenDative(string? name) =>
        string.IsNullOrWhiteSpace(name) ? string.Empty : ApplicationProfileInstance.AddTurkmenCase(name.Trim(), "na", "ne");

    /// <summary>Genitive: <c>Energetika ministrligi</c> → <c>Energetika ministrliginiň</c>.</summary>
    public static string TurkmenGenitive(string? name) =>
        string.IsNullOrWhiteSpace(name) ? string.Empty : ApplicationProfileInstance.AddTurkmenCase(name.Trim(), "nyň", "niň");

    public static byte[] BuildDefaultTemplate()
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            body.Append(Paragraph("№ {{ds.LetterNumber}}", JustificationValues.Left));
            body.Append(Paragraph("{{ds.LetterDate}}ý.", JustificationValues.Left));
            body.Append(Paragraph(string.Empty, JustificationValues.Left));
            body.Append(Paragraph("{{ds.AddresseeName}}", JustificationValues.Right, bold: true));
            body.Append(Paragraph(string.Empty, JustificationValues.Left));
            body.Append(Paragraph("{{ds.Salutation}}", JustificationValues.Center, bold: true));
            body.Append(Paragraph(
                "{{ds.CompanyName}} Size hormat goýýandygyny beýan edip, {{ds.ReferenceMinistryGenitive}} "
                + "{{ds.ReferenceLetterDate}}ý. seneli {{ds.ReferenceLetterNumber}} belgili {{ds.Subject}} barada "
                + "Size ýüzlenip ýazan {{ds.OriginalRosterCount}} adamlyk hatyndan aşakdaky:",
                JustificationValues.Both,
                firstLineIndent: true));
            body.Append(BorderlessPeopleTable());
            body.Append(Paragraph(
                "{{ds.ExcludedCountText}} işgäriň {{ds.Subject}} baradaky meselä seretmezligiňizi Sizden uly haýyş edýäris.",
                JustificationValues.Both,
                firstLineIndent: true));
            body.Append(Paragraph(string.Empty, JustificationValues.Left));
            body.Append(Paragraph("Hormatlamak bilen,", JustificationValues.Left));
            body.Append(Paragraph("{{ds.SignatoryPosition}}", JustificationValues.Left, bold: true));
            body.Append(Paragraph("{{ds.SignatoryName}}", JustificationValues.Right, bold: true));

            mainPart.Document.Save();
        }

        return stream.ToArray();
    }

    public static byte[] BuildDefaultRosterTemplate()
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            body.Append(Paragraph("{{ds.LetterDate}}ý. seneli № {{ds.LetterNumber}} hata goşundy", JustificationValues.Right));
            body.Append(Paragraph(string.Empty, JustificationValues.Left));
            body.Append(Paragraph("SANAW", JustificationValues.Center, bold: true));
            body.Append(Paragraph(
                "{{ds.Subject}} baradaky meselesine seredilmezligi haýyş edilýän işgärleriň",
                JustificationValues.Center));
            body.Append(Paragraph(string.Empty, JustificationValues.Left));
            body.Append(RosterTable());
            body.Append(Paragraph(string.Empty, JustificationValues.Left));
            body.Append(Paragraph("{{ds.SignatoryPosition}}", JustificationValues.Left, bold: true));
            body.Append(Paragraph("{{ds.SignatoryName}}", JustificationValues.Right, bold: true));

            mainPart.Document.Save();
        }

        return stream.ToArray();
    }

    private static Dictionary<string, object> BuildRow(int index, ApplicationProfileInstanceExclusionPerson line)
    {
        var person = line.Person;
        return new Dictionary<string, object>
        {
            ["Index"] = index.ToString(CultureInfo.InvariantCulture),
            ["FullName"] = line.FullName ?? string.Empty,
            ["LastName"] = person?.LastName ?? string.Empty,
            ["FirstName"] = person?.FirstName ?? string.Empty,
            ["MiddleName"] = person?.MiddleName ?? string.Empty,
            ["PassportNumber"] = line.PassportNumber ?? string.Empty,
            ["DateOfBirth"] = person != null ? FormatDate(person.DateOfBirth) : string.Empty,
            ["BirthPlace"] = person?.BirthPlace ?? string.Empty,
            ["Nationality"] = !string.IsNullOrWhiteSpace(person?.Nationality?.NameTm)
                ? person.Nationality.NameTm
                : person?.Nationality?.Name ?? string.Empty,
        };
    }

    private static string ReplaceTokens(string text, IReadOnlyDictionary<string, object> header, IReadOnlyDictionary<string, object>? row) =>
        UserReportPlaceholderPatterns.PlaceholderRegex.Replace(text, match =>
        {
            var token = match.Groups[1].Value.Trim();
            if (token.StartsWith("#", StringComparison.Ordinal) || token.StartsWith("/", StringComparison.Ordinal))
                return string.Empty;
            if (token.StartsWith(".", StringComparison.Ordinal))
                return row != null && row.TryGetValue(token[1..], out var rowValue) ? rowValue?.ToString() ?? string.Empty : string.Empty;
            var key = token.StartsWith("ds.", StringComparison.Ordinal) ? token[3..] : token;
            return header.TryGetValue(key, out var value) && value is not IEnumerable<Dictionary<string, object>>
                ? value?.ToString() ?? string.Empty
                : string.Empty;
        });

    private static IReadOnlyList<string> ExtractWordTokens(byte[] content)
    {
        using var stream = new MemoryStream(content, writable: false);
        using var document = WordprocessingDocument.Open(stream, false);
        var main = document.MainDocumentPart;
        if (main?.Document?.Body == null)
            return Array.Empty<string>();

        var texts = new List<string>();
        texts.AddRange(main.Document.Body.Descendants<Paragraph>().Select(p => p.InnerText));
        foreach (var header in main.HeaderParts)
            texts.AddRange(header.Header?.Descendants<Paragraph>().Select(p => p.InnerText) ?? Enumerable.Empty<string>());
        foreach (var footer in main.FooterParts)
            texts.AddRange(footer.Footer?.Descendants<Paragraph>().Select(p => p.InnerText) ?? Enumerable.Empty<string>());
        return ExtractTokens(texts);
    }

    private static IReadOnlyList<string> ExtractExcelTokens(byte[] content)
    {
        using var stream = new MemoryStream(content, writable: false);
        using var workbook = new XLWorkbook(stream);
        var texts = workbook.Worksheets.SelectMany(ws => ws.CellsUsed()).Select(c => c.GetFormattedString());
        return ExtractTokens(texts);
    }

    private static IReadOnlyList<string> ExtractTokens(IEnumerable<string> texts) =>
        texts
            .Where(t => !string.IsNullOrEmpty(t))
            .SelectMany(t => UserReportPlaceholderPatterns.PlaceholderRegex.Matches(t).Select(m => m.Groups[1].Value))
            .ToList();

    private static string FormatDate(DateTime? value) =>
        value is DateTime d && d != default ? d.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) : string.Empty;

    private static Table BorderlessPeopleTable()
    {
        var table = new Table(new TableProperties(
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
            new TableBorders(
                new TopBorder { Val = BorderValues.None },
                new LeftBorder { Val = BorderValues.None },
                new BottomBorder { Val = BorderValues.None },
                new RightBorder { Val = BorderValues.None },
                new InsideHorizontalBorder { Val = BorderValues.None },
                new InsideVerticalBorder { Val = BorderValues.None })));
        table.Append(new TableGrid(new GridColumn { Width = "700" }, new GridColumn { Width = "8600" }));
        table.Append(new TableRow(
            Cell("{{#ds.People}}{{.Index}}.", "700"),
            Cell("{{.FullName}} – {{.PassportNumber}}{{/ds.People}}", "8600")));
        return table;
    }

    private static Table RosterTable()
    {
        var line = BorderValues.Single;
        var table = new Table(new TableProperties(
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
            new TableBorders(
                new TopBorder { Val = line, Size = 4 },
                new LeftBorder { Val = line, Size = 4 },
                new BottomBorder { Val = line, Size = 4 },
                new RightBorder { Val = line, Size = 4 },
                new InsideHorizontalBorder { Val = line, Size = 4 },
                new InsideVerticalBorder { Val = line, Size = 4 })));
        string[] widths = ["600", "3400", "1900", "1600", "1800"];
        table.Append(new TableGrid(widths.Select(w => new GridColumn { Width = w }).ToArray()));
        table.Append(new TableRow(
            Cell("T/b", widths[0], bold: true, center: true),
            Cell("Familiýasy, ady", widths[1], bold: true, center: true),
            Cell("Pasport belgisi", widths[2], bold: true, center: true),
            Cell("Doglan senesi", widths[3], bold: true, center: true),
            Cell("Raýatlygy", widths[4], bold: true, center: true)));
        table.Append(new TableRow(
            Cell("{{#ds.People}}{{.Index}}", widths[0], center: true),
            Cell("{{.FullName}}", widths[1]),
            Cell("{{.PassportNumber}}", widths[2], center: true),
            Cell("{{.DateOfBirth}}", widths[3], center: true),
            Cell("{{.Nationality}}{{/ds.People}}", widths[4], center: true)));
        return table;
    }

    private static TableCell Cell(string text, string width, bool bold = false, bool center = false) =>
        new(
            new TableCellProperties(new TableCellWidth { Width = width, Type = TableWidthUnitValues.Dxa }),
            Paragraph(text, center ? JustificationValues.Center : JustificationValues.Left, bold: bold));

    private static Paragraph Paragraph(
        string text,
        JustificationValues justification,
        bool bold = false,
        bool firstLineIndent = false)
    {
        var properties = new ParagraphProperties(
            new SpacingBetweenLines { After = "0", Line = "276", LineRule = LineSpacingRuleValues.Auto });
        if (firstLineIndent)
            properties.Append(new Indentation { FirstLine = "709" });
        properties.Append(new Justification { Val = justification });

        var runProperties = new RunProperties(
            new RunFonts { Ascii = FontName, HighAnsi = FontName, ComplexScript = FontName });
        if (bold)
            runProperties.Append(new Bold());
        runProperties.Append(new FontSize { Val = FontSizeHalfPts });

        var paragraph = new Paragraph(properties);
        if (!string.IsNullOrEmpty(text))
            paragraph.Append(new Run(runProperties, new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
        return paragraph;
    }
}
