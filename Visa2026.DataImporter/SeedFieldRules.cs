namespace Visa2026.DataImporter;

/// <summary>
/// Maps yaml/excel column headers to <see cref="ApplicationType"/> Show* flags and obsolete seed fields.
/// </summary>
internal static class SeedFieldRules
{
    /// <summary>Always kept on ApplicationItem rows when <c>ShowApplicationItems</c> is true.</summary>
  private static readonly HashSet<string> ApplicationItemCoreHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Application",
        "Person",
        "Passport Number",
    };

    /// <summary>ApplicationItem yaml headers gated by ApplicationType Show* (matches ApplicationItem.cs Appearance rules).</summary>
    private static readonly Dictionary<string, string> ApplicationItemHeaderToShowFlag =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Visa Number"] = "ShowCurrentVisa",
            ["Next Visa Number"] = "ShowNextVisa",
            ["Previous Passport"] = "ShowPreviousPassport",
            ["Work Permit Item"] = "ShowCurrentWorkPermitItem",
            ["Previous Work Permit Item"] = "ShowPreviousWorkPermitItem",
            ["Invitation Item"] = "ShowCurrentInvitationItem",
            ["Address"] = "ShowCurrentAddressOfResidence",
            ["Contract"] = "ShowCurrentEmployeeContract",
            ["Medical Record"] = "ShowCurrentMedicalRecord",
            ["Education"] = "ShowCurrentEducation",
            ["Registration Date"] = "ShowRegistrations",
            ["Travel Date"] = "ShowRegistrations",
            ["Travel Type"] = "ShowRegistrations",
            ["Movement Type"] = "ShowRegistrations",
            ["Check Point"] = "ShowRegistrations",
            ["Purpose of Travel"] = "ShowRegistrations",
            ["Travel Notes"] = "ShowRegistrations",
            ["Business Trip Address"] = "ShowBusinessTrips",
            ["Invitation Issued"] = "ShowInvitationItemIsIssued",
            ["Work Permit Issued"] = "ShowWorkPermitItemIsIssued",
            ["Rejection Issued"] = "ShowRejectionIssued",
            ["Visa Issued"] = "ShowVisaIssued",
            ["Inv Item Cancelled"] = "ShowInvitationItemIsCancelled",
            ["WP Item Cancelled"] = "ShowWorkPermitItemIsCancelled",
            ["Inv Item Changed"] = "ShowInvitationItemIsChanged",
            ["WP Item Changed"] = "ShowWorkPermitItemIsChanged",
            ["Visa Cancelled"] = "ShowVisaIsCancelled",
            ["Visa Changed"] = "ShowVisaIsChanged",
            ["Border Zone Location"] = "ShowBorderZoneLocation",
        };

    /// <summary>Application yaml headers gated by ApplicationType Show*.</summary>
    private static readonly Dictionary<string, string> ApplicationHeaderToShowFlag =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Project Contract"] = "ShowProjectContract",
            ["Visa Period"] = "ShowVisaPeriod",
            ["Visa Category"] = "ShowVisaCategory",
            ["Urgency"] = "ShowUrgency",
            ["Migration Service"] = "ShowMigrationService",
            ["Visa Type"] = "ShowVisaType",
            ["From City"] = "ShowFromCity",
            ["To City"] = "ShowToCity",
            ["Border Zone Location"] = "ShowBorderZoneLocation",
            ["Movement Permit Location"] = "ShowMovementPermitLocation",
            ["Business Trip Start Date"] = "ShowBusinessTrips",
            ["Business Trip End Date"] = "ShowBusinessTrips",
            ["Business Trip Purpose"] = "ShowBusinessTrips",
        };

    /// <summary>Child sheets under <c>data:</c> gated by ApplicationType Show* (first application row in scenario).</summary>
    private static readonly Dictionary<string, string> SheetToShowFlag =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["ApplicationItems"] = "ShowApplicationItems",
            ["Invitations"] = "ShowInvitations",
            ["InvitationItems"] = "ShowInvitations",
            ["WorkPermits"] = "ShowWorkPermits",
            ["WorkPermitItems"] = "ShowWorkPermits",
            ["Registrations"] = "ShowRegistrations",
            ["Visas"] = "ShowVisas",
            ["Rejections"] = "ShowRejections",
            ["RejectionItems"] = "ShowRejections",
            ["BusinessTrips"] = "ShowBusinessTrips",
            ["BusinessTripAddress"] = "ShowBusinessTrips",
        };

    /// <summary>Deprecated — not on Application BO / removed org FKs. Stripped from seed yaml.</summary>
    private static readonly HashSet<string> ObsoleteHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Filter",
        "Company",
        "ApplicationTypeFilter",
        "ApplicationTypeFilterNames",
        "ApplicabilityMode",
    };

    /// <summary>Deprecated sheet names — never imported; strip from yaml.</summary>
    private static readonly HashSet<string> ObsoleteSheets = new(StringComparer.OrdinalIgnoreCase)
    {
        "ApplicationTypeFilter",
        "ApplicationStatus",
    };

    /// <summary>Passport.PersonalNumber is legacy; prefer Person.PersonalNumber.</summary>
    private static readonly HashSet<string> ObsoleteHeadersBySheet = new(StringComparer.OrdinalIgnoreCase)
    {
        "Passports|Personal Number",
    };

    public static bool IsObsoleteSheet(string sheetName) =>
        ObsoleteSheets.Contains(sheetName);

    public static bool IsObsoleteHeader(string sheetName, string header)
    {
        if (ObsoleteHeaders.Contains(header))
            return true;

        return ObsoleteHeadersBySheet.Contains($"{sheetName}|{header}");
    }

    public static bool IsApplicationItemHeaderAllowed(string header, IReadOnlyDictionary<string, bool> flags)
    {
        if (ApplicationItemCoreHeaders.Contains(header))
            return true;

        if (ApplicationItemHeaderToShowFlag.TryGetValue(header, out string? flag))
            return flags.TryGetValue(flag, out bool allowed) && allowed;

        // Position History has no Show* gate — allowed when application items are shown.
        if (header.Equals("Position History", StringComparison.OrdinalIgnoreCase))
            return flags.GetValueOrDefault("ShowApplicationItems");

        return false;
    }

    public static bool IsApplicationHeaderAllowed(string header, IReadOnlyDictionary<string, bool> flags)
    {
        if (header.Equals("Application Number", StringComparison.OrdinalIgnoreCase)
            || header.Equals("Date", StringComparison.OrdinalIgnoreCase)
            || header.Equals("Application Type", StringComparison.OrdinalIgnoreCase)
            || header.Equals("Category", StringComparison.OrdinalIgnoreCase)
            || header.Equals("Is Active", StringComparison.OrdinalIgnoreCase)
            || header.Equals("Prefix", StringComparison.OrdinalIgnoreCase)
            || header.Equals("Year", StringComparison.OrdinalIgnoreCase)
            || header.Equals("Full Application Number", StringComparison.OrdinalIgnoreCase))
            return true;

        if (ApplicationHeaderToShowFlag.TryGetValue(header, out string? flag))
            return flags.TryGetValue(flag, out bool allowed) && allowed;

        return false;
    }

    public static bool IsSheetAllowedForApplicationType(string sheetName, IReadOnlyDictionary<string, bool> flags)
    {
        if (!SheetToShowFlag.TryGetValue(sheetName, out string? flag))
            return true;

        return flags.TryGetValue(flag, out bool allowed) && allowed;
    }

    public static string? GetApplicationItemHeaderFlagName(string header) =>
        ApplicationItemHeaderToShowFlag.TryGetValue(header, out string? flag) ? flag : null;

    public static string? GetApplicationHeaderFlagName(string header) =>
        ApplicationHeaderToShowFlag.TryGetValue(header, out string? flag) ? flag : null;

    public static string? GetSheetFlagName(string sheetName) =>
        SheetToShowFlag.TryGetValue(sheetName, out string? flag) ? flag : null;
}
