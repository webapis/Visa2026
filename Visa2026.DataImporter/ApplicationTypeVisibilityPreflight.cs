using System.Reflection;
using System.Text;

namespace Visa2026.DataImporter;

internal static class ApplicationTypeVisibilityPreflight
{
    public static async Task EnsureSeedVisibilityMatchesServerAsync(
        ApiClient api,
        ApplicationTypeVisibilityCatalog seedCatalog)
    {
        var serverTypes = await api.GetAllAsync<ApplicationType>("ApplicationType");
        var serverByName = serverTypes
            .Where(t => !string.IsNullOrWhiteSpace(t.Name))
            .ToDictionary(t => t.Name.Trim(), StringComparer.OrdinalIgnoreCase);

        var seedTypeNames = seedCatalog.ApplicationTypeNames
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var errors = new List<string>();

        // Types present in seed but missing on server
        foreach (var seedName in seedTypeNames.OrderBy(x => x))
        {
            if (!serverByName.ContainsKey(seedName))
                errors.Add($"Missing ApplicationType on server: '{seedName}' (present in seed json).");
        }

        // Types present on server but missing in seed
        foreach (var serverName in serverByName.Keys.OrderBy(x => x))
        {
            if (serverName.StartsWith("App_", StringComparison.OrdinalIgnoreCase) && !seedTypeNames.Contains(serverName))
                errors.Add($"Missing ApplicationType in seed json: '{serverName}' (present on server).");
        }

        // Flag mismatches
        foreach (var seedName in seedTypeNames.OrderBy(x => x))
        {
            if (!serverByName.TryGetValue(seedName, out var serverType))
                continue;

            if (!seedCatalog.TryGetFlags(seedName, out var seedFlags))
                continue;

            foreach (var (flagName, expected) in seedFlags.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase))
            {
                if (!TryGetBoolFlag(serverType, flagName, out var actual))
                {
                    errors.Add($"'{seedName}': server ApplicationType missing flag '{flagName}'.");
                    continue;
                }

                if (actual != expected)
                    errors.Add($"'{seedName}': {flagName} seed={expected.ToString().ToLowerInvariant()} server={actual.ToString().ToLowerInvariant()}");
            }
        }

        if (errors.Count == 0)
            return;

        var sb = new StringBuilder();
        sb.AppendLine("Seed visibility does not match the server ApplicationType configuration.");
        sb.AppendLine("Fix before importing:");
        sb.AppendLine("- If server is correct: regenerate seed JSON via scripts/local/Export-ApplicationTypeSeedVisibility.ps1");
        sb.AppendLine("- If seed JSON is correct: run Module updaters / start app so ApplicationType rows update");
        sb.AppendLine();
        foreach (var e in errors.Take(200))
            sb.AppendLine($"- {e}");
        if (errors.Count > 200)
            sb.AppendLine($"- ... and {errors.Count - 200} more");

        throw new InvalidOperationException(sb.ToString());
    }

    private static bool TryGetBoolFlag(ApplicationType serverType, string flagName, out bool value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(flagName))
            return false;

        var prop = typeof(ApplicationType).GetProperty(
            flagName.Trim(),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        if (prop == null || prop.PropertyType != typeof(bool))
            return false;

        value = (bool)prop.GetValue(serverType)!;
        return true;
    }
}

