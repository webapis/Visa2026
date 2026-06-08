using Microsoft.Extensions.Options;
using Visa2026.Module.BusinessObjects.Operations;
using Visa2026.Module.Services.RuntimeLogging;
using Xunit;

namespace Visa2026.Module.Tests.Services.RuntimeLogging;

public sealed class ApplicationRuntimeLogErrorReporterTests
{
    [Fact]
    public void Report_enqueues_entry_with_explicit_error_code_and_category()
    {
        var options = Options.Create(new ApplicationRuntimeLogOptions
        {
            Enabled = true,
            ReportUiErrors = true
        });
        var optionsMonitor = new TestOptionsMonitor<ApplicationRuntimeLogOptions>(options.Value);
        var queue = new ApplicationRuntimeLogQueue(options);
        var reporter = new ApplicationRuntimeLogErrorReporter(
            queue,
            new ApplicationRuntimeLogContextAccessor(),
            optionsMonitor);

        reporter.Report(new ApplicationErrorReport
        {
            ErrorCode = ApplicationRuntimeLogErrorCodes.UiDocumentCopies,
            Message = "Document copies package enqueue failed.",
            Category = ApplicationRuntimeLogCategories.DocumentCopiesComponent,
            RelatedBatchId = Guid.Parse("33333333-3333-3333-3333-333333333333")
        });

        Assert.True(queue.TryEnqueueForTest(out var entry));
        Assert.Equal(ApplicationRuntimeLogErrorCodes.UiDocumentCopies, entry!.ErrorCode);
        Assert.Equal(ApplicationRuntimeLogCategories.DocumentCopiesComponent, entry.Category);
        Assert.Equal(ApplicationRuntimeLogSeverity.Error, entry.Severity);
        Assert.Contains("Document copies package enqueue failed", entry.Message, StringComparison.Ordinal);
        Assert.Equal(Guid.Parse("33333333-3333-3333-3333-333333333333"), entry.RelatedBatchId);
    }

    [Fact]
    public void Report_skips_enqueue_when_ui_reporting_disabled()
    {
        var options = Options.Create(new ApplicationRuntimeLogOptions
        {
            Enabled = true,
            ReportUiErrors = false
        });
        var optionsMonitor = new TestOptionsMonitor<ApplicationRuntimeLogOptions>(options.Value);
        var queue = new ApplicationRuntimeLogQueue(options);
        var reporter = new ApplicationRuntimeLogErrorReporter(
            queue,
            new ApplicationRuntimeLogContextAccessor(),
            optionsMonitor);

        reporter.Report(new ApplicationErrorReport
        {
            ErrorCode = ApplicationRuntimeLogErrorCodes.UiReportPackage,
            Message = "Resminamalar preview failed.",
            Category = ApplicationRuntimeLogCategories.ReportPackagePreviewDialog
        });

        Assert.False(queue.TryEnqueueForTest(out _));
    }

    [Fact]
    public void ReportDocumentCopiesError_uses_ui_document_copies_code()
    {
        var options = Options.Create(new ApplicationRuntimeLogOptions { Enabled = true, ReportUiErrors = true });
        var optionsMonitor = new TestOptionsMonitor<ApplicationRuntimeLogOptions>(options.Value);
        var queue = new ApplicationRuntimeLogQueue(options);
        var reporter = new ApplicationRuntimeLogErrorReporter(
            queue,
            new ApplicationRuntimeLogContextAccessor(),
            optionsMonitor);

        reporter.ReportDocumentCopiesError(
            "Preview failed.",
            ApplicationRuntimeLogCategories.DocumentCopiesPreviewDialog);

        Assert.True(queue.TryEnqueueForTest(out var entry));
        Assert.Equal(ApplicationRuntimeLogErrorCodes.UiDocumentCopies, entry!.ErrorCode);
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
