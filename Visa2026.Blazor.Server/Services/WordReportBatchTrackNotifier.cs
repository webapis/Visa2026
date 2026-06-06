using System;
using System.Collections.Concurrent;
using Visa2026.Module.Services.WordReports;

namespace Visa2026.Blazor.Server.Services;

public sealed class WordReportBatchTrackNotifier : IWordReportBatchTrackNotifier
{
    private readonly ConcurrentDictionary<string, Guid> pendingByUser = new(StringComparer.OrdinalIgnoreCase);

    public void TrackQueuedBatch(Guid batchId, string requestedBy)
    {
        if (batchId == Guid.Empty || string.IsNullOrWhiteSpace(requestedBy))
            return;

        pendingByUser[requestedBy] = batchId;
    }

    public bool TryTakePendingBatchId(string requestedBy, out Guid batchId)
    {
        if (string.IsNullOrWhiteSpace(requestedBy))
        {
            batchId = default;
            return false;
        }

        return pendingByUser.TryRemove(requestedBy, out batchId);
    }
}
