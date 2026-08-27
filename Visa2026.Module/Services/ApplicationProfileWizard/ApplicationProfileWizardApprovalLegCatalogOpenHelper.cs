using System;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.Services.PreviewSlot;

namespace Visa2026.Module.Services.ApplicationProfileWizard;

/// <summary>
/// Opens the shared Approval leg profile catalog in <c>#visa-preview-slot</c> from the Application Profile wizard.
/// </summary>
public static class ApplicationProfileWizardApprovalLegCatalogOpenHelper
{
    public static bool TryOpen(XafApplication application, Action? onChanged = null, string? ownerViewId = null)
    {
        if (application == null)
            return false;

        var slotService = application.ServiceProvider?.GetService<IVisaPreviewSlotService>();
        if (slotService == null)
            return false;

        if (onChanged != null)
        {
            var notifier = application.ServiceProvider.GetService<IApprovalLegCatalogChangeNotifier>();
            if (notifier != null)
            {
                notifier.Changed -= onChanged;
                notifier.Changed += onChanged;
            }
        }

        _ = OpenAsync(slotService, ownerViewId);
        return true;
    }

    private static Task OpenAsync(IVisaPreviewSlotService slotService, string? ownerViewId) =>
        slotService.OpenApprovalLegCatalogAsync(new ApprovalLegCatalogSlotRequest(), ownerViewId);
}
