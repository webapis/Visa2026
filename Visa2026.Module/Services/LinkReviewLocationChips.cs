using System;
using System.Collections.Generic;
using System.Linq;

namespace Visa2026.Module.Services;

public enum LinkReviewLocationChipSource
{
    Joined = 0,
    Case = 1,
    Visa = 2,
    WorkPermitItem = 3,
    Manual = 4,
}

public sealed class PersonLinkReviewLocationChip
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Value { get; init; } = string.Empty;
    public LinkReviewLocationChipSource Source { get; init; }
    /// <summary>Hidden for this Join only; does not clear Case/Visa/WP source data.</summary>
    public bool Hidden { get; set; }
}

/// <summary>
/// Builds and merges requested-location chips for the Linked records dialog.
/// Before the first Join: Case / Visa / WP stay separate as suggestions.
/// After Join: only the persisted case↔person row is shown (officer can still Add).
/// </summary>
public static class LinkReviewLocationChips
{
    public static IReadOnlyList<PersonLinkReviewLocationChip> Build(
        string? joinedLocations,
        string? caseLocations,
        string? visaLocations,
        string? workPermitItemLocations)
    {
        var chips = new List<PersonLinkReviewLocationChip>();
        // Persisted Join wins: do not re-surface Case/Visa/WP chips the officer already removed.
        if (HasTokens(joinedLocations))
        {
            AppendDistinct(chips, joinedLocations, LinkReviewLocationChipSource.Joined);
            return chips;
        }

        AppendDistinct(chips, caseLocations, LinkReviewLocationChipSource.Case);
        AppendDistinct(chips, visaLocations, LinkReviewLocationChipSource.Visa);
        AppendDistinct(chips, workPermitItemLocations, LinkReviewLocationChipSource.WorkPermitItem);
        return chips;
    }

    /// <summary>Backward-compatible build without a joined row (Case first).</summary>
    public static IReadOnlyList<PersonLinkReviewLocationChip> Build(
        string? caseLocations,
        string? visaLocations,
        string? workPermitItemLocations) =>
        Build(null, caseLocations, visaLocations, workPermitItemLocations);

    public static void Hide(PersonLinkReviewLocationChip chip)
    {
        if (chip != null)
            chip.Hidden = true;
    }

    public static void AddManual(IList<PersonLinkReviewLocationChip> chips, string? locations)
    {
        if (chips == null)
            return;
        foreach (var token in Split(locations))
        {
            var existing = chips.FirstOrDefault(c => string.Equals(c.Value, token, StringComparison.Ordinal));
            if (existing != null)
            {
                existing.Hidden = false;
                continue;
            }

            chips.Add(new PersonLinkReviewLocationChip
            {
                Value = token,
                Source = LinkReviewLocationChipSource.Manual,
            });
        }
    }

    public static string JoinVisible(IEnumerable<PersonLinkReviewLocationChip>? chips)
    {
        if (chips == null)
            return string.Empty;
        var values = chips
            .Where(c => c != null && !c.Hidden && !string.IsNullOrWhiteSpace(c.Value))
            .Select(c => c.Value.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        return values.Count == 0 ? string.Empty : string.Join(", ", values);
    }

    public static string DisplaySummary(IEnumerable<PersonLinkReviewLocationChip>? chips)
    {
        var joined = JoinVisible(chips);
        return string.IsNullOrWhiteSpace(joined) ? "—" : joined;
    }

    /// <summary>Tile/field text prefers the persisted joined row.</summary>
    public static string DisplayPreferJoined(string? joinedLocations, IEnumerable<PersonLinkReviewLocationChip>? chips)
    {
        if (HasTokens(joinedLocations))
            return joinedLocations!.Trim();
        return DisplaySummary(chips);
    }

    public static string SourceCssClass(LinkReviewLocationChipSource source) => source switch
    {
        LinkReviewLocationChipSource.Joined => "is-joined",
        LinkReviewLocationChipSource.Case => "is-case",
        LinkReviewLocationChipSource.Visa => "is-visa",
        LinkReviewLocationChipSource.WorkPermitItem => "is-wp",
        LinkReviewLocationChipSource.Manual => "is-manual",
        _ => "is-manual",
    };

    private static bool HasTokens(string? stored) => Split(stored).Any();

    private static void AppendDistinct(
        List<PersonLinkReviewLocationChip> chips,
        string? stored,
        LinkReviewLocationChipSource source)
    {
        foreach (var token in Split(stored))
        {
            if (chips.Any(c => string.Equals(c.Value, token, StringComparison.Ordinal)))
                continue;
            chips.Add(new PersonLinkReviewLocationChip { Value = token, Source = source });
        }
    }

    private static IEnumerable<string> Split(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored) || stored.Trim() == "—")
            yield break;
        foreach (var part in stored.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (string.IsNullOrWhiteSpace(part))
                continue;
            // Ýok is a Case/Visa sentinel, not a useful merge chip.
            if (BorderZoneSelectionHelper.IsNoneValue(part))
                continue;
            yield return part.Trim();
        }
    }
}