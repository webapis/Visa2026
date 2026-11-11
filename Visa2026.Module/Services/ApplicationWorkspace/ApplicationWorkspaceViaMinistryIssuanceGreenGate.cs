using System;
using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// Via Ministries ListView row green: AS/CO received (<c>PROCESS_STARTED</c>) and required
/// Result-section documents fully issued. Invitation-getting profiles ignore Issued visa.
/// </summary>
public static class ApplicationWorkspaceViaMinistryIssuanceGreenGate
{
    /// <summary>
    /// True when the row should use <see cref="ApplicationProfileInstanceProgressStateCodes.ProcessIssued"/> green.
    /// </summary>
    public static bool IsGreen(
        ApplicationProfileInstance? application,
        string? latestPrimaryStateCode)
    {
        if (application == null)
            return false;

        if (ApplicationProfileConfigurationResolver.GetProgressRoute(application)
            != ApplicationProfileInstanceProgressRouteKind.ViaMinistries)
            return false;

        if (!string.Equals(
                latestPrimaryStateCode?.Trim(),
                ApplicationProfileInstanceProgressStateCodes.ProcessStarted,
                StringComparison.OrdinalIgnoreCase))
            return false;

        var required = RequiredChipKeys(application);
        if (required.Count == 0)
            return false;

        var chips = application.ListViewResultCoverageChips;
        foreach (var key in required)
        {
            if (!IsChipComplete(chips, key))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Result chip keys that must be N/N for green. Visa is omitted whenever Invitation is required.
    /// </summary>
    public static IReadOnlyList<string> RequiredChipKeys(ApplicationProfileInstance? application)
    {
        if (application == null)
            return Array.Empty<string>();

        var invitation = ApplicationProfileConfigurationResolver.CanIssueInvitation(application);
        var workPermit = ApplicationProfileConfigurationResolver.CanIssueWorkPermit(application);
        var visa = ApplicationProfileConfigurationResolver.CanIssueVisa(application);
        var borderZone = ApplicationProfileConfigurationResolver.CanIssueBorderZone(application);
        var locationGrant = ApplicationProfileConfigurationResolver.CanIssueWorkPermitLocationGrant(application);

        if (invitation && workPermit)
        {
            return
            [
                ApplicationWorkspaceIssuedRecordsCatalog.Invitation,
                ApplicationWorkspaceIssuedRecordsCatalog.WorkPermit,
            ];
        }

        if (invitation)
            return [ApplicationWorkspaceIssuedRecordsCatalog.Invitation];

        if (visa && workPermit)
        {
            return
            [
                ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa,
                ApplicationWorkspaceIssuedRecordsCatalog.WorkPermit,
            ];
        }

        if (visa)
            return [ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa];

        if (workPermit)
            return [ApplicationWorkspaceIssuedRecordsCatalog.WorkPermit];

        if (borderZone)
            return [ApplicationWorkspaceIssuedRecordsCatalog.BorderZone];

        if (locationGrant)
            return [ApplicationWorkspaceIssuedRecordsCatalog.WorkPermitLocationGrant];

        return Array.Empty<string>();
    }

    private static bool IsChipComplete(
        IReadOnlyList<ApplicationWorkspaceIssuedResultListCoverage.Chip> chips,
        string key)
    {
        foreach (var chip in chips)
        {
            if (!string.Equals(chip.Key, key, StringComparison.OrdinalIgnoreCase))
                continue;

            return chip.ExpectedCount > 0 && chip.CoverageCount >= chip.ExpectedCount;
        }

        return false;
    }
}