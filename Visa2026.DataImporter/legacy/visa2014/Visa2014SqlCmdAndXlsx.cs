using System.Globalization;
using System.Security;
using System.Text;
using Microsoft.Data.SqlClient;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014SqlCmdReader
{
    public static IReadOnlyList<IReadOnlyDictionary<string, string?>> Query(
        string connectionString,
        string sql,
        bool verbose)
    {
        var rows = new List<IReadOnlyDictionary<string, string?>>();

        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = new SqlCommand(sql, connection) { CommandTimeout = 180 };
        using var reader = command.ExecuteReader();

        var fieldCount = reader.FieldCount;
        var columnNames = new string[fieldCount];
        for (int i = 0; i < fieldCount; i++)
            columnNames[i] = reader.GetName(i);

        while (reader.Read())
        {
            var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < fieldCount; i++)
                dict[columnNames[i]] = reader.IsDBNull(i) ? null : FormatSqlValue(reader.GetValue(i));

            rows.Add(dict);
        }

        if (verbose)
            Console.WriteLine($"  SQL reader returned {rows.Count} row(s).");

        return rows;
    }

    private static string? FormatSqlValue(object value) => value switch
    {
        null => null,
        DateTime dt => dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        DateTimeOffset dto => dto.ToString("yyyy-MM-dd HH:mm:ss.fffffff zzz", CultureInfo.InvariantCulture),
        bool b => b ? "1" : "0",
        byte[] bytes => Convert.ToBase64String(bytes),
        Guid g => g.ToString("D"),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture),
    };
}

internal static class Visa2014MinimalXlsxWriter
{
    /// <summary>
    /// Writes the workbook to <paramref name="outputPath"/> when possible.
    /// If the target is locked (e.g. open in Excel), writes a timestamped sibling file instead and returns that path.
    /// </summary>
    public static string WriteWorkbook(string outputPath, IReadOnlyList<Visa2014Worksheet> worksheets)
    {
        var fullPath = Path.GetFullPath(outputPath);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new InvalidOperationException($"Invalid output path: {outputPath}");
        Directory.CreateDirectory(directory);

        var tempPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            WriteWorkbookToFile(tempPath, worksheets);
            if (TryReplaceFile(tempPath, fullPath))
                return fullPath;

            var fallbackPath = Path.Combine(
                directory,
                $"{Path.GetFileNameWithoutExtension(fullPath)}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx");
            File.Move(tempPath, fallbackPath, overwrite: true);
            return fallbackPath;
        }
        finally
        {
            TryDeleteQuietly(tempPath);
        }
    }

    private static void WriteWorkbookToFile(string filePath, IReadOnlyList<Visa2014Worksheet> worksheets)
    {
        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var zip = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Create);

        WriteEntry(zip, "[Content_Types].xml", BuildContentTypes(worksheets.Count));
        WriteEntry(zip, "_rels/.rels", RootRels());
        WriteEntry(zip, "xl/workbook.xml", WorkbookXml(worksheets));
        WriteEntry(zip, "xl/_rels/workbook.xml.rels", WorkbookRels(worksheets.Count));
        WriteEntry(zip, "xl/styles.xml", StylesXml());

        for (int i = 0; i < worksheets.Count; i++)
            WriteEntry(zip, $"xl/worksheets/sheet{i + 1}.xml", WorksheetXml(worksheets[i]));
    }

    private static bool TryReplaceFile(string sourcePath, string destinationPath)
    {
        const int maxAttempts = 5;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                if (File.Exists(destinationPath))
                    File.Delete(destinationPath);

                File.Move(sourcePath, destinationPath);
                return true;
            }
            catch (IOException) when (attempt < maxAttempts)
            {
                Thread.Sleep(150 * attempt);
            }
            catch (UnauthorizedAccessException) when (attempt < maxAttempts)
            {
                Thread.Sleep(150 * attempt);
            }
        }

        return false;
    }

    private static void TryDeleteQuietly(string path)
    {
        if (!File.Exists(path))
            return;

        try { File.Delete(path); }
        catch { /* best effort cleanup */ }
    }

    private static void WriteEntry(System.IO.Compression.ZipArchive zip, string path, string content)
    {
        var entry = zip.CreateEntry(path, System.IO.Compression.CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private static string BuildContentTypes(int sheetCount)
    {
        var sb = new StringBuilder();
        sb.Append("""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
              <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
              <Default Extension="xml" ContentType="application/xml"/>
              <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
              <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
            """);

        for (int i = 1; i <= sheetCount; i++)
        {
            sb.Append("<Override PartName=\"/xl/worksheets/sheet")
                .Append(i)
                .Append(".xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
        }

        sb.Append("</Types>");
        return sb.ToString();
    }

    private static string RootRels() =>
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
        </Relationships>
        """;

    private static string WorkbookRels(int sheetCount)
    {
        var sb = new StringBuilder("""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
            """);

        for (int i = 1; i <= sheetCount; i++)
        {
            sb.Append("<Relationship Id=\"rId").Append(i).Append("\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet").Append(i).Append(".xml\"/>");
        }

        sb.Append("<Relationship Id=\"rId").Append(sheetCount + 1).Append("\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>");
        sb.Append("</Relationships>");
        return sb.ToString();
    }

    private static string WorkbookXml(IReadOnlyList<Visa2014Worksheet> worksheets)
    {
        var sb = new StringBuilder("""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
              <sheets>
            """);

        for (int i = 0; i < worksheets.Count; i++)
        {
            sb.Append("<sheet name=\"")
                .Append(EscapeXml(worksheets[i].Name))
                .Append("\" sheetId=\"")
                .Append(i + 1)
                .Append("\" r:id=\"rId")
                .Append(i + 1)
                .Append("\"/>");
        }

        sb.Append("</sheets></workbook>");
        return sb.ToString();
    }

    private static string StylesXml() =>
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
          <fonts count="1"><font/></fonts>
          <fills count="1"><fill><patternFill patternType="none"/></fill></fills>
          <borders count="1"><border/></borders>
          <cellStyleXfs count="1"><xf/></cellStyleXfs>
          <cellXfs count="1"><xf/></cellXfs>
        </styleSheet>
        """;

    private static string WorksheetXml(Visa2014Worksheet sheet)
    {
        var sb = new StringBuilder("""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
              <sheetData>
            """);

        sb.Append("<row r=\"1\">");
        for (int c = 0; c < sheet.Columns.Count; c++)
        {
            var headerRef = ColumnName(c + 1) + "1";
            sb.Append("<c r=\"").Append(headerRef).Append("\" t=\"inlineStr\"><is><t>")
                .Append(EscapeXml(sheet.Columns[c]))
                .Append("</t></is></c>");
        }
        sb.Append("</row>");

        for (int r = 0; r < sheet.Rows.Count; r++)
        {
            var rowNumber = r + 2;
            sb.Append("<row r=\"").Append(rowNumber).Append("\">");
            var row = sheet.Rows[r];
            for (int c = 0; c < sheet.Columns.Count; c++)
            {
                var colName = sheet.Columns[c];
                row.TryGetValue(colName, out var value);
                var cellRef = ColumnName(c + 1) + rowNumber;
                sb.Append("<c r=\"").Append(cellRef).Append("\" t=\"inlineStr\"><is><t>")
                    .Append(EscapeXml(FormatCell(value)))
                    .Append("</t></is></c>");
            }
            sb.Append("</row>");
        }

        sb.Append("</sheetData></worksheet>");
        return sb.ToString();
    }

    private static string FormatCell(object? value) => value switch
    {
        null => "",
        DateTime dt => dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        bool b => b ? "TRUE" : "FALSE",
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "",
    };

    private static string ColumnName(int index)
    {
        var name = new StringBuilder();
        while (index > 0)
        {
            index--;
            name.Insert(0, (char)('A' + index % 26));
            index /= 26;
        }
        return name.ToString();
    }

    private static string EscapeXml(string text) =>
        SecurityElement.Escape(text) ?? "";
}

internal sealed class Visa2014Worksheet
{
    public required string Name { get; init; }
    public required IReadOnlyList<string> Columns { get; init; }
    public required IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; init; }
}
