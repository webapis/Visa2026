using System;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Check in / check out / info change only apply when Related to is Registration.
/// </summary>
public static class ApplicationProfileRegistrationKindHelper
{
    public static ApplicationProfileRegistrationKind Resolve(
        ApplicationProfileActionFamily family,
        ApplicationProfileRegistrationKind kind)
    {
        if (family != ApplicationProfileActionFamily.Registration)
            return ApplicationProfileRegistrationKind.None;

        return kind;
    }

    public static ApplicationProfileRegistrationKind ResolveFromTypeName(
        ApplicationProfileActionFamily family,
        string? applicationTypeName)
    {
        if (family != ApplicationProfileActionFamily.Registration)
            return ApplicationProfileRegistrationKind.None;

        return InferFromApplicationTypeName(applicationTypeName);
    }

    public static ApplicationProfileRegistrationKind InferFromApplicationTypeName(string? applicationTypeName)
    {
        if (string.IsNullOrWhiteSpace(applicationTypeName))
            return ApplicationProfileRegistrationKind.None;

        if (applicationTypeName.Contains("Info_Change", StringComparison.OrdinalIgnoreCase)
            || applicationTypeName.Contains("InfoChange", StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationProfileRegistrationKind.InfoChange;
        }

        if (applicationTypeName.Contains("Check_Out", StringComparison.OrdinalIgnoreCase)
            || applicationTypeName.Contains("CheckOut", StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationProfileRegistrationKind.CheckOut;
        }

        if (applicationTypeName.Contains("Check_In", StringComparison.OrdinalIgnoreCase)
            || applicationTypeName.Contains("CheckIn", StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationProfileRegistrationKind.CheckIn;
        }

        return ApplicationProfileRegistrationKind.None;
    }

    /// <summary>
    /// Officer rule: Registration profiles always require Position (employee position history).
    /// </summary>
    public static void ApplyRegistrationPersonDefaults(ApplicationProfile profile)
    {
        if (profile.ActionFamily != ApplicationProfileActionFamily.Registration)
            return;

        profile.RequirePersonPosition = true;
    }
}