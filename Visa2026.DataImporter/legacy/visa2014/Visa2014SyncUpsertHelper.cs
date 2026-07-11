namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Import progress sidecars (shared by OData import loops). Name retained for call-site stability;
/// delta Sync upsert/soft-delete lived here previously and was removed.
/// </summary>
internal static class Visa2014SyncUpsertHelper
{
    /// <summary>
    /// Writes <c>{entity}.sync-progress.json</c> next to the id-map so watchers see progress
    /// even when redirected stdout is buffered.
    /// </summary>
    internal static void WriteSyncProgressFile(
        string? idMapOutputPath,
        string entityName,
        int processed,
        int total,
        int updated,
        int inserted,
        int skippedUnchanged,
        int failed,
        string? phase = null)
    {
        if (string.IsNullOrWhiteSpace(idMapOutputPath))
            return;

        try
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(idMapOutputPath));
            if (string.IsNullOrWhiteSpace(dir))
                return;

            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, $"{entityName}.sync-progress.json");
            var pct = total > 0 ? (100.0 * processed / total) : 0;
            var phaseJson = string.IsNullOrWhiteSpace(phase)
                ? ""
                : $",\"phase\":{System.Text.Json.JsonSerializer.Serialize(phase)}";
            var json =
                "{" +
                $"\"entity\":{System.Text.Json.JsonSerializer.Serialize(entityName)}," +
                $"\"processed\":{processed}," +
                $"\"total\":{total}," +
                $"\"percent\":{pct:0.##}," +
                $"\"updated\":{updated}," +
                $"\"inserted\":{inserted}," +
                $"\"skippedUnchanged\":{skippedUnchanged}," +
                $"\"failed\":{failed}" +
                phaseJson +
                $",\"utc\":{System.Text.Json.JsonSerializer.Serialize(DateTime.UtcNow.ToString("o"))}" +
                "}";
            File.WriteAllText(path, json);
        }
        catch
        {
            // Progress is best-effort; never fail the import for a watch file.
        }
    }

    /// <summary>
    /// Import-loop progress: console line with percent + sidecar JSON + flush
    /// (redirected stdout otherwise buffers for minutes under OnPrem-Sync).
    /// </summary>
    internal static void ReportImportLoopProgress(
        string? idMapOutputPath,
        string entityName,
        int processed,
        int total,
        int inserted,
        int failed,
        int skipped = 0,
        int interval = 100)
    {
        if (total <= 0)
            return;
        if (processed != total && (interval <= 0 || processed % interval != 0))
            return;

        var pct = 100.0 * processed / total;
        Console.WriteLine(
            $"INF Progress: {processed}/{total} ({pct:0.#}%) posted={inserted} failed={failed} skipped={skipped}");
        Console.Out.Flush();
        WriteSyncProgressFile(
            idMapOutputPath,
            entityName,
            processed,
            total,
            updated: 0,
            inserted: inserted,
            skippedUnchanged: skipped,
            failed: failed,
            phase: "posting");
    }
}
