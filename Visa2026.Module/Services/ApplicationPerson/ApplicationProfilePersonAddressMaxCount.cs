using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationPersonRoster;

/// <summary>
/// Address slots: Address 1 required when <see cref="ApplicationProfile.RequireAddress1"/>
/// (or legacy person-address flag); Address 2 optional when RequireAddress2 / max 2.
/// </summary>
public static class ApplicationProfilePersonAddressMaxCount
{
    public const int Min = 1;
    public const int Max = 2;
    public const int Default = 1;

    public static int Clamp(int value) =>
        value < Min ? Default : (value > Max ? Max : value);

    /// <summary>
    /// Dual-read: when either RequireAddress* is set, use those; otherwise fall back to
    /// obsolete <see cref="ApplicationProfile.RequirePersonAddressOfResidence"/> + max count.
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
    /// Map legacy person-address flags onto RequireAddress* when both Requires are still false,
    /// then keep obsolete flags aligned with Requires.
    /// </summary>
    public static void SyncRequireFlagsFromLegacy(ApplicationProfile profile)
    {
        if (profile == null)
            return;
#pragma warning disable CS0618
        if (!profile.RequireAddress1 && !profile.RequireAddress2 && profile.RequirePersonAddressOfResidence)
        {
            profile.RequireAddress1 = true;
            profile.RequireAddress2 = Clamp(profile.PersonAddressOfResidenceMaxCount) >= 2;
        }

        profile.RequirePersonAddressOfResidence = profile.RequireAddress1 || profile.RequireAddress2;
        profile.PersonAddressOfResidenceMaxCount = profile.RequireAddress2 ? 2 : (profile.RequireAddress1 ? 1 : Default);
#pragma warning restore CS0618
    }
}