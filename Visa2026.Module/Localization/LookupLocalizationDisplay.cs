using System;
using System.Linq;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Localization;

/// <summary>Helpers for global lookup UI display (see <see cref="GlobalLookupCatalogBase"/>).</summary>
public static class LookupLocalizationDisplay
{
    public static bool UsesLocalizedDisplay(Type? lookupType) =>
        lookupType != null && LocalizedLookupTypes.All.Contains(lookupType);
}
