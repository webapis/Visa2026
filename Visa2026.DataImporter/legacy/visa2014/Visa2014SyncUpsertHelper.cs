namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014SyncUpsertHelper
{
    public static async Task<Visa2014SyncEntityResult> RunAsync(
        IVisa2014ImportTarget target,
        Type entityType,
        string entityName,
        IReadOnlyList<Dictionary<string, object?>> rows,
        Visa2014SyncContext sync,
        Func<Dictionary<string, object?>, Dictionary<string, object?>?> buildPayload,
        int legacyRowCount,
        int skippedTransformCount,
        int dedupeMergedCount,
        bool verbose,
        Func<Dictionary<string, object?>, Guid?>? resolveExistingOnInsert = null,
        Action<IReadOnlyDictionary<string, object?>, Guid>? onInserted = null,
        CancellationToken cancellationToken = default)
    {
        var idMap = sync.IdMap;
        var errors = new List<string>();
        int inserted = 0;
        int updated = 0;
        int relinked = 0;
        int skippedUnchanged = 0;
        int failed = 0;

        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (row["_legacyRowId"] is not Guid legacyOid)
                continue;

            Dictionary<string, object?>? payload;
            try
            {
                payload = buildPayload(row);
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{legacyOid}: payload build failed: {ex.Message}");
                continue;
            }

            if (payload == null)
            {
                failed++;
                errors.Add($"{legacyOid}: incomplete payload");
                continue;
            }

            if (idMap.TryGetValue(legacyOid, out var targetId))
            {
                if (!sync.RowFilter.ShouldUpdateMappedRow(legacyOid))
                {
                    skippedUnchanged++;
                    continue;
                }

                try
                {
                    await target.UpdateAsync(entityType, targetId, payload);
                    updated++;
                    if (verbose)
                        Console.WriteLine($"  UPDATE {entityName} {targetId} <- legacy {legacyOid}");
                    continue;
                }
                catch (Exception ex) when (IsStaleIdMapTarget(ex))
                {
                    idMap.Remove(legacyOid);
                    if (verbose)
                        Console.WriteLine($"  REMAP stale id-map {entityName} legacy {legacyOid} (target {targetId} missing) - insert");
                }
                catch (Exception ex)
                {
                    failed++;
                    errors.Add($"{legacyOid}: update failed: {ex.Message}");
                    Console.Error.WriteLine($"ERR {legacyOid}: {ex.Message}");
                    continue;
                }
            }

            if (resolveExistingOnInsert?.Invoke(payload) is Guid existingId)
            {
                try
                {
                    await target.UpdateAsync(entityType, existingId, payload);
                    idMap[legacyOid] = existingId;
                    updated++;
                    relinked++;
                    if (verbose)
                        Console.WriteLine($"  RELINK {entityName} {existingId} <- legacy {legacyOid} (existing Application+Person)");
                    continue;
                }
                catch (Exception ex)
                {
                    failed++;
                    errors.Add($"{legacyOid}: relink update failed: {ex.Message}");
                    Console.Error.WriteLine($"ERR {legacyOid}: {ex.Message}");
                    continue;
                }
            }

            try
            {
                var createdId = await target.CreateAsync(entityType, payload);
                if (!createdId.HasValue)
                {
                    failed++;
                    errors.Add($"{legacyOid}: create returned null");
                    continue;
                }

                idMap[legacyOid] = createdId.Value;
                onInserted?.Invoke(payload, createdId.Value);
                inserted++;
                if (inserted % 250 == 0 && !string.IsNullOrWhiteSpace(sync.IdMapOutputPath))
                    await Visa2014IdMapHelper.SaveAsync(sync.IdMapOutputPath, idMap);

                if (verbose)
                    Console.WriteLine($"  INSERT {entityName} {createdId.Value} <- legacy {legacyOid}");
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{legacyOid}: create failed: {ex.Message}");
                Console.Error.WriteLine($"ERR {legacyOid}: {ex.Message}");
            }
        }

        await target.FlushAsync();

        string? idMapPath = null;
        if (idMap.Count > 0 && !string.IsNullOrWhiteSpace(sync.IdMapOutputPath))
        {
            await Visa2014IdMapHelper.SaveAsync(sync.IdMapOutputPath, idMap);
            idMapPath = Path.GetFullPath(sync.IdMapOutputPath);
        }

        return new Visa2014SyncEntityResult
        {
            LegacyRowCount = legacyRowCount,
            PreparedCount = rows.Count,
            InsertedCount = inserted,
            UpdatedCount = updated,
            SkippedUnchangedCount = skippedUnchanged,
            SoftDeletedCount = 0,
            FailedCount = failed,
            SkippedCount = skippedTransformCount,
            DedupeMergedCount = dedupeMergedCount,
            RelinkedCount = relinked,
            IdMapPath = idMapPath,
            Errors = errors,
        };
    }

    public static Visa2014SyncEntityResult WithSoftDeletedCount(Visa2014SyncEntityResult result, int softDeletedCount) =>
        new()
        {
            LegacyRowCount = result.LegacyRowCount,
            PreparedCount = result.PreparedCount,
            InsertedCount = result.InsertedCount,
            UpdatedCount = result.UpdatedCount,
            SkippedUnchangedCount = result.SkippedUnchangedCount,
            SoftDeletedCount = softDeletedCount,
            FailedCount = result.FailedCount,
            SkippedCount = result.SkippedCount,
            DedupeMergedCount = result.DedupeMergedCount,
            RelinkedCount = result.RelinkedCount,
            IdMapPath = result.IdMapPath,
            Errors = result.Errors,
        };

    public static async Task<int> ApplySoftDeletesForEntityAsync(
        IVisa2014ImportTarget target,
        Type entityType,
        string entityName,
        string legacyConnectionString,
        Visa2014SyncContext sync,
        bool verbose,
        IList<string> errors,
        CancellationToken cancellationToken = default)
    {
        if (!sync.PropagateSoftDeletes
            || !Visa2014LegacySoftDeleteQuery.TryGetLegacyTable(entityName, out var legacyTable))
        {
            return 0;
        }

        return await ApplySoftDeletesAsync(
            target,
            entityType,
            entityName,
            sync,
            legacyConnectionString,
            legacyTable,
            sync.IdMap,
            verbose,
            errors,
            cancellationToken);
    }

    private static async Task<int> ApplySoftDeletesAsync(
        IVisa2014ImportTarget target,
        Type entityType,
        string entityName,
        Visa2014SyncContext sync,
        string legacyConnectionString,
        string legacyTable,
        Dictionary<Guid, Guid> idMap,
        bool verbose,
        IList<string> errors,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(legacyConnectionString))
            return 0;

        var deletedLegacyOids = await Visa2014LegacySoftDeleteQuery.LoadSoftDeletedLegacyOidsAsync(
            legacyConnectionString,
            legacyTable,
            idMap.Keys,
            cancellationToken);

        int softDeleted = 0;
        foreach (var legacyOid in deletedLegacyOids)
        {
            if (!idMap.TryGetValue(legacyOid, out var targetId))
                continue;

            try
            {
                await target.SoftDeleteAsync(entityType, targetId);
                softDeleted++;
                if (verbose)
                    Console.WriteLine($"  SOFT-DELETE {entityName} {targetId} <- legacy {legacyOid}");
            }
            catch (Exception ex) when (IsStaleIdMapTarget(ex, "Soft-delete target"))
            {
                idMap.Remove(legacyOid);
                if (verbose)
                    Console.WriteLine($"  REMAP stale id-map {entityName} legacy {legacyOid} (soft-delete target {targetId} missing)");
            }
            catch (Exception ex)
            {
                errors.Add($"{legacyOid}: soft-delete failed: {ex.Message}");
                Console.Error.WriteLine($"ERR soft-delete {legacyOid}: {ex.Message}");
            }
        }

        if (softDeleted > 0)
            await target.FlushAsync();

        return softDeleted;
    }


    private static bool IsStaleIdMapTarget(Exception ex, string targetPrefix = "Update target") =>
        ex is InvalidOperationException ioe
        && ioe.Message.Contains("not found.", StringComparison.OrdinalIgnoreCase)
        && ioe.Message.Contains(targetPrefix, StringComparison.OrdinalIgnoreCase);
}
