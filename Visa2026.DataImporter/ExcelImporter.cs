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
    private readonly ScenarioRunOptions? _runOptions;

    // Cache: "EntityName|FilterProperty|Name" → { ID = guid } — each API lookup fires once per run.
    private readonly Dictionary<string, object?> _lookupCache = new(StringComparer.OrdinalIgnoreCase);

    // Name of the special Excel column that tags each row with its scenario.
    // Not an OData property — never included in the API payload.
    private const string ScenarioColumnHeader = "Scenario";

    public ExcelImporter(ApiClient api, ScenarioRunOptions? runOptions = null)
    {
        _api = api;
        _runOptions = runOptions;
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

        await SeedRowsAsync(sheetMap, headerIndex, dataRows, upsertMode: false);
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

            bool syncMode = ShouldSyncScenario(scenario.Name, syncFlagInYaml: false);
            if (_runOptions?.HasTargetedRun == true && !ShouldRunScenario(scenario.Name, syncFlagInYaml: false))
            {
                Console.WriteLine($"  ⊘ Skipped (not in this run).");
                continue;
            }

            if (!syncMode && await ScenarioAlreadySeededAsync(scenario))
            {
                Console.WriteLine($"  ℹ Already seeded (anchor found) — skipped.");
                continue;
            }

            if (syncMode)
                Console.WriteLine($"  ↻ Sync mode — upserting rows (anchor ignored).");

            foreach (var sheetMap in ExcelMappings.Sheets)
            {
                if (sheetMap == null)
                    continue;

                if (!allData.TryGetValue(sheetMap.SheetName, out var sheetData)) continue;
                if (!sheetData.Rows.TryGetValue(scenario.Name, out var rows) || rows.Count == 0) continue;

                Console.WriteLine($"\n  → Seeding '{sheetMap.SheetName}' ({rows.Count} row(s)) for [{scenario.Name}]");
                await SeedRowsAsync(sheetMap, sheetData.Header, rows, syncMode);
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
        IEnumerable<List<object>> dataRows,
        bool upsertMode)
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
                else if (upsertMode)
                {
                    var existingIds = await TryFindAllExistingForUpsertAsync(
                        sheetMap, headerIndex, row, payload);
                    if (existingIds.Count > 0)
                    {
                        foreach (var id in existingIds)
                            await _api.UpdateAsync(sheetMap.EntityName, id, payload);
                        created = new IdHolder { Id = existingIds[0] };
                        if (existingIds.Count > 1)
                            Console.WriteLine(
                                $"  ✓ Updated {existingIds.Count} {sheetMap.DisplayName} row(s) ({rowLabel}) — " +
                                "duplicate(s) remain; delete extras in the UI.");
                        else
                            Console.WriteLine($"  ✓ Updated {sheetMap.DisplayName} ({rowLabel})");
                    }
                    else
                    {
                        var upsertKeys = sheetMap.UpsertKeys ?? ExcelMappings.GetDefaultUpsertKeys(sheetMap.EntityName);
                        if (upsertKeys is { Count: > 0 })
                            Console.WriteLine($"  + Created {sheetMap.DisplayName} ({rowLabel}) — no existing row for upsert key.");
                        else
                            Console.WriteLine($"  + Created {sheetMap.DisplayName} ({rowLabel}) — no upsert key defined for entity.");
                        created = await _api.CreateAsync<IdHolder>(sheetMap.EntityName, payload);
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

    private async Task<List<Guid>> TryFindAllExistingForUpsertAsync(
        SheetMap sheetMap,
        Dictionary<string, int> headerIndex,
        List<object> row,
        Dictionary<string, object?> payload)
    {
        if (sheetMap.EntityName.Equals("Application", StringComparison.OrdinalIgnoreCase))
        {
            var appId = await TryFindApplicationForUpsertAsync(headerIndex, row);
            return appId != null && appId != Guid.Empty ? new List<Guid> { appId.Value } : new List<Guid>();
        }

        if (sheetMap.EntityName.Equals("ApplicationItem", StringComparison.OrdinalIgnoreCase))
        {
            var displayName = BuildApplicationItemDisplayName(headerIndex, row);
            if (displayName == null)
                return new List<Guid>();
            return await QueryMatchingIdsAsync("ApplicationItem",
                $"ApplicationItemName eq '{EscapeODataString(displayName)}'");
        }

        if (sheetMap.EntityName.Equals("InvitationItem", StringComparison.OrdinalIgnoreCase))
        {
            var displayName = BuildInvitationItemDisplayName(headerIndex, row);
            if (displayName == null)
                return new List<Guid>();
            return await QueryMatchingIdsAsync("InvitationItem",
                $"InvitationItemName eq '{EscapeODataString(displayName)}'");
        }

        var upsertKeys = sheetMap.UpsertKeys ?? ExcelMappings.GetDefaultUpsertKeys(sheetMap.EntityName);
        if (upsertKeys == null || upsertKeys.Count == 0)
            return new List<Guid>();

        var filter = BuildUpsertFilter(upsertKeys, headerIndex, row, payload);
        if (filter == null)
            return new List<Guid>();

        return await QueryMatchingIdsAsync(sheetMap.EntityName, filter);
    }

    /// <summary>Matches <see cref="ApplicationItem.ApplicationItemName"/> ({Person} - {FullApplicationNumber}).</summary>
    private static string? BuildApplicationItemDisplayName(Dictionary<string, int> headerIndex, List<object> row)
    {
        var person = GetRowCell(headerIndex, row, "Person");
        var application = GetRowCell(headerIndex, row, "Application");
        if (string.IsNullOrWhiteSpace(person) || string.IsNullOrWhiteSpace(application))
            return null;
        return $"{person} - {application}";
    }

    /// <summary>Matches <see cref="InvitationItem.InvitationItemName"/> ({Person} - {InvitationNumber}).</summary>
    private static string? BuildInvitationItemDisplayName(Dictionary<string, int> headerIndex, List<object> row)
    {
        var person = GetRowCell(headerIndex, row, "Person");
        var invitationNumber = GetRowCell(headerIndex, row, "Invitation Number");
        if (string.IsNullOrWhiteSpace(person) || string.IsNullOrWhiteSpace(invitationNumber))
            return null;
        return $"{person} - {invitationNumber}";
    }

    private async Task<Guid?> TryFindApplicationForUpsertAsync(
        Dictionary<string, int> headerIndex,
        List<object> row)
    {
        var fullNumber = GetRowCell(headerIndex, row, "Full Application Number");
        if (!string.IsNullOrWhiteSpace(fullNumber))
        {
            var byFull = await QueryMatchingIdsAsync("Application",
                $"FullApplicationNumber eq '{EscapeODataString(fullNumber)}'");
            return byFull.Count > 0 ? byFull[0] : null;
        }

        var appNumber = GetRowCell(headerIndex, row, "Application Number");
        var dateRaw = GetRowCell(headerIndex, row, "Date");
        if (string.IsNullOrWhiteSpace(appNumber) || string.IsNullOrWhiteSpace(dateRaw))
            return null;

        var dateLiteral = FormatODataLiteral(DataParser.ParseScalar(dateRaw));
        var filter =
            $"ApplicationNumber eq '{EscapeODataString(appNumber)}' and ApplicationDate eq {dateLiteral}";
        var ids = await QueryMatchingIdsAsync("Application", filter);
        return ids.Count > 0 ? ids[0] : null;
    }

    private static string? BuildUpsertFilter(
        IReadOnlyList<UpsertKeyPart> upsertKeys,
        Dictionary<string, int> headerIndex,
        List<object> row,
        Dictionary<string, object?> payload)
    {
        var parts = new List<string>();
        foreach (var key in upsertKeys)
        {
            string? literal;
            if (key.FromPayload)
            {
                var prop = key.PayloadProperty ?? key.Header;
                if (!payload.TryGetValue(prop, out var refObj))
                    return null;
                var id = ExtractReferenceId(refObj);
                if (id == null)
                    return null;
                literal = FormatODataGuidLiteral(id.Value);
            }
            else
            {
                var raw = GetRowCell(headerIndex, row, key.Header);
                if (string.IsNullOrWhiteSpace(raw))
                {
                    if (key.Optional)
                    {
                        parts.Add($"{key.ODataProperty} eq null");
                        continue;
                    }
                    return null;
                }
                literal = key.StringLiteral
                    ? $"'{EscapeODataString(raw)}'"
                    : FormatODataLiteral(DataParser.ParseScalar(raw));
            }

            parts.Add($"{key.ODataProperty} eq {literal}");
        }

        return parts.Count == 0 ? null : string.Join(" and ", parts);
    }

    private async Task<List<Guid>> QueryMatchingIdsAsync(
        string entityName,
        string filterClause,
        int maxResults = 20)
    {
        try
        {
            var results = await _api.QueryAsync<IdHolder>(
                entityName, $"$filter={filterClause}&$top={maxResults}");
            return results
                .Where(r => r.Id != Guid.Empty)
                .Select(r => r.Id)
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ⚠ Upsert lookup failed for {entityName}: {ex.Message}");
            return new List<Guid>();
        }
    }

    private static string GetRowCell(Dictionary<string, int> headerIndex, List<object> row, string header)
    {
        if (!headerIndex.TryGetValue(header, out int colIdx) || colIdx >= row.Count)
            return "";
        return row[colIdx]?.ToString()?.Trim() ?? "";
    }

    private static string EscapeODataString(string value) => value.Replace("'", "''");

    private static string FormatODataLiteral(object value) => value switch
    {
        bool b => b ? "true" : "false",
        int i => i.ToString(System.Globalization.CultureInfo.InvariantCulture),
        long l => l.ToString(System.Globalization.CultureInfo.InvariantCulture),
        decimal d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
        double dbl => dbl.ToString(System.Globalization.CultureInfo.InvariantCulture),
        float f => f.ToString(System.Globalization.CultureInfo.InvariantCulture),
        DateTime dt => dt.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", System.Globalization.CultureInfo.InvariantCulture),
        DateTimeOffset dto => dto.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", System.Globalization.CultureInfo.InvariantCulture),
        Guid g => FormatODataGuidLiteral(g),
        string s when System.Text.RegularExpressions.Regex.IsMatch(s, @"^\d{4}-\d{2}-\d{2}$")
            => $"{s}T00:00:00Z",
        string s => $"'{EscapeODataString(s)}'",
        _ => $"'{EscapeODataString(value.ToString() ?? "")}'",
    };

    /// <summary>XAF OData expects bare GUID literals (no <c>guid'…'</c> prefix).</summary>
    private static string FormatODataGuidLiteral(Guid id) => id.ToString("D");

    private static Guid? ExtractReferenceId(object? refObj)
    {
        if (refObj == null)
            return null;

        if (refObj is JsonElement je)
        {
            if (je.ValueKind == JsonValueKind.Object &&
                je.TryGetProperty("ID", out var idEl) &&
                Guid.TryParse(idEl.GetString(), out var fromJson))
                return fromJson;
            return null;
        }

        var idProp = refObj.GetType().GetProperty("ID");
        if (idProp?.GetValue(refObj) is Guid g)
            return g;
        if (idProp?.GetValue(refObj) is string s && Guid.TryParse(s, out var parsed))
            return parsed;

        return null;
    }

    /// <summary>
    /// Returns true if the scenario's anchor entity already exists in the API.
    /// Returns false (proceed) if no anchor is configured.
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
            throw new InvalidOperationException(
                $"Anchor check failed for scenario '{scenario.Name}' " +
                $"({scenario.AnchorEntity}.{scenario.AnchorKey}='{scenario.AnchorValue}'). " +
                "Refusing to proceed because re-running seed may create duplicate rows. " +
                "Fix the anchor (entity/key/value must be queryable via OData), or re-run using " +
                "--sync/--sync-scenario (upsert) or --clear-scenario (delete + re-import). " +
                $"Details: {ex.Message}", ex);
        }
    }

    // -----------------------------------------------------------------------
    // YAML scenario-based import
    //
    // Reads data.yaml, resolves scenarios in Order sequence, reuses the same
    // SeedRowsAsync / lookup resolution pipeline as the Excel path.
    // -----------------------------------------------------------------------

    public async Task ImportByScenariosFromYamlAsync(string seedPath)
    {
        List<YamlScenario> scenarios;
        try
        {
            if (!File.Exists(seedPath) && !Directory.Exists(seedPath))
            {
                Console.WriteLine($"  ✗ Seed path not found: {seedPath}");
                return;
            }

            scenarios = YamlSeedCatalog.LoadScenarios(seedPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Failed to load seed data from '{seedPath}': {ex.Message}");
            return;
        }

        if (scenarios.Count == 0)
        {
            Console.WriteLine("  ⚠ No scenarios found in seed data.");
            return;
        }

        // Safety: default seed scenarios must be re-runnable without duplicating data.
        // Enforce anchors for every non-sync scenario that contains data.
        var scenariosMissingAnchors = scenarios
            .Where(s => s.Data != null && s.Data.Count > 0)
            .Where(s => !s.Sync)
            .Where(s => s.Anchor == null
                        || string.IsNullOrWhiteSpace(s.Anchor.Entity)
                        || string.IsNullOrWhiteSpace(s.Anchor.Key)
                        || string.IsNullOrWhiteSpace(s.Anchor.Value))
            .Select(s => s.Name)
            .ToList();
        if (scenariosMissingAnchors.Count > 0)
        {
            Console.WriteLine("  ✗ Seed validation failed: one or more scenarios have data but no anchor.");
            Console.WriteLine("    This would allow duplicate rows on re-run.");
            Console.WriteLine("    Fix: add `anchor: { entity, key, value }` to each scenario,");
            Console.WriteLine("    or set `sync: true` and run with --sync/--sync-scenario for upsert behavior.");
            Console.WriteLine($"    Scenarios without anchors: {string.Join(", ", scenariosMissingAnchors)}");
            return;
        }

        string displayPath = YamlSeedCatalog.IsSeedIndexPath(seedPath) || YamlSeedCatalog.IsSeedDirectory(seedPath)
            ? seedPath
            : seedPath;
        Console.WriteLine($"\n=== Scenario-based import from '{displayPath}' ===");
        Console.WriteLine($"  Found {scenarios.Count} scenario(s): {string.Join(", ", scenarios.Select(s => s.Name))}");

        ApplicationTypeVisibilityCatalog visibility;
        try
        {
            visibility = ApplicationTypeVisibilityCatalog.Load();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ {ex.Message}");
            return;
        }

        var seedProcessor = new SeedScenarioProcessor(visibility);

        foreach (var scenario in scenarios)
        {
            var visibilityResult = seedProcessor.Normalize(scenario, removeDisallowed: true);
            foreach (var issue in visibilityResult.Issues.Where(i =>
                         i.Kind is SeedIssueKind.PrunedColumn or SeedIssueKind.PrunedSheet))
            {
                Console.WriteLine($"  ⊘ {issue.Message}");
            }

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

            if (_runOptions?.HasTargetedRun == true && !ShouldRunScenario(scenario.Name, scenario.Sync))
            {
                Console.WriteLine($"  ⊘ Skipped (not in this run).");
                continue;
            }

            bool clearFirst = ShouldClearScenario(scenario.Name);

            // Default behavior for YAML seed: sync/upsert.
            // Users can still force a delete+reimport for a scenario via --clear-scenario.
            bool syncMode = _runOptions == null || !_runOptions.HasTargetedRun
                ? true
                : ShouldSyncScenario(scenario.Name, scenario.Sync);

            if (scenario.Data == null || scenario.Data.Count == 0)
            {
                Console.WriteLine($"  ⚠ No data defined for scenario '{scenario.Name}'.");
                continue;
            }

            if (clearFirst)
            {
                Console.WriteLine($"  🗑 Clear mode — deleting scenario data, then re-importing.");
                await ClearScenarioDataAsync(scenario);
            }
            else if (!syncMode && await ScenarioAlreadySeededAsync(def))
            {
                Console.WriteLine($"  ℹ Already seeded (anchor found) — skipped.");
                continue;
            }
            else if (syncMode)
            {
                Console.WriteLine($"  ↻ Sync mode — upserting rows (anchor ignored).");
            }

            // Seed sheets in ExcelMappings dependency order
            foreach (var sheetMap in ExcelMappings.Sheets)
            {
                if (sheetMap == null)
                    continue;

                if (!scenario.Data.TryGetValue(sheetMap.SheetName, out var yamlRows)
                    || yamlRows == null || yamlRows.Count == 0)
                    continue;

                if (syncMode && !HasUpsertStrategy(sheetMap))
                {
                    throw new InvalidOperationException(
                        $"Cannot run default sync/upsert because '{sheetMap.SheetName}' → {sheetMap.EntityName} " +
                        "does not define an upsert strategy. " +
                        "Fix: add UpsertKeys for this entity in ExcelMappings, or remove this sheet from the scenario, " +
                        "or run with --clear-scenario to delete+reimport for this scenario.");
                }

                Console.WriteLine($"\n  → Seeding '{sheetMap.SheetName}' ({yamlRows.Count} row(s)) for [{scenario.Name}]");
                var (headerIndex, dataRows) = ConvertYamlRows(yamlRows);
                await SeedRowsAsync(sheetMap, headerIndex, dataRows, syncMode);
            }

            Console.WriteLine($"  ✓ Scenario '{scenario.Name}' complete.");
        }

        Console.WriteLine("\n=== YAML scenario-based import complete ===\n");
    }

    private static bool HasUpsertStrategy(SheetMap sheetMap)
    {
        if (sheetMap.SingletonUpsert)
            return true;

        // These entities have custom upsert matching logic in TryFindAllExistingForUpsertAsync.
        if (sheetMap.EntityName.Equals("Application", StringComparison.OrdinalIgnoreCase) ||
            sheetMap.EntityName.Equals("ApplicationItem", StringComparison.OrdinalIgnoreCase) ||
            sheetMap.EntityName.Equals("InvitationItem", StringComparison.OrdinalIgnoreCase))
            return true;

        var upsertKeys = sheetMap.UpsertKeys ?? ExcelMappings.GetDefaultUpsertKeys(sheetMap.EntityName);
        return upsertKeys is { Count: > 0 };
    }

    private bool ShouldRunScenario(string scenarioName, bool syncFlagInYaml) =>
        _runOptions == null || _runOptions.ShouldRunScenario(scenarioName, syncFlagInYaml);

    private bool ShouldClearScenario(string scenarioName) =>
        _runOptions != null && _runOptions.ShouldClearScenario(scenarioName);

    private bool ShouldSyncScenario(string scenarioName, bool syncFlagInYaml) =>
        _runOptions != null && _runOptions.ShouldSyncScenario(scenarioName, syncFlagInYaml);

    /// <summary>
    /// Deletes rows described in the scenario yaml (reverse sheet order + application cascade).
    /// </summary>
    private async Task ClearScenarioDataAsync(YamlScenario scenario)
    {
        await ClearApplicationScopedDataAsync(scenario);

        var sheets = ExcelMappings.Sheets
            .Where(s => s != null && scenario.Data!.ContainsKey(s.SheetName))
            .Reverse()
            .ToList();

        foreach (var sheetMap in sheets)
        {
            if (sheetMap.EntityName.Equals("Application", StringComparison.OrdinalIgnoreCase))
                continue;

            var yamlRows = scenario.Data![sheetMap.SheetName];
            if (yamlRows == null || yamlRows.Count == 0)
                continue;

            if (ExcelMappings.IsBlockedImportEntity(sheetMap.EntityName))
                continue;

            var (headerIndex, dataRows) = ConvertYamlRows(yamlRows);
            int deleted = 0;
            foreach (var row in dataRows)
            {
                var ids = await FindExistingIdsForClearAsync(sheetMap, headerIndex, row);
                foreach (var id in ids)
                {
                    try
                    {
                        await _api.DeleteAsync(sheetMap.EntityName, id);
                        deleted++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  ⚠ Could not delete {sheetMap.DisplayName} ({id}): {ex.Message}");
                    }
                }
            }

            if (deleted > 0)
                Console.WriteLine($"  🗑 Cleared {deleted} {sheetMap.DisplayName} row(s).");
        }
    }

    private async Task ClearApplicationScopedDataAsync(YamlScenario scenario)
    {
        if (!scenario.Data!.TryGetValue("Applications", out var appRows) || appRows.Count == 0)
            return;

        var (headerIndex, dataRows) = ConvertYamlRows(appRows);
        foreach (var row in dataRows)
        {
            var appId = await TryFindApplicationForUpsertAsync(headerIndex, row);
            if (appId == null || appId == Guid.Empty)
                continue;

            var idLiteral = FormatODataGuidLiteral(appId.Value);
            int deleted = 0;
            deleted += await DeleteAllMatchingAsync("ApplicationItem", $"Application/ID eq {idLiteral}");
            deleted += await DeleteAllMatchingAsync("InvitationItem", $"Invitation/Application/ID eq {idLiteral}");
            deleted += await DeleteAllMatchingAsync("Invitation", $"Application/ID eq {idLiteral}");
            deleted += await DeleteAllMatchingAsync("WorkPermitItem", $"WorkPermit/Application/ID eq {idLiteral}");
            deleted += await DeleteAllMatchingAsync("WorkPermit", $"Application/ID eq {idLiteral}");
            deleted += await DeleteAllMatchingAsync("RejectionItem", $"Rejection/Application/ID eq {idLiteral}");
            deleted += await DeleteAllMatchingAsync("Rejection", $"Application/ID eq {idLiteral}");
            deleted += await DeleteAllMatchingAsync("ApplicationProgress", $"Application/ID eq {idLiteral}");

            try
            {
                await _api.DeleteAsync("Application", appId.Value);
                deleted++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ⚠ Could not delete Application ({appId}): {ex.Message}");
            }

            if (deleted > 0)
            {
                var appNumber = GetRowCell(headerIndex, row, "Application Number");
                var label = string.IsNullOrWhiteSpace(appNumber) ? appId.ToString() : appNumber;
                Console.WriteLine($"  🗑 Cleared application scope for #{label} ({deleted} record(s)).");
            }
        }
    }

    private async Task<int> DeleteAllMatchingAsync(string entityName, string filterClause)
    {
        var ids = await QueryMatchingIdsAsync(entityName, filterClause, maxResults: 200);
        int count = 0;
        foreach (var id in ids)
        {
            try
            {
                await _api.DeleteAsync(entityName, id);
                count++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ⚠ Could not delete {entityName} ({id}): {ex.Message}");
            }
        }
        return count;
    }

  /// <summary>Resolve existing row IDs for clear (header-based; no payload required).</summary>
    private async Task<List<Guid>> FindExistingIdsForClearAsync(
        SheetMap sheetMap,
        Dictionary<string, int> headerIndex,
        List<object> row)
    {
        if (sheetMap.EntityName.Equals("Application", StringComparison.OrdinalIgnoreCase))
        {
            var id = await TryFindApplicationForUpsertAsync(headerIndex, row);
            return id != null && id != Guid.Empty ? new List<Guid> { id.Value } : new List<Guid>();
        }

        if (sheetMap.EntityName.Equals("ApplicationItem", StringComparison.OrdinalIgnoreCase))
        {
            var displayName = BuildApplicationItemDisplayName(headerIndex, row);
            if (displayName == null) return new List<Guid>();
            return await QueryMatchingIdsAsync("ApplicationItem",
                $"ApplicationItemName eq '{EscapeODataString(displayName)}'");
        }

        if (sheetMap.EntityName.Equals("InvitationItem", StringComparison.OrdinalIgnoreCase))
        {
            var displayName = BuildInvitationItemDisplayName(headerIndex, row);
            if (displayName == null) return new List<Guid>();
            return await QueryMatchingIdsAsync("InvitationItem",
                $"InvitationItemName eq '{EscapeODataString(displayName)}'");
        }

        if (sheetMap.EntityName.Equals("Invitation", StringComparison.OrdinalIgnoreCase))
        {
            var inv = GetRowCell(headerIndex, row, "Invitation Number");
            if (string.IsNullOrWhiteSpace(inv)) return new List<Guid>();
            return await QueryMatchingIdsAsync("Invitation",
                $"InvitationNumber eq '{EscapeODataString(inv)}'");
        }

        if (sheetMap.EntityName.Equals("Person", StringComparison.OrdinalIgnoreCase))
        {
            var email = GetRowCell(headerIndex, row, "Email");
            if (string.IsNullOrWhiteSpace(email)) return new List<Guid>();
            return await QueryMatchingIdsAsync("Person", $"Email eq '{EscapeODataString(email)}'");
        }

        if (sheetMap.EntityName.Equals("Passport", StringComparison.OrdinalIgnoreCase))
        {
            var num = GetRowCell(headerIndex, row, "Passport Number");
            if (string.IsNullOrWhiteSpace(num)) return new List<Guid>();
            return await QueryMatchingIdsAsync("Passport",
                $"PassportNumber eq '{EscapeODataString(num)}'");
        }

        if (sheetMap.EntityName.Equals("MedicalRecord", StringComparison.OrdinalIgnoreCase))
        {
            var doc = GetRowCell(headerIndex, row, "Document Number");
            if (string.IsNullOrWhiteSpace(doc)) return new List<Guid>();
            return await QueryMatchingIdsAsync("MedicalRecord",
                $"DocumentNumber eq '{EscapeODataString(doc)}'");
        }

        if (sheetMap.EntityName.Equals("Visa", StringComparison.OrdinalIgnoreCase))
        {
            var num = GetRowCell(headerIndex, row, "Visa Number");
            if (string.IsNullOrWhiteSpace(num)) return new List<Guid>();
            return await QueryMatchingIdsAsync("Visa", $"VisaNumber eq '{EscapeODataString(num)}'");
        }

        if (sheetMap.EntityName.Equals("Education", StringComparison.OrdinalIgnoreCase))
        {
            var personId = await ResolvePersonIdFromRowAsync(headerIndex, row);
            if (personId == null)
                return new List<Guid>();
            var yearRaw = GetRowCell(headerIndex, row, "Graduation Year");
            var yearClause = string.IsNullOrWhiteSpace(yearRaw)
                ? "GraduationYear eq null"
                : $"GraduationYear eq '{EscapeODataString(yearRaw)}'";
            return await QueryMatchingIdsAsync("Education",
                $"Person/ID eq {FormatODataGuidLiteral(personId.Value)} and {yearClause}");
        }

        if (sheetMap.EntityName.Equals("EmployeePositionHistory", StringComparison.OrdinalIgnoreCase))
        {
            var personId = await ResolvePersonIdFromRowAsync(headerIndex, row);
            var startRaw = GetRowCell(headerIndex, row, "Start Date");
            var position = GetRowCell(headerIndex, row, "Position");
            if (personId == null || string.IsNullOrWhiteSpace(startRaw) || string.IsNullOrWhiteSpace(position))
                return new List<Guid>();
            var startLit = FormatODataLiteral(DataParser.ParseScalar(startRaw));
            return await QueryMatchingIdsAsync("EmployeePositionHistory",
                $"Person/ID eq {FormatODataGuidLiteral(personId.Value)} and StartDate eq {startLit} and Position/Name eq '{EscapeODataString(position)}'");
        }

        if (sheetMap.EntityName.Equals("EmployeeContract", StringComparison.OrdinalIgnoreCase))
        {
            var personId = await ResolvePersonIdFromRowAsync(headerIndex, row);
            var startRaw = GetRowCell(headerIndex, row, "Start Date");
            if (personId == null || string.IsNullOrWhiteSpace(startRaw))
                return new List<Guid>();
            var startLit = FormatODataLiteral(DataParser.ParseScalar(startRaw));
            return await QueryMatchingIdsAsync("EmployeeContract",
                $"Person/ID eq {FormatODataGuidLiteral(personId.Value)} and ContractStartDate eq {startLit}");
        }

        if (sheetMap.EntityName.Equals("AddressOfResidence", StringComparison.OrdinalIgnoreCase))
        {
            var personId = await ResolvePersonIdFromRowAsync(headerIndex, row);
            var address = GetRowCell(headerIndex, row, "Full Address");
            if (personId == null || string.IsNullOrWhiteSpace(address))
                return new List<Guid>();
            return await QueryMatchingIdsAsync("AddressOfResidence",
                $"Person/ID eq {FormatODataGuidLiteral(personId.Value)} and FullAddress eq '{EscapeODataString(address)}'");
        }

        if (sheetMap.UpsertKeys is { Count: > 0 } keys && keys.All(k => !k.FromPayload))
        {
            var filter = BuildUpsertFilter(keys, headerIndex, row, new Dictionary<string, object?>());
            if (filter == null) return new List<Guid>();
            return await QueryMatchingIdsAsync(sheetMap.EntityName, filter);
        }

        return new List<Guid>();
    }

    private async Task<Guid?> ResolvePersonIdFromRowAsync(Dictionary<string, int> headerIndex, List<object> row)
    {
        var fullName = GetRowCell(headerIndex, row, "Person");
        if (string.IsNullOrWhiteSpace(fullName))
            return null;

        var refObj = await ResolvePersonByFullNameAsync(fullName);
        return ExtractReferenceId(refObj);
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