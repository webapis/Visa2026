using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

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

    // Cache: "EntityName|FilterProperty|Name" → { ID = guid } — each API lookup fires once per run.
    private readonly Dictionary<string, object?> _lookupCache = new(StringComparer.OrdinalIgnoreCase);

    // Name of the special Excel column that tags each row with its scenario.
    // Not an OData property — never included in the API payload.
    private const string ScenarioColumnHeader = "Scenario";

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
            if (sheetMap == null)
                continue;

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

        var (headerIndex, dataRows) = ReadSheetRows(filePath, sheetMap);
        if (headerIndex == null) return;

        await SeedRowsAsync(sheetMap, headerIndex, dataRows);
    }

    // -----------------------------------------------------------------------
    // Scenario-aware two-pass import
    //
    // Pass 1: read every mapped sheet into memory, grouping rows by their
    //         "Scenario" column value (empty or missing → "Shared").
    // Pass 2: for each scenario in Order sequence:
    //           - idempotency check via anchor entity
    //           - seed each sheet's rows for this scenario in dependency order
    //
    // Falls back to ImportFileAsync if no Scenarios sheet is present.
    // -----------------------------------------------------------------------

    public async Task ImportByScenariosAsync(string filePath)
    {
        if (!System.IO.File.Exists(filePath))
        {
            Console.WriteLine($"  ✗ File not found: {filePath}");
            return;
        }

        var scenarios = ReadScenarios(filePath);
        if (scenarios.Count == 0)
        {
            Console.WriteLine("  ⚠ No scenarios defined — falling back to full import.");
            await ImportFileAsync(filePath);
            return;
        }

        Console.WriteLine($"\n=== Scenario-based import from '{filePath}' ===");

        List<string> sheetsInFile;
        try { sheetsInFile = ExcelParser.GetSheetNames(filePath); }
        catch (Exception ex) { Console.WriteLine($"  ✗ Could not open file: {ex.Message}"); return; }

        // ------------------------------------------------------------------
        // Pass 1: read every mapped sheet into memory, grouped by scenario.
        // Structure: sheetName → (headerIndex, scenarioName → rows)
        // "Scenario" column value is read but NOT added to the OData payload.
        // ------------------------------------------------------------------
        var allData = new Dictionary<string,
            (Dictionary<string, int> Header, Dictionary<string, List<List<object>>> Rows)>
            (StringComparer.OrdinalIgnoreCase);

        foreach (var sheetMap in ExcelMappings.Sheets)
        {
            if (sheetMap == null)
                continue;

            if (!sheetsInFile.Any(s => s.Equals(sheetMap.SheetName, StringComparison.OrdinalIgnoreCase)))
                continue;

            var (headerIndex, rawRows) = ReadSheetRows(filePath, sheetMap);
            if (headerIndex == null) continue;

            bool hasScenarioCol = headerIndex.ContainsKey(ScenarioColumnHeader);
            var byScenario = new Dictionary<string, List<List<object>>>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rawRows)
            {
                string tag = "Shared";
                if (hasScenarioCol)
                {
                    int idx = headerIndex[ScenarioColumnHeader];
                    var cell = idx < row.Count ? row[idx]?.ToString()?.Trim() : null;
                    if (!string.IsNullOrWhiteSpace(cell)) tag = cell;
                }

                if (!byScenario.TryGetValue(tag, out var list))
                {
                    list = new List<List<object>>();
                    byScenario[tag] = list;
                }
                list.Add(row);
            }

            allData[sheetMap.SheetName] = (headerIndex, byScenario);
        }

        // ------------------------------------------------------------------
        // Pass 2: seed each scenario in Order sequence.
        // ------------------------------------------------------------------
        foreach (var scenario in scenarios)
        {
            Console.WriteLine($"\n{'=',3} Scenario [{scenario.Order}]: {scenario.Name} {'=',3}");
            if (!string.IsNullOrWhiteSpace(scenario.Description))
                Console.WriteLine($"    {scenario.Description}");
            if (!string.IsNullOrWhiteSpace(scenario.DependsOn))
                Console.WriteLine($"    Depends on: {scenario.DependsOn}");

            if (await ScenarioAlreadySeededAsync(scenario))
            {
                Console.WriteLine($"  ℹ Already seeded (anchor found) — skipped.");
                continue;
            }

            foreach (var sheetMap in ExcelMappings.Sheets)
            {
                if (sheetMap == null)
                    continue;

                if (!allData.TryGetValue(sheetMap.SheetName, out var sheetData)) continue;
                if (!sheetData.Rows.TryGetValue(scenario.Name, out var rows) || rows.Count == 0) continue;

                Console.WriteLine($"\n  → Seeding '{sheetMap.SheetName}' ({rows.Count} row(s)) for [{scenario.Name}]");
                await SeedRowsAsync(sheetMap, sheetData.Header, rows);
            }

            Console.WriteLine($"  ✓ Scenario '{scenario.Name}' complete.");
        }

        Console.WriteLine("\n=== Scenario-based import complete ===\n");
    }

    // -----------------------------------------------------------------------
    // Core helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Reads a sheet's header and data rows from disk.
    /// Returns (null, empty) if the sheet is empty.
    /// </summary>
    private (Dictionary<string, int>? headerIndex, List<List<object>> rows) ReadSheetRows(
        string filePath, SheetMap sheetMap)
    {
        var headerRow = ExcelParser.Parse<List<object>?>(
            filePath, row => row, hasHeader: false, sheetName: sheetMap.SheetName
        ).FirstOrDefault();

        if (headerRow == null)
        {
            Console.WriteLine($"  ✗ Sheet '{sheetMap.SheetName}' appears to be empty.");
            return (null, new List<List<object>>());
        }

        var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headerRow.Count; i++)
        {
            var text = headerRow[i]?.ToString()?.Trim() ?? "";
            if (!string.IsNullOrEmpty(text)) headerIndex[text] = i;
        }

        var dataRows = ExcelParser.Parse<List<object>?>(
            filePath, row => row, hasHeader: true, sheetName: sheetMap.SheetName
        )
        .Where(r => r != null && r.Any(c => c != null && !string.IsNullOrWhiteSpace(c.ToString())))
        .Cast<List<object>>()
        .ToList();

        return (headerIndex, dataRows);
    }

    /// <summary>
    /// Builds payloads from pre-read rows and POSTs each to the OData endpoint.
    /// The "Scenario" column is silently skipped — it is never sent to the API.
    /// </summary>
    private async Task SeedRowsAsync(
        SheetMap sheetMap,
        Dictionary<string, int> headerIndex,
        IEnumerable<List<object>> dataRows)
    {
        if (ExcelMappings.IsBlockedImportEntity(sheetMap.EntityName))
        {
            Console.WriteLine(
                $"  ⊘ Skipping '{sheetMap.SheetName}' → {sheetMap.EntityName}: " +
                "synced by Visa2026.Module (lookup catalogs + org singletons — docs/LOOKUP_SEEDING.md).");
            return;
        }

        // Warn about mapped columns missing from this sheet
        foreach (var col in sheetMap.Columns.Where(c => !headerIndex.ContainsKey(c.Header)))
            Console.WriteLine($"  ⚠ Column '{col.Header}' not found in sheet — will be skipped.");

        int success = 0, skipped = 0, fail = 0, rowNum = 2;

        foreach (var row in dataRows)
        {
            string rowLabel = $"row {rowNum++}";
            var payload = new Dictionary<string, object?>();
            bool skipRow = false;

            foreach (var colMap in sheetMap.Columns)
            {
                if (!headerIndex.TryGetValue(colMap.Header, out int colIdx)) continue;

                // Empty PayloadProperty = hook-only column, never sent to the API
                if (string.IsNullOrEmpty(colMap.PayloadProperty)) continue;

                var rawValue = colIdx < row.Count ? row[colIdx]?.ToString()?.Trim() ?? "" : "";

                if (string.IsNullOrWhiteSpace(rawValue))
                {
                    if (colMap.Required)
                    {
                        Console.WriteLine($"  ⚠ Skipping {rowLabel}: required column '{colMap.Header}' is empty.");
                        skipRow = true;
                    }
                    continue;
                }

                if (colMap.ValueMap != null && colMap.ValueMap.TryGetValue(rawValue, out var mappedValue))
                    rawValue = mappedValue;

                switch (colMap.Kind)
                {
                    case ColumnKind.Scalar:
                        payload[colMap.PayloadProperty] = DataParser.ParseScalar(rawValue);
                        break;

                    case ColumnKind.StringValue:
                        payload[colMap.PayloadProperty] = rawValue;
                        break;

                    case ColumnKind.Bool:
                        payload[colMap.PayloadProperty] = DataParser.IsTextTrue(rawValue);
                        break;

                    case ColumnKind.LookupByName:
                        var lookupRef = await ResolveLookupByNameAsync(
                            colMap.LookupEntity, rawValue, colMap.LookupFilterProperty);
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
                            payload[colMap.PayloadProperty] = personRef;
                        break;
                }

                if (skipRow) break;
            }

            EnsurePersonPersonalNumber(sheetMap, payload, rowLabel);

            if (skipRow || payload.Count == 0) { skipped++; continue; }

            IdHolder? created = null;
            try
            {
                if (sheetMap.SingletonUpsert)
                {
                    var existing = (await _api.QueryAsync<IdHolder>(sheetMap.EntityName, "$top=1")).FirstOrDefault();
                    if (existing != null && existing.Id != Guid.Empty)
                    {
                        await _api.UpdateAsync(sheetMap.EntityName, existing.Id, payload);
                        created = existing;
                        Console.WriteLine($"  ✓ Updated {sheetMap.DisplayName} ({rowLabel})");
                    }
                    else
                    {
                        created = await _api.CreateAsync<IdHolder>(sheetMap.EntityName, payload);
                        Console.WriteLine($"  ✓ Created {sheetMap.DisplayName} ({rowLabel})");
                    }
                }
                else
                {
                    created = await _api.CreateAsync<IdHolder>(sheetMap.EntityName, payload);
                    Console.WriteLine($"  ✓ Seeded {sheetMap.DisplayName} ({rowLabel})");
                }
                success++;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"  ✗ Failed {sheetMap.DisplayName} ({rowLabel}): {ex.Message}");
                try
                {
                    var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = false });
                    Console.WriteLine($"      Payload: {(payloadJson.Length > 500 ? payloadJson[..500] + "..." : payloadJson)}");
                }
                catch { }
                fail++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Failed {sheetMap.DisplayName} ({rowLabel}): {ex.Message}");
                fail++;
            }

            if (sheetMap.PostSeedHook != null && created != null && created.Id != Guid.Empty)
            {
                try
                {
                    await sheetMap.PostSeedHook(created.Id, row, headerIndex, _api);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ⚠ PostSeedHook failed for {sheetMap.DisplayName} ({rowLabel}): {ex.Message}");
                }
            }
        }

        Console.WriteLine($"    Done. ✓ {success} seeded, ⚠ {skipped} skipped, ✗ {fail} failed.");
    }

    private static void EnsurePersonPersonalNumber(SheetMap sheetMap, Dictionary<string, object?> payload, string rowLabel)
    {
        if (!sheetMap.EntityName.Equals("Person", StringComparison.OrdinalIgnoreCase))
            return;

        if (payload.TryGetValue("PersonalNumber", out var personalNumberValue)
            && !string.IsNullOrWhiteSpace(personalNumberValue?.ToString()))
            return;

        payload["PersonalNumber"] = GenerateFallbackPersonalNumber();
        Console.WriteLine($"  ⚠ {rowLabel}: 'Personal Number' missing for Person — generated fallback value.");
    }

    private static string GenerateFallbackPersonalNumber()
        => $"AUTO-{Guid.NewGuid():N}";

    /// <summary>
    /// Returns true if the scenario's anchor entity already exists in the API.
    /// Returns false (proceed) if no anchor is configured or the check fails.
    /// </summary>
    private async Task<bool> ScenarioAlreadySeededAsync(ScenarioDefinition scenario)
    {
        if (string.IsNullOrWhiteSpace(scenario.AnchorEntity) ||
            string.IsNullOrWhiteSpace(scenario.AnchorKey)    ||
            string.IsNullOrWhiteSpace(scenario.AnchorValue))
            return false;

        try
        {
            var escaped = scenario.AnchorValue.Replace("'", "''");
            var results = await _api.QueryAsync<IdHolder>(
                scenario.AnchorEntity,
                $"$filter={scenario.AnchorKey} eq '{escaped}'&$top=1");
            return results.Any();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ⚠ Anchor check failed for '{scenario.Name}': {ex.Message} — proceeding.");
            return false;
        }
    }

    // -----------------------------------------------------------------------
    // YAML scenario-based import
    //
    // Reads data.yaml, resolves scenarios in Order sequence, reuses the same
    // SeedRowsAsync / lookup resolution pipeline as the Excel path.
    // -----------------------------------------------------------------------

    public async Task ImportByScenariosFromYamlAsync(string filePath)
    {
        if (!System.IO.File.Exists(filePath))
        {
            Console.WriteLine($"  ✗ File not found: {filePath}");
            return;
        }

        var yaml = System.IO.File.ReadAllText(filePath);

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        List<YamlScenario> scenarios;
        try
        {
            var root = deserializer.Deserialize<YamlRoot>(yaml);
            scenarios = root?.Scenarios ?? new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Failed to parse {filePath}: {ex.Message}");
            return;
        }

        if (scenarios.Count == 0)
        {
            Console.WriteLine("  ⚠ No scenarios found in YAML file.");
            return;
        }

        scenarios.Sort((a, b) => a.Order.CompareTo(b.Order));
        Console.WriteLine($"\n=== Scenario-based import from '{filePath}' ===");
        Console.WriteLine($"  Found {scenarios.Count} scenario(s): {string.Join(", ", scenarios.Select(s => s.Name))}");

        foreach (var scenario in scenarios)
        {
            Console.WriteLine($"\n=== Scenario [{scenario.Order}]: {scenario.Name} ===");
            if (!string.IsNullOrWhiteSpace(scenario.Description))
                Console.WriteLine($"    {scenario.Description}");
            if (!string.IsNullOrWhiteSpace(scenario.DependsOn))
                Console.WriteLine($"    Depends on: {scenario.DependsOn}");

            var def = new ScenarioDefinition
            {
                Order        = scenario.Order,
                Name         = scenario.Name,
                DependsOn    = scenario.DependsOn,
                AnchorEntity = scenario.Anchor?.Entity ?? "",
                AnchorKey    = scenario.Anchor?.Key    ?? "",
                AnchorValue  = scenario.Anchor?.Value  ?? "",
            };

            if (await ScenarioAlreadySeededAsync(def))
            {
                Console.WriteLine($"  ℹ Already seeded (anchor found) — skipped.");
                continue;
            }

            if (scenario.Data == null || scenario.Data.Count == 0)
            {
                Console.WriteLine($"  ⚠ No data defined for scenario '{scenario.Name}'.");
                continue;
            }

            // Seed sheets in ExcelMappings dependency order
            foreach (var sheetMap in ExcelMappings.Sheets)
            {
                if (sheetMap == null)
                    continue;

                if (!scenario.Data.TryGetValue(sheetMap.SheetName, out var yamlRows)
                    || yamlRows == null || yamlRows.Count == 0)
                    continue;

                Console.WriteLine($"\n  → Seeding '{sheetMap.SheetName}' ({yamlRows.Count} row(s)) for [{scenario.Name}]");
                var (headerIndex, dataRows) = ConvertYamlRows(yamlRows);
                await SeedRowsAsync(sheetMap, headerIndex, dataRows);
            }

            Console.WriteLine($"  ✓ Scenario '{scenario.Name}' complete.");
        }

        Console.WriteLine("\n=== YAML scenario-based import complete ===\n");
    }

    /// <summary>
    /// Converts YAML row dictionaries into the (headerIndex, rows) format
    /// expected by SeedRowsAsync. The header index is built from the union
    /// of all keys across all rows so no column is lost.
    /// </summary>
    private static (Dictionary<string, int> headerIndex, List<List<object>> rows)
        ConvertYamlRows(List<Dictionary<string, string>> yamlRows)
    {
        var allKeys = yamlRows
            .SelectMany(r => r.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < allKeys.Count; i++)
            headerIndex[allKeys[i]] = i;

        var rows = yamlRows.Select(yamlRow =>
        {
            var row = Enumerable.Repeat<object>("", allKeys.Count).ToList();
            foreach (var (key, value) in yamlRow)
            {
                if (headerIndex.TryGetValue(key, out int idx))
                    row[idx] = value ?? "";
            }
            return row;
        }).ToList();

        return (headerIndex, rows);
    }

    // -----------------------------------------------------------------------
    // Scenario sheet reader — parses the "Scenarios" sheet locally (no API).
    // Returns scenarios sorted by Order ascending.
    // Returns an empty list (with a warning) if the sheet is absent.
    // -----------------------------------------------------------------------

    public static List<ScenarioDefinition> ReadScenarios(string filePath)
    {
        var scenarios = new List<ScenarioDefinition>();

        if (!System.IO.File.Exists(filePath))
        {
            Console.WriteLine($"  ✗ File not found: {filePath}");
            return scenarios;
        }

        List<string> sheetsInFile;
        try { sheetsInFile = ExcelParser.GetSheetNames(filePath); }
        catch (Exception ex) { Console.WriteLine($"  ✗ Could not open {filePath}: {ex.Message}"); return scenarios; }

        bool hasSheet = sheetsInFile.Any(s =>
            s.Trim().Equals(ExcelMappings.ScenariosSheet.SheetName, StringComparison.OrdinalIgnoreCase));

        if (!hasSheet)
        {
            Console.WriteLine("  ⚠ No 'Scenarios' sheet found — scenario-based seeding not available.");
            return scenarios;
        }

        // Build header index
        var headerRow = ExcelParser.Parse<List<object>?>(
            filePath, row => row, hasHeader: false,
            sheetName: ExcelMappings.ScenariosSheet.SheetName
        ).FirstOrDefault();

        if (headerRow == null) return scenarios;

        var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headerRow.Count; i++)
        {
            var text = headerRow[i]?.ToString()?.Trim() ?? "";
            if (!string.IsNullOrEmpty(text)) headerIndex[text] = i;
        }

        string Cell(List<object> row, string col) =>
            headerIndex.TryGetValue(col, out int idx) && idx < row.Count
                ? row[idx]?.ToString()?.Trim() ?? ""
                : "";

        // Parse data rows
        var dataRows = ExcelParser.Parse<List<object>?>(
            filePath, row => row, hasHeader: true,
            sheetName: ExcelMappings.ScenariosSheet.SheetName
        );

        foreach (var row in dataRows)
        {
            if (row == null || row.All(c => c == null || string.IsNullOrWhiteSpace(c?.ToString())))
                continue;

            var name = Cell(row, "Name");
            if (string.IsNullOrWhiteSpace(name)) continue;

            int order = int.TryParse(Cell(row, "Order"), out int o) ? o : 999;

            scenarios.Add(new ScenarioDefinition
            {
                Order        = order,
                Name         = name,
                Description  = Cell(row, "Description"),
                DependsOn    = Cell(row, "DependsOn"),
                AnchorEntity = Cell(row, "AnchorEntity"),
                AnchorKey    = Cell(row, "AnchorKey"),
                AnchorValue  = Cell(row, "AnchorValue"),
            });
        }

        scenarios.Sort((a, b) => a.Order.CompareTo(b.Order));

        Console.WriteLine($"  Found {scenarios.Count} scenario(s): {string.Join(", ", scenarios.Select(s => s.Name))}");
        return scenarios;
    }

    // -----------------------------------------------------------------------
    // Lookup resolution — results cached for the lifetime of this instance
    // -----------------------------------------------------------------------

    /// <summary>
    /// Resolves a value to { ID = guid } for a generic lookup entity.
    /// <paramref name="filterProperty"/> is the OData property path used in $filter.
    /// Defaults to "Name" but can be a navigation path such as "Position/Name"
    /// for entities that have no direct Name property (e.g. EmployeePositionHistory).
    /// </summary>
    private async Task<object?> ResolveLookupByNameAsync(
        string entityName, string name, string filterProperty = "Name")
    {
        // Include filterProperty in the cache key so different paths don't collide.
        string cacheKey = $"{entityName}|{filterProperty}|{name}";
        if (_lookupCache.TryGetValue(cacheKey, out var cached))
            return cached;

        try
        {
            // OData requires date/datetime values without string quotes.
            // Detect ISO date strings (YYYY-MM-DD) and promote to DateTimeOffset literal.
            string filterExpr;
            if (System.Text.RegularExpressions.Regex.IsMatch(name, @"^\d{4}-\d{2}-\d{2}$"))
                filterExpr = $"{filterProperty} eq {name}T00:00:00Z";
            else
                filterExpr = $"{filterProperty} eq '{name.Replace("'", "''")}'";

            var results = await _api.QueryAsync<IdHolder>(entityName, $"$filter={filterExpr}&$top=1");
            var found   = results.FirstOrDefault();
            var result  = found != null ? (object)new { ID = found.Id } : null;
            _lookupCache[cacheKey] = result;
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    ✗ Lookup error [{entityName}] {filterProperty}='{name}': {ex.Message}");
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
            
            IdHolder? found = null;
            try 
            {
                var results = await _api.QueryAsync<IdHolder>("Person", $"$filter=FullName eq '{escaped}'&$top=1");
                found = results.FirstOrDefault();
            }
            catch (Exception ex) when (ex.Message.Contains("FullName"))
            {
                // Fallback: If FullName property is not found, split by space and try FirstName/LastName
                var parts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    string f = parts[0].Replace("'", "''");
                    string l = parts[1].Replace("'", "''");
                    var results = await _api.QueryAsync<IdHolder>("Person", $"$filter=FirstName eq '{f}' and LastName eq '{l}'&$top=1");
                    found = results.FirstOrDefault();
                }
                else 
                {
                    // Last ditch: just try FirstName/LastName as OR
                    var results = await _api.QueryAsync<IdHolder>("Person", $"$filter=FirstName eq '{escaped}' or LastName eq '{escaped}'&$top=1");
                    found = results.FirstOrDefault();
                }
            }

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

    // Minimal DTO — only needs the ID from any OData lookup response
    private class IdHolder
    {
        [System.Text.Json.Serialization.JsonPropertyName("ID")]
        public Guid Id { get; set; }
    }
}