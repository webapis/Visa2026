using Visa2026.Module.BusinessObjects.Operations;
using Visa2026.Module.Services.RuntimeLogging;
using Xunit;

namespace Visa2026.Module.Tests.Services.RuntimeLogging;

public sealed class NullApplicationRuntimeLogSentryBridgeTests
{
    [Fact]
    public void TryCapture_returns_null()
    {
        var bridge = new NullApplicationRuntimeLogSentryBridge();
        var entry = new ApplicationRuntimeLogEntry
        {
            Severity = ApplicationRuntimeLogSeverity.Error,
            Message = "Test"
        };

        Assert.Null(bridge.TryCapture(entry));
    }
}
