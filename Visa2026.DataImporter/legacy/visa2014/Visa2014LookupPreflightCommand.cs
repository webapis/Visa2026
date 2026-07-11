using System.Data;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Pre-import gate: audit live VISA2015 lookup usage → translate via lookup-translations.yaml
/// → verify targets exist on Visa2026 (when --target-connection is set).
/// Exit 0 only when every blocking gap is resolved (block_row unmapped / missing target).
/// </summary>
internal static class Visa2014LookupPreflightCommand
{
    private static readonly string[] DefaultTransformEntities =
    [
        "Person",
        "Passport",
        "Visa",
        "Education",
        "EmployeePositionHistory",
        "AddressOfResidence",
        "Lodging",
        "Hotel",
        "Hospital",
        "OtherSite",
        "Application",
        "ApplicationItem",
        "WorkPermitItem",
    ];

    public static int Run(IReadOnlyList<string> args, bool verbose)
    {
        var dataImporterRoot = Visa2014ContentRoot.FindDataImporterRoot();
        if (dataImporterRoot == null)
        {
            Console.Error.WriteLine("ERR Could not locate Visa2026.DataImporter content root.");
            return 1;
        }

        var solutionRoot = Visa2014ContentRoot.FindSolutionRoot();
        Visa2014LegacySourceProfile source;
        try
        {
            source = Visa2014LegacySource.Resolve(dataImporterRoot, solutionRoot, args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERR {ex.Message}");
            return 1;
        }

        var catalogs = Visa2014LookupTranslator.Load(source.LookupTranslationPaths);
        var yamlCatalogs = LoadYamlCatalogNodes(source.LookupTranslationPaths);
        var catalogOnly = HasArg(args, "--catalog-only");
        var skipTargetCheck = HasArg(args, "--skip-target-check");
        var targetConnection = GetOptionValue(args, "--target-connection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? Environment.GetEnvironmentVariable("VISA2026_SQL_CONNECTION");

        int? maxRows = null;
        var maxRowsText = GetOptionValue(args, "--max-rows");
        if (int.TryParse(maxRowsText, out var parsedMax) && parsedMax > 0)
            maxRows = parsedMax;

        var entityFilter = GetOptionValue(args, "--entity");
        var transformEntities = string.IsNullOrWhiteSpace(entityFilter)
            ? DefaultTransformEntities
            : entityFilter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var output = GetOptionValue(args, "--output")
            ?? Path.Combine(
                Visa2014ContentRoot.LegacyRoot(dataImporterRoot),
                "preview-export",
                $"lookup-preflight-{source.Id}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");

        Console.WriteLine("=== VISA2014 lookup preflight (audit → translate → attach/map) ===");
        Console.WriteLine($"INF Legacy source: {source.Id} ({source.Label})");
        Console.WriteLine($"INF Database: {MaskConnectionForLog(source.ConnectionString)}");
        Console.WriteLine("INF Lookup translations:");
        foreach (var path in source.LookupTranslationPaths)
            Console.WriteLine($"INF   - {path}");
        Console.WriteLine($"INF Catalogs loaded: {catalogs.Count}");
        if (!string.IsNullOrWhiteSpace(targetConnection) && !skipTargetCheck)
            Console.WriteLine($"INF Target DB: {MaskConnectionForLog(targetConnection)}");
        else
            Console.WriteLine("INF Target DB check: skipped (pass --target-connection, or omit --skip-target-check)");

        var report = new PreflightReport
        {
            LegacySource = source.Id,
            GeneratedAtUtc = DateTime.UtcNow.ToString("O"),
        };

        Dictionary<string, HashSet<string>>? targetKeysByCatalog = null;
        if (!string.IsNullOrWhiteSpace(targetConnection) && !skipTargetCheck)
        {
            try
            {
                targetKeysByCatalog = LoadTargetCatalogKeys(targetConnection, catalogs, verbose);
                report.TargetCatalogsLoaded = targetKeysByCatalog.Count;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ERR Failed to load target lookup keys: {ex.Message}");
                return 1;
            }
        }

        // Phase A — DISTINCT sampleQuery per catalog in lookup-translations YAML
        Console.WriteLine();
        Console.WriteLine(">>> Phase A: catalog sampleQuery audit");
        using (var legacyConn = new SqlConnection(source.ConnectionString))
        {
            legacyConn.Open();
            foreach (var node in yamlCatalogs)
            {
                if (string.IsNullOrWhiteSpace(node.TargetCatalog))
                    continue;

                catalogs.TryGetValue(node.TargetCatalog, out var runtimeCatalog);
                var policy = runtimeCatalog?.UnmappedPolicy ?? node.UnmappedPolicy ?? "block_row";
                var matchProp = runtimeCatalog?.TargetMatchProperty ?? node.TargetMatchProperty ?? "Name";

                if (string.IsNullOrWhiteSpace(node.SampleQuery))
                {
                    report.CatalogResults.Add(new CatalogAuditRow
                    {
                        Catalog = node.TargetCatalog,
                        Status = "skipped_no_sample_query",
                        UnmappedPolicy = policy,
                        Notes = "Covered by Phase B entity transforms when applicable.",
                    });
                    if (verbose)
                        Console.WriteLine($"INF {node.TargetCatalog}: no sampleQuery — skip Phase A");
                    continue;
                }

                List<(string Legacy, int Count)> distinct;
                try
                {
                    distinct = ExecuteDistinctSampleQuery(legacyConn, node.SampleQuery, node.LegacyColumn);
                }
                catch (Exception ex)
                {
                    report.BlockingGaps.Add(new GapRow
                    {
                        Phase = "catalog_sample_query",
                        Catalog = node.TargetCatalog,
                        LegacyValue = "",
                        Reason = $"sample_query_failed:{ex.Message}",
                        Policy = policy,
                    });
                    report.CatalogResults.Add(new CatalogAuditRow
                    {
                        Catalog = node.TargetCatalog,
                        Status = "query_failed",
                        UnmappedPolicy = policy,
                        Notes = ex.Message,
                    });
                    Console.Error.WriteLine($"ERR {node.TargetCatalog}: sampleQuery failed — {ex.Message}");
                    continue;
                }

                var mapped = 0;
                var allowedUnmapped = 0;
                var blocking = 0;
                var missingTarget = 0;

                foreach (var (legacyValue, count) in distinct)
                {
                    if (string.IsNullOrWhiteSpace(legacyValue))
                        continue;

                    var ok = Visa2014LookupTranslator.TryTranslate(
                        catalogs,
                        node.TargetCatalog,
                        legacyValue,
                        out var targetValue,
                        out var unmappedReason);

                    if (!ok)
                    {
                        if (IsBlockingPolicy(policy))
                        {
                            blocking++;
                            report.BlockingGaps.Add(new GapRow
                            {
                                Phase = "catalog_sample_query",
                                Catalog = node.TargetCatalog,
                                LegacyValue = legacyValue,
                                Reason = unmappedReason ?? "unmapped",
                                Policy = policy,
                                RowCount = count,
                            });
                        }
                        else
                        {
                            allowedUnmapped++;
                            report.AllowedGaps.Add(new GapRow
                            {
                                Phase = "catalog_sample_query",
                                Catalog = node.TargetCatalog,
                                LegacyValue = legacyValue,
                                Reason = unmappedReason ?? $"unmapped_policy:{policy}",
                                Policy = policy,
                                RowCount = count,
                            });
                        }

                        continue;
                    }

                    mapped++;
                    if (targetKeysByCatalog != null
                        && !string.IsNullOrWhiteSpace(targetValue)
                        && IsBlockingPolicy(policy)
                        && !TargetContains(targetKeysByCatalog, node.TargetCatalog, targetValue!))
                    {
                        missingTarget++;
                        report.BlockingGaps.Add(new GapRow
                        {
                            Phase = "target_missing",
                            Catalog = node.TargetCatalog,
                            LegacyValue = legacyValue,
                            TargetValue = targetValue,
                            Reason = $"missing_target:{matchProp}={targetValue}",
                            Policy = policy,
                            RowCount = count,
                        });
                    }
                }

                var status = blocking > 0 || missingTarget > 0
                    ? "fail"
                    : allowedUnmapped > 0
                        ? "ok_with_allowed_unmapped"
                        : "ok";

                report.CatalogResults.Add(new CatalogAuditRow
                {
                    Catalog = node.TargetCatalog,
                    Status = status,
                    UnmappedPolicy = policy,
                    DistinctLegacy = distinct.Count,
                    Mapped = mapped,
                    AllowedUnmapped = allowedUnmapped,
                    BlockingUnmapped = blocking,
                    MissingTarget = missingTarget,
                });

                var color = status == "fail" ? "ERR" : "INF";
                Console.WriteLine(
                    $"{color} {node.TargetCatalog}: distinct={distinct.Count} mapped={mapped} " +
                    $"allowedUnmapped={allowedUnmapped} blocking={blocking} missingTarget={missingTarget} [{status}]");
            }
        }

        // Phase B — entity transforms (FullAddress → Region/City, tenant lookups, etc.)
        if (!catalogOnly)
        {
            Console.WriteLine();
            Console.WriteLine(">>> Phase B: entity transform unmapped lookups");
            foreach (var entity in transformEntities)
            {
                try
                {
                    var batch = PrepareEntityBatch(
                        entity,
                        source.ConnectionString,
                        source.LookupTranslationPaths,
                        maxRows,
                        verbose);

                    if (batch == null)
                    {
                        report.EntityResults.Add(new EntityAuditRow
                        {
                            Entity = entity,
                            Status = "skipped_unsupported",
                        });
                        continue;
                    }

                    var blocking = 0;
                    var allowed = 0;
                    foreach (var row in batch.UnmappedLookups)
                    {
                        var catalogName = row.GetValueOrDefault("catalog") as string
                            ?? ParseCatalogFromReason(row.GetValueOrDefault("reason") as string);
                        var legacyValue = row.GetValueOrDefault("legacyValue") as string
                            ?? row.GetValueOrDefault("legacy") as string
                            ?? "";
                        var reason = row.GetValueOrDefault("reason") as string ?? "unmapped";

                        var policy = "block_row";
                        if (!string.IsNullOrWhiteSpace(catalogName)
                            && catalogs.TryGetValue(catalogName, out var cat))
                            policy = cat.UnmappedPolicy;

                        var gap = new GapRow
                        {
                            Phase = "entity_transform",
                            Entity = entity,
                            Catalog = catalogName ?? "",
                            LegacyValue = legacyValue,
                            Reason = reason,
                            Policy = policy,
                        };

                        if (IsBlockingPolicy(policy) || string.IsNullOrWhiteSpace(catalogName))
                        {
                            blocking++;
                            report.BlockingGaps.Add(gap);
                        }
                        else
                        {
                            allowed++;
                            report.AllowedGaps.Add(gap);
                        }
                    }

                    // Target existence for successfully translated identity/mapped values is covered in Phase A
                    // for catalogs with sampleQuery. For transform-only catalogs, check distinct translated
                    // keys present on import rows when target DB is available.
                    var missingTarget = 0;
                    if (targetKeysByCatalog != null)
                    {
                        missingTarget = CountMissingTargetsOnImportRows(
                            batch.ImportRows,
                            catalogs,
                            targetKeysByCatalog,
                            entity,
                            report.BlockingGaps);
                    }

                    var status = blocking > 0 || missingTarget > 0 ? "fail" : "ok";
                    report.EntityResults.Add(new EntityAuditRow
                    {
                        Entity = entity,
                        Status = status,
                        LegacyRowCount = batch.LegacyRowCount,
                        ImportRowCount = batch.ImportRows.Count,
                        SkippedRowCount = batch.Skipped.Count,
                        UnmappedLookupCount = batch.UnmappedLookups.Count,
                        BlockingUnmapped = blocking,
                        AllowedUnmapped = allowed,
                        MissingTarget = missingTarget,
                    });

                    Console.WriteLine(
                        $"{(status == "fail" ? "ERR" : "INF")} {entity}: " +
                        $"import={batch.ImportRows.Count} skipped={batch.Skipped.Count} " +
                        $"unmapped={batch.UnmappedLookups.Count} blocking={blocking} " +
                        $"missingTarget={missingTarget} [{status}]");
                }
                catch (Exception ex)
                {
                    report.BlockingGaps.Add(new GapRow
                    {
                        Phase = "entity_transform",
                        Entity = entity,
                        Catalog = "",
                        LegacyValue = "",
                        Reason = $"transform_failed:{ex.Message}",
                        Policy = "block_row",
                    });
                    report.EntityResults.Add(new EntityAuditRow
                    {
                        Entity = entity,
                        Status = "transform_failed",
                        Notes = ex.Message,
                    });
                    Console.Error.WriteLine($"ERR {entity}: transform failed — {ex.Message}");
                    if (verbose)
                        Console.Error.WriteLine(ex);
                }
            }
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine("INF Phase B skipped (--catalog-only)");
        }

        // Deduplicate blocking gaps for report readability
        report.BlockingGaps = DeduplicateGaps(report.BlockingGaps);
        report.AllowedGaps = DeduplicateGaps(report.AllowedGaps);
        report.BlockingGapCount = report.BlockingGaps.Count;
        report.AllowedGapCount = report.AllowedGaps.Count;
        report.Passed = report.BlockingGapCount == 0;

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(output, json, Encoding.UTF8);

        Console.WriteLine();
        Console.WriteLine($"INF Report: {Path.GetFullPath(output)}");
        if (report.Passed)
        {
            Console.WriteLine("=== Lookup preflight PASSED (0 blocking gaps) ===");
            return 0;
        }

        Console.Error.WriteLine($"=== Lookup preflight FAILED ({report.BlockingGapCount} blocking gap(s)) ===");
        Console.Error.WriteLine("Fix: add seed row + lookup-translations map, or approved exclusion; then re-run preflight.");
        foreach (var gap in report.BlockingGaps.Take(40))
        {
            Console.Error.WriteLine(
                $"  - [{gap.Phase}] {gap.Catalog} legacy='{gap.LegacyValue}' " +
                $"target='{gap.TargetValue}' reason={gap.Reason} rows={gap.RowCount} entity={gap.Entity}");
        }

        if (report.BlockingGaps.Count > 40)
            Console.Error.WriteLine($"  … and {report.BlockingGaps.Count - 40} more (see report JSON)");

        return 2;
    }

    private static Visa2014PersonImportBatch? PrepareEntityBatch(
        string entity,
        string connectionString,
        IReadOnlyList<string> lookupPaths,
        int? maxRows,
        bool verbose) =>
        entity.Trim().ToLowerInvariant() switch
        {
            "person" => Visa2014PersonTransform.PrepareImportBatch(connectionString, lookupPaths, maxRows, verbose),
            "passport" => Visa2014PassportTransform.PrepareImportBatch(connectionString, lookupPaths, maxRows, verbose),
            "visa" => Visa2014VisaTransform.PrepareImportBatch(connectionString, lookupPaths, maxRows, verbose),
            "education" => Visa2014EducationTransform.PrepareImportBatch(connectionString, lookupPaths, maxRows, verbose),
            "employeepositionhistory" => Visa2014EmployeePositionHistoryTransform.PrepareImportBatch(connectionString, lookupPaths, maxRows, verbose),
            "addressofresidence" => Visa2014AddressOfResidenceTransform.PrepareImportBatch(connectionString, lookupPaths, maxRows, verbose),
            "lodging" => Visa2014LodgingTransform.PrepareImportBatch(connectionString, lookupPaths, maxRows, verbose),
            "hotel" => Visa2014HotelTransform.PrepareImportBatch(connectionString, lookupPaths, maxRows, verbose),
            "hospital" => Visa2014HospitalTransform.PrepareImportBatch(connectionString, lookupPaths, maxRows, verbose),
            "othersite" => Visa2014OtherSiteTransform.PrepareImportBatch(connectionString, lookupPaths, maxRows, verbose),
            "application" => Visa2014ApplicationTransform.PrepareImportBatch(connectionString, lookupPaths, maxRows, verbose),
            "applicationitem" => Visa2014ApplicationItemTransform.PrepareImportBatch(connectionString, lookupPaths, maxRows, verbose),
            "workpermititem" => Visa2014WorkPermitItemTransform.PrepareImportBatch(connectionString, lookupPaths, maxRows, verbose),
            _ => null,
        };

    private static int CountMissingTargetsOnImportRows(
        IReadOnlyList<Dictionary<string, object?>> importRows,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        Dictionary<string, HashSet<string>> targetKeysByCatalog,
        string entity,
        List<GapRow> blockingGaps)
    {
        // Columns that hold translated lookup display keys (not OData @odata.bind).
        var lookupColumns = new (string Column, string Catalog)[]
        {
            ("CountryOfBirth", "Country"),
            ("Citizenship", "Country"),
            ("IssuedCountry", "Country"),
            ("Gender", "Gender"),
            ("MaritalStatus", "MaritalStatus"),
            ("Relationship", "Relationship"),
            ("PassportType", "PassportType"),
            ("VisaType", "VisaType"),
            ("VisaCategory", "VisaCategory"),
            ("VisaIssuedPlace", "VisaIssuedPlace"),
            ("EducationLevel", "EducationLevel"),
            ("EducationInstitution", "EducationInstitution"),
            ("Specialty", "Specialty"),
            ("Position", "Position"),
            ("Department", "Department"),
            ("Subcontractor", "Subcontractor"),
            ("Region", "Region"),
            ("City", "City"),
            ("ApplicationType", "ApplicationType"),
            ("Urgency", "Urgency"),
            ("VisaPeriod", "VisaPeriod"),
            ("PurposeOfTravel", "PurposeOfTravel"),
            ("CheckPoint", "CheckPoint"),
            ("MigrationService", "MigrationService"),
            ("ProjectContract", "ProjectContract"),
        };

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var missing = 0;
        foreach (var row in importRows)
        {
            foreach (var (column, catalogName) in lookupColumns)
            {
                if (!row.TryGetValue(column, out var raw) || raw is null)
                    continue;
                var value = Convert.ToString(raw)?.Trim();
                if (string.IsNullOrWhiteSpace(value))
                    continue;
                if (!catalogs.TryGetValue(catalogName, out var cat))
                    continue;
                if (!IsBlockingPolicy(cat.UnmappedPolicy))
                    continue;
                if (TargetContains(targetKeysByCatalog, catalogName, value))
                    continue;

                var key = $"{catalogName}|{value}";
                if (!seen.Add(key))
                    continue;

                missing++;
                blockingGaps.Add(new GapRow
                {
                    Phase = "target_missing",
                    Entity = entity,
                    Catalog = catalogName,
                    LegacyValue = value,
                    TargetValue = value,
                    Reason = $"missing_target_on_import_row:{column}",
                    Policy = cat.UnmappedPolicy,
                });
            }
        }

        return missing;
    }

    private static bool TargetContains(
        Dictionary<string, HashSet<string>> targetKeysByCatalog,
        string catalogName,
        string targetValue)
    {
        if (!targetKeysByCatalog.TryGetValue(catalogName, out var keys))
            return true; // unknown catalog map — do not block

        foreach (var key in keys)
        {
            if (Visa2014CatalogMatchHelper.KeysEqual(key, targetValue))
                return true;
        }

        return false;
    }

    private static Dictionary<string, HashSet<string>> LoadTargetCatalogKeys(
        string targetConnection,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        bool verbose)
    {
        var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var tableMap = BuildTargetTableMap();

        using var conn = new SqlConnection(targetConnection);
        conn.Open();

        foreach (var (catalogName, catalog) in catalogs)
        {
            if (!tableMap.TryGetValue(catalogName, out var tableInfo))
            {
                if (verbose)
                    Console.WriteLine($"INF Target skip (no table map): {catalogName}");
                continue;
            }

            var matchProp = string.IsNullOrWhiteSpace(catalog.TargetMatchProperty)
                ? tableInfo.DefaultMatchProperty
                : catalog.TargetMatchProperty;

            // Prefer configured match property; fall back to known alternates.
            var candidates = new[] { matchProp, tableInfo.DefaultMatchProperty, "Name", "NameTm", "Code", "LocalizationKey", "PdfForm_Code", "FullAddress" }
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            Exception? lastError = null;
            foreach (var prop in candidates)
            {
                try
                {
                    var keys = new HashSet<string>(StringComparer.Ordinal);
                    var sql = $"SELECT DISTINCT [{prop}] FROM [{tableInfo.Table}] WHERE GCRecord IS NULL AND [{prop}] IS NOT NULL";
                    using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 120 };
                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        if (reader.IsDBNull(0))
                            continue;
                        var v = Convert.ToString(reader.GetValue(0))?.Trim();
                        if (!string.IsNullOrWhiteSpace(v))
                            keys.Add(v);
                    }

                    if (keys.Count == 0)
                    {
                        // Column exists but empty — try next match property (e.g. PdfForm_Code null, NameTm populated).
                        lastError = new InvalidOperationException($"{tableInfo.Table}.{prop} returned 0 keys");
                        continue;
                    }

                    result[catalogName] = keys;
                    if (verbose)
                        Console.WriteLine($"INF Target {catalogName}: {keys.Count} keys via {tableInfo.Table}.{prop}");
                    lastError = null;
                    break;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                }
            }

            if (lastError != null && verbose)
                Console.WriteLine($"WRN Target {catalogName}: could not load keys — {lastError.Message}");
        }

        // CityByName shares Cities
        if (result.TryGetValue("City", out var cityKeys) && !result.ContainsKey("CityByName"))
            result["CityByName"] = cityKeys;

        return result;
    }

    private static Dictionary<string, (string Table, string DefaultMatchProperty)> BuildTargetTableMap() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Gender"] = ("Genders", "Code"),
            ["Country"] = ("Countries", "Code"),
            ["MaritalStatus"] = ("MaritalStatuses", "Code"),
            ["Relationship"] = ("Relationships", "NameTm"),
            ["PassportType"] = ("PassportTypes", "Name"),
            ["EducationLevel"] = ("EducationLevels", "Name"),
            ["VisaType"] = ("VisaTypes", "Name"),
            ["VisaCategory"] = ("VisaCategories", "Name"),
            ["VisaIssuedPlace"] = ("VisaIssuedPlaces", "Name"),
            ["BorderZoneName"] = ("BorderZoneNames", "Name"),
            ["Region"] = ("Regions", "LocalizationKey"),
            ["City"] = ("Cities", "NameTm"),
            ["CityByName"] = ("Cities", "NameTm"),
            ["Urgency"] = ("Urgencies", "Name"),
            ["VisaPeriod"] = ("VisaPeriods", "Name"),
            ["ApplicationType"] = ("ApplicationTypes", "Name"),
            ["PurposeOfTravel"] = ("PurposeOfTravels", "Name"),
            ["CheckPoint"] = ("CheckPoints", "Name"),
            ["WorkPermittedLocationName"] = ("WorkPermittedLocationNames", "Name"),
            ["MigrationService"] = ("MigrationServices", "Name"),
            ["ProjectContract"] = ("ProjectContracts", "Code"),
            ["EducationInstitution"] = ("EducationInstitutions", "NameTm"),
            ["Specialty"] = ("Specialties", "NameTm"),
            ["Position"] = ("Positions", "Name"),
            ["Department"] = ("Departments", "Name"),
            ["Subcontractor"] = ("Subcontractors", "Name"),
            ["Lodging"] = ("Lodgings", "FullAddress"),
            ["Hotel"] = ("Hotels", "Name"),
            ["Hospital"] = ("Hospitals", "Name"),
            ["OtherSite"] = ("OtherSites", "FullAddress"),
        };

    private static List<(string Legacy, int Count)> ExecuteDistinctSampleQuery(
        SqlConnection connection,
        string sampleQuery,
        string? preferredColumn)
    {
        var sql = StripSqlComments(sampleQuery).Trim().TrimEnd(';');
        using var cmd = new SqlCommand(sql, connection) { CommandTimeout = 180 };
        using var reader = cmd.ExecuteReader();

        var legacyOrdinal = ResolveLegacyOrdinal(reader, preferredColumn);
        var countOrdinal = ResolveCountOrdinal(reader);

        var rows = new List<(string Legacy, int Count)>();
        while (reader.Read())
        {
            if (reader.IsDBNull(legacyOrdinal))
                continue;
            var legacy = Convert.ToString(reader.GetValue(legacyOrdinal))?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(legacy))
                continue;
            var count = 0;
            if (countOrdinal >= 0 && !reader.IsDBNull(countOrdinal))
                count = Convert.ToInt32(reader.GetValue(countOrdinal));
            rows.Add((legacy, count));
        }

        return rows;
    }

    private static int ResolveLegacyOrdinal(IDataRecord reader, string? preferredColumn)
    {
        if (!string.IsNullOrWhiteSpace(preferredColumn))
        {
            for (var i = 0; i < reader.FieldCount; i++)
            {
                if (string.Equals(reader.GetName(i), preferredColumn, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
        }

        for (var i = 0; i < reader.FieldCount; i++)
        {
            var name = reader.GetName(i);
            if (name.Contains("legacy", StringComparison.OrdinalIgnoreCase)
                || name.Equals("legacy_code", StringComparison.OrdinalIgnoreCase)
                || name.Equals("mgCode", StringComparison.OrdinalIgnoreCase))
                return i;
        }

        for (var i = 0; i < reader.FieldCount; i++)
        {
            var name = reader.GetName(i);
            if (name.Equals("cnt", StringComparison.OrdinalIgnoreCase)
                || name.Equals("count", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith("Count", StringComparison.OrdinalIgnoreCase))
                continue;
            return i;
        }

        return 0;
    }

    private static int ResolveCountOrdinal(IDataRecord reader)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            var name = reader.GetName(i);
            if (name.Equals("cnt", StringComparison.OrdinalIgnoreCase)
                || name.Equals("count", StringComparison.OrdinalIgnoreCase)
                || name.Equals("row_count", StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    private static string StripSqlComments(string sql)
    {
        var lines = sql.Replace("\r\n", "\n").Split('\n');
        var sb = new StringBuilder();
        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("--", StringComparison.Ordinal))
                continue;
            sb.AppendLine(line);
        }

        return sb.ToString();
    }

    private static List<YamlCatalogNode> LoadYamlCatalogNodes(IReadOnlyList<string> yamlPaths)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var byName = new Dictionary<string, YamlCatalogNode>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in yamlPaths)
        {
            if (!File.Exists(path))
                continue;
            var root = deserializer.Deserialize<YamlRoot>(File.ReadAllText(path));
            foreach (var catalog in root.Catalogs ?? [])
            {
                if (string.IsNullOrWhiteSpace(catalog.TargetCatalog))
                    continue;

                if (byName.TryGetValue(catalog.TargetCatalog, out var existing))
                {
                    // Overlay: keep sampleQuery from first file that had one; allow later override if present.
                    var overlayQuery = catalog.SampleQuery ?? catalog.Legacy?.SampleQuery;
                    if (!string.IsNullOrWhiteSpace(overlayQuery))
                        existing.SampleQuery = overlayQuery;
                    if (!string.IsNullOrWhiteSpace(catalog.UnmappedPolicy))
                        existing.UnmappedPolicy = catalog.UnmappedPolicy;
                    if (!string.IsNullOrWhiteSpace(catalog.TargetMatchProperty))
                        existing.TargetMatchProperty = catalog.TargetMatchProperty;
                    var overlayColumn = catalog.LegacyColumn ?? catalog.Legacy?.Column;
                    if (!string.IsNullOrWhiteSpace(overlayColumn))
                        existing.LegacyColumn = overlayColumn;
                }
                else
                {
                    byName[catalog.TargetCatalog] = new YamlCatalogNode
                    {
                        TargetCatalog = catalog.TargetCatalog,
                        TargetMatchProperty = catalog.TargetMatchProperty,
                        UnmappedPolicy = catalog.UnmappedPolicy,
                        SampleQuery = catalog.SampleQuery ?? catalog.Legacy?.SampleQuery,
                        LegacyColumn = catalog.LegacyColumn ?? catalog.Legacy?.Column,
                    };
                }
            }
        }

        return byName.Values.OrderBy(c => c.TargetCatalog, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static List<GapRow> DeduplicateGaps(List<GapRow> gaps)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<GapRow>();
        foreach (var gap in gaps.OrderBy(g => g.Catalog).ThenBy(g => g.LegacyValue))
        {
            var key = $"{gap.Phase}|{gap.Catalog}|{gap.LegacyValue}|{gap.TargetValue}|{gap.Reason}|{gap.Entity}";
            if (!seen.Add(key))
                continue;
            result.Add(gap);
        }

        return result;
    }

    private static bool IsBlockingPolicy(string? policy) =>
        // allow_null / skip_row / use_default are intentional non-block policies in YAML.
        // Unknown/null defaults to block_row (same as Visa2014LookupTranslator.Load).
        string.IsNullOrWhiteSpace(policy) || policy == "block_row";

    private static string? ParseCatalogFromReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return null;
        // unmapped_lookup:Catalog:value
        var parts = reason.Split(':', 3, StringSplitOptions.TrimEntries);
        if (parts.Length >= 2 && parts[0].Equals("unmapped_lookup", StringComparison.OrdinalIgnoreCase))
            return parts[1];
        return null;
    }

    private static bool HasArg(IReadOnlyList<string> args, string flag) =>
        args.Any(a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));

    private static string? GetOptionValue(IReadOnlyList<string> args, string optionName)
    {
        for (var i = 0; i < args.Count; i++)
        {
            if (!string.Equals(args[i], optionName, StringComparison.OrdinalIgnoreCase))
                continue;
            if (i + 1 < args.Count && !args[i + 1].StartsWith('-'))
                return args[i + 1];
            return null;
        }

        return null;
    }

    private static string MaskConnectionForLog(string connectionString)
    {
        string? server = null;
        string? database = null;
        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            if (part.StartsWith("Server=", StringComparison.OrdinalIgnoreCase)
                || part.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
                server = part[(part.IndexOf('=') + 1)..].Trim();
            if (part.StartsWith("Database=", StringComparison.OrdinalIgnoreCase)
                || part.StartsWith("Initial Catalog=", StringComparison.OrdinalIgnoreCase))
                database = part[(part.IndexOf('=') + 1)..].Trim();
        }

        return $"Server={server ?? "?"}; Database={database ?? "?"}";
    }

    private sealed class YamlRoot
    {
        public List<YamlCatalogRaw>? Catalogs { get; set; }
    }

    private sealed class YamlCatalogRaw
    {
        public string? TargetCatalog { get; set; }
        public string? TargetMatchProperty { get; set; }
        public string? UnmappedPolicy { get; set; }
        public string? SampleQuery { get; set; }
        public string? LegacyColumn { get; set; }
        public YamlLegacyRaw? Legacy { get; set; }
    }

    private sealed class YamlLegacyRaw
    {
        public string? Column { get; set; }
        public string? SampleQuery { get; set; }
    }

    private sealed class YamlCatalogNode
    {
        public string TargetCatalog { get; set; } = "";
        public string? TargetMatchProperty { get; set; }
        public string? UnmappedPolicy { get; set; }
        public string? SampleQuery { get; set; }
        public string? LegacyColumn { get; set; }
    }

    private sealed class PreflightReport
    {
        public string LegacySource { get; set; } = "";
        public string GeneratedAtUtc { get; set; } = "";
        public bool Passed { get; set; }
        public int BlockingGapCount { get; set; }
        public int AllowedGapCount { get; set; }
        public int TargetCatalogsLoaded { get; set; }
        public List<CatalogAuditRow> CatalogResults { get; set; } = [];
        public List<EntityAuditRow> EntityResults { get; set; } = [];
        public List<GapRow> BlockingGaps { get; set; } = [];
        public List<GapRow> AllowedGaps { get; set; } = [];
    }

    private sealed class CatalogAuditRow
    {
        public string Catalog { get; set; } = "";
        public string Status { get; set; } = "";
        public string? UnmappedPolicy { get; set; }
        public int DistinctLegacy { get; set; }
        public int Mapped { get; set; }
        public int AllowedUnmapped { get; set; }
        public int BlockingUnmapped { get; set; }
        public int MissingTarget { get; set; }
        public string? Notes { get; set; }
    }

    private sealed class EntityAuditRow
    {
        public string Entity { get; set; } = "";
        public string Status { get; set; } = "";
        public int LegacyRowCount { get; set; }
        public int ImportRowCount { get; set; }
        public int SkippedRowCount { get; set; }
        public int UnmappedLookupCount { get; set; }
        public int BlockingUnmapped { get; set; }
        public int AllowedUnmapped { get; set; }
        public int MissingTarget { get; set; }
        public string? Notes { get; set; }
    }

    private sealed class GapRow
    {
        public string Phase { get; set; } = "";
        public string? Entity { get; set; }
        public string Catalog { get; set; } = "";
        public string LegacyValue { get; set; } = "";
        public string? TargetValue { get; set; }
        public string Reason { get; set; } = "";
        public string Policy { get; set; } = "";
        public int RowCount { get; set; }
    }
}
