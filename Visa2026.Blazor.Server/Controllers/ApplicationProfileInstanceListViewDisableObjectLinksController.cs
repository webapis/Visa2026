using System.Collections.Generic;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Editors;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Application Profile Instance ListViews (via ministry / direct / staged / in-process clones)
/// show reference columns as plain text — no Lookup/Object hyperlinks.
/// Row activate still opens the case workspace.
/// </summary>
public sealed class ApplicationProfileInstanceListViewDisableObjectLinksController
    : ObjectViewController<ListView, ApplicationProfileInstance>
{
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