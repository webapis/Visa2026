using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationItemLinkedDocuments;

namespace Visa2026.Module.Services.ApplicationPersonRoster;

/// <summary>
/// Document copies for <see cref="ApplicationPerson"/> roster lines via resolved links
/// (successor to per-line <see cref="ApplicationItem"/> FK snapshots).
/// </summary>
public static class ApplicationPersonLinkedDocumentsResolver
{
    public static ApplicationItemLinkedDocumentsSnapshot Resolve(
        IObjectSpace objectSpace,
        ApplicationPerson applicationPerson)
    {
        if (objectSpace == null || applicationPerson == null)
        {
            return new ApplicationItemLinkedDocumentsSnapshot
            {
                ApplicationItemId = Guid.Empty,
                Groups = Array.Empty<ApplicationItemLinkedDocumentGroup>()
            };
        }

        var projection = ApplicationPersonPdfPackageLineHydrator.Hydrate(objectSpace, applicationPerson);
        var snapshot = ApplicationItemLinkedDocumentsResolver.ResolveProjection(
            objectSpace,
            projection,
            applicationPerson.ID);
        return snapshot;
    }

    public static ApplicationItemLinkedDocumentsLineSnapshot ResolveLine(
        IObjectSpace objectSpace,
        ApplicationPerson applicationPerson)
    {
        if (applicationPerson == null)
        {
            return new ApplicationItemLinkedDocumentsLineSnapshot
            {
                ApplicationItemId = Guid.Empty,
                Groups = Array.Empty<ApplicationItemLinkedDocumentGroup>()
            };
        }

        applicationPerson = objectSpace.GetObject(applicationPerson);
        var snapshot = Resolve(objectSpace, applicationPerson);
        return new ApplicationItemLinkedDocumentsLineSnapshot
        {
            ApplicationItemId = applicationPerson.ID,
            LineLabel = applicationPerson.Person?.FullName ?? string.Empty,
            Groups = snapshot.Groups
        };
    }

    public static IReadOnlyList<ApplicationItemLinkedDocumentsLineSnapshot> ResolveMany(
        IObjectSpace objectSpace,
        IEnumerable<ApplicationPerson> applicationPeople)
    {
        ArgumentNullException.ThrowIfNull(objectSpace);
        if (applicationPeople == null)
            return Array.Empty<ApplicationItemLinkedDocumentsLineSnapshot>();

        return applicationPeople
            .Where(row => row != null)
            .Select(row => ResolveLine(objectSpace, row))
            .ToList();
    }
}
