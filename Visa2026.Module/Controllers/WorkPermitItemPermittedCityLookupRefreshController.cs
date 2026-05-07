using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

// Blazor lookup editors can cache the data source.
// When switching between the "mostly used" and "all" City editors, force a view refresh so the lookup re-queries.
public class WorkPermitItemPermittedCityLookupRefreshController : ViewController<DetailView> {
    public WorkPermitItemPermittedCityLookupRefreshController() {
        TargetObjectType = typeof(WorkPermitItemPermittedCity);
    }

    protected override void OnActivated() {
        base.OnActivated();
        ObjectSpace.ObjectChanged += ObjectSpace_ObjectChanged;
    }

    private void ObjectSpace_ObjectChanged(object sender, ObjectChangedEventArgs e) {
        if (e.Object is WorkPermitItemPermittedCity && e.PropertyName == nameof(WorkPermitItemPermittedCity.ShowMostlyUsedOnly)) {
            // Ensure lookup dropdown re-queries its DataSourceProperty.
            // View.Refresh() alone may not invalidate the cached items list in Blazor.
            if (View != null) {
                var cityEditor = View.FindItem(nameof(WorkPermitItemPermittedCity.City)) as PropertyEditor;
                cityEditor?.Refresh();
                View.Refresh();
            }
        }
    }

    protected override void OnDeactivated() {
        ObjectSpace.ObjectChanged -= ObjectSpace_ObjectChanged;
        base.OnDeactivated();
    }
}

