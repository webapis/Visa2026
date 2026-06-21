using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014SqlCmdReader
{
    public static IReadOnlyList<IReadOnlyDictionary<string, string?>> Query(
        string connectionString,
        string sql,
        bool verbose)
    {
        var builder = ParseConnectionString(connectionString);
        var args = new StringBuilder();
        args.Append("-S \"").Append(builder.Server).Append('"');
        args.Append(" -d \"").Append(builder.Database).Append('"');
        if (builder.TrustedConnection)
            args.Append(" -E");
        else if (!string.IsNullOrEmpty(builder.UserId))
        {
            args.Append(" -U \"").Append(builder.UserId).Append('"');
            args.Append(" -P \"").Append(builder.Password).Append('"');
        }

        args.Append(" -C -W -s \"|\"");
        args.Append(" -Q \"").Append(EscapeSqlCmdQuery(sql)).Append('"');

        var psi = new ProcessStartInfo
        {
            FileName = "sqlcmd",
            Arguments = args.ToString(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start sqlcmd.");

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"sqlcmd failed ({process.ExitCode}): {stderr.Trim()}");

        var lines = stdout
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        if (lines.Count == 0)
            return [];

        var headers = SplitRow(lines[0]);
        var rows = new List<IReadOnlyDictionary<string, string?>>();

        for (int i = 1; i < lines.Count; i++)
        {
            var line = lines[i];
            if (line.StartsWith('(') && line.Contains("rows affected"))
                continue;
            if (line.All(c => c == '-' || c == '|' || c == ' '))
                continue;

            var values = SplitRow(line);
            if (values.Length == 0)
                continue;

            var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (int c = 0; c < headers.Length && c < values.Length; c++)
                dict[headers[c]] = NullIfEmpty(values[c]);

            rows.Add(dict);
        }

        if (verbose)
            Console.WriteLine($"  sqlcmd returned {rows.Count} row(s).");

        return rows;
    }

    private static string? NullIfEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) || value.Equals("NULL", StringComparison.OrdinalIgnoreCase)
            ? null
            : value;

    private static string[] SplitRow(string line) =>
        line.Split('|').Select(v => v.Trim()).ToArray();

    private static string EscapeSqlCmdQuery(string sql) =>
        sql.Replace("\"", "\"\"");

    private static SqlCmdConnection ParseConnectionString(string connectionString)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = part.IndexOf('=');
            if (idx <= 0) continue;
            map[part[..idx].Trim()] = part[(idx + 1)..].Trim();
        }

        map.TryGetValue("Server", out var server);
        map.TryGetValue("Data Source", out var dataSource);
        map.TryGetValue("Database", out var database);
        map.TryGetValue("Initial Catalog", out var catalog);
        map.TryGetValue("User ID", out var userId);
        map.TryGetValue("Password", out var password);

        bool trusted = map.TryGetValue("Trusted_Connection", out var tc) &&
                       (tc.Equals("True", StringComparison.OrdinalIgnoreCase) || tc.Equals("SSPI", StringComparison.OrdinalIgnoreCase));

        return new SqlCmdConnection
        {
            Server = server ?? dataSource ?? "localhost\\SQLEXPRESS",
            Database = database ?? catalog ?? "VISA2015",
            TrustedConnection = trusted || string.IsNullOrEmpty(userId),
            UserId = userId ?? "",
            Password = password ?? "",
        };
    }

    private sealed class SqlCmdConnection
    {
        public required string Server { get; init; }
        public required string Database { get; init; }
        public bool TrustedConnection { get; init; }
        public string UserId { get; init; } = "";
        public string Password { get; init; } = "";
    }
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
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}

internal sealed class Visa2014Worksheet
{
    public required string Name { get; init; }
    public required IReadOnlyList<string> Columns { get; init; }
    public required IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; init; }
}
