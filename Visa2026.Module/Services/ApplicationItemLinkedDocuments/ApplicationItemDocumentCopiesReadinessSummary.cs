using System.Collections.Generic;
using Visa2026.Module.Services;

namespace Visa2026.Module.Services.ApplicationItemLinkedDocuments;

/// <summary>Aggregate slot readiness for the Document copies export bar.</summary>
public sealed class ApplicationItemDocumentCopiesReadinessSummary
{
    public int ReadySlotCount { get; init; }

    public int PartialSlotCount { get; init; }

    public int GapSlotCount { get; init; }

    public bool HasPackagingGaps => PartialSlotCount > 0 || GapSlotCount > 0;

    public static ApplicationItemDocumentCopiesReadinessSummary Compute(
        IReadOnlyList<ApplicationItemLinkedDocumentMergedGroup> mergedGroups,
        ApplicationItemDocumentPackageOptions? packageOptions = null,
        bool includeApplicationFormSlot = true)
    {
        packageOptions ??= ApplicationItemDocumentPackageOptions.CreateDefaults();

        int ready = includeApplicationFormSlot ? 1 : 0;
        int partial = 0;
        int gap = 0;

        if (mergedGroups != null)
        {
            foreach (var group in mergedGroups)
            {
                if (!ApplicationItemDocumentCopiesPackageSlotRules.IsSlotIncludedInPackage(group.SlotKey, packageOptions))
                    continue;

                if (group.Files.Count == 0)
                    gap++;
                else if (group.MissingLines.Count > 0)
                    partial++;
                else
                    ready++;
            }
        }

        return new ApplicationItemDocumentCopiesReadinessSummary
        {
            ReadySlotCount = ready,
            PartialSlotCount = partial,
            GapSlotCount = gap
        };
    }
}
