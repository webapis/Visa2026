using System;

namespace Visa2026.Module.Services.PersonDossier;

/// <summary>
/// Lets the global person export toast pick up a batch queued from the dossier page, which lives in
/// a different component tree than the toast host in <c>_Host.cshtml</c>.
/// </summary>
public interface IPersonExportBatchTrackNotifier
{
    void TrackQueuedBatch(Guid batchId, string requestedBy);

    bool TryTakePendingBatchId(string requestedBy, out Guid batchId);
}
