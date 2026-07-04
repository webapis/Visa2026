using DevExpress.ExpressApp;

namespace Visa2026.Module.Services.MigrationImport;

/// <summary>
/// Request-scoped flag for VISA2014 / DataImporter OData loads.
/// Set by <c>X-Visa2014-DataImport</c> on API requests; read when Object Spaces are created.
/// </summary>
public static class MigrationImportContext
{
    public const string DataImportHeaderName = "X-Visa2014-DataImport";

    private static readonly AsyncLocal<bool> SuppressAuditTrail = new();
    private static readonly AsyncLocal<bool> DataImport = new();
    private static int scopeDepth;

    public static bool IsAuditTrailSuppressed => SuppressAuditTrail.Value;

    /// <summary>True during VISA2014 OData or headless in-process import — skip AppNumberFormat on save.</summary>
    public static bool IsDataImport => DataImport.Value;

    /// <summary>UTC start of the innermost active import scope (OData request or headless session).</summary>
    public static DateTime? ImportSessionStartedUtc { get; private set; }

    public static IDisposable BeginDataImportScope()
    {
        var previousAudit = SuppressAuditTrail.Value;
        var previousImport = DataImport.Value;
        SuppressAuditTrail.Value = true;
        DataImport.Value = true;

        if (Interlocked.Increment(ref scopeDepth) == 1)
            ImportSessionStartedUtc = DateTime.UtcNow;

        return new ScopeDisposable(previousAudit, previousImport);
    }

    /// <summary>Apply audit-trail suppression hooks on Object Spaces created outside XAF's normal pipeline.</summary>
    public static void ApplyImportObjectSpaceHooks(IObjectSpace objectSpace) =>
        MigrationImportAuditTrailObjectSpaceHooks.ApplyIfNeeded(objectSpace);

    private sealed class ScopeDisposable(bool previousAudit, bool previousImport) : IDisposable
    {
        public void Dispose()
        {
            SuppressAuditTrail.Value = previousAudit;
            DataImport.Value = previousImport;

            if (Interlocked.Decrement(ref scopeDepth) == 0)
                ImportSessionStartedUtc = null;
        }
    }
}
