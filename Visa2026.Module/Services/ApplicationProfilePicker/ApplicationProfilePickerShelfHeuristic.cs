using System;
using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationProfilePicker;

/// <summary>
/// Default multi-shelf assignment from <see cref="ApplicationProfile.Name"/> keywords
/// (and Seretmezlik via <see cref="ApplicationProfile.RequireTargetApplicationProfileInstance"/>).
/// Ignores Produce* flags, ActionFamily, Code, audience.
/// </summary>
public static class ApplicationProfilePickerShelfHeuristic
{
    public static IReadOnlyList<string> ResolveShelfCodes(ApplicationProfile profile)
    {
        if (profile == null)
            return Array.Empty<string>();

        var name = profile.Name ?? string.Empty;
        var codes = new List<string>(8);

        // Dual-list standalone exclusion profile — shelf even if Name is empty/renamed.
        if (ApplicationProfileConfigurationResolver.IsSeretmezlikProfile(profile)
            || NameMentionsSeretmezlik(name))
            codes.Add(ApplicationProfilePickerShelfCodes.Seretmezlik);

        if (string.IsNullOrWhiteSpace(name))
            return codes;

        var hasCakylyk = NameMentionsCakylyk(name);

        if (NameMentionsFamily(name))
            codes.Add(ApplicationProfilePickerShelfCodes.FamilyMemberOnly);

        if (NameMentionsRegistration(name))
            codes.Add(ApplicationProfilePickerShelfCodes.Registration);

        if (hasCakylyk)
            codes.Add(ApplicationProfilePickerShelfCodes.Invitations);

        // Visa tab: Wiza/Visa in name, but never when Çakylyk/Çakylygy is also present.
        if (!hasCakylyk && NameMentionsVisa(name))
            codes.Add(ApplicationProfilePickerShelfCodes.Visa);

        if (NameMentionsBusinessTrip(name))
            codes.Add(ApplicationProfilePickerShelfCodes.BusinessTrip);

        if (NameMentionsWorkPermit(name))
            codes.Add(ApplicationProfilePickerShelfCodes.WorkPermitOnly);

        if (NameMentionsBorderZone(name))
            codes.Add(ApplicationProfilePickerShelfCodes.BorderZone);

        return codes;
    }

    public static bool NameMentionsSeretmezlik(string? name) =>
        ContainsIgnoreCase(name, "seretmezlik");

    public static bool NameMentionsCakylyk(ApplicationProfile profile) =>
        NameMentionsCakylyk(profile?.Name);

    public static bool NameMentionsCakylyk(string? name) =>
        ContainsIgnoreCase(name, "çakylyk")
        || ContainsIgnoreCase(name, "cakylyk")
        || ContainsIgnoreCase(name, "çakylygy")
        || ContainsIgnoreCase(name, "cakylygy");

    public static bool NameMentionsVisa(string? name) =>
        ContainsIgnoreCase(name, "wiza") || ContainsIgnoreCase(name, "visa");

    public static bool NameMentionsWorkPermit(string? name)
    {
        if (ContainsIgnoreCase(name, "iş rugsat")
            || ContainsIgnoreCase(name, "is rugsat")
            || ContainsIgnoreCase(name, "işrugsat")
            || ContainsIgnoreCase(name, "isrugsat")
            || ContainsIgnoreCase(name, "work permit")
            || ContainsIgnoreCase(name, "workpermit"))
            return true;

        // Declined / compound forms: Iş Rugsatnama goşmaça…, …Rugsatnamasyny…
        // Require iş/is so border "Serhet … Rugsatnama" is not treated as work permit.
        if (ContainsIgnoreCase(name, "rugsatnama")
            || ContainsIgnoreCase(name, "rugsatnamasy")
            || ContainsIgnoreCase(name, "rugsatnamany"))
        {
            return ContainsIgnoreCase(name, "iş")
                || ContainsIgnoreCase(name, "is ");
        }

        return false;
    }

    public static bool NameMentionsBusinessTrip(string? name) =>
        ContainsIgnoreCase(name, "iş sapary")
        || ContainsIgnoreCase(name, "is sapary")
        || ContainsIgnoreCase(name, "iş saparyna")
        || ContainsIgnoreCase(name, "is saparyna")
        || ContainsIgnoreCase(name, "saparyna")
        || ContainsIgnoreCase(name, "sapary")
        || ContainsIgnoreCase(name, "business trip")
        || ContainsIgnoreCase(name, "businesstrip");

    public static bool NameMentionsBorderZone(string? name) =>
        ContainsIgnoreCase(name, "serhet ýaka")
        || ContainsIgnoreCase(name, "serhet yaka")
        || ContainsIgnoreCase(name, "serhetýaka")
        || ContainsIgnoreCase(name, "serhetyaka")
        || ContainsIgnoreCase(name, "serhet")
        || ContainsIgnoreCase(name, "border zone")
        || ContainsIgnoreCase(name, "borderzone");

    public static bool NameMentionsFamily(string? name)
    {
        if (ContainsIgnoreCase(name, "maşgala")
            || ContainsIgnoreCase(name, "masgala")
            || ContainsIgnoreCase(name, "family"))
            return true;

        // Legacy trailing " FM" until catalogs finish renaming to Maşgala agzasy.
        return NameEndsWithTrailingFm(name);
    }

    public static bool NameMentionsRegistration(string? name) =>
        ContainsIgnoreCase(name, "hasaba")
        || ContainsIgnoreCase(name, "registration");

    /// <summary>True when the display name ends with a standalone FM token (not GFM/TFM).</summary>
    public static bool NameEndsWithTrailingFm(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var trimmed = NormalizeTurkicI(name).TrimEnd();
        if (trimmed.Equals("FM", StringComparison.OrdinalIgnoreCase))
            return true;

        return trimmed.EndsWith(" FM", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Case-insensitive contains after normalizing Turkish İ/ı so "İş Rugsatnama…" matches "iş rugsat".
    /// </summary>
    private static bool ContainsIgnoreCase(string? haystack, string needle)
    {
        if (string.IsNullOrEmpty(haystack) || string.IsNullOrEmpty(needle))
            return false;

        return NormalizeTurkicI(haystack)
            .IndexOf(NormalizeTurkicI(needle), StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>Map Turkish dotted/dotless I so OrdinalIgnoreCase matches ASCII i/I needles.</summary>
    private static string NormalizeTurkicI(string value) =>
        value.Replace('\u0130', 'I').Replace('\u0131', 'i');
}