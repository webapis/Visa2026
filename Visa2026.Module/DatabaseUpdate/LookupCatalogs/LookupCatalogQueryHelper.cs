using System;
using System.Linq;
using DevExpress.ExpressApp;

namespace Visa2026.Module.DatabaseUpdate.LookupCatalogs;

internal static class LookupCatalogQueryHelper
{
    public static IQueryable AsQueryable(IObjectSpace objectSpace, Type entityType)
    {
        var method = typeof(IObjectSpace).GetMethods()
            .First(m => m.Name == nameof(IObjectSpace.GetObjectsQuery)
                && m.IsGenericMethodDefinition
                && m.GetParameters().Length == 0);
        var generic = method.MakeGenericMethod(entityType);
        return (IQueryable)generic.Invoke(objectSpace, null)!;
    }

    public static object? FirstOrDefault(IObjectSpace objectSpace, Type entityType, Func<object, bool> predicate)
    {
        foreach (var item in AsQueryable(objectSpace, entityType))
        {
            if (predicate(item))
                return item;
        }

        return null;
    }
}
