using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

/// <summary>
/// When New is used on nested Invitation / WorkPermit / BorderZone / Rejection / Issued visa lists under an
/// ApplicationProfileInstance, set the issuing FK immediately (1:N, not skip-nav).
/// </summary>
public sealed class IssuedHeaderNestedCreateController : ViewController<ListView>
{
    public IssuedHeaderNestedCreateController()
    {
        TargetViewNesting = Nesting.Nested;
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
        if (View?.CollectionSource is not PropertyCollectionSource pcs)
            return;
        if (pcs.MasterObject is not ApplicationProfileInstance instance)
            return;

        switch (e.CreatedObject)
        {
            case Invitation invitation:
                invitation.ApplicationProfileInstance ??= instance;
                break;
            case WorkPermit workPermit:
                workPermit.ApplicationProfileInstance ??= instance;
                break;
            case BorderZone borderZone:
                borderZone.ApplicationProfileInstance ??= instance;
                break;
            case Rejection rejection:
                rejection.ApplicationProfileInstance ??= instance;
                break;
            case Visa visa:
                visa.IssuingApplicationProfileInstance ??= instance;
                break;
        }
    }
}