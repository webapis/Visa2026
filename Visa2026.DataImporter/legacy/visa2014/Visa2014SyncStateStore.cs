using System.Text.Json;
using System.Text.Json.Serialization;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014SyncStateFile
{
    public int Version { get; set; } = 1;

    public string LegacySource { get; set; } = "";

    public DateTime? LastSuccessfulRunUtc { get; set; }

    public Dictionary<string, Visa2014SyncEntityState> Entities { get; set; } = new(StringComparer.Ordinal);
}

internal sealed class Visa2014SyncEntityState
{
    public DateTime? LastRunUtc { get; set; }

    public int Inserted { get; set; }

    public int Updated { get; set; }

    public int SoftDeleted { get; set; }

    public int Failed { get; set; }
}

internal static class Visa2014SyncStateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string ResolveStatePath(string dataImporterRoot, string legacySourceId, string? syncStateDir)
    {
        if (!string.IsNullOrWhiteSpace(syncStateDir))
            return Path.Combine(syncStateDir, $"{legacySourceId}.json");

        return Path.Combine(
            dataImporterRoot,
            "legacy",
            "visa2014",
            "sync-state",
            $"{legacySourceId}.json");
    }

    public static Visa2014SyncStateFile LoadOrCreate(string path, string legacySourceId)
    {
        if (!File.Exists(path))
        {
            return new Visa2014SyncStateFile
            {
                LegacySource = legacySourceId,
            };
        }

        var json = File.ReadAllText(path);
        var state = JsonSerializer.Deserialize<Visa2014SyncStateFile>(json, JsonOptions)
            ?? new Visa2014SyncStateFile { LegacySource = legacySourceId };
        if (string.IsNullOrWhiteSpace(state.LegacySource))
            state.LegacySource = legacySourceId;
        return state;
    }

    public static async Task SaveAsync(string path, Visa2014SyncStateFile state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(state, JsonOptions));
    }

    public static DateTime ResolveSyncSinceUtc(
        Visa2014SyncStateFile state,
        DateTime? explicitSinceUtc,
        bool syncFull)
    {
        if (syncFull)
            return DateTime.MinValue;

        if (explicitSinceUtc.HasValue)
            return DateTime.SpecifyKind(explicitSinceUtc.Value, DateTimeKind.Utc);

        if (state.LastSuccessfulRunUtc.HasValue)
            return DateTime.SpecifyKind(state.LastSuccessfulRunUtc.Value, DateTimeKind.Utc);

        // First sync without --sync-full: use epoch so audit query returns nothing;
        // inserts still run; mapped rows need --sync-full once or explicit --sync-since.
        return DateTime.MinValue;
    }
}
