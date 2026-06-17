using DevExpress.ExpressApp;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.Services.PreviewSlot;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Closes the global preview slot when the officer navigates away from the current view.
/// </summary>
public sealed class VisaPreviewSlotCloseController : ViewController
{
    protected override void OnDeactivated()
    {
        var slotService = Application?.ServiceProvider?.GetService<IVisaPreviewSlotService>();
        if (slotService?.State.Mode != VisaPreviewSlotMode.Closed)
            _ = slotService!.CloseAsync();

        base.OnDeactivated();
    }
}
