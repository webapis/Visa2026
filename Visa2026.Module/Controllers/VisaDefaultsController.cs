using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;

namespace Visa2026.Module.Controllers;

/// <summary>
/// When a new <see cref="Visa"/> is created from an <see cref="ApplicationItem"/> DetailView lookup,
/// auto-link it to the item's current passport (or the person's current passport) to satisfy required FKs.
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
        if (ApplicationItemCreationContext.TryGetApplicationItem(Frame, objectSpace, out var appItem)
            && appItem != null)
        {
            ApplicationItemVisaDefaults.TryApply(visa, appItem, objectSpace, Application);

            if (visa.Passport != null)
                ApplicationItemVisaDefaults.LockPassportEditor(View);
        }

        // Nested Passport → Visas create often links Passport after OnCreated (and may bypass the
        // Passport setter via EF fixup). Re-run Path A once the DetailView has the linked Passport.
        VisaIssuingLinkPathAMatcher.TryApplyOnce(visa);
    }
}
