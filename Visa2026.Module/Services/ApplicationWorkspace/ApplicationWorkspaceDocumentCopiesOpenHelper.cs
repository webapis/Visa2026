using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.PreviewSlot;

namespace Visa2026.Module.Services.ApplicationWorkspace;

public static class ApplicationWorkspaceDocumentCopiesOpenHelper
{
    public static bool TryOpen(
        XafApplication application,
        Guid applicationId,
        IReadOnlyList<Guid> applicationPersonIds,
        string? ownerViewId = null)
    {
        if (application == null)
            return false;

        var rowIds = applicationPersonIds?
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList() ?? [];

        if (applicationId == Guid.Empty || rowIds.Count == 0)
        {
            application.ShowViewStrategy.ShowMessage(
                VisaUiMessages.Get("Pdf.SelectAtLeastOneItem"),
                InformationType.Warning);
            return false;
        }

        var slotService = application.ServiceProvider?.GetService(typeof(IVisaPreviewSlotService)) as IVisaPreviewSlotService;
        if (slotService == null)
        {
            application.ShowViewStrategy.ShowMessage(
                VisaUiMessages.Get("ApplicationItemDocumentCopies.Preview.Error"),
                InformationType.Error);
            return false;
        }

        slotService.OpenDocumentCopiesAsync(new DocumentCopiesSlotRequest
        {
            ApplicationProfileInstanceId = applicationId,
            ApplicationProfileInstancePersonIds = rowIds,
        }, ownerViewId).GetAwaiter().GetResult();

        return true;
    }
}
