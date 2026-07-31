using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Blazor.SystemModule;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Nested DetailView collection ListViews: show all rows and disable virtual scrolling so the
/// grid grows with content instead of sitting in a short inner-scroll viewport (global Options
/// VirtualScrollingEnabled + site.css flex rules). Lookup pickers are skipped.
/// </summary>
public sealed class NestedListViewShowAllRowsController : ViewController<ListView>
{
    private const string NestedShowAllRowsCssClass = "xaf-nested-show-all-rows";

    public NestedListViewShowAllRowsController()
    {
        TargetViewNesting = Nesting.Nested;
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        if (ShouldSkipListView())
            return;

        if (View.Model is IModelListViewBlazor blazorModel)
        {
            blazorModel.ShowAllRows = true;
            blazorModel.VirtualScrollingEnabled = false;
        }

        if (View.Editor is DxGridListEditor { GridModel: { } gridModel })
        {
            gridModel.CssClass = AppendCssClass(gridModel.CssClass, NestedShowAllRowsCssClass);
        }
    }

    private bool ShouldSkipListView() =>
        View.Id.EndsWith("_LookupListView", StringComparison.Ordinal);

    private static string AppendCssClass(string? existing, string cssClass)
    {
        if (string.IsNullOrWhiteSpace(existing))
            return cssClass;
        if (existing.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Any(part => string.Equals(part, cssClass, StringComparison.Ordinal)))
            return existing;
        return $"{existing} {cssClass}";
    }
}