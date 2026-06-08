namespace Visa2026.Module.Services.RuntimeLogging;

/// <summary>
/// Stable error codes for <see cref="BusinessObjects.Operations.ApplicationRuntimeLog.ErrorCode"/>.
/// Catalog: <c>.cursor/skills/visa2026-runtime-error-tracking/reference.md</c>.
/// </summary>
public static class ApplicationRuntimeLogErrorCodes
{
    public const string PropertyName = "ErrorCode";

    // Infrastructure
    public const string InfraBatchSchema = "INFRA-BATCH-SCHEMA-001";
    public const string InfraTemplateSeed = "INFRA-TEMPLATE-SEED-001";
    public const string InfraDbUpdate = "INFRA-DB-002";
    public const string InfraLookupSync = "INFRA-LOOKUP-SYNC-001";

    // Mail merge
    public const string MailMergeMissing = "MAILMERGE-001";
    public const string MailMergeCriteria = "MAILMERGE-002";

    // PDF batch worker
    public const string PdfWorkerLoop = "PDF-WORKER-LOOP-001";
    public const string PdfBatchFailed = "PDF-BATCH-001";
    public const string PdfBatchWait = "PDF-BATCH-WAIT-001";
    public const string PdfBatchFlags = "PDF-BATCH-FLAGS-001";
    public const string PdfTemplateMissing = "PDF-TEMPLATE-001";

    // Word / Resminamalar batch worker
    public const string WordWorkerLoop = "WORD-WORKER-LOOP-001";
    public const string WordBatchFailed = "WORD-BATCH-001";
    public const string WordBatchWait = "WORD-BATCH-WAIT-001";

    // PDF form filling
    public const string PdfFillTemplateMissing = "PDF-FILL-001";
    public const string PdfFillNoAcroForm = "PDF-FILL-002";
    public const string PdfFillFieldError = "PDF-FILL-003";
    public const string PdfFillUnexpected = "PDF-FILL-004";

    // UI-only reporters (Phase 3c)
    public const string UiDocumentCopies = "UI-DOC-COPIES-001";
    public const string UiReportPackage = "UI-REPORT-PKG-001";

    // Maintenance
    public const string TempCleanup = "TEMP-CLEANUP-001";

    // Framework (inferred when not passed explicitly)
    public const string HttpUnhandled = "HTTP-UNHANDLED-001";
    public const string BlazorCircuit = "BLAZOR-CIRCUIT-001";
}
