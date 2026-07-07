using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Warns when an archived <see cref="Person"/> is selected on an <see cref="ApplicationItem"/> line.
/// Archived persons remain in <see cref="ApplicationItem.AvailablePeople"/>; this controller does not block save.
/// </summary>
public sealed class ApplicationItemArchivedPersonWarningController : ObjectViewController<ObjectView, ApplicationItem>
{
    protected override void OnActivated()
    {
        base.OnActivated();
        ObjectSpace.ObjectChanged += ObjectSpace_ObjectChanged;
        WarnIfArchivedPerson(ViewCurrentObject);
    }

    protected override void OnDeactivated()
    {
        ObjectSpace.ObjectChanged -= ObjectSpace_ObjectChanged;
        base.OnDeactivated();
    }

    private void ObjectSpace_ObjectChanged(object sender, ObjectChangedEventArgs e)
    {
        if (e.Object is not ApplicationItem item
            || e.PropertyName != nameof(ApplicationItem.Person))
        {
            return;
        }

        WarnIfArchivedPerson(item);
    }

    private void WarnIfArchivedPerson(ApplicationItem? item)
    {
        var person = item?.Person;
        if (person is not { IsArchived: true })
            return;

        var displayName = string.IsNullOrWhiteSpace(person.FullName)
            ? person.PersonalNumber
            : person.FullName;

        Application.ShowViewStrategy.ShowMessage(
            VisaUiMessages.Format("ApplicationItem.ArchivedPersonWarning", displayName ?? string.Empty),
            InformationType.Warning,
            6000,
            InformationPosition.Top);
    }
}