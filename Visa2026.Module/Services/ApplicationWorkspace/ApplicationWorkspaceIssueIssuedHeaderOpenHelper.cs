using System;
using DevExpress.ExpressApp;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.PreviewSlot;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// Opens the Issue issued-header preview-slot compose UI from case workspace New buttons
/// (Invitation / WorkPermit / Rejection / BorderZone). Issued visa still uses modal <see cref="ApplicationWorkspaceIssuedHeaderOpenHelper.TryCreate"/>.
/// </summary>
public static class ApplicationWorkspaceIssueIssuedHeaderOpenHelper
{
    public static bool TryOpenCompose(
        XafApplication application,
        Guid applicationProfileInstanceId,
        string catalogKey,
        string? ownerViewId = null)
    {
        if (application == null
            || applicationProfileInstanceId == Guid.Empty
            || string.IsNullOrWhiteSpace(catalogKey))
        {
            return false;
        }

        if (!IssueIssuedHeaderComposeService.TryResolveKind(catalogKey, out var kind))
            return false;

        var slotService = application.ServiceProvider?.GetService(typeof(IVisaPreviewSlotService)) as IVisaPreviewSlotService;
        if (slotService == null)
        {
            application.ShowViewStrategy.ShowMessage(
                VisaUiMessages.Get("ApplicationItemDocumentCopies.Preview.Error"),
                InformationType.Error);
            return false;
        }

        slotService.OpenIssueIssuedHeaderAsync(new IssueIssuedHeaderSlotRequest
        {
            ApplicationProfileInstanceId = applicationProfileInstanceId,
            Kind = kind,
            CatalogKey = IssueIssuedHeaderComposeService.CatalogKeyFor(kind),
        }, ownerViewId).GetAwaiter().GetResult();

        return true;
    }
}