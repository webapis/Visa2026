using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationPersonRoster;

/// <summary>
/// Application roster reads: prefer <see cref="ApplicationPerson"/> M2M; fall back to legacy
/// <see cref="ApplicationItem"/> rows until import cutover and phase B schema removal.
/// </summary>
public static class ApplicationRosterHelper
{
    public static bool IsPersonOnApplication(Application? application, Person? person)
    {
        if (application == null || person == null)
            return false;

        var personId = person.ID;
        if (personId == Guid.Empty)
            return false;

        if (application.People?.Any(ap => ap?.Person?.ID == personId) == true)
            return true;

        return application.ApplicationItems?.Any(ai => ai?.Person?.ID == personId) == true;
    }

    public static IList<Person> GetRosterPeople(Application? application)
    {
        if (application == null)
            return Array.Empty<Person>();

        var fromM2m = application.People?
            .Select(ap => ap.Person)
            .Where(p => p != null)
            .Cast<Person>()
            .ToList();
        if (fromM2m is { Count: > 0 })
            return fromM2m;

        return application.ApplicationItems?
            .Select(ai => ai.Person)
            .Where(p => p != null)
            .Cast<Person>()
            .ToList() ?? [];
    }

    public static int GetRosterPersonCountInMemory(Application? application)
    {
        if (application == null)
            return 0;

        var m2mCount = application.People?.Count(ap => ap?.Person != null) ?? 0;
        if (m2mCount > 0)
            return m2mCount;

        return application.ApplicationItems?.Count ?? 0;
    }

    public static int GetRosterPersonCount(IObjectSpace objectSpace, Guid applicationId)
    {
        if (objectSpace == null || applicationId == Guid.Empty)
            return 0;

        var m2mCount = objectSpace.GetObjectsQuery<ApplicationPerson>()
            .Count(ap => ap.ApplicationId == applicationId);
        if (m2mCount > 0)
            return m2mCount;

        return objectSpace.GetObjectsQuery<ApplicationItem>()
            .Count(i => i.Application != null && i.Application.ID == applicationId);
    }

    /// <summary>
    /// Merge/PDF/report line shape: hydrated projections from M2M when present; otherwise legacy DB rows.
    /// Projections are not persisted.
    /// </summary>
    public static IList<ApplicationItem> GetMergeLineItems(IObjectSpace objectSpace, Application application)
    {
        ArgumentNullException.ThrowIfNull(objectSpace);
        if (application == null)
            throw new ArgumentNullException(nameof(application));

        var applicationId = application.ID;
        if (applicationId == Guid.Empty)
            return [];

        var rosterRows = objectSpace.GetObjectsQuery<ApplicationPerson>()
            .Where(ap => ap.ApplicationId == applicationId)
            .OrderBy(ap => ap.Person!.LastName)
            .ThenBy(ap => ap.Person!.FirstName)
            .ToList();

        if (rosterRows.Count > 0)
        {
            return rosterRows
                .Select(ap => ApplicationPersonPdfPackageLineHydrator.Hydrate(objectSpace, ap))
                .ToList();
        }

        return objectSpace.GetObjectsQuery<ApplicationItem>()
            .Where(i => i.Application != null && i.Application.ID == applicationId)
            .OrderBy(i => i.ApplicationItemName)
            .ToList();
    }

    public static IList<ApplicationItem> GetMergeLineItems(Application application)
    {
        if (application == null)
            return [];

        var objectSpace = ObjectSpaceHelper.Get(application);
        if (objectSpace != null && application.ID != Guid.Empty)
            return GetMergeLineItems(objectSpace, application);

        if (application.People is { Count: > 0 } people && objectSpace != null)
        {
            return people
                .Where(ap => ap != null)
                .Select(ap => ApplicationPersonPdfPackageLineHydrator.Hydrate(objectSpace, ap))
                .ToList();
        }

        return (application.ApplicationItems ?? Enumerable.Empty<ApplicationItem>())
            .Where(i => i != null)
            .ToList();
    }
}
