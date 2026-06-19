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
    private IVisaPreviewSlotService? _slotService;

    protected override void OnActivated()
    {
        base.OnActivated();
        _slotService = TryGetService<IVisaPreviewSlotService>();
    }

    protected override void OnDeactivated()
    {
        var slotService = _slotService;
        _slotService = null;

        var state = slotService?.State;
        var ownerViewId = VisaPreviewSlotViewHelper.ResolveOwnerViewId(View);

        if (slotService != null
            && state?.Mode != VisaPreviewSlotMode.Closed
            && !string.IsNullOrEmpty(ownerViewId)
            && string.Equals(state.OwnerViewId, ownerViewId, StringComparison.Ordinal))
        {
            _ = slotService.CloseAsync();
        }

        base.OnDeactivated();
    }

    private T? TryGetService<T>() where T : class
    {
        try
        {
            return Application?.ServiceProvider?.GetService<T>();
        }
        catch (ObjectDisposedException)
        {
            return null;
        }
    }
}
