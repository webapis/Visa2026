using System.Text.Json;
using Bo = Visa2026.Module.BusinessObjects;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014PersonPhotoImportResult
{
    public int IdMapEntries { get; init; }
    public int Processed { get; init; }
    public int Patched { get; init; }
    public int SkippedNoBlob { get; init; }
    public int Failed { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

internal static class Visa2014PersonPhotoImporter
{
    private const int MaxPhotoBytes = 16 * 1024 * 1024;

    public static async Task<Visa2014PersonPhotoImportResult> RunAsync(
        IVisa2014ImportTarget target,
        string legacyConnectionString,
        string idMapPath,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        if (!File.Exists(idMapPath))
            throw new FileNotFoundException("Person id-map not found. Run scalar --import-visa2014 first.", idMapPath);

        var idMap = LoadIdMap(idMapPath);
        var entries = maxRows is > 0
            ? idMap.Take(maxRows.Value).ToList()
            : idMap;

        var errors = new List<string>();
        int patched = 0;
        int failed = 0;
        int skippedNoBlob = 0;

        foreach (var (legacyOid, targetId) in entries)
        {
            byte[]? photo;
            try
            {
                photo = ReadLegacyPhoto(legacyConnectionString, legacyOid);
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{legacyOid}: SQL read failed — {ex.Message}");
                continue;
            }

            if (photo == null || photo.Length == 0)
            {
                skippedNoBlob++;
                if (verbose)
                    Console.WriteLine($"  SKIP {legacyOid}: no Photo blob");
                continue;
            }

            if (photo.Length > MaxPhotoBytes)
            {
                failed++;
                errors.Add($"{legacyOid}: Photo {photo.Length} bytes exceeds limit ({MaxPhotoBytes}).");
                continue;
            }

            if (dryRun)
            {
                Console.WriteLine($"DRY RUN: PATCH Person {targetId} ← legacy {legacyOid} ({photo.Length} bytes)");
                patched++;
                continue;
            }

            try
            {
                await target.UpdateAsync(typeof(Bo.Person), targetId, new Dictionary<string, object?>
                {
                    ["Photo"] = photo,
                });
                patched++;
                if (verbose)
                    Console.WriteLine($"  PATCH Person {targetId} ← legacy {legacyOid} ({photo.Length} bytes)");
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{legacyOid}: {ex.Message}");
            }
        }

        if (!dryRun)
            await target.FlushAsync();

        return new Visa2014PersonPhotoImportResult
        {
            IdMapEntries = idMap.Count,
            Processed = entries.Count,
            Patched = patched,
            SkippedNoBlob = skippedNoBlob,
            Failed = failed,
            Errors = errors,
        };
    }

    private static List<KeyValuePair<Guid, Guid>> LoadIdMap(string path)
    {
        var json = File.ReadAllText(path);
        var raw = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
            ?? throw new InvalidOperationException($"Id-map is empty or invalid: {path}");

        var list = new List<KeyValuePair<Guid, Guid>>(raw.Count);
        foreach (var (legacyText, targetText) in raw)
        {
            if (!Guid.TryParse(legacyText, out var legacyOid))
                continue;
            if (!Guid.TryParse(targetText, out var targetId))
                continue;
            list.Add(new KeyValuePair<Guid, Guid>(legacyOid, targetId));
        }

        return list;
    }

    private static byte[]? ReadLegacyPhoto(string connectionString, Guid legacyOid)
    {
        using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        connection.Open();

        using var command = new Microsoft.Data.SqlClient.SqlCommand(
            "SELECT Photo FROM dbo.Person WHERE Oid = @oid AND GCRecord IS NULL",
            connection);
        command.Parameters.AddWithValue("@oid", legacyOid);

        var value = command.ExecuteScalar();
        return value is DBNull or null ? null : (byte[])value;
    }
}
