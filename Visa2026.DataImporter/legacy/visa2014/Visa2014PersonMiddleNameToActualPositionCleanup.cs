namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014PersonMiddleNameCleanupResult
{
    public int EmployeesWithMiddleName { get; init; }
    public int ActualPositionsPatched { get; init; }
    public int ActualPositionsCreated { get; init; }
    public int MiddleNamesCleared { get; init; }
    public int KeptNoPositionHistory { get; init; }
    public int Failed { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

/// <summary>
/// One-off cleanup: legacy Person.MiddleName held a free-text work-position title (no dedicated field in VISA2014).
/// For each employee with a MiddleName, copy it to the current/latest EmployeePositionHistory.ActualPosition
/// (find-or-create), then clear Person.MiddleName so FullName no longer shows the title. Employees without any
/// position-history row are reported and left untouched (nothing to attach to). OData only — no direct SQL writes.
/// </summary>
internal static class Visa2014PersonMiddleNameToActualPositionCleanup
{
    public static async Task<int> RunCommandAsync(IReadOnlyList<string> args, bool verbose)
    {
        var apiBaseUrl = GetOptionValue(args, "--api-base-url")
            ?? Environment.GetEnvironmentVariable("ApiOptions__BaseUrl")
            ?? Environment.GetEnvironmentVariable("API_BASE_URL")
            ?? "https://localhost:5001";
        var userName = GetOptionValue(args, "--user") ?? "Admin";
        var password = GetOptionValue(args, "--password") ?? "";

        bool dryRun = HasArg(args, "--dry-run");
        bool noWait = HasArg(args, "--no-wait");

        Console.WriteLine("=== VISA2014 Person.MiddleName -> EmployeePositionHistory.ActualPosition cleanup");
        Console.WriteLine($"INF Target API: {apiBaseUrl}");
        if (dryRun)
            Console.WriteLine("INF Mode: dry-run (no PATCH)");

        var api = new ApiClient(apiBaseUrl, userName, password) { Verbose = verbose };

        if (!noWait)
            await api.WaitForServerAsync();
        await api.LoginAsync();

        try
        {
            var result = await RunAsync(api, dryRun, verbose);

            Console.WriteLine($"INF Employees with MiddleName: {result.EmployeesWithMiddleName}");
            Console.WriteLine($"INF ActualPosition patched: {result.ActualPositionsPatched} (created {result.ActualPositionsCreated})");
            Console.WriteLine($"INF MiddleName cleared: {result.MiddleNamesCleared}");
            Console.WriteLine($"INF Kept (no position history): {result.KeptNoPositionHistory}");
            Console.WriteLine($"INF Failed: {result.Failed}");

            foreach (var error in result.Errors.Take(20))
                Console.Error.WriteLine($"ERR {error}");
            if (result.Errors.Count > 20)
                Console.Error.WriteLine($"ERR ... and {result.Errors.Count - 20} more");

            return result.Failed > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERR Cleanup failed: {ex.Message}");
            if (verbose)
                Console.Error.WriteLine(ex);
            return 1;
        }
    }

    public static async Task<Visa2014PersonMiddleNameCleanupResult> RunAsync(
        ApiClient api,
        bool dryRun,
        bool verbose)
    {
        var employees = await api.GetAllAsync<Person>("Person", "$filter=IsEmployee eq true&$select=ID,MiddleName,IsEmployee");
        var withMiddleName = employees
            .Where(p => p.Id != Guid.Empty && !string.IsNullOrWhiteSpace(p.MiddleName))
            .ToList();
        Console.WriteLine($"INF {withMiddleName.Count} employee(s) with a MiddleName to migrate");

        var history = await api.GetAllAsync<EmployeePositionHistory>(
            "EmployeePositionHistory", "$expand=Person,ActualPosition");
        var historyByPerson = history
            .Where(e => e.Person != null && e.Person.Id != Guid.Empty)
            .GroupBy(e => e.Person!.Id)
            .ToDictionary(g => g.Key, g => g.ToList());

        var actualPositions = await api.GetAllAsync<ActualPosition>("ActualPosition");
        var actualByName = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var ap in actualPositions)
        {
            var key = ap.Name?.Trim() ?? "";
            if (key.Length > 0 && !actualByName.ContainsKey(key))
                actualByName[key] = ap.Id;
        }

        var errors = new List<string>();
        int patched = 0, created = 0, cleared = 0, keptNoHistory = 0, failed = 0;

        foreach (var person in withMiddleName)
        {
            var title = person.MiddleName.Trim();
            try
            {
                if (!historyByPerson.TryGetValue(person.Id, out var rows) || rows.Count == 0)
                {
                    keptNoHistory++;
                    if (verbose)
                        Console.WriteLine($"  KEEP Person {person.Id}: MiddleName '{title}' (no position history)");
                    continue;
                }

                var current = rows
                    .OrderByDescending(r => r.EndDate == null)
                    .ThenByDescending(r => r.StartDate)
                    .First();

                if (dryRun)
                {
                    bool exists = actualByName.ContainsKey(title);
                    if (!exists) created++;
                    patched++;
                    cleared++;
                    if (verbose)
                        Console.WriteLine($"  DRY Person {person.Id}: set EPH {current.Id} ActualPosition='{title}', clear MiddleName");
                    continue;
                }

                var (actualId, wasCreated) = await ResolveOrCreateActualPositionAsync(api, actualByName, title, verbose);
                if (!actualId.HasValue)
                {
                    failed++;
                    errors.Add($"Person {person.Id}: could not resolve/create ActualPosition '{title}'");
                    continue;
                }
                if (wasCreated) created++;

                if (current.ActualPosition?.Id != actualId.Value)
                {
                    await api.UpdateAsync("EmployeePositionHistory", current.Id, new Dictionary<string, object?>
                    {
                        ["ActualPosition"] = new { ID = actualId.Value },
                    });
                    patched++;
                    if (verbose)
                        Console.WriteLine($"  PATCH EPH {current.Id} ActualPosition='{title}' (Person {person.Id})");
                }

                await api.UpdateAsync("Person", person.Id, new Dictionary<string, object?>
                {
                    ["MiddleName"] = "",
                });
                cleared++;

                if ((patched + cleared) % 250 == 0)
                    Console.WriteLine($"INF Progress: {patched} patched, {cleared} cleared, {keptNoHistory} kept...");
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"Person {person.Id} ('{title}'): {ex.Message}");
                Console.Error.WriteLine($"ERR Person {person.Id}: {ex.Message}");
            }
        }

        return new Visa2014PersonMiddleNameCleanupResult
        {
            EmployeesWithMiddleName = withMiddleName.Count,
            ActualPositionsPatched = patched,
            ActualPositionsCreated = created,
            MiddleNamesCleared = cleared,
            KeptNoPositionHistory = keptNoHistory,
            Failed = failed,
            Errors = errors,
        };
    }

    private static async Task<(Guid? Id, bool Created)> ResolveOrCreateActualPositionAsync(
        ApiClient api,
        Dictionary<string, Guid> cache,
        string name,
        bool verbose)
    {
        var key = name.Trim();
        if (key.Length == 0) key = "-";
        if (cache.TryGetValue(key, out var cached))
            return (cached, false);

        var trimmed = key.Length > 100 ? key[..100] : key;
        var createdRow = await api.CreateAsync<ActualPosition>("ActualPosition", new Dictionary<string, object?>
        {
            ["Name"] = trimmed,
        });
        if (createdRow == null || createdRow.Id == Guid.Empty)
            return (null, false);

        cache[key] = createdRow.Id;
        if (verbose)
            Console.WriteLine($"  POST ActualPosition '{trimmed}' -> {createdRow.Id}");
        return (createdRow.Id, true);
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
