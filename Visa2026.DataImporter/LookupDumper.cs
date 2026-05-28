using System.Text;

namespace Visa2026.DataImporter;

/// <summary>
/// Reads lookup.xlsm and writes all lookup sheets to a markdown file
/// for developer reference. Run with: dotnet run -- --dump-lookups
/// </summary>
public static class LookupDumper
{
    public static async Task DumpAsync(string xlsmPath, string outputPath)
    {
        if (!File.Exists(xlsmPath))
        {
            Console.WriteLine($"  ✗ File not found: {xlsmPath}");
            return;
        }

        List<string> sheetsInFile;
        try { sheetsInFile = ExcelParser.GetSheetNames(xlsmPath); }
        catch (Exception ex) { Console.WriteLine($"  ✗ Could not open {xlsmPath}: {ex.Message}"); return; }

        var sb = new StringBuilder();
        sb.AppendLine("# Lookup Data Reference");
        sb.AppendLine();
        sb.AppendLine($"> Generated from `{xlsmPath}` on {DateTime.Now:yyyy-MM-dd HH:mm}.  ");
        sb.AppendLine("> Human-readable snapshot from `lookup.xlsm`. Runtime seeding uses Module `LookupCatalogs/*.json` — see docs/LOOKUP_SEEDING.md.");
        sb.AppendLine("> Re-run `dotnet run -- --dump-lookups` to refresh after Excel changes.");
        sb.AppendLine();

        // Table of contents
        sb.AppendLine("## Contents");
        sb.AppendLine();
        foreach (var sheetMap in ExcelMappings.LookupSheets)
        {
            bool exists = sheetsInFile.Any(s => s.Trim().Equals(sheetMap.SheetName.Trim(), StringComparison.OrdinalIgnoreCase));
            if (!exists) continue;
            var anchor = ToAnchor(sheetMap.DisplayName);
            sb.AppendLine($"- [{sheetMap.DisplayName} (`{sheetMap.EntityName}`)](#${anchor})");
        }
        sb.AppendLine();

        foreach (var sheetMap in ExcelMappings.LookupSheets)
        {
            bool exists = sheetsInFile.Any(s => s.Trim().Equals(sheetMap.SheetName.Trim(), StringComparison.OrdinalIgnoreCase));
            if (!exists)
            {
                Console.WriteLine($"  ⚠ Sheet '{sheetMap.SheetName}' not found in file — skipped.");
                continue;
            }

            Console.WriteLine($"  → Dumping '{sheetMap.DisplayName}' ({sheetMap.EntityName})...");

            // Read all rows without skipping header so we can use row 0 as column names
            var allRows = ExcelParser.Parse<List<object>?>(
                xlsmPath, row => row, hasHeader: false, sheetName: sheetMap.SheetName
            ).ToList();

            if (allRows.Count == 0)
            {
                Console.WriteLine($"    ⚠ Sheet is empty — skipped.");
                continue;
            }

            // Row 0 = column headers
            var headerRow = allRows[0];
            var headers = (headerRow ?? new List<object>())
                .Select(h => h?.ToString()?.Trim() ?? "")
                .ToList();

            // Drop trailing empty columns
            while (headers.Count > 0 && string.IsNullOrWhiteSpace(headers[^1]))
                headers.RemoveAt(headers.Count - 1);

            if (headers.Count == 0) continue;

            sb.AppendLine($"## {sheetMap.DisplayName}");
            sb.AppendLine();
            sb.AppendLine($"**OData entity:** `{sheetMap.EntityName}`");
            sb.AppendLine();

            // Markdown table header
            sb.Append("| ");
            sb.AppendJoin(" | ", headers.Select(EscapeMd));
            sb.AppendLine(" |");

            // Separator row
            sb.Append("| ");
            sb.AppendJoin(" | ", headers.Select(_ => "---"));
            sb.AppendLine(" |");

            int dataRowCount = 0;
            for (int i = 1; i < allRows.Count; i++)
            {
                var row = allRows[i];
                if (row == null || row.All(c => c == null || string.IsNullOrWhiteSpace(c?.ToString())))
                    continue;

                // Skip sentinel rows
                var firstCell = row[0]?.ToString()?.Trim() ?? "";
                if (firstCell.StartsWith("Start ", StringComparison.OrdinalIgnoreCase) ||
                    firstCell.StartsWith("End ",   StringComparison.OrdinalIgnoreCase))
                    continue;

                var cells = new List<string>();
                for (int j = 0; j < headers.Count; j++)
                {
                    var val = j < row.Count ? row[j]?.ToString()?.Trim() ?? "" : "";

                    // Strip formula placeholders
                    if (val.StartsWith("=DETERMINISTIC_GUID") || val.StartsWith("<openpyxl"))
                        val = "";

                    cells.Add(EscapeMd(val));
                }

                sb.Append("| ");
                sb.AppendJoin(" | ", cells);
                sb.AppendLine(" |");
                dataRowCount++;
            }

            if (dataRowCount == 0)
                sb.AppendLine("_(no data rows)_");

            sb.AppendLine();
            Console.WriteLine($"    ✓ {dataRowCount} rows written.");
        }

        await File.WriteAllTextAsync(outputPath, sb.ToString(), Encoding.UTF8);
        Console.WriteLine($"\n  ✓ Lookup markdown written to: {Path.GetFullPath(outputPath)}");
    }

    /// <summary>
    /// Walks up the directory tree from <paramref name="startDir"/> until it finds
    /// a folder containing a .sln or .slnx file, then returns that folder path.
    /// Returns null if the solution root cannot be located.
    /// </summary>
    public static string? FindSolutionRoot(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir != null)
        {
            if (dir.GetFiles("*.sln").Length > 0 || dir.GetFiles("*.slnx").Length > 0)
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }

    private static string EscapeMd(string s) =>
        s.Replace("|", "\\|").Replace("\r", "").Replace("\n", " ");

    private static string ToAnchor(string text) =>
        text.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("/", "")
            .Replace("(", "")
            .Replace(")", "");
}
