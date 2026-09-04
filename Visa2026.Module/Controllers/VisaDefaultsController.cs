using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;

namespace Visa2026.Module.Controllers;

/// <summary>
/// When a new <see cref="Visa"/> DetailView opens from instance-side create, optionally link
/// <see cref="Visa.IssuingInvitationItem"/> once <see cref="Visa.IssuingApplicationProfileInstance"/> is set.
/// </summary>
public sealed class VisaDefaultsController : ObjectViewController<DetailView, Visa>
{
    private NewObjectViewController? _newObjectController;

    protected override void OnActivated()
    {
        base.OnActivated();

        _newObjectController = Frame.GetController<NewObjectViewController>();
        if (_newObjectController != null)
            _newObjectController.ObjectCreated += OnObjectCreated;

        ApplyDefaultsIfNeeded();
    }

    protected override void OnDeactivated()
    {
        if (_newObjectController != null)
        {
            _newObjectController.ObjectCreated -= OnObjectCreated;
            _newObjectController = null;
        }

        base.OnDeactivated();
    }

    private void OnObjectCreated(object sender, ObjectCreatedEventArgs e)
    {
        if (e.CreatedObject is Visa visa)
            ApplyDefaults(visa, View.ObjectSpace);
    }

    private void ApplyDefaultsIfNeeded()
    {
        if (View.ObjectSpace.IsNewObject(View.CurrentObject))
            ApplyDefaults((Visa)View.CurrentObject, View.ObjectSpace);
    }

    private void ApplyDefaults(Visa visa, IObjectSpace objectSpace)
    {
        _ = objectSpace;
        if (visa.IssuingApplicationProfileInstance != null)
            VisaIssuingLinkPathAMatcher.TryApplyOnce(visa);
    }
}
