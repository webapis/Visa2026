using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationItemLinkedDocuments;

namespace Visa2026.Module.Services.ApplicationPersonRoster;

/// <summary>
/// Document copies for skip-navigation People via resolved links
/// (successor to per-line <see cref="ApplicationRosterMergeLine"/> FK snapshots).
/// </summary>
public static class ApplicationPersonLinkedDocumentsResolver
{
    public static ApplicationItemLinkedDocumentsSnapshot Resolve(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person)
    {
        if (objectSpace == null || application == null || person == null)
        {
            return new ApplicationItemLinkedDocumentsSnapshot
            {
                ApplicationItemId = Guid.Empty,
                Groups = Array.Empty<ApplicationItemLinkedDocumentGroup>()
            };
        }

        var projection = ApplicationProfileInstancePersonPdfPackageLineHydrator.Hydrate(objectSpace, application, person);
        var snapshot = ApplicationItemLinkedDocumentsResolver.ResolveProjection(
            objectSpace,
            projection,
            person.ID);
        return snapshot;
    }

    public static ApplicationItemLinkedDocumentsLineSnapshot ResolveLine(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person)
    {
        if (person == null)
        {
            return new ApplicationItemLinkedDocumentsLineSnapshot
            {
                ApplicationItemId = Guid.Empty,
                Groups = Array.Empty<ApplicationItemLinkedDocumentGroup>()
            };
        }

        person = objectSpace.GetObject(person);
        application = objectSpace.GetObject(application);
        var snapshot = Resolve(objectSpace, application, person);
        return new ApplicationItemLinkedDocumentsLineSnapshot
        {
            ApplicationItemId = person.ID,
            LineLabel = person.FullName ?? string.Empty,
            Groups = snapshot.Groups
        };
    }

    public static IReadOnlyList<ApplicationItemLinkedDocumentsLineSnapshot> ResolveMany(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        IEnumerable<Person> people)
    {
        ArgumentNullException.ThrowIfNull(objectSpace);
        if (application == null || people == null)
            return Array.Empty<ApplicationItemLinkedDocumentsLineSnapshot>();

        return people
            .Where(person => person != null)
            .Select(person => ResolveLine(objectSpace, application, person!))
            .ToList();
    }
}
