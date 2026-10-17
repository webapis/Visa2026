using System.Collections.Generic;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Editors;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Person root ListViews show reference columns as plain text.
/// Clicking Gender, Nationality, Project contract, Subcontractor, and similar
/// values must not open the lookup business object.
/// </summary>
public sealed class PersonListViewDisableObjectLinksController : ObjectViewController<ListView, Person>
{
    public PersonListViewDisableObjectLinksController()
    {
        TargetViewNesting = Nesting.Root;
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();

        if (View.Editor is not DxGridListEditor gridListEditor)
            return;

        DisableLinks(gridListEditor.PropertyEditors);
    }

    private static void DisableLinks(IEnumerable<ViewItem> viewItems)
    {
        foreach (var viewItem in viewItems)
        {
            if (viewItem is LookupPropertyEditor lookupPropertyEditor)
                lookupPropertyEditor.ShowLink = false;
            else if (viewItem is ObjectPropertyEditor objectPropertyEditor)
                objectPropertyEditor.ShowLink = false;
        }
    }
}
