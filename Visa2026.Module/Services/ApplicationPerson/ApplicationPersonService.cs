using System;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationPersonRoster;

public static class ApplicationPersonService
{
    public static ApplicationPerson? LinkPerson(IObjectSpace objectSpace, Application application, Person person)
    {
        if (objectSpace == null || application == null || person == null)
            return null;

        if (objectSpace.IsNewObject(application) || person.ID == Guid.Empty)
            return null;

        var applicationId = application.ID;
        var personId = person.ID;
        if (applicationId == Guid.Empty)
            return null;

        var existing = objectSpace.GetObjectsQuery<ApplicationPerson>()
            .FirstOrDefault(ap => ap.ApplicationId == applicationId && ap.PersonId == personId);
        if (existing != null)
        {
            ApplicationPersonResolver.RefreshResolvedLinks(objectSpace, existing);
            return existing;
        }

        var link = objectSpace.CreateObject<ApplicationPerson>();
        link.Application = application;
        link.Person = person;
        link.LinkedAt = DateTime.Now;
        ApplicationPersonResolver.RefreshResolvedLinks(objectSpace, link);
        return link;
    }

    public static void UnlinkPerson(IObjectSpace objectSpace, ApplicationPerson applicationPerson)
    {
        if (objectSpace == null || applicationPerson == null)
            return;

        objectSpace.Delete(applicationPerson);
    }

    public static void RefreshApplication(IObjectSpace objectSpace, Application? application)
    {
        if (objectSpace == null || application == null || objectSpace.IsNewObject(application))
            return;

        foreach (var row in application.People?.ToList() ?? [])
            ApplicationPersonResolver.RefreshResolvedLinks(objectSpace, row);
    }

    public static void RefreshApplication(IObjectSpace objectSpace, Guid applicationId)
    {
        if (objectSpace == null || applicationId == Guid.Empty)
            return;

        var application = objectSpace.GetObjectByKey<Application>(applicationId);
        RefreshApplication(objectSpace, application);
    }
}
