using System;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationPersonRoster;

public static class ApplicationProfileInstancePersonService
{
    public static Person? LinkPerson(IObjectSpace objectSpace, ApplicationProfileInstance application, Person person)
    {
        if (objectSpace == null || application == null || person == null)
            return null;

        if (objectSpace.IsNewObject(application) || person.ID == Guid.Empty)
            return null;

        var applicationId = application.ID;
        var personId = person.ID;
        if (applicationId == Guid.Empty)
            return null;

        if (ApplicationProfileInstancePersonRosterLockHelper.AreResolvedLinksLocked(application))
            return null;

        application.People ??= new System.Collections.ObjectModel.ObservableCollection<Person>();
        var existing = application.People.FirstOrDefault(p => p != null && p.ID == personId);
        if (existing == null)
        {
            var tracked = objectSpace.GetObject(person) ?? person;
            application.People.Add(tracked);
            existing = tracked;
        }

        ApplicationProfileInstancePersonResolver.RefreshResolvedLinks(objectSpace, application, existing);
        return existing;
    }

    public static void UnlinkPerson(IObjectSpace objectSpace, ApplicationProfileInstance application, Person person)
    {
        if (objectSpace == null || application == null || person == null)
            return;

        if (ApplicationProfileInstancePersonRosterLockHelper.AreResolvedLinksLocked(application))
            return;

        var personId = person.ID;
        var tracked = application.People?.FirstOrDefault(p => p != null && p.ID == personId);
        if (tracked != null)
            application.People!.Remove(tracked);

        var applicationId = application.ID;
        if (applicationId == Guid.Empty || personId == Guid.Empty)
            return;

        var links = objectSpace.GetObjectsQuery<ApplicationProfileInstancePersonResolvedLink>()
            .Where(l => l.ApplicationProfileInstanceId == applicationId && l.PersonId == personId)
            .ToList();
        foreach (var link in links)
        {
            if (link.LinkKind is { } kind && link.LinkedObjectId is Guid linkedId && linkedId != Guid.Empty)
                ApplicationProfileInstanceChildMembership.Remove(application, kind, linkedId);
            objectSpace.Delete(link);
        }
    }

    public static void RefreshApplication(IObjectSpace objectSpace, ApplicationProfileInstance? application)
    {
        if (objectSpace == null || application == null || objectSpace.IsNewObject(application))
            return;

        if (ApplicationProfileInstancePersonRosterLockHelper.AreResolvedLinksLocked(application))
            return;

        foreach (var person in application.People?.ToList() ?? [])
        {
            if (person != null)
                ApplicationProfileInstancePersonResolver.RefreshResolvedLinks(objectSpace, application, person);
        }
    }

    public static void RefreshApplication(IObjectSpace objectSpace, Guid applicationId)
    {
        if (objectSpace == null || applicationId == Guid.Empty)
            return;

        var application = objectSpace.GetObjectByKey<ApplicationProfileInstance>(applicationId);
        RefreshApplication(objectSpace, application);
    }
}
