using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationPersonRoster;

/// <summary>
/// Address slots: Address 1 required when <see cref="ApplicationProfile.RequireAddress1"/>
/// (or legacy person-address flag for People tiles); Address 2 optional when RequireAddress2 / max 2.
/// Case summary Address 1 is catalog <c>RequireAddress1</c> only — not promoted from the People toggle.
/// </summary>
public static class ApplicationProfilePersonAddressMaxCount
{
    public const int Min = 1;
    public const int Max = 2;
    public const int Default = 1;

    public static int Clamp(int value) =>
        value < Min ? Default : (value > Max ? Max : value);

    /// <summary>
    /// Dual-read for People / Linked Records: when either RequireAddress* is set, use those;
    /// otherwise fall back to obsolete <see cref="ApplicationProfile.RequirePersonAddressOfResidence"/> + max count.
    /// </summary>
    public static bool EffectiveRequireAddress1(ApplicationProfile? profile)
    {
        if (profile == null)
            return false;
        if (profile.RequireAddress1 || profile.RequireAddress2)
            return profile.RequireAddress1;
#pragma warning disable CS0618
        return profile.RequirePersonAddressOfResidence;
#pragma warning restore CS0618
    }

    public static bool EffectiveRequireAddress2(ApplicationProfile? profile)
    {
        if (profile == null)
            return false;
        if (profile.RequireAddress1 || profile.RequireAddress2)
            return profile.RequireAddress2;
#pragma warning disable CS0618
        return profile.RequirePersonAddressOfResidence
            && Clamp(profile.PersonAddressOfResidenceMaxCount) >= 2;
#pragma warning restore CS0618
    }

    /// <summary>
    /// Visible address slots for Linked Records / People tiles. 0 when neither Address
    /// Require is on (including legacy dual-read). Completeness still expects only slot 1.
    /// </summary>
    public static int For(ApplicationProfile? profile)
    {
        if (EffectiveRequireAddress2(profile))
            return 2;
        if (EffectiveRequireAddress1(profile))
            return 1;
        return 0;
    }

    public static int For(ApplicationProfileInstance? application)
    {
        if (application?.ApplicationProfile != null)
            return For(application.ApplicationProfile);
        return ApplicationProfileInstancePersonResolver.IsAutoLinkEnabled(
            application,
            ApplicationProfileInstancePersonLinkKind.AddressOfResidence)
            ? Default
            : 0;
    }

    /// <summary>Required slot count for missing flags (never includes optional Address 2).</summary>
    public static int RequiredSlotCount(ApplicationProfile? profile) =>
        EffectiveRequireAddress1(profile) ? 1 : 0;

    public static int RequiredSlotCount(ApplicationProfileInstance? application) =>
        RequiredSlotCount(application?.ApplicationProfile);

    /// <summary>
    /// Keep People address aligned when Case Address 1/2 are on. Do not promote
    /// <see cref="ApplicationProfile.RequirePersonAddressOfResidence"/> onto RequireAddress1
    /// (Case summary stay is catalog/invitation-stay scoped).
    /// </summary>
    public static void SyncRequireFlagsFromLegacy(ApplicationProfile profile)
    {
        if (profile == null)
            return;
#pragma warning disable CS0618
        if (profile.RequireAddress1 || profile.RequireAddress2)
        {
            profile.RequirePersonAddressOfResidence = true;
            profile.PersonAddressOfResidenceMaxCount = profile.RequireAddress2 ? 2 : 1;
        }
        else
        {
            profile.PersonAddressOfResidenceMaxCount = profile.RequirePersonAddressOfResidence
                ? Clamp(profile.PersonAddressOfResidenceMaxCount)
                : Default;
        }
#pragma warning restore CS0618
    }
}