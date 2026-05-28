namespace Visa2026.DataImporter;

internal sealed class SeedScenarioProcessor
{
    private readonly ApplicationTypeVisibilityCatalog _visibility;

    public SeedScenarioProcessor(ApplicationTypeVisibilityCatalog visibility)
    {
        _visibility = visibility;
    }

    /// <param name="removeDisallowed">When true, strips hidden/obsolete columns (and sheets) from <paramref name="scenario"/> in memory.</param>
    public SeedScenarioProcessResult Normalize(YamlScenario scenario, bool removeDisallowed)
    {
        var result = new SeedScenarioProcessResult(scenario.Name);
        if (scenario.Data == null || scenario.Data.Count == 0)
            return result;

        var appTypesByRef = BuildApplicationTypeIndex(scenario);

        var sheetsToRemove = new List<string>();
        foreach (var (sheetName, rows) in scenario.Data.ToList())
        {
            if (rows == null || rows.Count == 0)
                continue;

            if (SeedFieldRules.IsObsoleteSheet(sheetName)
                || ExcelMappings.IsBlockedImportEntity(
                    ExcelMappings.Sheets.FirstOrDefault(s => s.SheetName.Equals(sheetName, StringComparison.OrdinalIgnoreCase))?.EntityName))
            {
                result.AddIssue(removeDisallowed ? SeedIssueKind.PrunedSheet : SeedIssueKind.Error,
                    $"Sheet '{sheetName}' is obsolete or module-managed — remove from seed.");
                if (removeDisallowed)
                    sheetsToRemove.Add(sheetName);
                continue;
            }

            string? primaryAppType = ResolvePrimaryApplicationType(scenario, sheetName, rows, appTypesByRef);
            if (primaryAppType != null
                && _visibility.TryGetFlags(primaryAppType, out var flags)
                && !SeedFieldRules.IsSheetAllowedForApplicationType(sheetName, flags))
            {
                string flag = SeedFieldRules.GetSheetFlagName(sheetName) ?? "Show*";
                result.AddIssue(removeDisallowed ? SeedIssueKind.PrunedSheet : SeedIssueKind.Error,
                    $"Sheet '{sheetName}' is hidden for ApplicationType '{primaryAppType}' ({flag}=false).");
                if (removeDisallowed)
                    sheetsToRemove.Add(sheetName);
                continue;
            }

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                string? appType = ResolveRowApplicationType(row, appTypesByRef, primaryAppType);

                if (appType == null && sheetName.Equals("Applications", StringComparison.OrdinalIgnoreCase))
                    appType = row.GetValueOrDefault("Application Type");

                if (appType != null)
                {
                    if (_visibility.TryGetFlags(appType, out var typeFlags))
                        ApplyRowColumns(sheetName, row, typeFlags, appType, result, removeDisallowed);
                    else
                        result.AddIssue(SeedIssueKind.Warning,
                            $"Unknown Application Type '{appType}' — visibility not checked.");
                }
                else
                {
                    ApplyObsoleteColumns(sheetName, row, result, removeDisallowed);
                }
            }
        }

        foreach (string sheet in sheetsToRemove)
            scenario.Data.Remove(sheet);

        return result;
    }

    private static void ApplyRowColumns(
        string sheetName,
        Dictionary<string, string> row,
        IReadOnlyDictionary<string, bool> flags,
        string appType,
        SeedScenarioProcessResult result,
        bool removeDisallowed)
    {
        foreach (string header in row.Keys.ToList())
        {
            if (SeedFieldRules.IsObsoleteHeader(sheetName, header))
            {
                result.AddIssue(removeDisallowed ? SeedIssueKind.PrunedColumn : SeedIssueKind.Error,
                    $"{sheetName}: obsolete column '{header}'.");
                if (removeDisallowed)
                    row.Remove(header);
                continue;
            }

            bool allowed = sheetName.Equals("Applications", StringComparison.OrdinalIgnoreCase)
                ? SeedFieldRules.IsApplicationHeaderAllowed(header, flags)
                : sheetName.Equals("ApplicationItems", StringComparison.OrdinalIgnoreCase)
                    ? SeedFieldRules.IsApplicationItemHeaderAllowed(header, flags)
                    : true;

            if (!allowed)
            {
                string? flagName = sheetName.Equals("Applications", StringComparison.OrdinalIgnoreCase)
                    ? SeedFieldRules.GetApplicationHeaderFlagName(header)
                    : SeedFieldRules.GetApplicationItemHeaderFlagName(header);

                result.AddIssue(removeDisallowed ? SeedIssueKind.PrunedColumn : SeedIssueKind.Error,
                    $"{sheetName} [{appType}]: column '{header}' is not visible ({flagName ?? "n/a"}=false).");
                if (removeDisallowed)
                    row.Remove(header);
            }
        }
    }

    private static void ApplyObsoleteColumns(
        string sheetName,
        Dictionary<string, string> row,
        SeedScenarioProcessResult result,
        bool removeDisallowed)
    {
        foreach (string header in row.Keys.ToList())
        {
            if (!SeedFieldRules.IsObsoleteHeader(sheetName, header))
                continue;

            result.AddIssue(removeDisallowed ? SeedIssueKind.PrunedColumn : SeedIssueKind.Error,
                $"{sheetName}: obsolete column '{header}'.");
            if (removeDisallowed)
                row.Remove(header);
        }
    }

    private static Dictionary<string, string> BuildApplicationTypeIndex(YamlScenario scenario)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!scenario.Data!.TryGetValue("Applications", out var apps) || apps == null)
            return map;

        foreach (var row in apps)
        {
            if (!row.TryGetValue("Application Type", out string? appType) || string.IsNullOrWhiteSpace(appType))
                continue;

            appType = appType.Trim();
            if (row.TryGetValue("Application Number", out string? num) && !string.IsNullOrWhiteSpace(num))
            {
                string normalized = num.Trim().PadLeft(3, '0');
                map[normalized] = appType;
                map[num.Trim()] = appType;
            }
        }

        return map;
    }

    private static string? ResolvePrimaryApplicationType(
        YamlScenario scenario,
        string sheetName,
        List<Dictionary<string, string>> rows,
        Dictionary<string, string> appTypesByRef)
    {
        if (sheetName.Equals("Applications", StringComparison.OrdinalIgnoreCase))
            return rows.FirstOrDefault()?.GetValueOrDefault("Application Type");

        string? appRef = rows.Select(r => r.GetValueOrDefault("Application")).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
        return ResolveRowApplicationType(
            new Dictionary<string, string> { ["Application"] = appRef ?? "" },
            appTypesByRef,
            null);
    }

    private static string? ResolveRowApplicationType(
        Dictionary<string, string> row,
        Dictionary<string, string> appTypesByRef,
        string? fallback)
    {
        if (!row.TryGetValue("Application", out string? appRef) || string.IsNullOrWhiteSpace(appRef))
            return fallback;

        appRef = appRef.Trim();
        if (appTypesByRef.TryGetValue(appRef, out string? byExact))
            return byExact;

        if (TryParseApplicationNumberSuffix(appRef, out string? suffix)
            && appTypesByRef.TryGetValue(suffix, out string? bySuffix))
            return bySuffix;

        return fallback;
    }

    private static bool TryParseApplicationNumberSuffix(string fullOrPartial, out string? suffix)
    {
        suffix = null;
        int idx = fullOrPartial.IndexOf("/-", StringComparison.Ordinal);
        if (idx < 0)
            return false;

        suffix = fullOrPartial[(idx + 2)..].Trim();
        return suffix.Length > 0;
    }
}

internal enum SeedIssueKind
{
    Warning,
    Error,
    PrunedColumn,
    PrunedSheet,
}

internal sealed class SeedScenarioProcessResult
{
    public SeedScenarioProcessResult(string scenarioName) => ScenarioName = scenarioName;

    public string ScenarioName { get; }
    public List<SeedIssue> Issues { get; } = new();

    public bool HasErrors => Issues.Any(i => i.Kind == SeedIssueKind.Error);
    public bool HasChanges => Issues.Any(i => i.Kind is SeedIssueKind.PrunedColumn or SeedIssueKind.PrunedSheet);

    public void AddIssue(SeedIssueKind kind, string message) =>
        Issues.Add(new SeedIssue(kind, message));
}

internal sealed record SeedIssue(SeedIssueKind Kind, string Message);

internal static class SeedScenarioValidator
{
    public static int ValidateAll(string seedPath, bool persistPruned, bool quiet)
    {
        var visibility = ApplicationTypeVisibilityCatalog.Load(ResolveSeedRoot(seedPath));
        var processor = new SeedScenarioProcessor(visibility);
        var scenarios = YamlSeedCatalog.LoadScenarios(seedPath);
        int errorCount = 0;
        int prunedFiles = 0;

        foreach (var scenario in scenarios)
        {
            var before = CloneData(scenario.Data);
            var result = processor.Normalize(scenario, removeDisallowed: persistPruned);

            foreach (var issue in result.Issues)
            {
                if (issue.Kind == SeedIssueKind.Warning && quiet)
                    continue;

                string prefix = issue.Kind switch
                {
                    SeedIssueKind.Error => "ERROR",
                    SeedIssueKind.Warning => "WARN",
                    SeedIssueKind.PrunedColumn => "PRUNE",
                    SeedIssueKind.PrunedSheet => "PRUNE",
                    _ => "INFO",
                };

                if (!quiet || issue.Kind is not (SeedIssueKind.PrunedColumn or SeedIssueKind.PrunedSheet))
                    Console.WriteLine($"  [{prefix}] {result.ScenarioName}: {issue.Message}");
            }

            if (result.HasErrors)
                errorCount += result.Issues.Count(i => i.Kind == SeedIssueKind.Error);

            if (persistPruned && !DataEquals(before, scenario.Data))
            {
                WriteScenarioBack(seedPath, scenario);
                prunedFiles++;
            }
        }

        if (!quiet)
        {
            Console.WriteLine();
            Console.WriteLine(persistPruned
                ? $"Validated {scenarios.Count} scenario(s); updated {prunedFiles} file(s)."
                : $"Validated {scenarios.Count} scenario(s); {errorCount} issue(s) to fix.");
        }

        return errorCount;
    }

    private static string ResolveSeedRoot(string seedPath)
    {
        if (YamlSeedCatalog.IsSeedDirectory(seedPath))
            return seedPath;

        string? dir = Path.GetDirectoryName(Path.GetFullPath(seedPath));
        if (dir != null && File.Exists(Path.Combine(dir, "application-type-visibility.json")))
            return dir;

        return AppContext.BaseDirectory;
    }

    private static Dictionary<string, List<Dictionary<string, string>>>? CloneData(
        Dictionary<string, List<Dictionary<string, string>>>? data) =>
        data == null
            ? null
            : data.ToDictionary(
                kv => kv.Key,
                kv => kv.Value.Select(row => new Dictionary<string, string>(row, StringComparer.OrdinalIgnoreCase)).ToList(),
                StringComparer.OrdinalIgnoreCase);

    private static bool DataEquals(
        Dictionary<string, List<Dictionary<string, string>>>? a,
        Dictionary<string, List<Dictionary<string, string>>>? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        if (a.Count != b.Count) return false;

        foreach (var (sheet, rowsA) in a)
        {
            if (!b.TryGetValue(sheet, out var rowsB))
                return false;
            if (rowsA.Count != rowsB.Count)
                return false;

            for (int i = 0; i < rowsA.Count; i++)
            {
                var ra = rowsA[i];
                var rb = rowsB[i];
                if (ra.Count != rb.Count)
                    return false;
                foreach (var (k, v) in ra)
                {
                    if (!rb.TryGetValue(k, out string? vb) || !string.Equals(v, vb, StringComparison.Ordinal))
                        return false;
                }
            }
        }

        return true;
    }

    private static void WriteScenarioBack(string seedPath, YamlScenario scenario)
    {
        string scenariosDir = ResolveScenariosDirectory(seedPath);
        string fileName = scenario.SourceFile
                          ?? $"{scenario.Order:D2}-{scenario.Name}.yaml";
        string path = Path.Combine(scenariosDir, fileName);
        var serializer = new YamlDotNet.Serialization.SerializerBuilder()
            .WithNamingConvention(new YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention())
            .Build();
        File.WriteAllText(path, serializer.Serialize(scenario));
    }

    private static string ResolveScenariosDirectory(string seedPath)
    {
        if (YamlSeedCatalog.IsSeedDirectory(seedPath))
            return Path.Combine(seedPath, "scenarios");

        string dir = Path.GetDirectoryName(Path.GetFullPath(seedPath)) ?? AppContext.BaseDirectory;
        return Path.Combine(dir, "scenarios");
    }
}
