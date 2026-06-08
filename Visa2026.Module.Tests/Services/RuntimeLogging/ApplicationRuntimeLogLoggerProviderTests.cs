using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Visa2026.Module.BusinessObjects.Operations;
using Visa2026.Module.Services.RuntimeLogging;
using Xunit;

namespace Visa2026.Module.Tests.Services.RuntimeLogging;

public sealed class ApplicationRuntimeLogLoggerProviderTests
{
    [Fact]
    public void LogError_enqueues_entry_with_severity_and_message()
    {
        var options = Options.Create(new ApplicationRuntimeLogOptions
        {
            Enabled = true,
            MinLevel = LogLevel.Error
        });
        var optionsMonitor = new TestOptionsMonitor<ApplicationRuntimeLogOptions>(options.Value);
        var queue = new ApplicationRuntimeLogQueue(options);
        var provider = new ApplicationRuntimeLogLoggerProvider(
            queue,
            new ApplicationRuntimeLogContextAccessor(),
            optionsMonitor);

        var logger = provider.CreateLogger("Visa2026.Module.Tests.SampleService");
        logger.LogError("PDF batch failed. BatchId={BatchId}", Guid.Parse("11111111-1111-1111-1111-111111111111"));

        Assert.True(queue.TryEnqueueForTest(out var entry));
        Assert.Equal(ApplicationRuntimeLogSeverity.Error, entry!.Severity);
        Assert.Contains("PDF batch failed", entry.Message, StringComparison.Ordinal);
        Assert.Equal(ApplicationRuntimeLogErrorCodes.PdfBatchFailed, entry.ErrorCode);
        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), entry.RelatedBatchId);
    }

    [Fact]
    public void LogErrorWithCode_uses_explicit_structured_error_code()
    {
        var options = Options.Create(new ApplicationRuntimeLogOptions
        {
            Enabled = true,
            MinLevel = LogLevel.Error
        });
        var optionsMonitor = new TestOptionsMonitor<ApplicationRuntimeLogOptions>(options.Value);
        var queue = new ApplicationRuntimeLogQueue(options);
        var provider = new ApplicationRuntimeLogLoggerProvider(
            queue,
            new ApplicationRuntimeLogContextAccessor(),
            optionsMonitor);

        var logger = provider.CreateLogger("Visa2026.Module.Tests.SampleService");
        logger.LogErrorWithCode(
            ApplicationRuntimeLogErrorCodes.WordBatchFailed,
            "Resminamalar batch failed. BatchId={BatchId}",
            Guid.Parse("22222222-2222-2222-2222-222222222222"));

        Assert.True(queue.TryEnqueueForTest(out var entry));
        Assert.Equal(ApplicationRuntimeLogErrorCodes.WordBatchFailed, entry!.ErrorCode);
        Assert.Contains("Resminamalar batch failed", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MapRow_maps_entry_fields_for_persistence()
    {
        var entry = new ApplicationRuntimeLogEntry
        {
            OccurredAtUtc = new DateTime(2026, 6, 8, 12, 0, 0, DateTimeKind.Utc),
            Severity = ApplicationRuntimeLogSeverity.Error,
            Category = "Visa2026.Module.Tests.SampleService",
            Message = "Test persistence",
            ErrorCode = "TEST-001",
            CorrelationId = "abc123",
            SentryEventId = "a1b2c3d4e5f6478990abcdef12345678",
            DeploymentEnvironment = ApplicationRuntimeLogDeploymentEnvironment.LocalVisualStudio
        };

        var row = EfApplicationRuntimeLogPersistence.MapRow(entry);

        Assert.Equal("Test persistence", row.Message);
        Assert.Equal(ApplicationRuntimeLogSeverity.Error, row.Severity);
        Assert.Equal("TEST-001", row.ErrorCode);
        Assert.Equal("a1b2c3d4e5f6478990abcdef12345678", row.SentryEventId);
        Assert.Equal("abc123", row.CorrelationId);
        Assert.Equal(ApplicationRuntimeLogDeploymentEnvironment.LocalVisualStudio, row.DeploymentEnvironment);
    }

    private sealed class TestOptionsMonitor<T> : IOptionsMonitor<T>
        where T : class
    {
        public TestOptionsMonitor(T value) => CurrentValue = value;

        public T CurrentValue { get; }

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}

internal static class ApplicationRuntimeLogQueueTestExtensions
{
    public static bool TryEnqueueForTest(this ApplicationRuntimeLogQueue queue, out ApplicationRuntimeLogEntry? entry)
    {
        entry = null;
        if (!queue.Reader.TryRead(out var read))
            return false;

        entry = read;
        return true;
    }
}
