using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

/// <summary>
/// Generic Excel importer driven by <see cref="ExcelMappings"/>.
///
/// Uses the existing <see cref="ExcelParser"/> (ExcelDataReader) — no extra
/// NuGet package required.
///
/// Usage:
///   var importer = new ExcelImporter(api);
///   await importer.ImportFileAsync("data.xlsx");            // all mapped sheets
///   await importer.ImportSheetAsync("data.xlsx", "Persons"); // one sheet only
/// </summary>
public class ExcelImporter
{
    private readonly ApiClient _api;

    // Cache: "EntityName|Name" → { ID = guid } — each API lookup fires once per run.
    private readonly Dictionary<string, object?> _lookupCache = new(StringComparer.OrdinalIgnoreCase);

    public ExcelImporter(ApiClient api)
    {
        _api = api;
    }

    // -----------------------------------------------------------------------
    // Public entry points
    // -----------------------------------------------------------------------

    /// <summary>
    /// Imports every mapped sheet found in <paramref name="filePath"/>.
    /// Sheets in the file with no entry in <see cref="ExcelMappings"/> are silently skipped.
    /// </summary>
    public async Task ImportFileAsync(string filePath)
    {
        if (!System.IO.File.Exists(filePath))
        {
            Console.WriteLine($"  ✗ File not found: {filePath}");
            return;
        }

        List<string> sheetsInFile;
        try
        {
            sheetsInFile = ExcelParser.GetSheetNames(filePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Could not open {filePath}: {ex.Message}");
            return;
        }

        foreach (var sheetMap in ExcelMappings.Sheets)
        {
            bool exists = sheetsInFile.Any(s =>
                s.Equals(sheetMap.SheetName, StringComparison.OrdinalIgnoreCase));

            if (!exists)
            {
                Console.WriteLine($"  ⚠ Sheet '{sheetMap.SheetName}' not found in {filePath} — skipped.");
                continue;
            }

            await ImportSheetCoreAsync(filePath, sheetMap);
        }
    }

    /// <summary>
    /// Imports only the sheet matching <paramref name="sheetName"/> from <paramref name="filePath"/>.
    /// </summary>
    public async Task ImportSheetAsync(string filePath, string sheetName)
    {
        if (!System.IO.File.Exists(filePath))
        {
            Console.WriteLine($"  ✗ File not found: {filePath}");
            return;
        }

        var sheetMap = ExcelMappings.Sheets
            .FirstOrDefault(s => s.SheetName.Equals(sheetName, StringComparison.OrdinalIgnoreCase));

        if (sheetMap == null)
        {
            Console.WriteLine($"  ✗ No mapping defined for sheet '{sheetName}'.");
            return;
        }

        await ImportSheetCoreAsync(filePath, sheetMap);
    }

    // -----------------------------------------------------------------------
    // Core per-sheet logic
    // -----------------------------------------------------------------------

    private async Task ImportSheetCoreAsync(string filePath, SheetMap sheetMap)
    {
        Console.WriteLine($"\n=== Importing '{sheetMap.SheetName}' → {sheetMap.EntityName} ===");

        // ------------------------------------------------------------------
        // Pass 1: read the header row to build  columnTitle → columnIndex  map.
        // ExcelParser.Parse with hasHeader:false yields every row including row 0.
        // We take only the first one.
        // ------------------------------------------------------------------
        var headerRow = ExcelParser.Parse<List<object>?>(
            filePath,
            row => row,
            hasHeader: false,
            sheetName: sheetMap.SheetName
        ).FirstOrDefault();

        if (headerRow == null)
        {
            Console.WriteLine($"  ✗ Sheet '{sheetMap.SheetName}' appears to be empty.");
            return;
        }

        // Map: header text (trimmed) → zero-based column index
        var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headerRow.Count; i++)
        {
            var text = headerRow[i]?.ToString()?.Trim() ?? "";
            if (!string.IsNullOrEmpty(text))
                headerIndex[text] = i;
        }

        // Warn about mapped columns not present in this sheet
        foreach (var col in sheetMap.Columns.Where(c => !headerIndex.ContainsKey(c.Header)))
            Console.WriteLine($"  ⚠ Column '{col.Header}' not found in sheet — will be skipped.");

        // ------------------------------------------------------------------
        // Pass 2: parse data rows (ExcelParser skips the header for us).
        // ------------------------------------------------------------------
        int success = 0, skipped = 0, fail = 0;
        int rowNum = 2; // first data row is Excel row 2

        var dataRows = ExcelParser.Parse<List<object>?>(
            filePath,
            row => row,
            hasHeader: true,           // skips row 1 (the header)
            sheetName: sheetMap.SheetName
        );

        foreach (var row in dataRows)
        {
            // Skip fully empty rows
            if (row == null || row.All(c => c == null || string.IsNullOrWhiteSpace(c.ToString())))
            {
                rowNum++;
                continue;
            }

            string rowLabel = $"row {rowNum}";
            var payload = new Dictionary<string, object?>();
            bool skipRow = false;

            foreach (var colMap in sheetMap.Columns)
            {
                if (!headerIndex.TryGetValue(colMap.Header, out int colIdx))
                    continue; // column not in this sheet

                var rawValue = colIdx < row.Count
                    ? row[colIdx]?.ToString()?.Trim() ?? ""
                    : "";

                if (string.IsNullOrWhiteSpace(rawValue))
                {
                    if (colMap.Required)
                    {
                        Console.WriteLine($"  ⚠ Skipping {rowLabel}: required column '{colMap.Header}' is empty.");
                        skipRow = true;
                    }
                    // Optional + empty → omit from payload
                    continue;
                }

                // Apply value substitution map if defined (e.g. int enum → string name)
                if (colMap.ValueMap != null && colMap.ValueMap.TryGetValue(rawValue, out var mappedValue))
                    rawValue = mappedValue;

                switch (colMap.Kind)
                {
                    case ColumnKind.Scalar:
                        payload[colMap.PayloadProperty] = ParseScalar(rawValue);
                        break;

                    case ColumnKind.Bool:
                        payload[colMap.PayloadProperty] =
                            rawValue != "0" &&
                            !rawValue.Equals("false", StringComparison.OrdinalIgnoreCase) &&
                            !rawValue.Equals("no",    StringComparison.OrdinalIgnoreCase);
                        break;

                    case ColumnKind.LookupByName:
                        var lookupRef = await ResolveLookupByNameAsync(colMap.LookupEntity, rawValue);
                        if (lookupRef == null)
                            Console.WriteLine($"  ⚠ {rowLabel}: '{rawValue}' not found in {colMap.LookupEntity} — field omitted.");
                        else
                            payload[colMap.PayloadProperty] = lookupRef;
                        break;

                    case ColumnKind.PersonLookupByName:
                        var personRef = await ResolvePersonByFullNameAsync(rawValue);
                        if (personRef == null)
                        {
                            Console.WriteLine(colMap.Required
                                ? $"  ⚠ Skipping {rowLabel}: Person '{rawValue}' not found."
                                : $"  ⚠ {rowLabel}: Person '{rawValue}' not found — field omitted.");
                            if (colMap.Required) skipRow = true;
                        }
                        else
                        {
                            payload[colMap.PayloadProperty] = personRef;
                        }
                        break;
                }

                if (skipRow) break;
            }

            rowNum++;
            if (skipRow)        { skipped++; continue; }
            if (payload.Count == 0) { skipped++; continue; }

            // POST row to OData
            try
            {
                await _api.CreateAsync<object>(sheetMap.EntityName, payload);
                Console.WriteLine($"  ✓ Imported {sheetMap.DisplayName} ({rowLabel})");
                success++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Failed {sheetMap.DisplayName} ({rowLabel}): {ex.Message}");
                fail++;
            }
        }

        Console.WriteLine($"  Done. Success={success}, Skipped={skipped}, Failed={fail}");
    }

    // -----------------------------------------------------------------------
    // Lookup resolution — results cached for the lifetime of this instance
    // -----------------------------------------------------------------------

    /// <summary>Resolves a Name string to { ID = guid } for a generic lookup entity.</summary>
    private async Task<object?> ResolveLookupByNameAsync(string entityName, string name)
    {
        string cacheKey = $"{entityName}|{name}";
        if (_lookupCache.TryGetValue(cacheKey, out var cached))
            return cached;

        try
        {
            var escaped = name.Replace("'", "''");
            var results = await _api.QueryAsync<IdHolder>(entityName, $"$filter=Name eq '{escaped}'&$top=1");
            var found   = results.FirstOrDefault();
            var result  = found != null ? (object)new { ID = found.Id } : null;
            _lookupCache[cacheKey] = result;
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    ✗ Lookup error [{entityName}] Name='{name}': {ex.Message}");
            _lookupCache[cacheKey] = null;
            return null;
        }
    }

    /// <summary>Resolves a full name to { ID = guid } against the Person entity.</summary>
    private async Task<object?> ResolvePersonByFullNameAsync(string fullName)
    {
        string cacheKey = $"Person|{fullName}";
        if (_lookupCache.TryGetValue(cacheKey, out var cached))
            return cached;

        try
        {
            var escaped = fullName.Replace("'", "''");
            var results = await _api.QueryAsync<IdHolder>("Person", $"$filter=FullName eq '{escaped}'&$top=1");
            var found   = results.FirstOrDefault();
            var result  = found != null ? (object)new { ID = found.Id } : null;
            _lookupCache[cacheKey] = result;
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    ✗ Person lookup error for '{fullName}': {ex.Message}");
            _lookupCache[cacheKey] = null;
            return null;
        }
    }

    // -----------------------------------------------------------------------
    // Scalar value parsing — bool > int > decimal > DateTime > string
    // -----------------------------------------------------------------------

    private static object ParseScalar(string raw)
    {
        // Integers FIRST — "1" and "0" are numeric, not bool.
        if (int.TryParse(raw, out int i)) return i;

        if (decimal.TryParse(raw,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal d)) return d;

        if (raw.Equals("true",  StringComparison.OrdinalIgnoreCase) ||
            raw.Equals("yes",   StringComparison.OrdinalIgnoreCase)) return true;
        if (raw.Equals("false", StringComparison.OrdinalIgnoreCase) ||
            raw.Equals("no",    StringComparison.OrdinalIgnoreCase)) return false;

        if (DateTime.TryParse(raw,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out DateTime dt)) return dt;

        return raw;
    }

    // Minimal DTO — only needs the ID from any OData lookup response
    private class IdHolder
    {
        [System.Text.Json.Serialization.JsonPropertyName("ID")]
        public Guid Id { get; set; }
    }
}