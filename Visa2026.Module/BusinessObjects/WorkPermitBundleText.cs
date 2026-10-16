using System;
using System.Collections.Generic;

namespace Visa2026.Module.BusinessObjects;

/// <summary>One valid work permit inside an A Maglumat person row.</summary>
public sealed class WorkPermitBundleSlot
{
    public Guid HeaderId { get; init; }
    public Guid ItemId { get; init; }
    public string Number { get; init; } = string.Empty;
    public string Start { get; init; } = string.Empty;
    public string End { get; init; } = string.Empty;
}

/// <summary>
/// WorkPermitBundle column: one permit per line, fields separated by the ASCII record separator.
/// Order is newest start date first.
/// </summary>
public static class WorkPermitBundleText
{
    private const char Field = '\u001e';

    public static IReadOnlyList<WorkPermitBundleSlot> Parse(string? bundle)
    {
        if (string.IsNullOrWhiteSpace(bundle))
            return Array.Empty<WorkPermitBundleSlot>();

        var slots = new List<WorkPermitBundleSlot>();
        foreach (var line in bundle.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split(Field);
            if (parts.Length < 5)
                continue;
            slots.Add(new WorkPermitBundleSlot
            {
                HeaderId = Guid.TryParse(parts[0], out var header) ? header : Guid.Empty,
                ItemId = Guid.TryParse(parts[1], out var item) ? item : Guid.Empty,
                Number = parts[2].Trim(),
                Start = parts[3].Trim(),
                End = parts[4].Trim()
            });
        }

        return slots;
    }

    public static string Display(string? bundle)
    {
        var blocks = new List<string>();
        foreach (var slot in Parse(bundle))
        {
            var lines = new List<string>();
            if (slot.Number.Length > 0)
                lines.Add(slot.Number);
            if (slot.Start.Length > 0)
                lines.Add(slot.Start);
            if (slot.End.Length > 0)
                lines.Add(slot.End);
            if (lines.Count > 0)
                blocks.Add(string.Join("\n", lines));
        }

        return string.Join("\n\n", blocks);
    }
}