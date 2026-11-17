using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Visa2026.Module.BusinessObjects.PersonIntake;

namespace Visa2026.Module.Services.PersonIntake;

public static class PersonIntakeWizardOpenHelper
{
    public static DetailView? OpenNew(XafApplication application, PersonRecordRole role)
    {
        if (application == null)
            return null;

        var personSpace = application.CreateObjectSpace(typeof(Person));
        var person = personSpace.CreateObject<Person>();
        PersonRoleHelper.ApplyRole(person, role);
        if (role == PersonRecordRole.Employee)
            VisaFamilyMemberLinesHelper.ApplyEmployeeDefaultIfEmpty(person);

        return CreateHostView(application, personSpace, person, role);
    }

    public static DetailView? OpenExisting(XafApplication application, Person person)
    {
        if (application == null || person == null)
            return null;

        var personSpace = application.CreateObjectSpace(typeof(Person));
        var loaded = personSpace.GetObject(person);
        if (loaded == null)
            return null;

        return CreateHostView(application, personSpace, loaded, loaded.PersonRole);
    }

    private static DetailView CreateHostView(
        XafApplication application,
        IObjectSpace personSpace,
        Person person,
        PersonRecordRole role)
    {
        var hostSpace = application.CreateObjectSpace(typeof(PersonIntakeWizardHost));
        var host = hostSpace.CreateObject<PersonIntakeWizardHost>();
        host.Person = person;
        host.PersonObjectSpace = personSpace;
        host.Role = role;
        host.PersonId = person.ID;

        var detailView = application.CreateDetailView(hostSpace, host);
        detailView.ViewEditMode = ViewEditMode.Edit;
        return detailView;
    }
}
