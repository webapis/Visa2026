namespace Visa2026.Module.Services.RuntimeLogging;

public static class ApplicationErrorReporterExtensions
{
    public static void ReportDocumentCopiesError(
        this IApplicationErrorReporter reporter,
        string message,
        string category,
        Exception? exception = null,
        Guid? relatedBatchId = null)
    {
        reporter.Report(new ApplicationErrorReport
        {
            ErrorCode = ApplicationRuntimeLogErrorCodes.UiDocumentCopies,
            Message = message,
            Category = category,
            Exception = exception,
            RelatedBatchId = relatedBatchId
        });
    }

    public static void ReportReportPackageError(
        this IApplicationErrorReporter reporter,
        string message,
        string category,
        Exception? exception = null,
        Guid? relatedBatchId = null)
    {
        reporter.Report(new ApplicationErrorReport
        {
            ErrorCode = ApplicationRuntimeLogErrorCodes.UiReportPackage,
            Message = message,
            Category = category,
            Exception = exception,
            RelatedBatchId = relatedBatchId
        });
    }
}
