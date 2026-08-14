using DevExpress.ExpressApp;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.Services.PreviewSlot;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Closes the global preview slot when the view that opened it deactivates.
/// A new open from another view preempts content without requiring close first.
/// </summary>
public sealed class VisaPreviewSlotCloseController : ViewController
{
    protected override void OnDeactivated()
    {
        try
        {
            var slotService = Application?.ServiceProvider?.GetService<IVisaPreviewSlotService>();
            var state = slotService?.State;
            var ownerViewId = VisaPreviewSlotViewHelper.ResolveOwnerViewId(View);

            if (slotService != null
                && state?.Mode != VisaPreviewSlotMode.Closed
                && !string.IsNullOrEmpty(ownerViewId)
                && string.Equals(state.OwnerViewId, ownerViewId, StringComparison.Ordinal))
            {
                _ = slotService.CloseAsync();
            }
        }
        catch (ObjectDisposedException)
        {
            // ApplicationProfileInstance scope may already be torn down during host shutdown.
        }

        base.OnDeactivated();
    }
}
