using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Services.ApplicationProfilePicker;

public static class ApplicationProfilePickerDisplayHelper
{
    public static string FormatActionFamily(ApplicationProfileActionFamily family) =>
        ApplicationProfileLocalization.ActionFamily(family);

    public static string FormatRegistrationKind(ApplicationProfileRegistrationKind kind) =>
        ApplicationProfileLocalization.RegistrationKind(kind);

    public static string FormatRelatedTo(ApplicationProfile profile) =>
        ApplicationProfileLocalization.RelatedTo(profile);

    public static string FormatRelatedTo(
        ApplicationProfileActionFamily family,
        ApplicationProfileRegistrationKind kind) =>
        ApplicationProfileLocalization.RelatedTo(family, kind);

    public static string FormatProgressRoute(ApplicationProfileInstanceProgressRouteKind route) =>
        ApplicationProfileLocalization.ProgressRoute(route);
}
