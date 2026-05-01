using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ReportsV2.Blazor;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Default Web Report Preview uses &quot;Whole page&quot; fit, which repeatedly refits when the
/// popup/client size changes and reads as trembling. Opening at 100% matches stable behavior
/// when users pick 100% from the toolbar.
/// </summary>
public sealed class ReportViewerDefaultZoomController : ViewController<DetailView>
{
    public ReportViewerDefaultZoomController()
    {
        TargetViewId = ReportsBlazorModuleV2.ReportViewerDetailViewName;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        View.CustomizeViewItemControl<ReportViewerViewItem>(this, static item =>
        {
            item.ReportViewerModel.Zoom = 1d;
        });
    }
}
