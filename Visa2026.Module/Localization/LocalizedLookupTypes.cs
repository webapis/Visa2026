using System;
using System.Collections.Generic;
using System.Linq;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Localization;

/// <summary>Lookup BO types that use <see cref="LookupBase.LocalizedDisplayName"/> in UI pickers.</summary>
internal static class LocalizedLookupTypes
{
    private static readonly Lazy<IReadOnlyList<Type>> TypesLazy = new(GetTypes, isThreadSafe: true);

    public static IReadOnlyList<Type> All => TypesLazy.Value;

    private static IReadOnlyList<Type> GetTypes() =>
        typeof(LookupBase).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && (typeof(GlobalLookupCatalogBase).IsAssignableFrom(t)
                            || t == typeof(ApplicationType)))
            .ToList();
}
