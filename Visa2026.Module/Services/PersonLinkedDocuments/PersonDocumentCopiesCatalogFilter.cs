using System;
using System.Collections.Generic;
using System.Linq;

namespace Visa2026.Module.Services.PersonLinkedDocuments;

/// <summary>
/// Default Person document copies catalog: current records first; history on demand per section.
/// </summary>
public static class PersonDocumentCopiesCatalogFilter
{
    public const int MaxUncategorizedPreview = 5;

    private static readonly HashSet<string> SectionsWithoutCurrent = new(StringComparer.Ordinal)
    {
        "PersonDocuments",
        "FamilyRelationDocuments"
    };

    public static bool UsesCurrentFilter(string sectionId) =>
        !SectionsWithoutCurrent.Contains(sectionId);

    public static IReadOnlyList<PersonLinkedDocumentRecord> GetVisibleRecords(
        PersonLinkedDocumentSection section,
        bool showAllDocuments,
        bool sectionExpanded)
    {
        var records = section.Records;
        if (records.Count == 0)
            return Array.Empty<PersonLinkedDocumentRecord>();

        if (showAllDocuments || sectionExpanded)
            return OrderCurrentFirst(records);

        if (!UsesCurrentFilter(section.SectionId))
        {
            if (records.Count <= MaxUncategorizedPreview)
                return records;

            return records.Take(MaxUncategorizedPreview).ToList();
        }

        var current = records.Where(record => record.IsCurrent).ToList();
        if (current.Count > 0)
            return OrderCurrentFirst(current);

        return new[] { records[0] };
    }

    public static int GetHiddenCount(
        PersonLinkedDocumentSection section,
        bool showAllDocuments,
        bool sectionExpanded)
    {
        if (showAllDocuments || sectionExpanded)
            return 0;

        var visible = GetVisibleRecords(section, showAllDocuments: false, sectionExpanded: false);
        return Math.Max(0, section.Records.Count - visible.Count);
    }

    public static bool ShowsRecentFallback(
        PersonLinkedDocumentSection section,
        bool showAllDocuments,
        bool sectionExpanded)
    {
        if (showAllDocuments || sectionExpanded || !UsesCurrentFilter(section.SectionId))
            return false;

        return !section.Records.Any(record => record.IsCurrent) && section.Records.Count > 1;
    }

    private static IReadOnlyList<PersonLinkedDocumentRecord> OrderCurrentFirst(
        IReadOnlyList<PersonLinkedDocumentRecord> records) =>
        records
            .Select((record, index) => (record, index))
            .OrderByDescending(tuple => tuple.record.IsCurrent)
            .ThenBy(tuple => tuple.index)
            .Select(tuple => tuple.record)
            .ToList();
}
