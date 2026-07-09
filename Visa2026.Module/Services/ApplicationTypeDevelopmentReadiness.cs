using System;
using System.Collections.Generic;
using Visa2026.Module.DatabaseUpdate;

namespace Visa2026.Module.Services;

/// <summary>
/// Code-only rollout map for application types (not persisted in the database).
/// <see cref="ApplicationTypeReadinessStatus.Ready"/> — stakeholder approved for production use.
/// <see cref="ApplicationTypeReadinessStatus.Pending"/> — developer implementation complete; awaiting stakeholder approval.
/// <see cref="ApplicationTypeReadinessStatus.NotReady"/> — implementation not complete (default when unlisted).
/// Edit sets via <c>PROMPT_APPLICATION_TYPE_READINESS.md</c> in this folder.
/// </summary>
public static class ApplicationTypeDevelopmentReadiness
{
    /// <summary>Stakeholder-approved types (<c>ApplicationType.Name</c>).</summary>
    public static readonly HashSet<string> ReadyByName = new(StringComparer.OrdinalIgnoreCase)
    {
        // Çakylyk (100)
        "App_Inv",
        "App_Inv_FM",
        "App_Inv_According_to_WP",
        "App_Change_Inv",
        "App_Inv_And_WP",

        // Gulluk Pasport (200)
        "App_Sevice_Passport",

        // Hasaba Alyş (300)
        "App_Reg_Check_In",
        "App_Reg_Check_In_Internal",
        "App_Reg_Info_Change_Passport",
        "App_Reg_Info_Change_Visa",
        "App_Reg_Info_Change_Address",
        "App_Reg_ext",
        "App_Reg_Check_Out",
        "App_Reg_Check_Out_Internal",

        // Iş Rugsatnama (400)
        "App_WP_Ext",
        "App_Additional_WP_location",

        // Iş Sapary (500)
        "App_Business_Trip_Departure",
        "App_Business_Trip_Arrival",

        // Serhet ýaka (600)
        "App_Border_Zone_Permission",

        // Wiza (700) — App_Visa_Ext (702) deprecated; hidden from picker
        "App_Visa_Ext_According_to_WP",
        "App_Exit_Visa",
        "App_Change_Visa_Category",
        "App_Change_Passport",
        "App_Visa_Ext_FM",
        "App_Visa_For_New_Born_FM",
        "App_Visa_and_WP_Ext",

        // Ýatyrmak (800)
        "App_Cancel_BZ",
        "App_Cancel_App",
        "App_Cancel_Visa_and_WP_Ext",
        "App_Cancel_Visa_Ext",
        "App_Cancel_Inv",
        "App_Cancel_Inv_WP",
        "App_Cancel_Visa",
        "App_Cancel_Visa_and_WP",
        "App_Cancell_WP",
    };

    /// <summary>Stakeholder-approved ministry <c>SelectionCode</c> values (optional; name match is enough).</summary>
    public static readonly HashSet<string> ReadyBySelectionCode = new(StringComparer.OrdinalIgnoreCase)
    {
        "101",
        "102",
        "103",
        "104",
        "105",
        "201",
        "301",
        "302",
        "303",
        "304",
        "305",
        "306",
        "307",
        "308",
        "401",
        "402",
        "501",
        "502",
        "601",
        "701",
        "703",
        "704",
        "705",
        "706",
        "707",
        "708",
        "801",
        "802",
        "803",
        "804",
        "805",
        "806",
        "807",
        "808",
        "809",
    };

    /// <summary>Implemented by developer; awaiting user/stakeholder testing and approval (<c>ApplicationType.Name</c>).</summary>
    public static readonly HashSet<string> PendingByName = new(StringComparer.OrdinalIgnoreCase)
    {
    };

    public static readonly HashSet<string> PendingBySelectionCode = new(StringComparer.OrdinalIgnoreCase)
    {
    };

    /// <summary>Deprecated types — omitted from the type-code picker entirely (row kept in DB for FK integrity).</summary>
    public static readonly HashSet<string> HiddenFromTypeCodePickerByName = new(StringComparer.OrdinalIgnoreCase)
    {
        "App_Visa_Ext",
    };

    public static readonly HashSet<string> HiddenFromTypeCodePickerBySelectionCode = new(StringComparer.OrdinalIgnoreCase)
    {
        "702",
    };

    public static bool IsHiddenFromTypeCodePicker(string? name, string? selectionCode) =>
        Matches(HiddenFromTypeCodePickerByName, HiddenFromTypeCodePickerBySelectionCode, name, selectionCode);

    public static ApplicationTypeReadinessStatus GetStatus(string? name, string? selectionCode)
    {
        if (IsHiddenFromTypeCodePicker(name, selectionCode))
            return ApplicationTypeReadinessStatus.NotReady;
        if (Matches(ReadyByName, ReadyBySelectionCode, name, selectionCode))
            return ApplicationTypeReadinessStatus.Ready;
        if (Matches(PendingByName, PendingBySelectionCode, name, selectionCode))
            return ApplicationTypeReadinessStatus.Pending;

        // Cloned / admin-created variants: valid code but legacy Name not in the seed map → Pending (selectable for testing).
        if (IsUserDefinedVariant(name, selectionCode))
            return ApplicationTypeReadinessStatus.Pending;

        return ApplicationTypeReadinessStatus.NotReady;
    }

    private static bool IsUserDefinedVariant(string? name, string? selectionCode)
    {
        if (string.IsNullOrWhiteSpace(selectionCode)
            || selectionCode.Length != 3
            || !int.TryParse(selectionCode, out _))
        {
            return false;
        }

        return !ApplicationTypeSelectionCodeSeed.TryGetByName(name, out _);
    }

    /// <summary>Ready and Pending types can be selected (Pending for stakeholder/testing before approval).</summary>
    public static bool CanSelectOnApplicationForm(ApplicationTypeReadinessStatus status) =>
        status is ApplicationTypeReadinessStatus.Ready or ApplicationTypeReadinessStatus.Pending;

    private static bool Matches(
        HashSet<string> names,
        HashSet<string> codes,
        string? name,
        string? selectionCode) =>
        (!string.IsNullOrWhiteSpace(name) && names.Contains(name.Trim()))
        || (!string.IsNullOrWhiteSpace(selectionCode) && codes.Contains(selectionCode.Trim()));
}
