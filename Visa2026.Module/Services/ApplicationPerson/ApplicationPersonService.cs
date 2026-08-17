using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
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

    /// <summary>
    /// Pins newly added person-owned records onto this case without replacing sticky links.
    /// Officers add missing data on Person detail, then Relink here.
    /// </summary>
    public static bool RelinkPerson(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person)
    {
        if (application == null || person == null)
            return false;
        if (ApplicationProfileInstancePersonRosterLockHelper.AreResolvedLinksLocked(application))
            return false;
        if (objectSpace == null)
            return false;

        var trackedPerson = person.ID != Guid.Empty
            ? objectSpace.GetObject(person) ?? person
            : person;
        var trackedApplication = application.ID != Guid.Empty
            ? objectSpace.GetObject(application) ?? application
            : application;
        if (trackedPerson == null || trackedApplication == null)
            return false;
        if (ApplicationProfileInstancePersonRosterLockHelper.AreResolvedLinksLocked(trackedApplication))
            return false;

        if (trackedPerson.ID != Guid.Empty && !objectSpace.IsNewObject(trackedPerson))
            objectSpace.ReloadObject(trackedPerson);

        ApplicationProfileInstancePersonResolver.RefreshResolvedLinks(
            objectSpace,
            trackedApplication,
            trackedPerson);

        foreach (var (kind, entity) in ApplicationProfileInstancePersonResolver.ResolveEntities(objectSpace, trackedPerson))
        {
            if (!ApplicationProfileInstancePersonResolver.IsAutoLinkEnabled(trackedApplication, kind))
                continue;
            if (entity is not BaseObject bo || bo.ID == Guid.Empty)
                continue;

            ApplicationProfileInstancePersonResolver.EnsureResolvedLink(
                objectSpace,
                trackedApplication,
                trackedPerson,
                kind,
                bo.ID);
        }

        return true;
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
