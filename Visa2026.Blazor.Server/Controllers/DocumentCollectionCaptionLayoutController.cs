using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Layout;
using Visa2026.Module;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Applies parent-specific captions to <c>Documents</c> / <c>Documents_Group</c> layout nodes
/// (tabs and non-tab panels). Property names stay <c>Documents</c>.
/// </summary>
public sealed class DocumentCollectionCaptionLayoutController : ViewController<DetailView>
{
    protected override void OnActivated()
    {
        base.OnActivated();
        if (DocumentCollectionTabCaptionHelper.TryGetCaptionKey(View.Id) is null)
            return;

        if (View.LayoutManager is BlazorLayoutManager layoutManager)
            layoutManager.ItemCreated += OnLayoutItemCreated;
    }

    protected override void OnDeactivated()
    {
        if (View.LayoutManager is BlazorLayoutManager layoutManager)
            layoutManager.ItemCreated -= OnLayoutItemCreated;
        base.OnDeactivated();
    }

    private void OnLayoutItemCreated(object? sender, BlazorLayoutManager.ItemCreatedEventArgs e)
    {
        if (!DocumentCollectionTabCaptionHelper.IsDocumentsLayoutId(e.ModelLayoutElement.Id))
            return;

        var caption = DocumentCollectionTabCaptionHelper.TryGetBaseCaptionForDetailView(View.Id);
        if (string.IsNullOrEmpty(caption))
            return;

        switch (e.LayoutControlItem)
        {
            case DxFormLayoutTabPageModel tabPage:
                tabPage.Caption = caption;
                break;
            case DxFormLayoutGroupModel group:
                group.Caption = caption;
                break;
        }
    }
}