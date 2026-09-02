using Visa2026.Module.Services.RuntimeLogging;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class ApplicationRuntimeLogRetentionHelperTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-30)]
    public void TryGetCutoffUtc_NonPositiveRetention_ReturnsNull(int retentionDays)
    {
        Assert.Null(ApplicationRuntimeLogRetentionHelper.TryGetCutoffUtc(
            retentionDays,
            new DateTime(2026, 9, 2, 12, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public void TryGetCutoffUtc_PositiveRetention_SubtractsDaysFromUtcNow()
    {
        var now = new DateTime(2026, 9, 2, 12, 0, 0, DateTimeKind.Utc);

        var cutoff = ApplicationRuntimeLogRetentionHelper.TryGetCutoffUtc(30, now);

        Assert.Equal(now.AddDays(-30), cutoff);
    }
}
