using System;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services;

/// <summary>
/// Çakylyk Almak (<c>App_Inv</c> / <c>get_invitation</c>) collects an invitation stay
/// and does not require the person's home address.
/// </summary>
public static class ApplicationProfileInvitationAddressPolicy
{
    public const string ProfileCatalogKey = "App_Inv";

    public const string ProfileCode = "get_invitation";

    public const string ApplicationTypeName = "App_Inv";

    public static bool IsCaklykAlmak(string? profileCatalogKey, string? code) =>
        string.Equals(profileCatalogKey?.Trim(), ProfileCatalogKey, StringComparison.OrdinalIgnoreCase)
        || string.Equals(code?.Trim(), ProfileCode, StringComparison.OrdinalIgnoreCase);

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
}
