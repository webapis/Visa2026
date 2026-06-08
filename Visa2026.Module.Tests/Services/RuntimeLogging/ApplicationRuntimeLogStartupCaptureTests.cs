using Visa2026.Module.Services.RuntimeLogging;
using Xunit;

namespace Visa2026.Module.Tests.Services.RuntimeLogging;

public sealed class ApplicationRuntimeLogStartupCaptureTests
{
    public ApplicationRuntimeLogStartupCaptureTests()
    {
        ApplicationRuntimeLogStartupCapture.ResetForTesting();
    }

    [Fact]
    public void CaptureError_before_attach_queues_then_flushes_on_attach()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new ApplicationRuntimeLogOptions { Enabled = true });
        var runtimeQueue = new ApplicationRuntimeLogQueue(options);

        ApplicationRuntimeLogStartupCapture.CaptureError(
            ApplicationRuntimeLogErrorCodes.InfraLookupSync,
            "Visa2026.Module.DatabaseUpdate.LookupCatalogSyncUpdater",
            "Lookup catalog sync failed for country.");

        ApplicationRuntimeLogStartupCapture.Attach(runtimeQueue);

        Assert.True(runtimeQueue.TryEnqueueForTest(out var entry));
        Assert.Equal(ApplicationRuntimeLogErrorCodes.InfraLookupSync, entry!.ErrorCode);
        Assert.Contains("Lookup catalog sync failed", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CaptureError_after_attach_enqueues_immediately()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new ApplicationRuntimeLogOptions { Enabled = true });
        var runtimeQueue = new ApplicationRuntimeLogQueue(options);
        ApplicationRuntimeLogStartupCapture.Attach(runtimeQueue);

        ApplicationRuntimeLogStartupCapture.CaptureError(
            ApplicationRuntimeLogErrorCodes.InfraDbUpdate,
            "Visa2026.Blazor.Server.Startup",
            "XAF database compatibility check failed.");

        Assert.True(runtimeQueue.TryEnqueueForTest(out var entry));
        Assert.Equal(ApplicationRuntimeLogErrorCodes.InfraDbUpdate, entry!.ErrorCode);
    }
}
