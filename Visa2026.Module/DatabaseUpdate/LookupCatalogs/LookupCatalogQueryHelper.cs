using System;
using System.Linq;
using DevExpress.ExpressApp;

namespace Visa2026.Module.DatabaseUpdate.LookupCatalogs;

internal static class LookupCatalogQueryHelper
{
    public static object? FirstOrDefault(IObjectSpace objectSpace, Type entityType, Func<object, bool> predicate)
    {
        foreach (var item in objectSpace.GetObjects(entityType))
        {
            if (predicate(item))
                return item;
        }

        return null;
    }
}
