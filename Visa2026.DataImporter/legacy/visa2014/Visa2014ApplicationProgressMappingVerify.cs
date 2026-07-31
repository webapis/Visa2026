using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Core;
using Visa2026.Blazor.Server.Services.Migration;
using Bo = Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.MigrationImport;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Post-import mapping verify for ApplicationProgress (MAPPING_VERIFICATION.md).
/// Expected values from <see cref="Visa2014ApplicationProgressTransform.PrepareImportBatch"/>;
/// id-map keys are synthetic <c>{legacyApplicationOid}:{stepCode}</c>.
/// </summary>
internal static class Visa2014ApplicationProgressMappingVerify
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static Task<int> RunAsync(IReadOnlyList<string> args, bool verbose)
    {
        var dataImporterRoot = Visa2014ContentRoot.FindDataImporterRoot();
        if (dataImporterRoot == null)
        {
            Console.Error.WriteLine("ERR Could not locate Visa2026.DataImporter content root.");
            return Task.FromResult(1);
        }

        var solutionRoot = Visa2014ContentRoot.FindSolutionRoot();
        Visa2014LegacySourceProfile source;
        try { source = Visa2014LegacySource.Resolve(dataImporterRoot, solutionRoot, args); }
        catch (Exception ex) { Console.Error.WriteLine($"ERR {ex.Message}"); return Task.FromResult(1); }

        var targetConnection = GetOptionValue(args, "--target-connection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? Environment.GetEnvironmentVariable("VISA2026_SQL_CONNECTION");
        if (string.IsNullOrWhiteSpace(targetConnection))
        {
            Console.Error.WriteLine("ERR --target-connection (or ConnectionStrings__DefaultConnection) is required.");
            return Task.FromResult(1);
        }

        var progressIdMapPath = GetOptionValue(args, "--progress-id-map")
            ?? GetOptionValue(args, "--application-progress-id-map")
            ?? source.IdMapPath(dataImporterRoot, "ApplicationProgress");
        var applicationIdMapPath = GetOptionValue(args, "--application-id-map")
            ?? source.IdMapPath(dataImporterRoot, "Application");

        var tierText = (GetOptionValue(args, "--tier") ?? "B").Trim().ToUpperInvariant();
        if (tierText is not ("A" or "B" or "C"))
        {
            Console.Error.WriteLine("ERR --tier must be A, B, or C.");
            return Task.FromResult(1);
        }

        var full = HasArg(args, "--full") || tierText == "C";
        var runTierA = true;
        var runParity = tierText is "B" or "C" || full;
        if (full)
            tierText = "C";

        var sample = 50;
        var sampleText = GetOptionValue(args, "--sample");
        if (int.TryParse(sampleText, out var parsedSample) && parsedSample > 0)
            sample = parsedSample;

        int? maxRows = null;
        var maxRowsText = GetOptionValue(args, "--max-rows");
        if (int.TryParse(maxRowsText, out var parsedMax) && parsedMax > 0)
            maxRows = parsedMax;

        var reportPath = GetOptionValue(args, "--report")
            ?? Path.Combine(
                Visa2014ContentRoot.LegacyRoot(dataImporterRoot),
                "import-logs",
                $"mapping-verify-ApplicationProgress-{source.Id}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");

        Console.WriteLine("=== VISA2014 mapping verify (ApplicationProgress) ===");
        Console.WriteLine($"INF Legacy source: {source.Id}");
        Console.WriteLine($"INF Target SQL: {MaskConnectionString(targetConnection)}");
        Console.WriteLine($"INF Progress id-map: {progressIdMapPath}");
        Console.WriteLine($"INF Application id-map: {applicationIdMapPath}");
        Console.WriteLine($"INF Tier: {tierText} (histograms={(runTierA ? "yes" : "no")}, parity={(runParity ? (full ? "full" : $"sample {sample}") : "no")})");
        if (maxRows.HasValue)
            Console.WriteLine($"INF Max legacy Application rows for synthesis: {maxRows.Value}");

        if (!File.Exists(progressIdMapPath))
        {
            Console.Error.WriteLine($"ERR ApplicationProgress id-map not found: {progressIdMapPath}");
            return Task.FromResult(1);
        }

        if (!File.Exists(applicationIdMapPath))
        {
            Console.Error.WriteLine($"ERR Application id-map not found: {applicationIdMapPath}");
            return Task.FromResult(1);
        }

        try
        {
            Visa2014LegacySqlGuard.EnsureLegacyReadCredentials(source.ConnectionString);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERR {ex.Message}");
            return Task.FromResult(1);
        }

        var progressIdMap = Visa2014IdMapHelper.LoadStringKeyMap(progressIdMapPath);
        var applicationIdMap = Visa2014IdMapHelper.Load(applicationIdMapPath);

        HeadlessMigrationHost? host = null;
        IDisposable? importScope = null;
        ProgressVerifyReport report;
        try
        {
            host = HeadlessMigrationHost.Start(targetConnection);
            importScope = MigrationImportContext.BeginDataImportScope();

            // Same ministry-leg resolution as import (ApprovalLegProfile snapshots on target Applications).
            var targetLegCounts = Visa2014ApplicationMinistryLegCountResolver.LoadFromObjectSpace(host.ObjectSpaceFactory);
            var ministryLegCountByLegacyApplicationOid =
                Visa2014ApplicationMinistryLegCountResolver.MapLegacyLegCounts(applicationIdMap, targetLegCounts);
            Console.WriteLine(
                $"INF Ministry-leg counts resolved for {ministryLegCountByLegacyApplicationOid.Count} legacy application(s).");

            Console.WriteLine("INF Building expected payloads via ApplicationProgress transform...");
            var batch = Visa2014ApplicationProgressTransform.PrepareImportBatch(
                source.ConnectionString,
                source.LookupTranslationPaths,
                maxRows: maxRows,
                verbose: verbose,
                ministryLegCountByLegacyApplicationOid: ministryLegCountByLegacyApplicationOid);

            var fields = ProgressVerifyFields.All;
            var candidates = BuildCandidates(batch.ImportRows, batch.Skipped.Count, progressIdMap, applicationIdMap, fields);

            Console.WriteLine($"INF Importable transform rows: {candidates.ImportableCount}");
            Console.WriteLine($"INF Progress id-map entries: {progressIdMap.Count}");
            Console.WriteLine($"INF Mapped candidates: {candidates.Mapped.Count}");
            Console.WriteLine($"INF Missing id-map: {candidates.MissingIdMap}");
            Console.WriteLine($"INF Transform skips (parent/other): {candidates.SkippedTransform}");
            Console.WriteLine($"INF Skipped (no Application id-map): {candidates.SkippedNoApplicationMap}");

            report = RunVerify(
                host.ObjectSpaceFactory,
                candidates,
                fields,
                runTierA,
                runParity,
                full,
                sample,
                source.Id,
                tierText,
                verbose);
        }
        finally
        {
            importScope?.Dispose();
            host?.Dispose();
        }

        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        File.WriteAllText(reportPath, JsonSerializer.Serialize(report, JsonOptions), Encoding.UTF8);
        Console.WriteLine($"INF Report JSON: {reportPath}");

        var htmlPath = GetOptionValue(args, "--report-html")
            ?? Path.ChangeExtension(reportPath, ".html");
        File.WriteAllText(htmlPath, BuildHtmlReport(report), Encoding.UTF8);
        Console.WriteLine($"INF Report HTML: {htmlPath}");

        Console.WriteLine($"INF Histograms ok: {report.Histograms.Count(h => h.Ok)}/{report.Histograms.Count}");
        Console.WriteLine($"INF Parity sampled: {report.Sampled}; mismatches: {report.Mismatches.Count}; missingIdMap: {report.MissingIdMap}; missingTarget: {report.MissingTarget}");
        Console.WriteLine($"INF Exit: {report.ExitCode}");

        foreach (var mismatch in report.Mismatches.Take(25))
        {
            Console.Error.WriteLine(
                $"ERR {mismatch.Field}: legacy={mismatch.LegacyKey} target={mismatch.TargetId} " +
                $"expected={mismatch.Expected ?? "(null)"} actual={mismatch.Actual ?? "(null)"}");
        }

        foreach (var hist in report.Histograms.Where(h => !h.Ok))
        {
            Console.Error.WriteLine($"ERR Histogram {hist.Field} delta keys: {string.Join(", ", hist.Delta.Keys.Take(20))}");
        }

        return Task.FromResult(report.ExitCode);
    }

    private static CandidateSet BuildCandidates(
        IReadOnlyList<Dictionary<string, object?>> importRows,
        int skippedParentRows,
        IReadOnlyDictionary<string, Guid> progressIdMap,
        IReadOnlyDictionary<Guid, Guid> applicationIdMap,
        IReadOnlyList<VerifyFieldDef> fields)
    {
        var mapped = new List<MappedCandidate>();
        var missingIdMap = 0;
        var skippedNoApplicationMap = 0;
        var importable = 0;
        var skippedTransform = skippedParentRows;

        foreach (var row in importRows)
        {
            if (row.GetValueOrDefault("_importAction") as string == "skip")
            {
                skippedTransform++;
                continue;
            }

            importable++;
            var syntheticKey = row.GetValueOrDefault("_syntheticStepKey") as string
                ?? row.GetValueOrDefault("_legacyRowId") as string;
            if (string.IsNullOrWhiteSpace(syntheticKey))
            {
                missingIdMap++;
                continue;
            }

            var legacyAppText = row.GetValueOrDefault("Application") as string
                ?? row.GetValueOrDefault("_legacyApplicationOid") as string;
            var hasApplicationMap = Guid.TryParse(legacyAppText, out var legacyAppOid)
                && applicationIdMap.ContainsKey(legacyAppOid);

            if (!progressIdMap.TryGetValue(syntheticKey, out var targetId))
            {
                // Mirror import: no Application id-map → skip (not a mapping failure).
                if (!hasApplicationMap)
                    skippedNoApplicationMap++;
                else
                    missingIdMap++;
                continue;
            }

            var expected = new Dictionary<string, string?>(StringComparer.Ordinal);
            foreach (var field in fields)
            {
                if (field.Name == "Application")
                {
                    expected["Application"] = hasApplicationMap
                        ? applicationIdMap[legacyAppOid].ToString("D")
                        : null;
                }
                else if (field.Name == "Order")
                {
                    expected["Order"] = NormalizeExpected(row.GetValueOrDefault("Order"));
                }
                else
                {
                    expected[field.Name] = NormalizeExpected(row.GetValueOrDefault(field.Name));
                }
            }

            var lineage = new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["State"] = row.GetValueOrDefault("_lineage_State") as string,
                ["Date"] = row.GetValueOrDefault("_lineage_Date") as string,
                ["Description"] = row.GetValueOrDefault("_lineage_Description") as string,
                ["Order"] = row.GetValueOrDefault("_lineage_Order") as string,
                ["Application"] = row.GetValueOrDefault("_lineage_Application") as string,
            };
            var stepCode = row.GetValueOrDefault("_stepCode") as string;
            mapped.Add(new MappedCandidate(syntheticKey, targetId, expected, lineage, stepCode));
        }

        return new CandidateSet(importable, missingIdMap, skippedTransform, skippedNoApplicationMap, mapped);
    }

    private static ProgressVerifyReport RunVerify(
        INonSecuredObjectSpaceFactory objectSpaceFactory,
        CandidateSet candidates,
        IReadOnlyList<VerifyFieldDef> fields,
        bool runTierA,
        bool runParity,
        bool full,
        int sample,
        string legacySourceId,
        string tierText,
        bool verbose)
    {
        using var objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Bo.ApplicationProgress));
        MigrationImportContext.ApplyImportObjectSpaceHooks(objectSpace);

        var rowsById = LoadProgressRows(objectSpace, candidates.Mapped.Select(c => c.TargetId));
        var mismatches = new List<MappingMismatch>();
        var missingTarget = 0;
        var histograms = new List<HistogramResult>();

        if (runTierA)
        {
            foreach (var field in fields.Where(f => f.IncludeInHistogram))
            {
                var expectedHist = new Dictionary<string, int>(StringComparer.Ordinal);
                var actualHist = new Dictionary<string, int>(StringComparer.Ordinal);

                foreach (var c in candidates.Mapped)
                {
                    var exp = c.Expected.GetValueOrDefault(field.Name);
                    if (string.IsNullOrEmpty(exp))
                    {
                        if (field.OptionalWhenNull)
                            continue;
                    }
                    else
                    {
                        Increment(expectedHist, HistogramKey(field, exp));
                    }

                    if (!rowsById.TryGetValue(c.TargetId, out var progress))
                        continue;

                    var act = ReadActual(progress, field);
                    if (!string.IsNullOrEmpty(act))
                        Increment(actualHist, HistogramKey(field, act!));
                    else if (!string.IsNullOrEmpty(exp))
                        Increment(actualHist, "(null)");
                }

                var delta = BuildDelta(expectedHist, actualHist);
                var ok = delta.Count == 0;
                histograms.Add(new HistogramResult
                {
                    Field = field.Name,
                    Ok = ok,
                    Expected = expectedHist.OrderByDescending(kv => kv.Value).ToDictionary(kv => kv.Key, kv => kv.Value),
                    Actual = actualHist.OrderByDescending(kv => kv.Value).ToDictionary(kv => kv.Key, kv => kv.Value),
                    Delta = delta,
                });

                if (verbose)
                    Console.WriteLine($"INF Histogram {field.Name}: {(ok ? "OK" : "FAIL")}");
            }
        }

        var sampled = 0;
        if (runParity)
        {
            IReadOnlyList<MappedCandidate> paritySet = full
                ? candidates.Mapped
                : StratifiedSample(candidates.Mapped, sample);

            sampled = paritySet.Count;
            foreach (var c in paritySet)
            {
                if (!rowsById.TryGetValue(c.TargetId, out var progress))
                {
                    missingTarget++;
                    mismatches.Add(new MappingMismatch
                    {
                        LegacyKey = c.LegacyKey,
                        TargetId = c.TargetId.ToString("D"),
                        Field = "(row)",
                        Expected = "(mapped)",
                        Actual = "(missing target ApplicationProgress)",
                        Severity = "error",
                    });
                    continue;
                }

                foreach (var field in fields)
                {
                    var expected = c.Expected.GetValueOrDefault(field.Name);
                    var actual = ReadActual(progress, field);

                    if (ValuesMatch(field, expected, actual))
                        continue;

                    if (string.IsNullOrEmpty(expected) && field.OptionalWhenNull)
                        continue;

                    mismatches.Add(new MappingMismatch
                    {
                        LegacyKey = c.LegacyKey,
                        TargetId = c.TargetId.ToString("D"),
                        Field = field.Name,
                        Expected = expected,
                        Actual = actual,
                        Severity = field.Severity,
                    });
                }
            }
        }

        var errorMismatches = mismatches.Count(m => string.Equals(m.Severity, "error", StringComparison.OrdinalIgnoreCase));
        var histFail = histograms.Count(h => !h.Ok);
        var exitCode = (errorMismatches > 0 || histFail > 0 || candidates.MissingIdMap > 0) ? 1 : 0;

        var lineageSource = full
            ? (IReadOnlyList<MappedCandidate>)candidates.Mapped
            : StratifiedSample(candidates.Mapped, Math.Max(sample, 1));
        var sampleCap = Math.Min(Math.Max(sample, 20), lineageSource.Count);
        var sampleLineage = lineageSource
            .Take(sampleCap)
            .Select(c => new SampleLineageRow
            {
                LegacyKey = c.LegacyKey,
                StepCode = c.StepCode,
                State = c.Expected.GetValueOrDefault("State"),
                Date = c.Expected.GetValueOrDefault("Date"),
                Description = c.Expected.GetValueOrDefault("Description"),
                LineageState = c.Lineage.GetValueOrDefault("State"),
                LineageDate = c.Lineage.GetValueOrDefault("Date"),
                LineageDescription = c.Lineage.GetValueOrDefault("Description"),
                LineageOrder = c.Lineage.GetValueOrDefault("Order"),
                LineageApplication = c.Lineage.GetValueOrDefault("Application"),
            })
            .ToList();

        return new ProgressVerifyReport
        {
            Entity = "ApplicationProgress",
            LegacySource = legacySourceId,
            Tier = tierText,
            Sampled = sampled,
            IdMapCount = candidates.Mapped.Count + candidates.MissingIdMap,
            ImportableCount = candidates.ImportableCount,
            MappedCount = candidates.Mapped.Count,
            MissingIdMap = candidates.MissingIdMap,
            MissingTarget = missingTarget,
            SkippedTransform = candidates.SkippedTransform,
            SkippedNoApplicationMap = candidates.SkippedNoApplicationMap,
            PropertyLineage = BuildStaticPropertyLineage(),
            SampleLineage = sampleLineage,
            Histograms = histograms,
            Mismatches = mismatches,
            ExitCode = exitCode,
        };
    }

    private static List<PropertyLineageEntry> BuildStaticPropertyLineage() =>
    [
        new()
        {
            Target = "Application",
            LegacySource = "dbo.Application.Oid",
            How = "FK via Application id-map",
        },
        new()
        {
            Target = "State",
            LegacySource = "(none - synthesized)",
            How = "Step template -> ApplicationState.Code (e.g. 1_REVIEW_STARTED, PROCESS_ISSUED)",
        },
        new()
        {
            Target = "Date",
            LegacySource = "per step: DateForwardedToMonistery, MinisteriesDocumentDate, DateForwardedToMinConstruction, ProcessDate, completion IssuedDate, ...",
            How = "SynthesizeSteps date ladder (see Sample row lineage Date <- column)",
        },
        new()
        {
            Target = "Description",
            LegacySource = "MinisteriesDocumentNumber, DocNumberForwardedToMinConstruction, ProcessNumber, Invitation/WP number, Cancelled/Rejected flags",
            How = "Formatted per step; null when leg has no legacy doc",
        },
        new()
        {
            Target = "Order",
            LegacySource = "(none)",
            How = "1-based index after workflow sort; may change after order correction (verify warn)",
        },
        new()
        {
            Target = "MinistryLetterFile",
            LegacySource = "(not imported)",
            How = "Deferred to file/image wave",
        },
        new()
        {
            Target = "Location",
            LegacySource = "(removed from BO / not posted)",
            How = "Field-map historical; current importer posts State/Date/Order/Description only",
        },
    ];

    private static Dictionary<Guid, Bo.ApplicationProgress> LoadProgressRows(
        IObjectSpace objectSpace,
        IEnumerable<Guid> targetIds)
    {
        var ids = targetIds.Distinct().ToList();
        var result = new Dictionary<Guid, Bo.ApplicationProgress>();
        const int chunkSize = 400;
        for (var i = 0; i < ids.Count; i += chunkSize)
        {
            var chunk = ids.Skip(i).Take(chunkSize).ToList();
            var chunkSet = chunk.ToHashSet();
            var rows = objectSpace.GetObjectsQuery<Bo.ApplicationProgress>()
                .Where(p => chunkSet.Contains(p.ID))
                .ToList();
            foreach (var row in rows)
                result[row.ID] = row;
        }

        return result;
    }

    private static string? ReadActual(Bo.ApplicationProgress progress, VerifyFieldDef field) =>
        field.Name switch
        {
            "State" => progress.State?.Code?.Trim(),
            "Date" => progress.Date == default ? null : progress.Date.ToString("yyyy-MM-dd"),
            "Order" => progress.Order > 0 ? progress.Order.ToString() : null,
            "Description" => string.IsNullOrWhiteSpace(progress.Description) ? null : progress.Description.Trim(),
            "Application" => progress.Application?.ID.ToString("D"),
            _ => null,
        };

    private static bool ValuesMatch(VerifyFieldDef field, string? expected, string? actual)
    {
        if (string.IsNullOrEmpty(expected) && string.IsNullOrEmpty(actual))
            return true;

        if (string.IsNullOrEmpty(expected) && field.OptionalWhenNull)
            return true;

        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(actual))
            return false;

        return field.CompareKind switch
        {
            CompareKind.LookupKey => Visa2014CatalogMatchHelper.KeysEqual(expected, actual)
                || string.Equals(expected.Trim(), actual.Trim(), StringComparison.OrdinalIgnoreCase),
            CompareKind.Date => string.Equals(expected, actual, StringComparison.Ordinal),
            CompareKind.Scalar => string.Equals(expected.Trim(), actual.Trim(), StringComparison.Ordinal),
            CompareKind.Guid => Guid.TryParse(expected, out var e)
                && Guid.TryParse(actual, out var a)
                && e == a,
            _ => string.Equals(expected, actual, StringComparison.Ordinal),
        };
    }

    private static IReadOnlyList<MappedCandidate> StratifiedSample(
        IReadOnlyList<MappedCandidate> mapped,
        int sampleSize)
    {
        if (mapped.Count <= sampleSize)
            return mapped;

        var groups = mapped
            .GroupBy(c => c.Expected.GetValueOrDefault("State") ?? "(null)")
            .OrderByDescending(g => g.Count())
            .ToList();

        var rng = new Random(20260722);
        var selected = new List<MappedCandidate>();
        var perGroup = Math.Max(1, sampleSize / Math.Max(1, groups.Count));

        foreach (var g in groups)
        {
            var take = Math.Min(perGroup, g.Count());
            selected.AddRange(g.OrderBy(_ => rng.Next()).Take(take));
        }

        if (selected.Count < sampleSize)
        {
            var remaining = mapped.Except(selected).OrderBy(_ => rng.Next()).Take(sampleSize - selected.Count);
            selected.AddRange(remaining);
        }

        return selected.Take(sampleSize).ToList();
    }

    private static string BuildHtmlReport(ProgressVerifyReport report)
    {
        var pass = report.ExitCode == 0;
        var sb = new StringBuilder(32_768);
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\"/>");
        sb.AppendLine("<title>Mapping verify — ApplicationProgress</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body{font-family:Segoe UI,system-ui,sans-serif;margin:24px;background:#f6f7f9;color:#1a1a1a}");
        sb.AppendLine(".badge{display:inline-block;padding:6px 14px;border-radius:6px;font-weight:700;color:#fff}");
        sb.AppendLine(".pass{background:#1b7f3a}.fail{background:#b42318}");
        sb.AppendLine(".cards{display:flex;flex-wrap:wrap;gap:12px;margin:16px 0}");
        sb.AppendLine(".card{background:#fff;border:1px solid #ddd;border-radius:8px;padding:12px 16px;min-width:140px}");
        sb.AppendLine("table{border-collapse:collapse;width:100%;background:#fff;margin:12px 0}");
        sb.AppendLine("th,td{border:1px solid #ddd;padding:6px 8px;text-align:left;font-size:13px}");
        sb.AppendLine("th{background:#eee}.ok{color:#1b7f3a}.bad{color:#b42318}");
        sb.AppendLine("</style></head><body>");
        sb.AppendLine($"<h1>Mapping verify — ApplicationProgress</h1>");
        sb.AppendLine($"<div class=\"badge {(pass ? "pass" : "fail")}\">{(pass ? "PASS" : "FAIL")}</div>");
        sb.AppendLine($"<p>Source <code>{H(report.LegacySource)}</code> · Tier {H(report.Tier)} · Sampled {report.Sampled:N0}</p>");
        sb.AppendLine("<div class=\"cards\">");
        AppendCard(sb, "Importable", report.ImportableCount.ToString("N0"));
        AppendCard(sb, "Mapped", report.MappedCount.ToString("N0"));
        AppendCard(sb, "Missing id-map", report.MissingIdMap.ToString("N0"));
        AppendCard(sb, "Mismatches", report.Mismatches.Count.ToString("N0"));
        AppendCard(sb, "Histograms OK", $"{report.Histograms.Count(h => h.Ok)}/{report.Histograms.Count}");
        sb.AppendLine("</div>");

        sb.AppendLine("<section><h2>Property lineage</h2>");
        sb.AppendLine("<p>Destination <code>ApplicationProgress</code> properties vs legacy <code>dbo.Application</code> (and related) sources. Most values are <strong>synthesized</strong>, not 1:1 columns.</p>");
        sb.AppendLine("<table><tr><th>Visa2026 property</th><th>Legacy source</th><th>How</th></tr>");
        foreach (var row in report.PropertyLineage)
        {
            sb.AppendLine($"<tr><td><code>{H(row.Target)}</code></td><td>{H(row.LegacySource)}</td><td>{H(row.How)}</td></tr>");
        }
        sb.AppendLine("</table></section>");

        sb.AppendLine("<section><h2>Sample row lineage</h2>");
        if (report.SampleLineage.Count == 0)
            sb.AppendLine("<p>No sample rows.</p>");
        else
        {
            sb.AppendLine("<table><tr><th>Step</th><th>State</th><th>Date</th><th>Date ←</th><th>Description ←</th><th>Legacy key</th></tr>");
            foreach (var s in report.SampleLineage.Take(50))
            {
                sb.AppendLine(
                    $"<tr><td><code>{H(s.StepCode)}</code></td><td><code>{H(s.State)}</code></td><td>{H(s.Date)}</td>" +
                    $"<td>{H(s.LineageDate)}</td><td>{H(s.LineageDescription)}</td><td><code>{H(s.LegacyKey)}</code></td></tr>");
            }
            sb.AppendLine("</table>");
        }
        sb.AppendLine("</section>");

        sb.AppendLine("<section><h2>Histograms (State)</h2>");
        foreach (var h in report.Histograms)
        {
            sb.AppendLine($"<h3 class=\"{(h.Ok ? "ok" : "bad")}\">{H(h.Field)} — {(h.Ok ? "OK" : "FAIL")}</h3>");
            sb.AppendLine("<table><tr><th>Key</th><th>Expected</th><th>Actual</th><th>Δ</th></tr>");
            var keys = h.Expected.Keys.Union(h.Actual.Keys).OrderByDescending(k =>
                h.Expected.GetValueOrDefault(k) + h.Actual.GetValueOrDefault(k));
            foreach (var key in keys.Take(40))
            {
                h.Expected.TryGetValue(key, out var e);
                h.Actual.TryGetValue(key, out var a);
                h.Delta.TryGetValue(key, out var d);
                sb.AppendLine($"<tr><td><code>{H(key)}</code></td><td>{e:N0}</td><td>{a:N0}</td><td>{d}</td></tr>");
            }
            sb.AppendLine("</table>");
        }
        sb.AppendLine("</section>");

        sb.AppendLine("<section><h2>Mismatches (sample)</h2>");
        if (report.Mismatches.Count == 0)
            sb.AppendLine("<p>None.</p>");
        else
        {
            sb.AppendLine("<table><tr><th>Field</th><th>Legacy key</th><th>Expected</th><th>Actual</th></tr>");
            foreach (var m in report.Mismatches.Take(200))
            {
                sb.AppendLine(
                    $"<tr><td>{H(m.Field)}</td><td><code>{H(m.LegacyKey)}</code></td>" +
                    $"<td>{H(m.Expected)}</td><td>{H(m.Actual)}</td></tr>");
            }
            sb.AppendLine("</table>");
        }
        sb.AppendLine("</section></body></html>");
        return sb.ToString();
    }

    private static void AppendCard(StringBuilder sb, string label, string value)
    {
        sb.AppendLine($"<div class=\"card\"><div style=\"font-size:12px;color:#666\">{H(label)}</div><div style=\"font-size:20px;font-weight:700\">{H(value)}</div></div>");
    }

    private static string H(string? value) => System.Net.WebUtility.HtmlEncode(value ?? "");

    private static Dictionary<string, int> BuildDelta(
        IReadOnlyDictionary<string, int> expected,
        IReadOnlyDictionary<string, int> actual)
    {
        var keys = expected.Keys.Union(actual.Keys, StringComparer.Ordinal).ToList();
        var delta = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var key in keys)
        {
            expected.TryGetValue(key, out var e);
            actual.TryGetValue(key, out var a);
            if (e != a)
                delta[key] = a - e;
        }

        return delta;
    }

    private static string HistogramKey(VerifyFieldDef field, string value) =>
        field.CompareKind == CompareKind.LookupKey
            ? Visa2014CatalogMatchHelper.NormalizeKey(value)
            : value.Trim();

    private static void Increment(Dictionary<string, int> hist, string key)
    {
        hist.TryGetValue(key, out var n);
        hist[key] = n + 1;
    }

    private static string? NormalizeExpected(object? value) =>
        value switch
        {
            null => null,
            string s => string.IsNullOrWhiteSpace(s) ? null : s.Trim(),
            int i => i.ToString(),
            DateTime dt => dt.ToString("yyyy-MM-dd"),
            _ => value.ToString()?.Trim(),
        };

    private static string MaskConnectionString(string connectionString) =>
        System.Text.RegularExpressions.Regex.Replace(
            connectionString,
            @"(Password|Pwd)\s*=\s*[^;]+",
            "$1=***",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    private static bool HasArg(IReadOnlyList<string> args, string flag) =>
        args.Any(a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));

    private static string? GetOptionValue(IReadOnlyList<string> args, string optionName)
    {
        for (int i = 0; i < args.Count - 1; i++)
        {
            if (string.Equals(args[i], optionName, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }

        return null;
    }

    private sealed record MappedCandidate(
        string LegacyKey,
        Guid TargetId,
        Dictionary<string, string?> Expected,
        Dictionary<string, string?> Lineage,
        string? StepCode);

    private sealed record CandidateSet(
        int ImportableCount,
        int MissingIdMap,
        int SkippedTransform,
        int SkippedNoApplicationMap,
        List<MappedCandidate> Mapped);

    private enum CompareKind
    {
        LookupKey,
        Scalar,
        Date,
        Guid,
    }

    private sealed record VerifyFieldDef(
        string Name,
        CompareKind CompareKind,
        bool IncludeInHistogram,
        bool OptionalWhenNull,
        string Severity);

    private static class ProgressVerifyFields
    {
        public static readonly IReadOnlyList<VerifyFieldDef> All =
        [
            new("State", CompareKind.LookupKey, IncludeInHistogram: true, OptionalWhenNull: false, Severity: "error"),
            new("Date", CompareKind.Date, IncludeInHistogram: false, OptionalWhenNull: false, Severity: "warn"),
            new("Order", CompareKind.Scalar, IncludeInHistogram: false, OptionalWhenNull: false, Severity: "warn"),
            new("Description", CompareKind.Scalar, IncludeInHistogram: false, OptionalWhenNull: true, Severity: "error"),
            new("Application", CompareKind.Guid, IncludeInHistogram: false, OptionalWhenNull: false, Severity: "error"),
        ];
    }

    private sealed class ProgressVerifyReport
    {
        public string Entity { get; init; } = "";
        public string LegacySource { get; init; } = "";
        public string Tier { get; init; } = "";
        public int Sampled { get; init; }
        public int IdMapCount { get; init; }
        public int ImportableCount { get; init; }
        public int MappedCount { get; init; }
        public int MissingIdMap { get; init; }
        public int MissingTarget { get; init; }
        public int SkippedTransform { get; init; }
        public int SkippedNoApplicationMap { get; init; }
        public List<PropertyLineageEntry> PropertyLineage { get; init; } = [];
        public List<SampleLineageRow> SampleLineage { get; init; } = [];
        public List<HistogramResult> Histograms { get; init; } = [];
        public List<MappingMismatch> Mismatches { get; init; } = [];
        public int ExitCode { get; init; }
    }

    private sealed class PropertyLineageEntry
    {
        public string Target { get; init; } = "";
        public string LegacySource { get; init; } = "";
        public string How { get; init; } = "";
    }

    private sealed class SampleLineageRow
    {
        public string LegacyKey { get; init; } = "";
        public string? StepCode { get; init; }
        public string? State { get; init; }
        public string? Date { get; init; }
        public string? Description { get; init; }
        public string? LineageState { get; init; }
        public string? LineageDate { get; init; }
        public string? LineageDescription { get; init; }
        public string? LineageOrder { get; init; }
        public string? LineageApplication { get; init; }
    }

    private sealed class HistogramResult
    {
        public string Field { get; init; } = "";
        public bool Ok { get; init; }
        public Dictionary<string, int> Expected { get; init; } = new();
        public Dictionary<string, int> Actual { get; init; } = new();
        public Dictionary<string, int> Delta { get; init; } = new();
    }

    private sealed class MappingMismatch
    {
        public string LegacyKey { get; init; } = "";
        public string TargetId { get; init; } = "";
        public string Field { get; init; } = "";
        public string? Expected { get; init; }
        public string? Actual { get; init; }
        public string Severity { get; init; } = "error";
    }
}