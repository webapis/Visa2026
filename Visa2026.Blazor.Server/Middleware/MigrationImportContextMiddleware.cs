using Microsoft.AspNetCore.Http;
using Visa2026.Module.Services.MigrationImport;

namespace Visa2026.Blazor.Server.Middleware;

/// <summary>
/// Marks DataImporter OData requests so migration hooks (audit trail off, etc.) apply per Object Space.
/// </summary>
public sealed class MigrationImportContextMiddleware
{
    private readonly RequestDelegate next;

    public MigrationImportContextMiddleware(RequestDelegate next) => this.next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsDataImportRequest(context))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        using (MigrationImportContext.BeginDataImportScope())
            await next(context).ConfigureAwait(false);
    }

    private static bool IsDataImportRequest(HttpContext context) =>
        string.Equals(
            context.Request.Headers[MigrationImportContext.DataImportHeaderName].FirstOrDefault(),
            "true",
            StringComparison.OrdinalIgnoreCase);
}
