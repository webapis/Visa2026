using System;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.PreviewSlot;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// Opens issued-visa compose in #visa-preview-slot from case workspace + Add issued visa.
/// Invitation+visa profiles use issued invitation lines; visa-only (extension / direct) uses the case roster.
/// </summary>
public static class ApplicationWorkspaceIssueIssuedVisaOpenHelper
{
    public static bool TryOpenCompose(
        XafApplication application,
        Guid applicationProfileInstanceId,
        string? ownerViewId = null,
        Guid? existingVisaId = null)
    {
        if (application == null || applicationProfileInstanceId == Guid.Empty)
            return false;

        using (var objectSpace = application.CreateObjectSpace(typeof(ApplicationProfileInstance)))
        {
            var instance = objectSpace.GetObjectByKey<ApplicationProfileInstance>(applicationProfileInstanceId);
            if (!IssueIssuedVisaComposeService.CanOpenInSlot(instance))
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

        slotService.OpenIssueIssuedVisaAsync(new IssueIssuedVisaSlotRequest
        {
            ApplicationProfileInstanceId = applicationProfileInstanceId,
            ExistingVisaId = existingVisaId is Guid visaId && visaId != Guid.Empty ? visaId : null,
        }, ownerViewId).GetAwaiter().GetResult();

        return true;
    }
}