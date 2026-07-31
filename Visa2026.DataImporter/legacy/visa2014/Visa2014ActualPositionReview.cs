using System.Text;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Review / cleanup helper for EmployeePositionHistory.ActualPosition values created during the VISA2014 import
/// (many came from legacy Person.MiddleName free text and are not real position titles).
///
/// Two phases, OData only:
///   --export-visa2014-actual-positions   → writes an editable CSV of distinct ActualPosition values + usage counts.
///   --apply-visa2014-actual-positions    → reads the edited CSV; for rows marked in the SetToDash column, repoints
///                                          their EmployeePositionHistory rows to the canonical "-" ActualPosition,
///                                          then deletes the now-unused marked ActualPosition rows.
/// </summary>
internal static class Visa2014ActualPositionReview
{
    private static string DashName => Visa2014ActualPositionNormalizer.DashName;

    private static string DefaultCsvPath()
    {
        var solutionRoot = Visa2014ContentRoot.FindSolutionRoot();
        var dir = solutionRoot != null
            ? Path.Combine(solutionRoot, "docs", "VISA2014_MIGRATION")
            : AppContext.BaseDirectory;
        return Path.Combine(dir, "actual-position-review.csv");
    }

    // ------------------------------------------------------------------ export
    public static async Task<int> RunExportCommandAsync(IReadOnlyList<string> args, bool verbose)
    {
        var api = BuildApi(args, verbose, out var noWait);
        if (!noWait) await api.WaitForServerAsync();
        await api.LoginAsync();

        var outPath = GetOptionValue(args, "--file") ?? DefaultCsvPath();

        Console.WriteLine("=== VISA2014 ActualPosition review export");
        Console.WriteLine($"INF Output: {outPath}");

        var history = await api.GetAllAsync<EmployeePositionHistory>(
            "EmployeePositionHistory", "$expand=ActualPosition");
        var allActuals = await api.GetAllAsync<ActualPosition>("ActualPosition");

        var usage = new Dictionary<Guid, int>();
        foreach (var eph in history)
        {
            if (eph.ActualPosition is { Id: var id } && id != Guid.Empty)
                usage[id] = usage.GetValueOrDefault(id) + 1;
        }

        var rows = allActuals
            .Select(ap => new
            {
                ap.Id,
                Name = ap.Name ?? "",
                Usage = usage.GetValueOrDefault(ap.Id),
            })
            .OrderByDescending(r => r.Usage)
            .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine("SetToDash,Name,UsageCount,Guess,ActualPositionId");
        foreach (var r in rows)
        {
            var guess = Visa2014ActualPositionNormalizer.IsNonTitlePlaceholder(r.Name)
                ? "no-letters"
                : LooksLikeNonTitle(r.Name) ? "review" : "";
            sb.Append(',')                       // SetToDash (empty — user fills)
              .Append(Csv(r.Name)).Append(',')
              .Append(r.Usage).Append(',')
              .Append(guess).Append(',')
              .Append(r.Id.ToString())
              .Append('\n');
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
        await File.WriteAllTextAsync(outPath, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        var noLetters = rows.Count(r => Visa2014ActualPositionNormalizer.IsNonTitlePlaceholder(r.Name));
        var reviewGuess = rows.Count(r => !Visa2014ActualPositionNormalizer.IsNonTitlePlaceholder(r.Name) && LooksLikeNonTitle(r.Name));
        Console.WriteLine($"INF Distinct ActualPosition rows: {rows.Count}");
        Console.WriteLine($"INF Heuristic 'no-letters' (safe to dash via --auto-no-letters): {noLetters}");
        Console.WriteLine($"INF Heuristic 'review' guesses (long/task-like — human review): {reviewGuess}");
        Console.WriteLine($"INF Mark non-titles by putting any value (e.g. x) in the SetToDash column, then run --apply-visa2014-actual-positions.");
        Console.WriteLine($"INF Or run --apply-visa2014-actual-positions --auto-no-letters to dash all no-letter Names without editing the CSV.");
        return 0;
    }

    // ------------------------------------------------------------------- apply
    public static async Task<int> RunApplyCommandAsync(IReadOnlyList<string> args, bool verbose)
    {
        var api = BuildApi(args, verbose, out var noWait);
        bool dryRun = HasArg(args, "--dry-run");
        // --auto-no-letters: select every ActualPosition whose Name has no alphabetic letter (numeric codes,
        // dashes, dots, "1 561 -", "209550-8-1-1226", etc.) — these never represent a real position title.
        bool autoNoLetters = HasArg(args, "--auto-no-letters");
        var inPath = GetOptionValue(args, "--file") ?? DefaultCsvPath();

        Console.WriteLine("=== VISA2014 ActualPosition cleanup (set non-title values to '-')");
        Console.WriteLine(autoNoLetters
            ? "INF Selection: --auto-no-letters (Name contains no alphabetic letter)"
            : $"INF Selection: marked SetToDash column in {inPath}");
        if (dryRun) Console.WriteLine("INF Mode: dry-run (no PATCH/DELETE)");

        if (!autoNoLetters && !File.Exists(inPath))
        {
            Console.Error.WriteLine($"ERR Review CSV not found: {inPath}. Run --export-visa2014-actual-positions first, or use --auto-no-letters.");
            return 1;
        }

        if (!noWait) await api.WaitForServerAsync();
        await api.LoginAsync();

        var history = await api.GetAllAsync<EmployeePositionHistory>(
            "EmployeePositionHistory", "$expand=ActualPosition");
        var allActuals = await api.GetAllAsync<ActualPosition>("ActualPosition");

        HashSet<Guid> markedSet;
        if (autoNoLetters)
        {
            markedSet = allActuals
                .Where(a => !string.Equals(a.Name?.Trim(), DashName, StringComparison.Ordinal))
                .Where(a => Visa2014ActualPositionNormalizer.IsNonTitlePlaceholder(a.Name))
                .Select(a => a.Id)
                .ToHashSet();
            Console.WriteLine($"INF Auto-selected (no-letter) ActualPosition rows: {markedSet.Count}");
            if (verbose)
                foreach (var a in allActuals.Where(a => markedSet.Contains(a.Id)).OrderBy(a => a.Name, StringComparer.Ordinal))
                    Console.WriteLine($"  DASH '{a.Name}' ({a.Id})");
        }
        else
        {
            markedSet = ReadMarkedIds(inPath);
            Console.WriteLine($"INF Marked rows to dash: {markedSet.Count}");
        }

        if (markedSet.Count == 0)
        {
            Console.WriteLine("INF Nothing selected. Done.");
            return 0;
        }

        var dashId = await ResolveOrCreateDashAsync(api, allActuals, dryRun, verbose);
        if (!dashId.HasValue && !dryRun)
        {
            Console.Error.WriteLine("ERR Could not resolve/create the '-' ActualPosition.");
            return 1;
        }

        var errors = new List<string>();
        int repointed = 0, deleted = 0, failed = 0;

        // 1) Repoint EmployeePositionHistory rows that use a marked ActualPosition to '-'.
        foreach (var eph in history)
        {
            var apId = eph.ActualPosition?.Id ?? Guid.Empty;
            if (apId == Guid.Empty || !markedSet.Contains(apId) || apId == dashId)
                continue;

            if (dryRun)
            {
                repointed++;
                if (verbose)
                    Console.WriteLine($"  DRY repoint EPH {eph.Id}: '{eph.ActualPosition?.Name}' -> '-'");
                continue;
            }

            try
            {
                await api.UpdateAsync("EmployeePositionHistory", eph.Id, new Dictionary<string, object?>
                {
                    ["ActualPosition"] = new { ID = dashId!.Value },
                });
                repointed++;
                if (repointed % 250 == 0)
                    Console.WriteLine($"INF Progress: {repointed} repointed...");
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"EPH {eph.Id}: {ex.Message}");
            }
        }

        // 2) Delete the now-unused marked ActualPosition rows (never the canonical '-').
        foreach (var apId in markedSet)
        {
            if (apId == dashId)
                continue;

            if (dryRun)
            {
                deleted++;
                continue;
            }

            try
            {
                await api.DeleteAsync("ActualPosition", apId);
                deleted++;
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"DELETE ActualPosition {apId}: {ex.Message}");
            }
        }

        Console.WriteLine($"INF EmployeePositionHistory repointed to '-': {repointed}");
        Console.WriteLine($"INF Marked ActualPosition rows deleted: {deleted}");
        Console.WriteLine($"INF Failed: {failed}");
        foreach (var e in errors.Take(20))
            Console.Error.WriteLine($"ERR {e}");
        if (errors.Count > 20)
            Console.Error.WriteLine($"ERR ... and {errors.Count - 20} more");

        return failed > 0 ? 1 : 0;
    }

    private static async Task<Guid?> ResolveOrCreateDashAsync(
        ApiClient api, List<ActualPosition> allActuals, bool dryRun, bool verbose)
    {
        var existing = allActuals.FirstOrDefault(a => string.Equals(a.Name?.Trim(), DashName, StringComparison.Ordinal));
        if (existing != null)
            return existing.Id;

        if (dryRun)
            return Guid.Empty;

        var created = await api.CreateAsync<ActualPosition>("ActualPosition", new Dictionary<string, object?>
        {
            ["Name"] = DashName,
        });
        if (created == null || created.Id == Guid.Empty)
            return null;
        if (verbose)
            Console.WriteLine($"  POST ActualPosition '-' -> {created.Id}");
        return created.Id;
    }

    /// <summary>Heuristic hint only (not authoritative): flags strings that look like tasks/descriptions, not titles.</summary>
    private static bool LooksLikeNonTitle(string name)
    {
        var n = name?.Trim() ?? "";
        if (n.Length == 0 || n == DashName)
            return false;
        if (Visa2014ActualPositionNormalizer.IsNonTitlePlaceholder(n))
            return false; // covered by Guess=no-letters / --auto-no-letters
        if (n.Length > 45) return true;
        if (n.Contains('/') || n.Contains('&')) return true;
        if (n.EndsWith('.')) return true;
        return false;
    }

    // ------------------------------------------------------------------- CSV
    private static HashSet<Guid> ReadMarkedIds(string path)
    {
        var ids = new HashSet<Guid>();
        var lines = File.ReadAllLines(path, Encoding.UTF8);
        for (int i = 1; i < lines.Length; i++) // skip header
        {
            var fields = ParseCsvLine(lines[i]);
            if (fields.Count < 5)
                continue;

            var setToDash = fields[0].Trim();
            if (setToDash.Length == 0)
                continue;

            if (Guid.TryParse(fields[4].Trim(), out var id))
                ids.Add(id);
        }

        return ids;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                    else inQuotes = false;
                }
                else sb.Append(c);
            }
            else
            {
                if (c == '"') inQuotes = true;
                else if (c == ',') { fields.Add(sb.ToString()); sb.Clear(); }
                else sb.Append(c);
            }
        }
        fields.Add(sb.ToString());
        return fields;
    }

    private static string Csv(string value)
    {
        var v = value ?? "";
        if (v.Contains('"') || v.Contains(',') || v.Contains('\n') || v.Contains('\r'))
            return "\"" + v.Replace("\"", "\"\"") + "\"";
        return v;
    }

    // ----------------------------------------------------------------- helpers
    private static ApiClient BuildApi(IReadOnlyList<string> args, bool verbose, out bool noWait)
    {
        var apiBaseUrl = GetOptionValue(args, "--api-base-url")
            ?? Environment.GetEnvironmentVariable("ApiOptions__BaseUrl")
            ?? Environment.GetEnvironmentVariable("API_BASE_URL")
            ?? "https://localhost:5001";
        var userName = GetOptionValue(args, "--user") ?? "Admin";
        var password = GetOptionValue(args, "--password") ?? "";
        noWait = HasArg(args, "--no-wait");
        return new ApiClient(apiBaseUrl, userName, password) { Verbose = verbose };
    }

    private static bool HasArg(IReadOnlyList<string> args, string flag) =>
        args.Any(a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));

    private static string? GetOptionValue(IReadOnlyList<string> args, string optionName)
    {
        for (int i = 0; i < args.Count; i++)
        {
            if (!string.Equals(args[i], optionName, StringComparison.OrdinalIgnoreCase))
                continue;
            if (i + 1 < args.Count && !args[i + 1].StartsWith('-'))
                return args[i + 1];
            return null;
        }
        return null;
    }
}
