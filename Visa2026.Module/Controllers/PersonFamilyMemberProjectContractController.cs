using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

/// <summary>
/// When a family member's sponsoring employee changes, copy their <see cref="ProjectContract"/>.
/// </summary>
public sealed class PersonFamilyMemberProjectContractController : ObjectViewController<DetailView, Person>
{
    protected override void OnActivated()
    {
        base.OnActivated();
        ObjectSpace.ObjectChanged += ObjectSpace_ObjectChanged;
    }

    protected override void OnDeactivated()
    {
        ObjectSpace.ObjectChanged -= ObjectSpace_ObjectChanged;
        base.OnDeactivated();
    }

    private void ObjectSpace_ObjectChanged(object sender, ObjectChangedEventArgs e)
    {
        if (e.Object != View.CurrentObject || e.Object is not Person person)
            return;

        if (!string.Equals(e.PropertyName, nameof(Person.SponsoringEmployee), StringComparison.Ordinal))
            return;

        person.SyncProjectContractFromSponsoringEmployee();
    }
}
