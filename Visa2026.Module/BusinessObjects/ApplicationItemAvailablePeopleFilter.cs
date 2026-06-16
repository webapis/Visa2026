using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Excludes persons already linked on sibling <see cref="ApplicationItem"/> rows for the same <see cref="Application"/>.
/// </summary>
internal static class ApplicationItemAvailablePeopleFilter
{
    internal static HashSet<Guid> GetExcludedPersonIds(
        Application? application,
        Guid currentApplicationItemId,
        IObjectSpace? objectSpace)
    {
        if (application?.ApplicationItems == null || application.ApplicationItems.Count == 0)
            return [];

        var excluded = new HashSet<Guid>();
        foreach (ApplicationItem item in application.ApplicationItems)
        {
            if (item == null
                || item.ID == currentApplicationItemId
                || item.Person?.ID is not Guid personId)
            {
                continue;
            }

            if (objectSpace != null && objectSpace.IsDeletedObject(item))
                continue;

            excluded.Add(personId);
        }

        return excluded;
    }
}
