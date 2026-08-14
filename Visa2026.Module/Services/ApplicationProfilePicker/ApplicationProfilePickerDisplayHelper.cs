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

    public static string FormatProgressRoute(ApplicationProfileInstanceProgressRouteKind route) =>
        route == ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService
            ? "Direct migration"
            : "Via ministry";
}
