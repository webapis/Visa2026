using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Visa2026.Module.Services.LegacySyncDashboard;

internal sealed class LegacySyncDashboardSnapshotRefresher
{
    private static readonly JsonSerializerOptions JsonWriteOptions = new() { WriteIndented = true };
    private static readonly JsonSerializerOptions JsonReadOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly Regex IdMapEntryRegex = new(@"""[0-9a-fA-F-]{36}""\s*:", RegexOptions.Compiled);

    private readonly LegacySyncDashboardOptions options;
    private readonly string? targetConnectionString;

    public LegacySyncDashboardSnapshotRefresher(
        LegacySyncDashboardOptions options,
        IConfiguration configuration)
    {
        this.options = options;
        targetConnectionString = configuration.GetConnectionString("DefaultConnection");
    }

    public LegacySyncDashboardRefreshResult Refresh(Func<LegacySyncDashboardSnapshot> readSnapshot)
    {
        if (!options.Enabled)
            return Fail("Legacy sync dashboard is disabled in configuration.");

        if (string.IsNullOrWhiteSpace(options.SyncHostRoot))
            return Fail("LegacySyncDashboard:SyncHostRoot is not configured.");

        if (!Directory.Exists(options.SyncHostRoot))
            return Fail($"Sync host root not found: {options.SyncHostRoot}");

        if (string.IsNullOrWhiteSpace(targetConnectionString))
            return Fail("DefaultConnection is not configured.");

        var legacyPassword = Environment.GetEnvironmentVariable("SQL_SERVER_10.100.128.15")
            ?? Environment.GetEnvironmentVariable("VISA2014_SQL_PASSWORD");
        if (string.IsNullOrWhiteSpace(legacyPassword))
            return Fail("Set SQL_SERVER_10.100.128.15 or VISA2014_SQL_PASSWORD for legacy SQL.");

        try
        {
            var legacyBuilder = new SqlConnectionStringBuilder
            {
                DataSource = options.LegacyServer,
                InitialCatalog = options.LegacyDatabase,
                UserID = options.LegacyUser,
                Password = legacyPassword,
                TrustServerCertificate = true,
                Encrypt = false,
            };

            var targetBuilder = new SqlConnectionStringBuilder(targetConnectionString);
            var mapRoot = Path.Combine(options.SyncHostRoot, "data", "id-maps", options.LegacySource);
            var entities = new List<LegacySyncEntityRowDto>();

            foreach (var row in LegacySyncDashboardScalarDefinitions.Rows)
            {
                var legacy = ExecuteScalarCount(legacyBuilder.ConnectionString, row.LegacyQuery);
                var migrated = ExecuteScalarCount(targetBuilder.ConnectionString, row.TargetQuery);
                var notCompleted = legacy.HasValue && migrated.HasValue
                    ? Math.Max(0, legacy.Value - migrated.Value)
                    : (int?)null;
                var idMap = GetIdMapCount(mapRoot, row.BO);
                var dupDef = LegacySyncDashboardDuplicateDefinitions.TryGet(row.BO);
                int? duplicateGroups = null;
                int? duplicateExtraRows = null;
                if (dupDef?.GroupsQuery is { Length: > 0 } groupsQuery)
                    duplicateGroups = ExecuteScalarCount(targetBuilder.ConnectionString, groupsQuery);
                if (dupDef?.ExtraRowsQuery is { Length: > 0 } extraRowsQuery)
                    duplicateExtraRows = ExecuteScalarCount(targetBuilder.ConnectionString, extraRowsQuery);
                var syncState = legacy.HasValue && migrated.HasValue
                    ? GetScalarSyncState(row.BO, legacy.Value, migrated.Value, notCompleted ?? 0)
                    : "Unknown";

                entities.Add(new LegacySyncEntityRowDto
                {
                    Kind = "Scalar",
                    BO = row.BO,
                    Legacy = legacy,
                    Migrated = migrated,
                    NotCompleted = notCompleted,
                    IdMap = idMap,
                    DuplicateGroups = duplicateGroups,
                    DuplicateExtraRows = duplicateExtraRows,
                    SyncState = syncState,
                    Note = row.Note,
                });
            }

            foreach (var row in LegacySyncDashboardFileDataDefinitions.Rows)
            {
                int? legacy = row.LegacyQuery is { Length: > 0 }
                    ? ExecuteScalarCount(legacyBuilder.ConnectionString, row.LegacyQuery)
                    : null;
                var migrated = ExecuteScalarCount(targetBuilder.ConnectionString, row.TargetQuery);
                var idMap = row.IdMapEntity is { Length: > 0 }
                    ? GetIdMapCount(mapRoot, row.IdMapEntity)
                    : null;
                if (!legacy.HasValue && idMap is > 0)
                    legacy = idMap;
                var notCompleted = legacy.HasValue && migrated.HasValue
                    ? Math.Max(0, legacy.Value - migrated.Value)
                    : (int?)null;
                var syncState = GetFileDataSyncState(legacy, migrated, idMap ?? 0);

                entities.Add(new LegacySyncEntityRowDto
                {
                    Kind = "FileData",
                    BO = row.BO,
                    Legacy = legacy,
                    Migrated = migrated,
                    NotCompleted = notCompleted,
                    IdMap = idMap,
                    SyncState = syncState,
                    Note = row.Note,
                });
            }

            foreach (var row in LegacySyncDashboardLookupDefinitions.Rows)
            {
                var migrated = ExecuteScalarCount(targetBuilder.ConnectionString, row.TargetCountQuery);
                var duplicateGroups = ExecuteScalarCount(targetBuilder.ConnectionString, row.GroupsQuery);
                var duplicateExtraRows = ExecuteScalarCount(targetBuilder.ConnectionString, row.ExtraRowsQuery);
                var syncState = (duplicateGroups ?? 0) > 0 ? "Has duplicates" : "Clean";

                entities.Add(new LegacySyncEntityRowDto
                {
                    Kind = "Lookup",
                    BO = row.BO,
                    Legacy = null,
                    Migrated = migrated,
                    NotCompleted = null,
                    IdMap = null,
                    DuplicateGroups = duplicateGroups,
                    DuplicateExtraRows = duplicateExtraRows,
                    SyncState = syncState,
                    Note = row.Note,
                });
            }

            var runStatus = ReadRunStatus(options.SyncHostRoot);
            var waveSummary = MapWaveSummary(runStatus);
            var dashboard = new Dictionary<string, object?>
            {
                ["Version"] = 1,
                ["GeneratedUtc"] = DateTime.UtcNow.ToString("o"),
                ["LegacySource"] = options.LegacySource,
                ["LegacyServer"] = options.LegacyServer,
                ["LegacyDatabase"] = options.LegacyDatabase,
                ["TargetServer"] = targetBuilder.DataSource,
                ["TargetDatabase"] = targetBuilder.InitialCatalog,
                ["WatermarkUtc"] = ReadWatermarkUtc(options.SyncHostRoot, options.LegacySource),
                ["RunStatus"] = runStatus,
                ["WaveSummary"] = waveSummary,
                ["Entities"] = entities.Select(e => new Dictionary<string, object?>
                {
                    ["Kind"] = e.Kind,
                    ["BO"] = e.BO,
                    ["Legacy"] = e.Legacy,
                    ["Migrated"] = e.Migrated,
                    ["NotCompleted"] = e.NotCompleted,
                    ["IdMap"] = e.IdMap,
                    ["DuplicateGroups"] = e.DuplicateGroups,
                    ["DuplicateExtraRows"] = e.DuplicateExtraRows,
                    ["SyncState"] = e.SyncState,
                    ["Note"] = e.Note,
                }).ToList(),
            };

            var jsonPath = Path.Combine(options.SyncHostRoot, LegacySyncDashboardPaths.DashboardJsonFileName);
            File.WriteAllText(jsonPath, JsonSerializer.Serialize(dashboard, JsonWriteOptions));

            return new LegacySyncDashboardRefreshResult
            {
                Success = true,
                Snapshot = readSnapshot(),
            };
        }
        catch (Exception ex)
        {
            return Fail($"Failed to refresh dashboard: {ex.Message}");
        }
    }

    private static LegacySyncDashboardRefreshResult Fail(string message) =>
        new() { Success = false, ErrorMessage = message };

    private static int? ExecuteScalarCount(string connectionString, string query)
    {
        using var connection = new SqlConnection(connectionString);
        using var command = new SqlCommand(query, connection);
        connection.Open();
        var value = command.ExecuteScalar();
        return value is null or DBNull ? null : Convert.ToInt32(value);
    }

    private static int? GetIdMapCount(string mapRoot, string entity)
    {
        var path = Path.Combine(mapRoot, $"{entity}.json");
        if (!File.Exists(path))
            return null;

        var text = File.ReadAllText(path);
        return IdMapEntryRegex.Matches(text).Count;
    }

    private static string GetScalarSyncState(string bo, int legacy, int migrated, int notCompleted)
    {
        if (bo == "ApplicationProgress")
            return "Synthetic (multi-step)";
        if (bo == "AddressOfResidence" && migrated > legacy)
            return "Complete (PIA inferred)";
        if (bo == "WorkPermit" && migrated >= legacy)
            return "Complete";
        if (notCompleted == 0)
            return "Complete";
        if (notCompleted <= 100)
            return "Near complete";
        return "Partial";
    }

    private static string GetFileDataSyncState(int? legacyScope, int? migrated, int fileIdMap)
    {
        var legacy = legacyScope ?? 0;
        var target = migrated ?? 0;
        if (legacy == 0)
            return target > 0 ? "Bootstrap only" : "N/A";
        if (target == 0 && fileIdMap == 0)
            return "Not started";
        if (target >= legacy && fileIdMap >= legacy)
            return "Bootstrap complete";
        if (target > 0 && fileIdMap > 0)
            return $"Partial ({fileIdMap} mapped)";
        if (target > 0)
            return "Prod rows; no file id-map";
        return "Not started";
    }

    private static string? ReadWatermarkUtc(string syncHostRoot, string legacySource)
    {
        var path = Path.Combine(syncHostRoot, "data", "sync-state", $"{legacySource}.json");
        if (!File.Exists(path))
            return null;

        try
        {
            using var stream = File.OpenRead(path);
            using var doc = JsonDocument.Parse(stream);
            if (doc.RootElement.TryGetProperty("LastSuccessfulRunUtc", out var value))
                return value.GetString();
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static object? ReadRunStatus(string syncHostRoot)
    {
        var path = Path.Combine(syncHostRoot, "sync-run-status.json");
        if (!File.Exists(path))
            return null;

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<object>(json, JsonReadOptions);
        }
        catch
        {
            return null;
        }
    }

    private static Dictionary<string, int>? MapWaveSummary(object? runStatus)
    {
        if (runStatus is not JsonElement element || element.ValueKind != JsonValueKind.Object)
            return null;

        if (!element.TryGetProperty("Waves", out var waves) || waves.ValueKind != JsonValueKind.Array)
            return null;

        var summary = new Dictionary<string, int>
        {
            ["Pending"] = 0,
            ["Running"] = 0,
            ["Completed"] = 0,
            ["Failed"] = 0,
        };

        foreach (var wave in waves.EnumerateArray())
        {
            if (!wave.TryGetProperty("Status", out var statusProp))
                continue;

            var status = statusProp.GetString() ?? "";
            if (summary.ContainsKey(status))
                summary[status]++;
            else if (status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                summary["Pending"]++;
            else if (status.Equals("Running", StringComparison.OrdinalIgnoreCase))
                summary["Running"]++;
            else if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                summary["Completed"]++;
            else if (status.Equals("Failed", StringComparison.OrdinalIgnoreCase))
                summary["Failed"]++;
        }

        return summary;
    }
}
