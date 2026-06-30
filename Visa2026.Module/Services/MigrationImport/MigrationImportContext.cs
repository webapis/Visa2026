namespace Visa2026.Module.Services.MigrationImport;

/// <summary>
/// Request-scoped flag for VISA2014 / DataImporter OData loads.
/// Set by <c>X-Visa2014-DataImport</c> on API requests; read when Object Spaces are created.
/// </summary>
public static class MigrationImportContext
{
    public const string DataImportHeaderName = "X-Visa2014-DataImport";

    private static readonly AsyncLocal<bool> SuppressAuditTrail = new();

    public static bool IsAuditTrailSuppressed => SuppressAuditTrail.Value;

    public static IDisposable BeginDataImportScope()
    {
        var previous = SuppressAuditTrail.Value;
        SuppressAuditTrail.Value = true;
        return new ScopeDisposable(previous);
    }

    private sealed class ScopeDisposable(bool previous) : IDisposable
    {
        public void Dispose() => SuppressAuditTrail.Value = previous;
    }
}
