using System;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Visa2026.Module.Services.ApplicationWorkspace;
using Visa2026.Module.Services.PreviewSlot;

namespace Visa2026.Blazor.Server.Services;

/// <summary>
/// Opens Issue issued-header on the preview-slot host via JS bridge + DI fallback.
/// </summary>
public static class IssueIssuedHeaderPreviewSlotOpenHelper
{
    public static Task<bool> TryOpenComposeAsync(
        XafApplication application,
        Guid applicationProfileInstanceId,
        string catalogKey,
        string? ownerViewId = null) =>
        TryOpenAsync(application, applicationProfileInstanceId, catalogKey, existingHeaderId: null, ownerViewId);

    public static async Task<bool> TryOpenAsync(
        XafApplication application,
        Guid applicationProfileInstanceId,
        string catalogKey,
        Guid? existingHeaderId,
        string? ownerViewId = null)
    {
        if (application == null
            || applicationProfileInstanceId == Guid.Empty
            || string.IsNullOrWhiteSpace(catalogKey)
            || !IssueIssuedHeaderComposeService.TryResolveKind(catalogKey, out var kind))
        {
            return false;
        }

        var catalog = IssueIssuedHeaderComposeService.CatalogKeyFor(kind);
        var headerId = existingHeaderId is Guid eh && eh != Guid.Empty ? eh : (Guid?)null;
        var existingText = headerId is Guid h ? h.ToString("D") : string.Empty;
        var request = new IssueIssuedHeaderSlotRequest
        {
            ApplicationProfileInstanceId = applicationProfileInstanceId,
            Kind = kind,
            CatalogKey = catalog,
            ExistingHeaderId = headerId,
        };

        var slotService = application.ServiceProvider?.GetService<IVisaPreviewSlotService>();
        if (slotService != null)
            await slotService.OpenIssueIssuedHeaderAsync(request, ownerViewId);
        else if (headerId == null)
            return ApplicationWorkspaceIssueIssuedHeaderOpenHelper.TryOpenCompose(
                application,
                applicationProfileInstanceId,
                catalogKey,
                ownerViewId);

        var js = application.ServiceProvider?.GetService<IJSRuntime>();
        if (js != null)
        {
            try
            {
                await js.InvokeAsync<bool>(
                    "visaPreviewDrawer.openIssueIssuedHeader",
                    applicationProfileInstanceId.ToString("D"),
                    kind.ToString(),
                    catalog,
                    ownerViewId ?? string.Empty,
                    existingText);
            }
            catch (JSException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }
        return true;
    }
}