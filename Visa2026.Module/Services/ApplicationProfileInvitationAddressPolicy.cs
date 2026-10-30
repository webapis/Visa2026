using System;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services;

/// <summary>
/// Invitation-receiving templates collect the case stay (region, city, address type, place).
/// Other invitation places stay optional. Çakylyk Almak still has its own person-link exceptions.
/// </summary>
public static class ApplicationProfileInvitationAddressPolicy
{
    public const string ProfileCatalogKey = "App_Inv";

    public const string ProfileCode = "get_invitation";

    public const string ApplicationTypeName = "App_Inv";

    private static readonly HashSet<string> InvitationStayKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "App_Inv",
        "get_invitation",
        "App_Inv_And_WP",
        "get_invitation_wp",
        "App_Inv_FM",
        "get_invitation_fm",
        "App_Inv_According_to_WP",
        "get_invitation_according_to_wp",
        // App_Change_Inv / change_invitation — no case stay (region/city/place); officers change invitation without re-collecting address.
    };

    /// <summary>
    /// Case summary shows Invitation region, city, address type, and other invitation places.
    /// </summary>
    public static bool CollectsInvitationStay(string? profileCatalogKey, string? code) =>
        IsStayKey(profileCatalogKey) || IsStayKey(code);

    public static bool CollectsInvitationStay(ApplicationProfile? profile) =>
        CollectsInvitationStay(null, profile?.Code);

    public static bool IsCaklykAlmak(string? profileCatalogKey, string? code) =>
        string.Equals(profileCatalogKey?.Trim(), ProfileCatalogKey, StringComparison.OrdinalIgnoreCase)
        || string.Equals(code?.Trim(), ProfileCode, StringComparison.OrdinalIgnoreCase);

    public static bool IsChangeInvitation(string? profileCatalogKey, string? code) =>
        string.Equals(profileCatalogKey?.Trim(), "App_Change_Inv", StringComparison.OrdinalIgnoreCase)
        || string.Equals(code?.Trim(), "change_invitation", StringComparison.OrdinalIgnoreCase);

    private static bool IsStayKey(string? value) =>
        !string.IsNullOrWhiteSpace(value) && InvitationStayKeys.Contains(value.Trim());

    public static bool IsCaklykAlmakType(ApplicationType? type)
    {
        if (type == null)
            return false;

        // Name is the ApplicationType seed key (App_Inv). NameTm is the Turkmen title.
#pragma warning disable CS0618
        var seedName = type.Name;
#pragma warning restore CS0618
        return string.Equals(seedName?.Trim(), ApplicationTypeName, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsChangeInvitationType(ApplicationType? type)
    {
        if (type == null)
            return false;

#pragma warning disable CS0618
        var seedName = type.Name;
        var code = type.Code;
#pragma warning restore CS0618
        return IsChangeInvitation(seedName, code);
    }

    public static bool CollectsInvitationStayType(ApplicationType? type)
    {
        if (type == null)
            return false;

#pragma warning disable CS0618
        var seedName = type.Name;
#pragma warning restore CS0618
        return IsStayKey(seedName);
    }
}
