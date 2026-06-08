namespace Visa2026.Module.Services.RuntimeLogging;

public interface IApplicationRuntimeLogRetention
{
    /// <summary>Deletes rows with <see cref="BusinessObjects.Operations.ApplicationRuntimeLog.OccurredAtUtc"/> before the retention cutoff.</summary>
    /// <returns>Total rows deleted across all batches.</returns>
    Task<int> PurgeExpiredAsync(CancellationToken cancellationToken = default);
}
