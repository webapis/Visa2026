using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationPersonRoster;

/// <summary>
/// ApplicationProfileInstance roster reads via skip-navigation <see cref="ApplicationProfileInstance.People"/>.
/// </summary>
public static class ApplicationRosterHelper
{
    public static bool IsPersonOnApplication(ApplicationProfileInstance? application, Person? person)
    {
        if (application == null || person == null)
            return false;

        var personId = person.ID;
        if (personId == Guid.Empty)
            return false;

        return application.People?.Any(p => p != null && p.ID == personId) == true;
    }

    public static IList<Person> GetRosterPeople(ApplicationProfileInstance? application)
    {
        if (application == null)
            return Array.Empty<Person>();

        return application.People?
            .Where(p => p != null)
            .Cast<Person>()
            .ToList() ?? [];
    }

    public static int GetRosterPersonCountInMemory(ApplicationProfileInstance? application)
    {
        if (application == null)
            return 0;

        return application.People?.Count(p => p != null) ?? 0;
    }

    public static int GetRosterPersonCount(IObjectSpace objectSpace, Guid applicationId)
    {
        if (objectSpace == null || applicationId == Guid.Empty)
            return 0;

        return objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
            .Where(a => a.ID == applicationId)
            .Select(a => a.People.Count)
            .FirstOrDefault();
    }

    /// <summary>
    /// Merge/PDF/report line shape: hydrated non-persistent projections from skip-navigation People.
    /// </summary>
    public static IList<ApplicationRosterMergeLine> GetMergeLineItems(IObjectSpace objectSpace, ApplicationProfileInstance application)
    {
        ArgumentNullException.ThrowIfNull(objectSpace);
        if (application == null)
            throw new ArgumentNullException(nameof(application));

        var applicationId = application.ID;
        if (applicationId == Guid.Empty)
            return [];

        var tracked = objectSpace.GetObject(application) ?? application;
        var people = tracked.People?
            .Where(p => p != null)
            .OrderBy(p => p!.LastName)
            .ThenBy(p => p!.FirstName)
            .ToList() ?? [];

        return people
            .Select(person => ApplicationProfileInstancePersonPdfPackageLineHydrator.Hydrate(objectSpace, tracked, person!))
            .ToList();
    }

    public static IList<ApplicationRosterMergeLine> GetMergeLineItems(ApplicationProfileInstance application)
    {
        if (application == null)
            return [];

        var objectSpace = ObjectSpaceHelper.Get(application);
        if (objectSpace != null && application.ID != Guid.Empty)
            return GetMergeLineItems(objectSpace, application);

        if (application.People is { Count: > 0 } people && objectSpace != null)
        {
            return people
                .Where(p => p != null)
                .Select(p => ApplicationProfileInstancePersonPdfPackageLineHydrator.Hydrate(objectSpace, application, p!))
                .ToList();
        }

        return [];
    }

    public static (ApplicationProfileInstance? Application, IList<Person> People) LoadApplicationPeople(
        IObjectSpace objectSpace,
        Guid applicationId,
        IReadOnlyList<Guid>? personIds = null)
    {
        if (objectSpace == null || applicationId == Guid.Empty)
            return (null, Array.Empty<Person>());

        var application = objectSpace.GetObjectByKey<ApplicationProfileInstance>(applicationId);
        if (application == null)
            return (null, Array.Empty<Person>());

        var people = GetRosterPeople(application);
        if (personIds is { Count: > 0 })
        {
            var set = personIds.Where(id => id != Guid.Empty).ToHashSet();
            people = people.Where(p => set.Contains(p.ID)).ToList();
        }

        return (application, people);
    }

    /// <summary>
    /// Loads people and their shared application. Pass <paramref name="applicationId"/>
    /// when the case is known — required if a person is on more than one instance
    /// (VISA2014 import). Empty id falls back to a single shared intersection.
    /// </summary>
    public static bool TryLoadSharedApplicationPeople(
        IObjectSpace objectSpace,
        IReadOnlyList<Guid> personIds,
        Guid applicationId,
        out ApplicationProfileInstance? application,
        out IList<Person> people)
    {
        application = null;
        people = Array.Empty<Person>();
        if (objectSpace == null || personIds == null || personIds.Count == 0)
            return false;

        var ids = personIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (ids.Count == 0)
            return false;

        if (applicationId != Guid.Empty)
        {
            (application, people) = LoadApplicationPeople(objectSpace, applicationId, ids);
            return application != null && people.Count == ids.Count;
        }

        HashSet<Guid>? intersection = null;
        var loaded = new List<Person>();
        foreach (var id in ids)
        {
            var person = objectSpace.GetObjectByKey<Person>(id);
            if (person == null)
                return false;

            loaded.Add(person);
            var appIds = person.ApplicationProfileInstances?
                .Select(a => a.ID)
                .Where(appId => appId != Guid.Empty)
                .ToHashSet() ?? [];
            intersection = intersection == null ? appIds : intersection.Intersect(appIds).ToHashSet();
        }

        if (intersection == null || intersection.Count != 1)
            return false;

        application = objectSpace.GetObjectByKey<ApplicationProfileInstance>(intersection.First());
        people = loaded;
        return application != null;
    }
}
