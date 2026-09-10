#nullable enable

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Optional Review checkboxes before Remap unmarked. They steer AI (and mark selection);
/// locked yellows stay frozen either way.
/// </summary>
public sealed record ScanRemapOfficerHints(bool UnidentifiedYellows, bool IncorrectPlaceholders)
{
    public static ScanRemapOfficerHints None { get; } = new(false, false);

    public bool Any => UnidentifiedYellows || IncorrectPlaceholders;

    public string ToInstruction()
    {
        var parts = new List<string>();
        if (UnidentifiedYellows)
        {
            parts.Add(
                "Officer: some yellow highlights still have no numbered placeholder. "
                + "Map marks with no localProposedToken. Use page images for leftover yellow. "
                + "Do not invent fieldIds.");
        }

        if (IncorrectPlaceholders)
        {
            parts.Add(
                "Officer: some unlocked placeholders are wrong. "
                + "Re-choose from allowedTokens; do not keep localProposedToken just because it looks High.");
        }

        return string.Join(' ', parts);
    }

    public object ToPayload() => new
    {
        unidentifiedYellows = UnidentifiedYellows,
        incorrectPlaceholders = IncorrectPlaceholders,
        instruction = ToInstruction(),
    };
}