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
/// Post-import expected-vs-actual mapping verify (MAPPING_VERIFICATION.md).
/// ApplicationProfileInstance + ApplicationProfileInstanceProgress — reuses each entity's PrepareImportBatch for expected values.
/// </summary>
internal static class Visa2014MappingVerifyCommand
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

        var entity = GetOptionValue(args, "--entity") ?? "ApplicationProfileInstance";
        if (string.Equals(entity, "Application", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("ERR Entity 'Application' was renamed to ApplicationProfileInstance (hard break). Use --entity ApplicationProfileInstance.");
            return Task.FromResult(2);
        }
        if (string.Equals(entity, "ApplicationProfileInstanceProgress", StringComparison.OrdinalIgnoreCase))
            return Visa2014ApplicationProfileInstanceProgressMappingVerify.RunAsync(args, verbose);

        if (!string.Equals(entity, "ApplicationProfileInstance", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine(
                $"ERR Mapping verify supports --entity ApplicationProfileInstance|ApplicationProfileInstanceProgress (got '{entity}'). " +
                "See docs/VISA2014_MIGRATION/MAPPING_VERIFICATION.md");
            return Task.FromResult(1);
        }

        var targetConnection = GetOptionValue(args, "--target-connection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? Environment.GetEnvironmentVariable("VISA2026_SQL_CONNECTION");
        if (string.IsNullOrWhiteSpace(targetConnection))
        {
            Console.Error.WriteLine("ERR --target-connection (or ConnectionStrings__DefaultConnection) is required.");
            return Task.FromResult(1);
        }

        var applicationIdMapPath = GetOptionValue(args, "--application-id-map")
            ?? source.IdMapPath(dataImporterRoot, "ApplicationProfileInstance");

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
                $"mapping-verify-Application-{source.Id}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");

        Console.WriteLine("=== VISA2014 mapping verify (ApplicationProfileInstance) ===");
        Console.WriteLine($"INF Legacy source: {source.Id}");
        Console.WriteLine($"INF Target SQL: {MaskConnectionString(targetConnection)}");
        Console.WriteLine($"INF ApplicationProfileInstance id-map: {applicationIdMapPath}");
        Console.WriteLine($"INF Tier: {tierText} (histograms={(runTierA ? "yes" : "no")}, parity={(runParity ? (full ? "full" : $"sample {sample}") : "no")})");
        if (maxRows.HasValue)
            Console.WriteLine($"INF Max transform rows: {maxRows.Value}");

        if (!File.Exists(applicationIdMapPath))
        {
            Console.Error.WriteLine($"ERR ApplicationProfileInstance id-map not found: {applicationIdMapPath}");
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

        Console.WriteLine("INF Building expected payloads via ApplicationProfileInstance transform...");
        var catalogs = Visa2014LookupTranslator.Load(source.LookupTranslationPaths);
        var batch = Visa2014ApplicationTransform.PrepareImportBatch(
            source.ConnectionString,
            source.LookupTranslationPaths,
            maxRows: maxRows,
            verbose: verbose);

        var idMap = Visa2014IdMapHelper.Load(applicationIdMapPath);
        var fields = ApplicationVerifyFields.All;
        var candidates = BuildCandidates(batch.ImportRows, idMap, fields, catalogs);

        Console.WriteLine($"INF Importable transform rows: {candidates.ImportableCount}");
        Console.WriteLine($"INF Id-map entries: {idMap.Count}");
        Console.WriteLine($"INF Mapped candidates: {candidates.Mapped.Count}");
        Console.WriteLine($"INF Missing id-map: {candidates.MissingIdMap}");
        Console.WriteLine($"INF Transform skips: {candidates.SkippedTransform}");

        HeadlessMigrationHost? host = null;
        IDisposable? importScope = null;
        MappingVerifyReport report;
        try
        {
            host = HeadlessMigrationHost.Start(targetConnection);
            importScope = MigrationImportContext.BeginDataImportScope();

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
        var unexpectedSilent = report.SilentFields.Sum(f => f.Buckets.GetValueOrDefault(SilentBuckets.ActualWithoutExpected));
        var defaultApplied = report.SilentFields.Sum(f => f.Buckets.GetValueOrDefault(SilentBuckets.DefaultApplied));
        Console.WriteLine($"INF Silent: unexpected={unexpectedSilent}; default_applied={defaultApplied}; tolerated_defaults={report.SilentFields.Sum(f => f.Buckets.GetValueOrDefault(SilentBuckets.ActualDefaultTolerated))}");
        Console.WriteLine($"INF Exit: {report.ExitCode}");

        foreach (var mismatch in report.Mismatches.Take(25))
        {
            Console.Error.WriteLine(
                $"ERR {mismatch.Field}: legacy={mismatch.LegacyOid} target={mismatch.TargetId} " +
                $"expected={mismatch.Expected ?? "(null)"} actual={mismatch.Actual ?? "(null)"}");
        }

        foreach (var hist in report.Histograms.Where(h => !h.Ok))
        {
            Console.Error.WriteLine($"ERR Histogram {hist.Field} delta keys: {string.Join(", ", hist.Delta.Keys.Take(20))}");
        }

        foreach (var sampleSilent in report.SilentUnexpectedSamples.Take(25))
        {
            Console.Error.WriteLine(
                $"ERR Silent {sampleSilent.Field}: legacy={sampleSilent.LegacyOid} " +
                $"expected=(null) actual={sampleSilent.Actual ?? "(null)"} bucket={sampleSilent.Bucket}");
        }

        return Task.FromResult(report.ExitCode);
    }

    private static string BuildHtmlReport(MappingVerifyReport report)
    {
        var pass = report.ExitCode == 0;
        var sb = new StringBuilder(32_768);
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\"/>");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"/>");
        sb.AppendLine($"<title>Mapping verify — {H(report.Entity)} — {H(report.LegacySource)}</title>");
        sb.AppendLine("""
<style>
  :root { --ok:#1b7f4a; --fail:#b42318; --warn:#b54708; --bg:#f6f7f9; --card:#fff; --line:#d8dde6; --text:#1a1f2c; --muted:#5b6578; }
  * { box-sizing: border-box; }
  body { margin:0; font:14px/1.45 system-ui,Segoe UI,sans-serif; color:var(--text); background:var(--bg); }
  header { padding:20px 24px; border-bottom:1px solid var(--line); background:var(--card); }
  header h1 { margin:0 0 6px; font-size:1.35rem; }
  header .meta { color:var(--muted); }
  .badge { display:inline-block; padding:2px 10px; border-radius:999px; font-weight:600; font-size:12px; color:#fff; }
  .badge.ok { background:var(--ok); }
  .badge.fail { background:var(--fail); }
  main { padding:20px 24px 40px; max-width:1100px; margin:0 auto; }
  .cards { display:grid; grid-template-columns:repeat(auto-fit,minmax(140px,1fr)); gap:12px; margin-bottom:20px; }
  .card { background:var(--card); border:1px solid var(--line); border-radius:10px; padding:12px 14px; }
  .card .label { color:var(--muted); font-size:12px; }
  .card .value { font-size:1.35rem; font-weight:700; margin-top:2px; }
  section { background:var(--card); border:1px solid var(--line); border-radius:10px; padding:16px 18px; margin-bottom:16px; }
  section h2 { margin:0 0 12px; font-size:1.05rem; }
  table { width:100%; border-collapse:collapse; font-size:13px; }
  th, td { text-align:left; padding:7px 8px; border-bottom:1px solid var(--line); vertical-align:top; }
  th { color:var(--muted); font-weight:600; }
  tr.fail td { background:#fff5f4; }
  tr.ok td.flag { color:var(--ok); font-weight:600; }
  tr.fail td.flag { color:var(--fail); font-weight:600; }
  code { font-family:ui-monospace,Consolas,monospace; font-size:12px; }
  .delta { color:var(--fail); }
  .empty { color:var(--muted); font-style:italic; }
</style>
""");
        sb.AppendLine("</head><body>");
        sb.AppendLine("<header>");
        sb.AppendLine($"<h1>VISA2014 mapping verify — {H(report.Entity)}</h1>");
        var unexpectedSilent = report.SilentFields.Sum(f => f.Buckets.GetValueOrDefault(SilentBuckets.ActualWithoutExpected));
        sb.AppendLine($"<div class=\"meta\">Source <code>{H(report.LegacySource)}</code> · Tier <code>{H(report.Tier)}</code> · ");
        sb.AppendLine($"<span class=\"badge {(pass ? "ok" : "fail")}\">{(pass ? "PASS" : "FAIL")}</span>");
        sb.AppendLine($" · Silent unexpected: {unexpectedSilent:N0}</div>");
        sb.AppendLine("</header><main>");

        sb.AppendLine("<div class=\"cards\">");
        AppendCard(sb, "Importable", report.ImportableCount.ToString("N0"));
        AppendCard(sb, "Mapped", report.MappedCount.ToString("N0"));
        AppendCard(sb, "Missing id-map", report.MissingIdMap.ToString("N0"));
        AppendCard(sb, "Missing target", report.MissingTarget.ToString("N0"));
        AppendCard(sb, "Sampled", report.Sampled.ToString("N0"));
        AppendCard(sb, "Mismatches", report.Mismatches.Count.ToString("N0"));
        AppendCard(sb, "Silent unexpected", unexpectedSilent.ToString("N0"));
        AppendCard(sb, "Histograms OK", $"{report.Histograms.Count(h => h.Ok)}/{report.Histograms.Count}");
        AppendCard(sb, "Exit", report.ExitCode.ToString());
        sb.AppendLine("</div>");

        sb.AppendLine("<section><h2>Lookup histograms (Tier A)</h2>");
        if (report.Histograms.Count == 0)
        {
            sb.AppendLine("<p class=\"empty\">No histograms in this run.</p>");
        }
        else
        {
            foreach (var hist in report.Histograms)
            {
                sb.AppendLine($"<h3>{H(hist.Field)} — <span class=\"{(hist.Ok ? "ok" : "fail")}\" style=\"color:{(hist.Ok ? "var(--ok)" : "var(--fail)")}\">{(hist.Ok ? "OK" : "FAIL")}</span></h3>");
                sb.AppendLine("<table><thead><tr><th>Key</th><th>Expected</th><th>Actual</th><th>Delta</th></tr></thead><tbody>");
                var keys = hist.Expected.Keys
                    .Union(hist.Actual.Keys)
                    .OrderByDescending(k => Math.Max(
                        hist.Expected.GetValueOrDefault(k),
                        hist.Actual.GetValueOrDefault(k)))
                    .ThenBy(k => k, StringComparer.Ordinal);
                foreach (var key in keys)
                {
                    hist.Expected.TryGetValue(key, out var exp);
                    hist.Actual.TryGetValue(key, out var act);
                    var delta = act - exp;
                    var rowClass = delta == 0 ? "ok" : "fail";
                    sb.AppendLine($"<tr class=\"{rowClass}\"><td><code>{H(key)}</code></td><td>{exp:N0}</td><td>{act:N0}</td>");
                    sb.AppendLine(delta == 0
                        ? "<td>0</td></tr>"
                        : $"<td class=\"delta\">{(delta > 0 ? "+" : "")}{delta:N0}</td></tr>");
                }
                sb.AppendLine("</tbody></table>");
            }
        }
        sb.AppendLine("</section>");

        sb.AppendLine("<section><h2>Field parity mismatches (Tier B/C)</h2>");
        if (report.Mismatches.Count == 0)
        {
            sb.AppendLine("<p class=\"empty\">No mismatches in the sampled/full parity set.</p>");
        }
        else
        {
            sb.AppendLine("<table><thead><tr><th>Field</th><th>Expected</th><th>Actual</th><th>Legacy Oid</th><th>Target Id</th><th>Severity</th></tr></thead><tbody>");
            foreach (var m in report.Mismatches.Take(500))
            {
                sb.AppendLine("<tr class=\"fail\">");
                sb.AppendLine($"<td>{H(m.Field)}</td><td><code>{H(m.Expected ?? "(null)")}</code></td><td><code>{H(m.Actual ?? "(null)")}</code></td>");
                sb.AppendLine($"<td><code>{H(m.LegacyOid)}</code></td><td><code>{H(m.TargetId)}</code></td><td>{H(m.Severity)}</td></tr>");
            }
            if (report.Mismatches.Count > 500)
                sb.AppendLine($"<tr><td colspan=\"6\" class=\"empty\">… and {report.Mismatches.Count - 500:N0} more (see JSON report)</td></tr>");
            sb.AppendLine("</tbody></table>");
        }
        sb.AppendLine("</section>");

        sb.AppendLine("<section><h2>Silent / implicit outcomes</h2>");
        if (report.SilentFields.Count == 0)
        {
            sb.AppendLine("<p class=\"empty\">No silent inventory for this run.</p>");
        }
        else
        {
            sb.AppendLine("<table><thead><tr><th>Field</th><th>Bucket</th><th>Count</th></tr></thead><tbody>");
            foreach (var field in report.SilentFields)
            {
                foreach (var kv in field.Buckets.OrderByDescending(x => x.Value).ThenBy(x => x.Key))
                {
                    var rowClass = SilentBuckets.IsUnexpectedFail(kv.Key) ? "fail" : "ok";
                    sb.AppendLine($"<tr class=\"{rowClass}\"><td>{H(field.Field)}</td><td><code>{H(kv.Key)}</code></td><td>{kv.Value:N0}</td></tr>");
                }
            }
            sb.AppendLine("</tbody></table>");
        }

        if (report.SilentUnexpectedSamples.Count > 0)
        {
            sb.AppendLine("<h3>Unexpected samples (actual without expected)</h3>");
            sb.AppendLine("<table><thead><tr><th>Field</th><th>Actual</th><th>Legacy Oid</th><th>Target Id</th></tr></thead><tbody>");
            foreach (var s in report.SilentUnexpectedSamples.Take(200))
            {
                sb.AppendLine("<tr class=\"fail\">");
                sb.AppendLine($"<td>{H(s.Field)}</td><td><code>{H(s.Actual ?? "(null)")}</code></td>");
                sb.AppendLine($"<td><code>{H(s.LegacyOid)}</code></td><td><code>{H(s.TargetId)}</code></td></tr>");
            }
            sb.AppendLine("</tbody></table>");
        }
        sb.AppendLine("</section>");

        sb.AppendLine($"<p class=\"meta\" style=\"color:var(--muted)\">Generated {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC · VISA2014 mapping verify</p>");
        sb.AppendLine("</main></body></html>");
        return sb.ToString();
    }

    private static void AppendCard(StringBuilder sb, string label, string value)
    {
        sb.AppendLine("<div class=\"card\">");
        sb.AppendLine($"<div class=\"label\">{H(label)}</div>");
        sb.AppendLine($"<div class=\"value\">{H(value)}</div>");
        sb.AppendLine("</div>");
    }

    private static string H(string? value) =>
        System.Net.WebUtility.HtmlEncode(value ?? "");

    private static CandidateSet BuildCandidates(
        IReadOnlyList<Dictionary<string, object?>> importRows,
        IReadOnlyDictionary<Guid, Guid> idMap,
        IReadOnlyList<VerifyFieldDef> fields,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs)
    {
        var mapped = new List<MappedCandidate>();
        var missingIdMap = 0;
        var importable = 0;
        var skippedTransform = 0;

        foreach (var row in importRows)
        {
            if (row.GetValueOrDefault("_importAction") as string == "skip")
            {
                skippedTransform++;
                continue;
            }

            importable++;
            var legacyOid = (Guid)row["_legacyRowId"]!;
            if (!idMap.TryGetValue(legacyOid, out var targetId))
            {
                missingIdMap++;
                continue;
            }

            var expected = new Dictionary<string, string?>(StringComparer.Ordinal);
            var outcomes = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var field in fields)
            {
                expected[field.Name] = NormalizeExpected(row.GetValueOrDefault(field.Name));
                if (field.TrackSilent)
                    outcomes[field.Name] = ClassifyTransformOutcome(field.Name, row, catalogs, expected[field.Name]);
            }

            mapped.Add(new MappedCandidate(legacyOid, targetId, expected, outcomes));
        }

        return new CandidateSet(importable, missingIdMap, skippedTransform, mapped);
    }

    private static string ClassifyTransformOutcome(
        string fieldName,
        Dictionary<string, object?> row,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        string? expected)
    {
        return fieldName switch
        {
            "ApplicationType" => ClassifyLookupField(
                catalogs, "ApplicationType",
                row.GetValueOrDefault("_legacy_ApplicationTypeComposite") as string,
                expected, documentedDefault: null),
            "Urgency" => ClassifyLookupField(
                catalogs, "Urgency",
                row.GetValueOrDefault("_legacy_UrgencyComposite") as string,
                expected, documentedDefault: "NORM"),
            "VisaPeriod" => ClassifyLookupField(
                catalogs, "VisaPeriod",
                row.GetValueOrDefault("_legacy_VisaPeriodComposite") as string,
                expected, documentedDefault: "Month6"),
            "VisaCategory" => ClassifyLookupField(
                catalogs, "VisaCategory",
                row.GetValueOrDefault("_legacy_VisaCategoryComposite") as string,
                expected, documentedDefault: "Multiple"),
            "ProjectContract" => ClassifyProjectContractOutcome(catalogs, row, expected),
            _ => SilentBuckets.NullAllowed,
        };
    }

    private static string ClassifyLookupField(
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        string catalogName,
        string? legacyComposite,
        string? expected,
        string? documentedDefault)
    {
        var kind = Visa2014LookupOutcomeClassifier.Classify(catalogs, catalogName, legacyComposite, out _);
        if (kind == Visa2014LookupResolveKind.Empty && string.IsNullOrEmpty(expected))
            return SilentBuckets.NullAllowed;
        return Visa2014LookupOutcomeClassifier.ToSilentBucket(kind, expected, documentedDefault);
    }

    private static string ClassifyProjectContractOutcome(
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        Dictionary<string, object?> row,
        string? expected)
    {
        if (string.IsNullOrEmpty(expected))
            return SilentBuckets.NullAllowed;

        var legacy = row.GetValueOrDefault("_legacy_NumberOfContract") as string;
        var kind = Visa2014LookupOutcomeClassifier.Classify(catalogs, "ProjectContract", legacy, out _);
        return Visa2014LookupOutcomeClassifier.ToSilentBucket(kind, expected, documentedDefault: null);
    }

    private static MappingVerifyReport RunVerify(
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
        using var objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Bo.ApplicationProfileInstance));
        MigrationImportContext.ApplyImportObjectSpaceHooks(objectSpace);

        var appsById = LoadApplications(objectSpace, candidates.Mapped.Select(c => c.TargetId));
        var mismatches = new List<MappingMismatch>();
        var missingTarget = 0;
        var histograms = new List<HistogramResult>();
        var silentByField = fields.Where(f => f.TrackSilent)
            .ToDictionary(f => f.Name, _ => new Dictionary<string, int>(StringComparer.Ordinal), StringComparer.Ordinal);
        var unexpectedSamples = new List<SilentSample>();

        // Run-level skipped_unmapped from transform skips
        if (candidates.SkippedTransform > 0 && silentByField.ContainsKey("ApplicationType"))
            silentByField["ApplicationType"][SilentBuckets.SkippedUnmapped] = candidates.SkippedTransform;

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

                    if (!appsById.TryGetValue(c.TargetId, out var app))
                        continue;

                    var act = ReadActual(app, field);
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
                    Console.WriteLine($"INF Histogram {field.Name}: {(ok ? "OK" : "FAIL")} ({expectedHist.Count} expected keys, {actualHist.Count} actual keys)");
            }
        }

        // Silent inventory over all mapped candidates
        foreach (var c in candidates.Mapped)
        {
            if (!appsById.TryGetValue(c.TargetId, out var app))
                continue;

            foreach (var field in fields.Where(f => f.TrackSilent))
            {
                var expected = c.Expected.GetValueOrDefault(field.Name);
                var actual = ReadActual(app, field);
                var bucket = RefineSilentBucket(
                    field.Name,
                    c.Outcomes.GetValueOrDefault(field.Name) ?? SilentBuckets.NullAllowed,
                    expected,
                    actual);

                Increment(silentByField[field.Name], bucket);

                if (SilentBuckets.IsUnexpectedFail(bucket) && unexpectedSamples.Count < 200)
                {
                    unexpectedSamples.Add(new SilentSample
                    {
                        Field = field.Name,
                        LegacyOid = c.LegacyOid.ToString("D"),
                        TargetId = c.TargetId.ToString("D"),
                        Actual = actual,
                        Bucket = bucket,
                    });
                }
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
                if (!appsById.TryGetValue(c.TargetId, out var app))
                {
                    missingTarget++;
                    mismatches.Add(new MappingMismatch
                    {
                        LegacyOid = c.LegacyOid.ToString("D"),
                        TargetId = c.TargetId.ToString("D"),
                        Field = "(row)",
                        Expected = "(mapped)",
                        Actual = "(missing target ApplicationProfileInstance)",
                        Severity = "error",
                    });
                    continue;
                }

                foreach (var field in fields)
                {
                    var expected = c.Expected.GetValueOrDefault(field.Name);
                    var actual = ReadActual(app, field);

                    if (ValuesMatch(field, expected, actual, app))
                        continue;

                    if (string.IsNullOrEmpty(expected) && field.OptionalWhenNull)
                        continue;

                    mismatches.Add(new MappingMismatch
                    {
                        LegacyOid = c.LegacyOid.ToString("D"),
                        TargetId = c.TargetId.ToString("D"),
                        Field = field.Name,
                        Expected = expected,
                        Actual = actual,
                        Severity = field.Severity,
                    });
                }
            }
        }

        var silentFields = silentByField
            .Select(kv => new SilentFieldResult
            {
                Field = kv.Key,
                Buckets = kv.Value.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value),
            })
            .OrderBy(f => f.Field, StringComparer.Ordinal)
            .ToList();

        var errorMismatches = mismatches.Count(m => string.Equals(m.Severity, "error", StringComparison.OrdinalIgnoreCase));
        var histFail = histograms.Count(h => !h.Ok);
        var unexpectedSilent = unexpectedSamples.Count > 0
            || silentFields.Any(f => f.Buckets.GetValueOrDefault(SilentBuckets.ActualWithoutExpected) > 0);
        var exitCode = (errorMismatches > 0 || histFail > 0 || candidates.MissingIdMap > 0 || unexpectedSilent) ? 1 : 0;

        if (verbose)
        {
            foreach (var f in silentFields)
            {
                var summary = string.Join(", ", f.Buckets.Select(kv => $"{kv.Key}={kv.Value}"));
                Console.WriteLine($"INF Silent {f.Field}: {summary}");
            }
        }

        return new MappingVerifyReport
        {
            Entity = "Application",
            LegacySource = legacySourceId,
            Tier = tierText,
            Sampled = sampled,
            IdMapCount = candidates.Mapped.Count + candidates.MissingIdMap,
            ImportableCount = candidates.ImportableCount,
            MappedCount = candidates.Mapped.Count,
            MissingIdMap = candidates.MissingIdMap,
            MissingTarget = missingTarget,
            SkippedTransform = candidates.SkippedTransform,
            Histograms = histograms,
            Mismatches = mismatches,
            SilentFields = silentFields,
            SilentUnexpectedSamples = unexpectedSamples,
            ExitCode = exitCode,
        };
    }

    private static string RefineSilentBucket(
        string fieldName,
        string transformBucket,
        string? expected,
        string? actual)
    {
        if (!string.IsNullOrEmpty(expected))
            return transformBucket;

        if (string.IsNullOrEmpty(actual))
            return SilentBuckets.NullAllowed;

        if (IsToleratedActualDefault(fieldName, actual))
            return SilentBuckets.ActualDefaultTolerated;

        return SilentBuckets.ActualWithoutExpected;
    }

    private static bool IsToleratedActualDefault(string fieldName, string actual) =>
        fieldName switch
        {
            "VisaPeriod" => Visa2014CatalogMatchHelper.KeysEqual(actual, "Month6"),
            "VisaCategory" => Visa2014CatalogMatchHelper.KeysEqual(actual, "Multiple"),
            "Urgency" => Visa2014CatalogMatchHelper.KeysEqual(actual, "NORM"),
            _ => false,
        };

    private static Dictionary<Guid, Bo.ApplicationProfileInstance> LoadApplications(
        IObjectSpace objectSpace,
        IEnumerable<Guid> targetIds)
    {
        var ids = targetIds.Distinct().ToList();
        var result = new Dictionary<Guid, Bo.ApplicationProfileInstance>();
        const int chunkSize = 400;
        for (var i = 0; i < ids.Count; i += chunkSize)
        {
            var chunk = ids.Skip(i).Take(chunkSize).ToList();
            var chunkSet = chunk.ToHashSet();
            var rows = objectSpace.GetObjectsQuery<Bo.ApplicationProfileInstance>()
                .Where(a => chunkSet.Contains(a.ID))
                .ToList();
            foreach (var app in rows)
                result[app.ID] = app;
        }

        return result;
    }

    private static string? ReadActual(Bo.ApplicationProfileInstance app, VerifyFieldDef field) =>
        field.Name switch
        {
            "ApplicationType" => app.ApplicationType?.Name,
            "Urgency" => FirstNonEmpty(app.Urgency?.Code, app.Urgency?.LocalizationKey),
            "VisaPeriod" => FirstNonEmpty(app.VisaPeriod?.LocalizationKey, app.VisaPeriod?.Code),
            "VisaCategory" => FirstNonEmpty(app.VisaCategory?.LocalizationKey, app.VisaCategory?.Code),
            "FullApplicationNumber" => app.FullApplicationNumber?.Trim(),
            "ApplicationDate" => app.ApplicationDate == default
                ? null
                : app.ApplicationDate.ToString("yyyy-MM-dd"),
            "ProjectContract" => FirstNonEmpty(app.ProjectContract?.Code, app.ProjectContract?.NameTm),
            _ => null,
        };

    private static bool ValuesMatch(VerifyFieldDef field, string? expected, string? actual, Bo.ApplicationProfileInstance app)
    {
        if (string.IsNullOrEmpty(expected) && string.IsNullOrEmpty(actual))
            return true;

        if (string.IsNullOrEmpty(expected) && field.OptionalWhenNull)
            return true;

        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(actual))
            return false;

        return field.CompareKind switch
        {
            CompareKind.LookupKey => LookupKeyEquals(expected, actual),
            CompareKind.Scalar => string.Equals(expected.Trim(), actual.Trim(), StringComparison.Ordinal),
            CompareKind.Date => string.Equals(expected, actual, StringComparison.Ordinal),
            CompareKind.ProjectContract => ProjectContractMatches(expected, app.ProjectContract),
            _ => string.Equals(expected, actual, StringComparison.Ordinal),
        };
    }

    private static bool ProjectContractMatches(string expectedCode, Bo.ProjectContract? contract)
    {
        if (contract == null)
            return false;

        if (LookupKeyEquals(contract.Code, expectedCode))
            return true;

        var nameTm = contract.NameTm?.Trim() ?? "";
        var code = expectedCode.Trim();
        if (nameTm.StartsWith(code, StringComparison.OrdinalIgnoreCase))
            return true;

        return Visa2014CatalogMatchHelper.KeysEqual(nameTm, code);
    }

    private static bool LookupKeyEquals(string? a, string? b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
            return false;
        if (Visa2014CatalogMatchHelper.KeysEqual(a, b))
            return true;
        return string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<MappedCandidate> StratifiedSample(
        IReadOnlyList<MappedCandidate> mapped,
        int sampleSize)
    {
        if (mapped.Count <= sampleSize)
            return mapped;

        var groups = mapped
            .GroupBy(c => c.Expected.GetValueOrDefault("ApplicationType") ?? "(null)")
            .OrderByDescending(g => g.Count())
            .ToList();

        var rng = new Random(20260721);
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
        field.CompareKind is CompareKind.LookupKey or CompareKind.ProjectContract
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
            DateTime dt => dt.ToString("yyyy-MM-dd"),
            _ => value.ToString()?.Trim(),
        };

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();

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
        Guid LegacyOid,
        Guid TargetId,
        Dictionary<string, string?> Expected,
        Dictionary<string, string> Outcomes);

    private sealed record CandidateSet(
        int ImportableCount,
        int MissingIdMap,
        int SkippedTransform,
        List<MappedCandidate> Mapped);

    private enum CompareKind
    {
        LookupKey,
        Scalar,
        Date,
        ProjectContract,
    }

    private sealed record VerifyFieldDef(
        string Name,
        CompareKind CompareKind,
        bool IncludeInHistogram,
        bool OptionalWhenNull,
        string Severity,
        bool TrackSilent = false);

    private static class ApplicationVerifyFields
    {
        public static readonly IReadOnlyList<VerifyFieldDef> All =
        [
            new("ApplicationType", CompareKind.LookupKey, IncludeInHistogram: true, OptionalWhenNull: false, Severity: "error", TrackSilent: true),
            new("Urgency", CompareKind.LookupKey, IncludeInHistogram: true, OptionalWhenNull: false, Severity: "error", TrackSilent: true),
            new("VisaPeriod", CompareKind.LookupKey, IncludeInHistogram: true, OptionalWhenNull: true, Severity: "error", TrackSilent: true),
            new("VisaCategory", CompareKind.LookupKey, IncludeInHistogram: true, OptionalWhenNull: true, Severity: "error", TrackSilent: true),
            new("FullApplicationNumber", CompareKind.Scalar, IncludeInHistogram: false, OptionalWhenNull: false, Severity: "error"),
            new("ApplicationDate", CompareKind.Date, IncludeInHistogram: false, OptionalWhenNull: false, Severity: "error"),
            new("ProjectContract", CompareKind.ProjectContract, IncludeInHistogram: false, OptionalWhenNull: true, Severity: "error", TrackSilent: true),
        ];
    }

    private sealed class MappingVerifyReport
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
        public List<HistogramResult> Histograms { get; init; } = [];
        public List<MappingMismatch> Mismatches { get; init; } = [];
        public List<SilentFieldResult> SilentFields { get; init; } = [];
        public List<SilentSample> SilentUnexpectedSamples { get; init; } = [];
        public int ExitCode { get; init; }
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
        public string LegacyOid { get; init; } = "";
        public string TargetId { get; init; } = "";
        public string Field { get; init; } = "";
        public string? Expected { get; init; }
        public string? Actual { get; init; }
        public string Severity { get; init; } = "error";
    }

    private sealed class SilentFieldResult
    {
        public string Field { get; init; } = "";
        public Dictionary<string, int> Buckets { get; init; } = new();
    }

    private sealed class SilentSample
    {
        public string Field { get; init; } = "";
        public string LegacyOid { get; init; } = "";
        public string TargetId { get; init; } = "";
        public string? Actual { get; init; }
        public string Bucket { get; init; } = "";
    }
}