using System;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;
using Visa2026.Module.Services.PreviewSlot;

namespace Visa2026.Blazor.Server.Services;

/// <summary>
/// Opens issued-visa compose on the preview-slot host via JS bridge + DI fallback
/// (invitation lines or case roster, depending on the profile).
/// </summary>
public static class IssueIssuedVisaPreviewSlotOpenHelper
{
    public static async Task<bool> TryOpenComposeAsync(
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

        Guid? visaId = existingVisaId is Guid id && id != Guid.Empty ? id : null;
        var request = new IssueIssuedVisaSlotRequest
        {
            ApplicationProfileInstanceId = applicationProfileInstanceId,
            ExistingVisaId = visaId,
        };

        var slotService = application.ServiceProvider?.GetService<IVisaPreviewSlotService>();
        if (slotService != null)
            await slotService.OpenIssueIssuedVisaAsync(request, ownerViewId);
        else
            return ApplicationWorkspaceIssueIssuedVisaOpenHelper.TryOpenCompose(
                application,
                applicationProfileInstanceId,
                ownerViewId,
                visaId);

        var js = application.ServiceProvider?.GetService<IJSRuntime>();
        if (js != null)
        {
            try
            {
                await js.InvokeAsync<bool>(
                    "visaPreviewDrawer.openIssueIssuedVisa",
                    applicationProfileInstanceId.ToString("D"),
                    ownerViewId ?? string.Empty,
                    visaId is Guid v ? v.ToString("D") : string.Empty);
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