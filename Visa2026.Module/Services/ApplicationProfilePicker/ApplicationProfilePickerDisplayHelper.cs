using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationProfilePicker;

public static class ApplicationProfilePickerDisplayHelper
{
    public static string FormatActionFamily(ApplicationProfileActionFamily family) =>
        family switch
        {
            ApplicationProfileActionFamily.Issuance => "Issuance",
            ApplicationProfileActionFamily.Cancellation => "Cancellation",
            ApplicationProfileActionFamily.Registration => "Registration",
            ApplicationProfileActionFamily.BusinessTrip => "Business trip",
            _ => family.ToString(),
        };

    public static string FormatRegistrationKind(ApplicationProfileRegistrationKind kind) =>
        kind switch
        {
            ApplicationProfileRegistrationKind.CheckIn => "Check in",
            ApplicationProfileRegistrationKind.CheckOut => "Check out",
            ApplicationProfileRegistrationKind.InfoChange => "Info change",
            _ => string.Empty,
        };

    public static string FormatRelatedTo(ApplicationProfile profile) =>
        FormatRelatedTo(profile.ActionFamily, profile.RegistrationKind);

    public static string FormatRelatedTo(
        ApplicationProfileActionFamily family,
        ApplicationProfileRegistrationKind kind)
    {
        var familyLabel = FormatActionFamily(family);
        if (family != ApplicationProfileActionFamily.Registration)
            return familyLabel;

        var kindLabel = FormatRegistrationKind(kind);
        return string.IsNullOrEmpty(kindLabel) ? familyLabel : $"{familyLabel} · {kindLabel}";
    }

    public static string FormatProgressRoute(ApplicationProfileInstanceProgressRouteKind route) =>
        route == ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService
            ? "Direct migration"
            : "Via ministry";
}
