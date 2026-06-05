using System;
using System.Collections.Generic;
using System.Linq;

namespace Visa2026.Module.Services.ApplicationItemLinkedDocuments;

public static class ApplicationItemLinkedDocumentsMerger
{
    private sealed class LineSlotState
    {
        public Guid ApplicationItemId { get; init; }

        public string LineLabel { get; init; } = string.Empty;

        public bool LinkMissing { get; set; }

        public List<ApplicationItemLinkedDocumentFileEntry> Files { get; } = new();
    }

    public static IReadOnlyList<ApplicationItemLinkedDocumentMergedGroup> MergeBySlot(
        IReadOnlyList<ApplicationItemLinkedDocumentsLineSnapshot> lines)
    {
        if (lines == null || lines.Count == 0)
            return Array.Empty<ApplicationItemLinkedDocumentMergedGroup>();

        var slotOrder = new List<string>();
        var slotLabels = new Dictionary<string, string>(StringComparer.Ordinal);
        var lineStatesBySlot = new Dictionary<string, Dictionary<Guid, LineSlotState>>(StringComparer.Ordinal);

        foreach (var line in lines)
        {
            if (line?.Groups == null || line.ApplicationItemId == Guid.Empty)
                continue;

            foreach (var group in line.Groups)
            {
                if (string.IsNullOrWhiteSpace(group.SlotKey))
                    continue;

                if (!slotOrder.Contains(group.SlotKey))
                {
                    slotOrder.Add(group.SlotKey);
                    slotLabels[group.SlotKey] = group.SlotLabel;
                }

                if (!lineStatesBySlot.TryGetValue(group.SlotKey, out var lineStates))
                {
                    lineStates = new Dictionary<Guid, LineSlotState>();
                    lineStatesBySlot[group.SlotKey] = lineStates;
                }

                if (!lineStates.TryGetValue(line.ApplicationItemId, out var state))
                {
                    state = new LineSlotState
                    {
                        ApplicationItemId = line.ApplicationItemId,
                        LineLabel = line.LineLabel ?? string.Empty
                    };
                    lineStates[line.ApplicationItemId] = state;
                }

                if (group.LinkMissing)
                    state.LinkMissing = true;

                foreach (var file in group.Files.Where(f => f.HasContent && f.FileDataId != Guid.Empty))
                {
                    state.Files.Add(new ApplicationItemLinkedDocumentFileEntry
                    {
                        ApplicationItemId = line.ApplicationItemId,
                        LineLabel = line.LineLabel ?? string.Empty,
                        File = file
                    });
                }
            }
        }

        return slotOrder
            .Select(slotKey =>
            {
                lineStatesBySlot.TryGetValue(slotKey, out var lineStates);
                lineStates ??= new Dictionary<Guid, LineSlotState>();

                var files = lineStates.Values
                    .SelectMany(state => state.Files)
                    .ToList();

                var missingLines = lineStates.Values
                    .Where(state => state.Files.Count == 0)
                    .Select(state => new ApplicationItemLinkedDocumentMissingLineEntry
                    {
                        ApplicationItemId = state.ApplicationItemId,
                        LineLabel = state.LineLabel,
                        LinkMissing = state.LinkMissing
                    })
                    .ToList();

                int inScopeLineCount = lineStates.Count;
                int linesWithFilesCount = lineStates.Values.Count(state => state.Files.Count > 0);
                bool hasFiles = files.Count > 0;
                bool linkMissing = !hasFiles && missingLines.Count > 0 && missingLines.All(entry => entry.LinkMissing);

                return new ApplicationItemLinkedDocumentMergedGroup
                {
                    SlotKey = slotKey,
                    SlotLabel = slotLabels.TryGetValue(slotKey, out var label) ? label : slotKey,
                    Files = files,
                    MissingLines = missingLines,
                    InScopeLineCount = inScopeLineCount,
                    LinesWithFilesCount = linesWithFilesCount,
                    LinkMissing = linkMissing
                };
            })
            .ToList();
    }
}
