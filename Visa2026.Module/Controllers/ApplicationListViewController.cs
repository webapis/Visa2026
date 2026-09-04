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
        TargetObjectType = typeof(ApplicationProfileInstance);
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        var newObjectController = Frame.GetController<NewObjectViewController>();
        if (newObjectController != null)
        {
            newObjectController.ObjectCreated += OnObjectCreated;
            if (newObjectController.NewObjectAction != null)
            {
                var hideNew = View.Id is
                    ApplicationProfileInstanceProgressRouteNavigation.ListViewStaged
                    or ApplicationProfileInstanceProgressRouteNavigation.ListViewInProcess;
                newObjectController.NewObjectAction.Active["StagedInProcessQueue"] = !hideNew;
            }
        }
    }

    protected override void OnDeactivated()
    {
        var newObjectController = Frame.GetController<NewObjectViewController>();
        if (newObjectController != null)
        {
            newObjectController.ObjectCreated -= OnObjectCreated;
            if (newObjectController.NewObjectAction != null)
                newObjectController.NewObjectAction.Active["StagedInProcessQueue"] = true;
        }
        base.OnDeactivated();
    }

    private void OnObjectCreated(object sender, ObjectCreatedEventArgs e)
    {
        if (e.CreatedObject is not ApplicationProfileInstance application || View == null)
            return;

        if (View.Id == ApplicationProfileInstanceProgressRouteNavigation.ListViewViaMinistries)
            application.CreationProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries;
        else if (View.Id == ApplicationProfileInstanceProgressRouteNavigation.ListViewDirectMigration)
            application.CreationProgressRoute = ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService;
    }
}
