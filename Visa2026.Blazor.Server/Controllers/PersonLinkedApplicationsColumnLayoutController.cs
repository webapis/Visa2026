using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using Visa2026.Module;
using Visa2026.Module.Model;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Keeps Applications (linked) column captions readable on the Person detail form.
/// </summary>
public sealed class PersonLinkedApplicationsColumnLayoutController : ViewController<ListView>
{
    public PersonLinkedApplicationsColumnLayoutController()
    {
        TargetViewId = PersonNestedCollectionLayout.ApplicationProfileInstancesListView;
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        if (View?.Editor is not DxGridListEditor editor)
            return;

        foreach (var column in editor.GridDataColumnModels)
        {
            if (string.IsNullOrEmpty(column.FieldName)
                || !column.Visible
                || PersonLinkedApplicationsColumnWidths.UsesOwnWidth(column.FieldName))
                continue;

            var width = PersonLinkedApplicationsColumnWidths.ResolveWidth(column.FieldName);
            column.MinWidth = width;
            column.Width = width + "px";
        }
    }
}
