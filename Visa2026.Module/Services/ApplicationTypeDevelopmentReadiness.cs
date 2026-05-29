using System;
using System.Collections.Generic;

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
        // None yet — move here after user/stakeholder testing and sign-off.
    };

    /// <summary>Stakeholder-approved ministry <c>SelectionCode</c> values (optional; name match is enough).</summary>
    public static readonly HashSet<string> ReadyBySelectionCode = new(StringComparer.OrdinalIgnoreCase)
    {
    };

    /// <summary>Implemented by developer; awaiting user/stakeholder testing and approval (<c>ApplicationType.Name</c>).</summary>
    public static readonly HashSet<string> PendingByName = new(StringComparer.OrdinalIgnoreCase)
    {
        "App_Inv",
        "App_Inv_FM",
        "App_Reg_Check_In",
        "App_Inv_And_WP",
        "App_Visa_and_WP_Ext",
        "App_Cancel_Visa",
    };

    public static readonly HashSet<string> PendingBySelectionCode = new(StringComparer.OrdinalIgnoreCase)
    {
        "101",
        "102",
        "105",
        "301",
        "708",
        "807",
    };

    public static ApplicationTypeReadinessStatus GetStatus(string? name, string? selectionCode)
    {
        if (Matches(ReadyByName, ReadyBySelectionCode, name, selectionCode))
            return ApplicationTypeReadinessStatus.Ready;
        if (Matches(PendingByName, PendingBySelectionCode, name, selectionCode))
            return ApplicationTypeReadinessStatus.Pending;
        return ApplicationTypeReadinessStatus.NotReady;
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
