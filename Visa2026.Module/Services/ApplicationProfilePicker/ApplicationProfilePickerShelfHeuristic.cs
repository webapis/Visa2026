using System;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationProfilePicker;

/// <summary>One-time default shelf assignment for profiles with no picker shelf yet.</summary>
public static class ApplicationProfilePickerShelfHeuristic
{
    public static string? ResolveShelfCode(ApplicationProfile profile)
    {
        if (profile == null)
            return null;

        if (profile.ForFamilyMember && !profile.ForEmployee && !profile.ForTemporaryVisitor)
            return ApplicationProfilePickerShelfCodes.FamilyMemberOnly;

        if (profile.ActionFamily == ApplicationProfileActionFamily.Registration)
            return ApplicationProfilePickerShelfCodes.Registration;

        if (IsInvitationRelated(profile))
            return ApplicationProfilePickerShelfCodes.Invitations;

        if (IsWorkPermitRelated(profile))
            return ApplicationProfilePickerShelfCodes.WorkPermitOnly;

        return null;
    }

    public static bool IsInvitationRelated(ApplicationProfile profile)
    {
        if (profile.ProduceInvitation || profile.CancelInvitations || profile.ChangeInvitations)
            return true;

        var code = profile.Code ?? string.Empty;
        return code.Contains("invitation", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsWorkPermitRelated(ApplicationProfile profile)
    {
        if (profile.ProduceWorkPermit || profile.CancelWorkPermits || profile.ChangeWorkPermits)
            return true;

        var code = profile.Code ?? string.Empty;
        return code.Contains("workpermit", StringComparison.OrdinalIgnoreCase)
            || code.Contains("work_permit", StringComparison.OrdinalIgnoreCase)
            || string.Equals(code, "extend_visa_wp", StringComparison.OrdinalIgnoreCase)
            || string.Equals(code, "cancel_visa_wp", StringComparison.OrdinalIgnoreCase)
            || string.Equals(code, "cancel_visa_wp_ext", StringComparison.OrdinalIgnoreCase);
    }
}