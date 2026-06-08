namespace Visa2026.Module.Services.RuntimeLogging;

public static class ApplicationRuntimeLogRetentionHelper
{
    public const int DefaultBatchSize = 500;

    public static DateTime? TryGetCutoffUtc(int retentionDays, DateTime utcNow)
    {
        if (retentionDays <= 0)
            return null;

        return utcNow.AddDays(-retentionDays);
    }
}
