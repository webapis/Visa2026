using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Shared defaults when a person-scoped object is created from an <see cref="ApplicationItem"/> lookup.
/// </summary>
public static class ApplicationItemPersonLinkedDefaults
{
    public const string LockedByApplicationItemKey = "ApplicationItemLookup";

    public static bool TryApply(
        BaseObject linkedObject,
        ApplicationItem appItem,
        IObjectSpace childObjectSpace,
        XafApplication? application)
    {
        if (!TryGetPerson(linkedObject, out var getPerson, out var setPerson))
            return false;

        if (getPerson() != null)
            return true;

        if (appItem.Person == null)
        {
            application?.ShowViewStrategy.ShowMessage(
                "Please select Person on the application line first.",
                InformationType.Warning);
            return false;
        }

        var parentObjectSpace = ObjectSpaceHelper.Get(appItem) ?? childObjectSpace;
        var person = parentObjectSpace.GetObject(appItem.Person);
        setPerson(childObjectSpace.GetObject(person));
        return true;
    }

    public static void LockPersonEditor(DetailView detailView) =>
        LockPropertyEditor(detailView, nameof(Person));

    public static void LockPropertyEditor(DetailView detailView, string propertyName)
    {
        if (detailView.FindItem(propertyName) is PropertyEditor editor)
            editor.AllowEdit[LockedByApplicationItemKey] = false;
    }

    private static bool TryGetPerson(
        BaseObject linkedObject,
        out Func<Person?> getPerson,
        out Action<Person> setPerson)
    {
        getPerson = () => null;
        setPerson = _ => { };

        switch (linkedObject)
        {
            case Passport passport:
                getPerson = () => passport.Person;
                setPerson = person => passport.Person = person;
                return true;
            case EmployeePositionHistory positionHistory:
                getPerson = () => positionHistory.Person;
                setPerson = person => positionHistory.Person = person;
                return true;
            case Education education:
                getPerson = () => education.Person;
                setPerson = person => education.Person = person;
                return true;
            case AddressOfResidence address:
                getPerson = () => address.Person;
                setPerson = person => address.Person = person;
                return true;
            case MedicalRecord medicalRecord:
                getPerson = () => medicalRecord.Person;
                setPerson = person => medicalRecord.Person = person;
                return true;
            case WorkDuty workDuty:
                getPerson = () => workDuty.Person;
                setPerson = person => workDuty.Person = person;
                return true;
            case EmployeeSalary salary:
                getPerson = () => salary.Person;
                setPerson = person => salary.Person = person;
                return true;
            default:
                return false;
        }
    }
}
