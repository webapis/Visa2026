using DevExpress.ExpressApp;

namespace Visa2026.Module.Services.PreviewSlot;

/// <summary>
/// Maps XAF views to preview-slot owner ids for auto-close on deactivate.
/// </summary>
public static class VisaPreviewSlotViewHelper
{
    public static string ResolveOwnerViewId(View? view) =>
        string.IsNullOrWhiteSpace(view?.Id) ? string.Empty : view.Id.Trim();
}
