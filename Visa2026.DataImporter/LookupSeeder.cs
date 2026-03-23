using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

/// <summary>
/// Seeds all lookup/reference tables from the lookup.xlsm file.
///
/// Reads <see cref="ExcelMappings.LookupSheets"/> in order (dependency order
/// is built into the list — independent tables first, Region before City, etc.)
/// and POSTs each row to the matching OData endpoint.
///
/// Each entity is checked for existing records first: if the table already has
/// data, the sheet is skipped to prevent duplicates.
///
/// Usage:
///   var seeder = new LookupSeeder(api);
///   await seeder.SeedAllAsync("lookup.xlsm");
///
///   // Or seed a single entity:
///   await seeder.SeedOneAsync("lookup.xlsm", "Country");
/// </summary>
public class LookupSeeder
{
    private readonly ApiClient _api;
    private readonly Dictionary<string, object?> _lookupCache = new(StringComparer.OrdinalIgnoreCase);

    public LookupSeeder(ApiClient api)
    {
        _api = api;
    }

    // -----------------------------------------------------------------------
    // Public entry points
    // -----------------------------------------------------------------------

    /// <summary>Seeds every lookup sheet defined in ExcelMappings.LookupSheets.</summary>
    public async Task SeedAllAsync(string filePath)
    {
        if (!System.IO.File.Exists(filePath))
        {
            Console.WriteLine($"  ✗ Lookup file not found: {filePath}");
            return;
        }

        List<string> sheetsInFile;
        try { sheetsInFile = ExcelParser.GetSheetNames(filePath); }
        catch (Exception ex) { Console.WriteLine($"  ✗ Could not open {filePath}: {ex.Message}"); return; }

        Console.WriteLine($"\n=== Seeding lookup tables from '{filePath}' ===");

        foreach (var sheetMap in ExcelMappings.LookupSheets)
        {
            bool exists = sheetsInFile.Any(s => s.Equals(sheetMap.SheetName, StringComparison.OrdinalIgnoreCase));
            if (!exists)
            {
                Console.WriteLine($"  ⚠ Sheet '{sheetMap.SheetName}' not found — skipped.");
                continue;
            }
            await SeedSheetAsync(filePath, sheetMap);
        }

        Console.WriteLine("\n=== Lookup seeding complete ===\n");
    }

    /// <summary>Seeds a single lookup entity by its OData entity name (e.g. "Country").</summary>
    public async Task SeedOneAsync(string filePath, string entityName)
    {
        var sheetMap = ExcelMappings.LookupSheets
            .FirstOrDefault(s => s.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));

        if (sheetMap == null)
        {
            Console.WriteLine($"  ✗ No lookup mapping for entity '{entityName}'.");
            return;
        }

        if (!System.IO.File.Exists(filePath))
        {
            Console.WriteLine($"  ✗ File not found: {filePath}");
            return;
        }

        await SeedSheetAsync(filePath, sheetMap);
    }

    // -----------------------------------------------------------------------
    // Core seeding logic
    // -----------------------------------------------------------------------

    private async Task SeedSheetAsync(string filePath, SheetMap sheetMap)
    {
        Console.WriteLine($"\n  → Seeding '{sheetMap.DisplayName}' ({sheetMap.EntityName}) from sheet '{sheetMap.SheetName}'");

        // Check if this entity already has data — skip if so (idempotent)
        try
        {
            var existing = await _api.QueryAsync<IdHolder>(sheetMap.EntityName, "$top=1");
            if (existing.Any())
            {
                Console.WriteLine($"    ℹ Already has data — skipped. (Delete records first to re-seed.)");
                return;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    ⚠ Could not check existing data for '{sheetMap.EntityName}': {ex.Message}");
            Console.WriteLine($"    Proceeding anyway...");
        }

        // --- Read header row ---
        var headerRow = ExcelParser.Parse<List<object>?>(
            filePath, row => row, hasHeader: false, sheetName: sheetMap.SheetName
        ).FirstOrDefault();

        if (headerRow == null)
        {
            Console.WriteLine($"    ✗ Sheet '{sheetMap.SheetName}' is empty.");
            return;
        }

        var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headerRow.Count; i++)
        {
            var text = headerRow[i]?.ToString()?.Trim() ?? "";
            if (!string.IsNullOrEmpty(text))
                headerIndex[text] = i;
        }

        // Warn about mapped columns missing from this sheet
        foreach (var col in sheetMap.Columns.Where(c => !headerIndex.ContainsKey(c.Header)))
            Console.WriteLine($"    ⚠ Column '{col.Header}' not found in sheet — will be skipped.");

        // --- Read and POST data rows ---
        int success = 0, skipped = 0, fail = 0;
        int rowNum = 2;

        var dataRows = ExcelParser.Parse<List<object>?>(
            filePath, row => row, hasHeader: true, sheetName: sheetMap.SheetName
        );

        foreach (var row in dataRows)
        {
            if (row == null || row.All(c => c == null || string.IsNullOrWhiteSpace(c.ToString())))
            { rowNum++; continue; }

            // Skip SaveToDB sentinel rows (start with "Start " or "End ")
            var firstCell = row[0]?.ToString()?.Trim() ?? "";
            if (firstCell.StartsWith("Start ", StringComparison.OrdinalIgnoreCase) ||
                firstCell.StartsWith("End ",   StringComparison.OrdinalIgnoreCase))
            { rowNum++; continue; }

            // Skip SaveToDB update-tracking rows: _RowNum column contains a POSITIVE integer (1, 2, 3...)
            // Original seed rows have _RowNum = "0" or empty. Only skip 1+.
            if (int.TryParse(firstCell, out int rowNumVal) && rowNumVal > 0)
            { rowNum++; continue; }

            string rowLabel = $"row {rowNum}";
            var payload = new Dictionary<string, object?>();
            bool skipRow = false;

            foreach (var colMap in sheetMap.Columns)
            {
                if (!headerIndex.TryGetValue(colMap.Header, out int colIdx)) continue;

                var rawValue = colIdx < row.Count ? row[colIdx]?.ToString()?.Trim() ?? "" : "";

                // Skip internal SaveToDB formula placeholders
                if (rawValue.StartsWith("=DETERMINISTIC_GUID") ||
                    rawValue.StartsWith("<openpyxl"))
                { continue; }

                if (string.IsNullOrWhiteSpace(rawValue))
                {
                    if (colMap.Required) { skipRow = true; break; }
                    continue;
                }

                switch (colMap.Kind)
                {
                    case ColumnKind.Scalar:
                        payload[colMap.PayloadProperty] = ParseScalar(rawValue);
                        break;

                    case ColumnKind.LookupByName:
                        var lookupRef = await ResolveLookupByNameAsync(colMap.LookupEntity, rawValue);
                        if (lookupRef != null) payload[colMap.PayloadProperty] = lookupRef;
                        break;

                    case ColumnKind.PersonLookupByName:
                        // Not expected in lookup sheets, but handled for completeness
                        var personRef = await ResolvePersonByFullNameAsync(rawValue);
                        if (personRef != null) payload[colMap.PayloadProperty] = personRef;
                        break;
                }
            }

            rowNum++;
            if (skipRow || payload.Count == 0) { skipped++; continue; }

            try
            {
                await _api.CreateAsync<object>(sheetMap.EntityName, payload);
                success++;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"    ✗ Failed ({rowLabel}): {ex.Message}");
                // Log the payload for easier debugging
                try
                {
                    var payloadJson = System.Text.Json.JsonSerializer.Serialize(payload,
                        new System.Text.Json.JsonSerializerOptions { WriteIndented = false });
                    Console.WriteLine($"      Payload: {(payloadJson.Length > 300 ? payloadJson[..300] + "..." : payloadJson)}");
                }
                catch { }
                fail++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    ✗ Failed ({rowLabel}): {ex.GetType().Name}: {ex.Message}");
                fail++;
            }
        }

        Console.WriteLine($"    Done. ✓ {success} seeded, ⚠ {skipped} skipped, ✗ {fail} failed.");
    }

    // -----------------------------------------------------------------------
    // Lookup resolution — cached
    // -----------------------------------------------------------------------

    private async Task<object?> ResolveLookupByNameAsync(string entityName, string name)
    {
        string cacheKey = $"{entityName}|{name}";
        if (_lookupCache.TryGetValue(cacheKey, out var cached)) return cached;
        try
        {
            var escaped = name.Replace("'", "''");
            var results = await _api.QueryAsync<IdHolder>(entityName, $"$filter=Name eq '{escaped}'&$top=1");
            var found = results.FirstOrDefault();
            var result = found != null ? (object)new { ID = found.Id } : null;
            _lookupCache[cacheKey] = result;
            return result;
        }
        catch { _lookupCache[cacheKey] = null; return null; }
    }

    private async Task<object?> ResolvePersonByFullNameAsync(string fullName)
    {
        string cacheKey = $"Person|{fullName}";
        if (_lookupCache.TryGetValue(cacheKey, out var cached)) return cached;
        try
        {
            var escaped = fullName.Replace("'", "''");
            var results = await _api.QueryAsync<IdHolder>("Person", $"$filter=FullName eq '{escaped}'&$top=1");
            var found = results.FirstOrDefault();
            var result = found != null ? (object)new { ID = found.Id } : null;
            _lookupCache[cacheKey] = result;
            return result;
        }
        catch { _lookupCache[cacheKey] = null; return null; }
    }

    // -----------------------------------------------------------------------
    // Scalar parsing
    // -----------------------------------------------------------------------

    private static object ParseScalar(string raw)
    {
        if (raw.Equals("true",  StringComparison.OrdinalIgnoreCase) ||
            raw.Equals("yes",   StringComparison.OrdinalIgnoreCase) || raw == "1") return true;
        if (raw.Equals("false", StringComparison.OrdinalIgnoreCase) ||
            raw.Equals("no",    StringComparison.OrdinalIgnoreCase) || raw == "0") return false;
        if (int.TryParse(raw, out int i)) return i;
        if (decimal.TryParse(raw, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal d)) return d;
        if (DateTime.TryParse(raw, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out DateTime dt)) return dt;
        return raw;
    }

    private class IdHolder
    {
        [System.Text.Json.Serialization.JsonPropertyName("ID")]
        public Guid Id { get; set; }
    }
}