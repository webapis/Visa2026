using System;
using System.Linq;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services;

/// <summary>
/// Dual-read helpers for <see cref="Visa.IssuingApplication"/> (profile-first) vs legacy
/// <see cref="Visa.IssuingApplicationItem"/>.
/// </summary>
public static class VisaIssuingApplicationHelper
{
    public static Application? GetEffectiveIssuingApplication(Visa? visa) =>
        visa?.IssuingApplication ?? visa?.IssuingApplicationItem?.Application;

    public static bool CanIssueInvitationForVisa(Visa? visa) =>
        CanIssueInvitationForApplication(GetEffectiveIssuingApplication(visa));

    public static bool CanIssueInvitationForApplication(Application? application)
    {
        if (application == null)
            return false;

        if (application.ApplicationProfile != null)
            return application.ApplicationProfile.ProduceInvitation;

        return ApplicationTypeCapabilities.CanIssueInvitation(application.ApplicationType);
    }

    public static bool IsEligibleIssuingApplication(Application? application)
    {
        if (application == null)
            return false;

        if (application.ApplicationProfile != null)
            return application.ApplicationProfile.ProduceVisa
                || application.ApplicationProfile.ProduceInvitation;

        return application.ApplicationType != null
            && (application.ApplicationType.CanIssueVisa
                || application.ApplicationType.CanIssueInvitation);
    }
}
