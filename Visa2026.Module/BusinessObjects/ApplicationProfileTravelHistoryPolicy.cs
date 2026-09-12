using System;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Travel history is for Registration (and visa/WP/border-zone) templates — not Business trip,
/// Invitation produce/change/cancel, or visa-document cancellation
/// (<c>cancel_visa</c> / <c>cancel_visa_wp</c>). Extension-request cancels keep the catalog flag.
/// </summary>
public static class ApplicationProfileTravelHistoryPolicy
{
    public static bool AllowsPersonTravelHistory(ApplicationProfile? profile)
    {
        if (profile == null)
            return false;
        if (profile.ActionFamily == ApplicationProfileActionFamily.BusinessTrip)
            return false;
        if (IsInvitationRelated(profile))
            return false;
        if (ApplicationProfileEducationPolicy.IsVisaDocumentCancellation(profile))
            return false;
        return true;
    }

    public static bool IsInvitationRelated(ApplicationProfile profile) =>
        profile.ProduceInvitation
        || profile.CancelInvitations
        || profile.ChangeInvitations
        || CodeLooksLikeInvitation(profile.Code);

    public static bool IsInvitationRelated(
        bool produceInvitation,
        bool cancelInvitations,
        bool changeInvitations,
        string? code) =>
        produceInvitation
        || cancelInvitations
        || changeInvitations
        || CodeLooksLikeInvitation(code);

    public static bool CodeLooksLikeInvitation(string? code) =>
        !string.IsNullOrWhiteSpace(code)
        && code.Contains("invitation", StringComparison.OrdinalIgnoreCase);
}