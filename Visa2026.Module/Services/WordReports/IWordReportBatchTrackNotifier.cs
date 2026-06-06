using System;

namespace Visa2026.Module.Services.WordReports;

/// <summary>
/// Lets the Blazor Resminamalar toast track the batch queued from an XAF controller action.
/// </summary>
public interface IWordReportBatchTrackNotifier
{
    void TrackQueuedBatch(Guid batchId, string requestedBy);

    bool TryTakePendingBatchId(string requestedBy, out Guid batchId);
}
