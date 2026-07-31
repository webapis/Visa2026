using System.Collections.Concurrent;
using DevExpress.ExpressApp;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal enum ParallelRowKind
{
    Posted,
    Failed,
    SkippedAlready,
    SkippedMissing,
}

internal readonly record struct ParallelRowOutcome(ParallelRowKind Kind, string? Error = null);

internal sealed class ParallelPostStats
{
    public int Posted { get; init; }
    public int Failed { get; init; }
    public int SkippedAlready { get; init; }
    public int SkippedMissing { get; init; }
    public int Processed { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

/// <summary>
/// Bounded parallel posting for large import waves. Each worker owns its own
/// <see cref="Visa2014ObjectSpaceImportTarget"/> (ObjectSpace is not thread-safe).
/// When <paramref name="partitionKeySelector"/> is set, rows with the same key always
/// go to the same worker (required for ApplicationProgress → Application optimistic lock).
/// </summary>
internal static class Visa2014ParallelImportPoster
{
    public const int DefaultDegree = 4;

    public static int ResolveDegree(IReadOnlyList<string>? args = null, int defaultDegree = DefaultDegree)
    {
        if (args != null)
        {
            for (var i = 0; i < args.Count; i++)
            {
                if (!string.Equals(args[i], "--parallelism", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (i + 1 < args.Count
                    && int.TryParse(args[i + 1], out var cli)
                    && cli > 0)
                    return cli;
            }
        }

        var env = Environment.GetEnvironmentVariable("VISA2026_IMPORT_PARALLELISM");
        if (int.TryParse(env, out var fromEnv) && fromEnv > 0)
            return fromEnv;

        return defaultDegree;
    }

    public static async Task<ParallelPostStats> PostAsync<T>(
        IReadOnlyList<T> rows,
        int parallelism,
        IVisa2014ImportTarget sharedTarget,
        INonSecuredObjectSpaceFactory? objectSpaceFactory,
        int batchSize,
        Func<T, IVisa2014ImportTarget, Task<ParallelRowOutcome>> processRow,
        string entityName,
        string? progressIdMapPath,
        int progressInterval = 100,
        Func<T, string?>? partitionKeySelector = null)
    {
        var total = rows.Count;
        if (total == 0)
            return new ParallelPostStats();

        var degree = Math.Max(1, parallelism);
        var needsPrivateObjectSpaces = sharedTarget is Visa2014ObjectSpaceImportTarget;
        if (degree > 1 && (!needsPrivateObjectSpaces || objectSpaceFactory == null))
        {
            Console.WriteLine(
                $"WRN {entityName}: parallelism={degree} requires headless --inprocess ObjectSpace; " +
                "falling back to workers=1.");
            Console.Out.Flush();
            degree = 1;
        }

        var useWorkerTargets = degree > 1 && needsPrivateObjectSpaces && objectSpaceFactory != null;

        Console.WriteLine(
            $"INF {entityName} parallel post: {total} row(s), workers={degree}" +
            (useWorkerTargets ? " (per-worker ObjectSpace)" : " (shared target)") +
            (partitionKeySelector != null && degree > 1 ? ", sticky partition" : string.Empty));
        Console.Out.Flush();
        Visa2014SyncUpsertHelper.WriteSyncProgressFile(
            progressIdMapPath, entityName, 0, total, 0, 0, 0, 0, phase: "posting");

        var posted = 0;
        var failed = 0;
        var skippedAlready = 0;
        var skippedMissing = 0;
        var processed = 0;
        var errors = new ConcurrentBag<string>();
        var progressGate = new object();
        var lastReported = 0;

        // One queue per worker. Sticky partition keeps related rows on the same worker.
        var buckets = Enumerable.Range(0, degree).Select(_ => new ConcurrentQueue<T>()).ToArray();
        if (partitionKeySelector != null && degree > 1)
        {
            foreach (var row in rows)
            {
                var key = partitionKeySelector(row);
                var idx = string.IsNullOrEmpty(key)
                    ? StableBucket(Guid.NewGuid().ToString("N"), degree)
                    : StableBucket(key, degree);
                buckets[idx].Enqueue(row);
            }
        }
        else
        {
            // Round-robin preserves approximate order while balancing load.
            for (var i = 0; i < rows.Count; i++)
                buckets[i % degree].Enqueue(rows[i]);
        }

        void ReportIfNeeded(bool force = false)
        {
            var done = Volatile.Read(ref processed);
            if (!force && done - lastReported < progressInterval && done < total)
                return;
            lock (progressGate)
            {
                done = Volatile.Read(ref processed);
                if (!force && done - lastReported < progressInterval && done < total)
                    return;
                lastReported = done;
                Visa2014SyncUpsertHelper.ReportImportLoopProgress(
                    progressIdMapPath,
                    entityName,
                    done,
                    total,
                    Volatile.Read(ref posted),
                    Volatile.Read(ref failed),
                    Volatile.Read(ref skippedAlready) + Volatile.Read(ref skippedMissing),
                    interval: 1);
            }
        }

        async Task WorkerAsync(int workerIndex)
        {
            Visa2014ObjectSpaceImportTarget? owned = null;
            IVisa2014ImportTarget target;
            if (useWorkerTargets)
            {
                owned = new Visa2014ObjectSpaceImportTarget(objectSpaceFactory!, Math.Max(1, batchSize));
                target = owned;
            }
            else
            {
                target = sharedTarget;
            }

            var queue = buckets[workerIndex];
            try
            {
                while (queue.TryDequeue(out var row))
                {
                    ParallelRowOutcome outcome;
                    try
                    {
                        outcome = await processRow(row, target).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        outcome = new ParallelRowOutcome(ParallelRowKind.Failed, ex.Message);
                        Console.Error.WriteLine($"ERR {entityName}: {ex.Message}");
                    }

                    switch (outcome.Kind)
                    {
                        case ParallelRowKind.Posted:
                            Interlocked.Increment(ref posted);
                            break;
                        case ParallelRowKind.Failed:
                            Interlocked.Increment(ref failed);
                            if (!string.IsNullOrWhiteSpace(outcome.Error))
                                errors.Add(outcome.Error!);
                            break;
                        case ParallelRowKind.SkippedAlready:
                            Interlocked.Increment(ref skippedAlready);
                            break;
                        case ParallelRowKind.SkippedMissing:
                            Interlocked.Increment(ref skippedMissing);
                            break;
                    }

                    Interlocked.Increment(ref processed);
                    ReportIfNeeded();
                }
            }
            finally
            {
                if (owned != null)
                {
                    await owned.FlushAsync().ConfigureAwait(false);
                    owned.Dispose();
                }
            }
        }

        var workers = Enumerable.Range(0, degree).Select(WorkerAsync);
        await Task.WhenAll(workers).ConfigureAwait(false);

        if (!useWorkerTargets)
            await sharedTarget.FlushAsync().ConfigureAwait(false);

        ReportIfNeeded(force: true);
        Visa2014SyncUpsertHelper.WriteSyncProgressFile(
            progressIdMapPath,
            entityName,
            Volatile.Read(ref processed),
            total,
            updated: 0,
            inserted: Volatile.Read(ref posted),
            skippedUnchanged: Volatile.Read(ref skippedAlready) + Volatile.Read(ref skippedMissing),
            failed: Volatile.Read(ref failed),
            phase: "done");
        Console.Out.Flush();

        return new ParallelPostStats
        {
            Posted = posted,
            Failed = failed,
            SkippedAlready = skippedAlready,
            SkippedMissing = skippedMissing,
            Processed = processed,
            Errors = errors.ToArray(),
        };
    }

    private static int StableBucket(string key, int degree)
    {
        unchecked
        {
            var hash = 23;
            foreach (var c in key)
                hash = (hash * 31) + c;
            if (hash == int.MinValue)
                hash = 0;
            return Math.Abs(hash) % degree;
        }
    }
}