using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Fallback for nested / non-Blazor lookup creation: prefill person-scoped fields on objects
/// created from <see cref="ApplicationItem"/> reference lookups.
/// </summary>
public sealed class ApplicationItemPersonLinkedDefaultsController : ViewController<DetailView>
{
    private static readonly Type[] SupportedTypes =
    {
        typeof(Passport),
        typeof(EmployeePositionHistory),
        typeof(Education),
        typeof(AddressOfResidence),
        typeof(MedicalRecord),
        typeof(WorkDuty),
        typeof(EmployeeSalary),
    };

    private NewObjectViewController? _newObjectController;

    public ApplicationItemPersonLinkedDefaultsController()
    {
        TypeOfView = typeof(DetailView);
    }

    protected override void OnActivated()
    {
        base.OnActivated();

        if (!SupportedTypes.Contains(View.ObjectTypeInfo.Type))
            return;

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
        if (e.CreatedObject is BaseObject linkedObject && SupportedTypes.Contains(linkedObject.GetType()))
            ApplyDefaults(linkedObject, View.ObjectSpace);
    }

    private void ApplyDefaultsIfNeeded()
    {
        if (View.CurrentObject is BaseObject linkedObject
            && View.ObjectSpace.IsNewObject(linkedObject)
            && SupportedTypes.Contains(linkedObject.GetType()))
        {
            ApplyDefaults(linkedObject, View.ObjectSpace);
        }
    }

    private void ApplyDefaults(BaseObject linkedObject, IObjectSpace objectSpace)
    {
        if (!ApplicationItemCreationContext.TryGetApplicationItem(Frame, objectSpace, out var appItem)
            || appItem == null)
        {
            return;
        }

        if (!ApplicationItemPersonLinkedDefaults.TryApply(linkedObject, appItem, objectSpace, Application))
            return;

        ApplicationItemPersonLinkedDefaults.LockPersonEditor(View);
    }
}
