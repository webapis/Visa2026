using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Sets <see cref="Application.CreationProgressRoute"/> when a new application is created
/// from a route-specific navigation ListView.
/// </summary>
public sealed class ApplicationListViewController : ViewController<ListView>
{
    public ApplicationListViewController()
    {
        TargetObjectType = typeof(Application);
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        var newObjectController = Frame.GetController<NewObjectViewController>();
        if (newObjectController != null)
            newObjectController.ObjectCreated += OnObjectCreated;
    }

    protected override void OnDeactivated()
    {
        var newObjectController = Frame.GetController<NewObjectViewController>();
        if (newObjectController != null)
            newObjectController.ObjectCreated -= OnObjectCreated;
        base.OnDeactivated();
    }

    private void OnObjectCreated(object sender, ObjectCreatedEventArgs e)
    {
        if (e.CreatedObject is not Application application || View == null)
            return;

        if (View.Id == ApplicationProgressRouteNavigation.ListViewViaMinistries)
            application.CreationProgressRoute = ApplicationProgressRouteKind.ViaMinistries;
        else if (View.Id == ApplicationProgressRouteNavigation.ListViewDirectMigration)
            application.CreationProgressRoute = ApplicationProgressRouteKind.DirectToMigrationService;
    }
}
