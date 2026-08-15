using System;
using System.Collections.Generic;
using System.Linq;

namespace Visa2026.Module.Services.ApplicationItemLinkedDocuments;

/// <summary>
/// Groups linked-record document copies by family (Passport, Visa, …)
/// for the workspace By type catalog. Preview key is <c>Family:{family}</c>.
/// </summary>
public static class ApplicationItemDocumentCopiesTypeCatalog
{
    public const string FamilyPreviewPrefix = "Family:";

    private static readonly string[] FamilyOrder =
    [
        "Passport",
        "Education",
        "AddressOfResidence",
        "Visa",
        "Invitation",
        "WorkPermit",
        "MedicalRecord",
        "Rejection",
        "BorderZone",
        "FamilyRelationship",
    ];

    public static string? FamilyKey(string? slotKey)
    {
        if (string.IsNullOrWhiteSpace(slotKey))
            return null;

        if (FamilyOrder.Contains(slotKey, StringComparer.Ordinal))
            return slotKey;

        if (slotKey.StartsWith("AddressOfResidence.", StringComparison.Ordinal))
            return "AddressOfResidence";

        var dot = slotKey.IndexOf('.');
        if (dot <= 0)
            return null;

        var family = slotKey[..dot];
        return FamilyOrder.Contains(family, StringComparer.Ordinal) ? family : null;
    }

    public static string FamilyTitle(string familyKey) => familyKey switch
    {
        "Passport" => "Passport",
        "Education" => "Education",
        "AddressOfResidence" => "Address",
        "Visa" => "Visa",
        "Invitation" => "Invitation",
        "WorkPermit" => "Work permit",
        "MedicalRecord" => "Medical",
        "Rejection" => "Rejection",
        "BorderZone" => "Border zone",
        "FamilyRelationship" => "Family relationship",
        _ => familyKey,
    };

    public static string NavIconKey(string familyKey) => familyKey switch
    {
        "Passport" => "Passports",
        "Education" => "Education",
        "AddressOfResidence" => "Addresses",
        "WorkPermit" => "WorkPermits",
        "Invitation" => "Invitations",
        "Rejection" => "Rejections",
        "MedicalRecord" => "MedicalRecords",
        "FamilyRelationship" => "FamilyRelationDocuments",
        _ => "Documents",
    };

    public static string SectionId(string familyKey) => "type:" + familyKey;

    public static string FamilyPreviewKey(string familyKey) => FamilyPreviewPrefix + familyKey;

    public static bool TryParseFamilyPreviewKey(string? slotKey, out string familyKey)
    {
        familyKey = string.Empty;
        if (string.IsNullOrWhiteSpace(slotKey)
            || !slotKey.StartsWith(FamilyPreviewPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        familyKey = slotKey[FamilyPreviewPrefix.Length..].Trim();
        return familyKey.Length > 0;
    }

    public static string RecordCaption(string familyKey, string? slotLabel, string? sourceCaption)
    {
        if (!string.IsNullOrWhiteSpace(sourceCaption))
            return sourceCaption.Trim();

        var label = slotLabel?.Trim() ?? string.Empty;
        var title = FamilyTitle(familyKey);
        if (label.StartsWith(title, StringComparison.OrdinalIgnoreCase))
        {
            var rest = label[title.Length..].Trim(' ', '-', '—');
            if (rest.Length > 0)
                return rest;
        }

        return label.Length > 0 ? label : title;
    }

    public static IReadOnlyList<ApplicationItemDocumentCopiesTypeSection> Build(
        IReadOnlyList<ApplicationItemLinkedDocumentsLineSnapshot>? lines)
    {
        if (lines == null || lines.Count == 0)
            return Array.Empty<ApplicationItemDocumentCopiesTypeSection>();

        var rowsByFamily = new Dictionary<string, List<ApplicationItemDocumentCopiesTypeRow>>(StringComparer.Ordinal);
        foreach (var line in lines)
        {
            if (line?.Groups == null || line.ApplicationItemId == Guid.Empty)
                continue;

            var personName = ApplicationItemDocumentCopiesPersonCatalog.DisplayPersonName(line.LineLabel);
            foreach (var group in line.Groups)
            {
                var family = FamilyKey(group.SlotKey);
                if (family == null)
                    continue;

                if (!rowsByFamily.TryGetValue(family, out var rows))
                {
                    rows = [];
                    rowsByFamily[family] = rows;
                }

                var fileCount = group.Files?.Count(f => f.HasContent) ?? 0;
                rows.Add(new ApplicationItemDocumentCopiesTypeRow
                {
                    PersonId = line.ApplicationItemId,
                    PersonName = personName,
                    RecordLabel = RecordCaption(family, group.SlotLabel, group.SourceCaption),
                    SlotKey = group.SlotKey,
                    FileCount = fileCount,
                    IsReady = fileCount > 0,
                });
            }
        }

        return FamilyOrder
            .Where(rowsByFamily.ContainsKey)
            .Select(family =>
            {
                var rows = rowsByFamily[family];
                return new ApplicationItemDocumentCopiesTypeSection
                {
                    FamilyKey = family,
                    Title = FamilyTitle(family),
                    NavIconKey = NavIconKey(family),
                    Rows = rows,
                    ReadyCount = rows.Count(r => r.IsReady),
                    TotalCount = rows.Count,
                };
            })
            .ToList();
    }

    public static IReadOnlyList<ApplicationItemLinkedDocumentFileEntry> CollectFamilyFiles(
        IReadOnlyList<ApplicationItemLinkedDocumentsLineSnapshot>? lines,
        string familyKey)
    {
        if (lines == null || string.IsNullOrWhiteSpace(familyKey))
            return Array.Empty<ApplicationItemLinkedDocumentFileEntry>();

        var entries = new List<ApplicationItemLinkedDocumentFileEntry>();
        foreach (var line in lines)
        {
            if (line?.Groups == null)
                continue;

            foreach (var group in line.Groups)
            {
                if (!string.Equals(FamilyKey(group.SlotKey), familyKey, StringComparison.Ordinal))
                    continue;

                foreach (var file in group.Files ?? [])
                {
                    if (!file.HasContent || file.FileDataId == Guid.Empty)
                        continue;

                    entries.Add(new ApplicationItemLinkedDocumentFileEntry
                    {
                        ApplicationItemId = line.ApplicationItemId,
                        LineLabel = line.LineLabel ?? string.Empty,
                        File = file,
                    });
                }
            }
        }

        return entries;
    }
}

public sealed class ApplicationItemDocumentCopiesTypeSection
{
    public string FamilyKey { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string NavIconKey { get; init; } = string.Empty;

    public int ReadyCount { get; init; }

    public int TotalCount { get; init; }

    public IReadOnlyList<ApplicationItemDocumentCopiesTypeRow> Rows { get; init; } =
        Array.Empty<ApplicationItemDocumentCopiesTypeRow>();
}

public sealed class ApplicationItemDocumentCopiesTypeRow
{
    public Guid PersonId { get; init; }

    public string PersonName { get; init; } = string.Empty;

    public string RecordLabel { get; init; } = string.Empty;

    public string SlotKey { get; init; } = string.Empty;

    public int FileCount { get; init; }

    public bool IsReady { get; init; }
}