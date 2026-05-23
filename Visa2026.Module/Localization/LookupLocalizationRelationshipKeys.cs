using System;
using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Localization;

internal static partial class LookupLocalizationKeys
{
    private static readonly HashSet<string> RelationshipSemanticKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "Grandchild", "Sister", "Father", "MotherInLaw", "Daughter", "YoungerSister", "Wife", "Husband", "Son",
    };

    private static readonly Dictionary<string, string> RelationshipNameToKey =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["baldyzy"] = "Grandchild",
            ["aýal dogany"] = "Sister",
            ["ayal dogany"] = "Sister",
            ["kakasy"] = "Father",
            ["(gaýny) aýalynyň ejesi"] = "MotherInLaw",
            ["(gayny) ayalynyn ejesi"] = "MotherInLaw",
            ["gyzy"] = "Daughter",
            ["İnisi"] = "YoungerSister",
            ["Inisi"] = "YoungerSister",
            ["inisi"] = "YoungerSister",
            ["aýaly"] = "Wife",
            ["ayaly"] = "Wife",
            ["adamsy"] = "Husband",
            ["ogly"] = "Son",
        };

    private static string ResolveRelationship(LookupBase row) =>
        ResolveCatalogKey(row, RelationshipSemanticKeys, RelationshipNameToKey);
}
