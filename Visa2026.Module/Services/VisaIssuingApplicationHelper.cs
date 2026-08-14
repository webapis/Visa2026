using System;
using System.Linq;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services;

/// <summary>
/// Helpers for <see cref="Visa.IssuingApplicationProfileInstance"/>.
/// </summary>
public static class VisaIssuingApplicationProfileInstanceHelper
{
    public static ApplicationProfileInstance? GetEffectiveIssuingApplicationProfileInstance(Visa? visa) =>
        visa?.IssuingApplicationProfileInstance;

    public static bool CanIssueInvitationForVisa(Visa? visa) =>
        CanIssueInvitationForApplication(GetEffectiveIssuingApplicationProfileInstance(visa));

    public static bool CanIssueInvitationForApplication(ApplicationProfileInstance? application)
    {
        if (application == null)
            return false;

        if (application.ApplicationProfile != null)
            return application.ApplicationProfile.ProduceInvitation;

        return ApplicationTypeCapabilities.CanIssueInvitation(application.ApplicationType);
    }

    public static bool IsEligibleIssuingApplicationProfileInstance(ApplicationProfileInstance? application)
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
