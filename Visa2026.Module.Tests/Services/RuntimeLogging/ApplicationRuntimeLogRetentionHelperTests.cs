using Visa2026.Module.Services.RuntimeLogging;
using Xunit;

namespace Visa2026.Module.Tests.Services.RuntimeLogging;

public sealed class ApplicationRuntimeLogRetentionHelperTests
{
    [Fact]
    public void TryGetCutoffUtc_returns_null_when_retention_disabled()
    {
        Assert.Null(ApplicationRuntimeLogRetentionHelper.TryGetCutoffUtc(0, new DateTime(2026, 6, 8, 12, 0, 0, DateTimeKind.Utc)));
        Assert.Null(ApplicationRuntimeLogRetentionHelper.TryGetCutoffUtc(-1, new DateTime(2026, 6, 8, 12, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public void TryGetCutoffUtc_subtracts_retention_days_from_utc_now()
    {
        var utcNow = new DateTime(2026, 6, 8, 12, 0, 0, DateTimeKind.Utc);

        var cutoff = ApplicationRuntimeLogRetentionHelper.TryGetCutoffUtc(90, utcNow);

        Assert.Equal(new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc), cutoff);
    }
}
