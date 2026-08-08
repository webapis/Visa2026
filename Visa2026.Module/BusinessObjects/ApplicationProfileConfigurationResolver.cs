using System;
using System.Linq;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Effective Application configuration: <see cref="Application.ApplicationProfile"/> first,
/// deprecated <see cref="Application.ApplicationType"/> fallback during dual-read (slice 6).
/// Inverse of <see cref="DatabaseUpdate.ApplicationProfileFromApplicationTypeMapper"/> where mapped.
/// </summary>
public static class ApplicationProfileConfigurationResolver
{
    public static ApplicationProgressRouteKind? GetProgressRoute(Application? application)
    {
        if (application == null)
            return null;

        if (application.CreationProgressRoute.HasValue)
            return application.CreationProgressRoute.Value;

        if (application.ApplicationProfile != null)
            return application.ApplicationProfile.ProgressRoute;

        return application.ApplicationType?.ApplicationProgressRoute;
    }

    public static bool HasConfiguration(Application? application) =>
        application?.ApplicationProfile != null || application?.ApplicationType != null;

    public static bool CanIssueVisa(Application? application) =>
        Resolve(application, p => p.ProduceVisa, t => t.CanIssueVisa);

    public static bool CanIssueInvitation(Application? application) =>
        Resolve(application, p => p.ProduceInvitation, t => t.CanIssueInvitation);

    public static bool CanIssueWorkPermit(Application? application) =>
        Resolve(application, p => p.ProduceWorkPermit, t => t.CanIssueWorkPermit);

    public static bool CanBeIssuingApplicationForVisa(Application? application) =>
        CanIssueVisa(application) || CanIssueInvitation(application);

    public static int GetMigrationSlaMaxDays(Application? application)
    {
        if (application?.ApplicationProfile is { MigrationSlaDays: > 0 } profile)
            return profile.MigrationSlaDays;

        if (application?.ApplicationType?.MigrationSlaProfile?.MaxDaysInReview is > 0 and var maxDays)
            return maxDays;

        return 0;
    }

    public static bool HasMigrationSlaConfigured(Application? application) =>
        GetMigrationSlaMaxDays(application) > 0;

    public static int GetEmbeddedProfileMinistryLegCount(Application? application) =>
        application?.ApplicationProfile?.ApprovalLegs?
            .Count(l => l.ApprovingMinistry != null) ?? 0;

    // --- Application DetailView visibility (maps from profile Require* / ActionFamily) ---

    public static bool ShowVisaType(Application? application) =>
        Resolve(application, p => p.RequireVisaType, t => t.ShowVisaType);

    public static bool ShowVisaCategory(Application? application) =>
        Resolve(application, p => p.RequireVisaCategory, t => t.ShowVisaCategory);

    public static bool ShowVisaPeriod(Application? application) =>
        Resolve(application, p => p.RequireVisaPeriod, t => t.ShowVisaPeriod);

    public static bool ShowProjectContract(Application? application) =>
        Resolve(application, p => p.RequireProject, t => t.ShowProjectContract);

    public static bool ShowUrgency(Application? application) =>
        Resolve(application, p => p.RequireUrgency, t => t.ShowUrgency);

    public static bool ShowBorderZoneLocation(Application? application) =>
        Resolve(application, p => p.RequireBorderZone, t => t.ShowBorderZoneLocation);

    public static bool ShowMovementPermitLocation(Application? application) =>
        Resolve(application, p => p.RequireWorkPermitLocation, t => t.ShowMovementPermitLocation);

    public static bool ShowWorkPermittedLocations(Application? application) =>
        Resolve(application, p => p.RequireWorkPermitLocation, t => t.ShowWorkPermittedLocations);

    public static bool ShowFromCity(Application? application) =>
        Resolve(application, p => p.RequireRegionCity, t => t.ShowFromCity);

    public static bool ShowToCity(Application? application) =>
        Resolve(application, p => p.RequireRegionCity, t => t.ShowToCity);

    public static bool ShowBusinessTrips(Application? application) =>
        Resolve(
            application,
            p => p.ActionFamily == ApplicationProfileActionFamily.BusinessTrip || p.RequireStartDate,
            t => t.ShowBusinessTrips);

    public static bool ShowRegistrations(Application? application) =>
        Resolve(
            application,
            p => p.ActionFamily == ApplicationProfileActionFamily.Registration,
            t => t.ShowRegistrations);

    public static bool ShowMigrationService(Application? application) =>
        !ShowRegistrations(application) && !ShowBusinessTrips(application);

    public static bool ShowInvitations(Application? application) =>
        Resolve(application, p => p.ProduceInvitation, t => t.ShowInvitations || t.CanIssueInvitation);

    public static bool ShowWorkPermits(Application? application) =>
        Resolve(application, p => p.ProduceWorkPermit, t => t.ShowWorkPermits || t.CanIssueWorkPermit);

    public static bool ShowRejections(Application? application) =>
        Resolve(application, p => p.RequirePersonRejectionItem, t => t.ShowRejections);

    public static bool ShowApplicationItems(Application? application)
    {
        if (application?.ApplicationProfile is { } profile)
        {
            if (profile.ActionFamily is ApplicationProfileActionFamily.Registration
                or ApplicationProfileActionFamily.BusinessTrip)
            {
                return true;
            }

            if (application.ApplicationType != null)
                return application.ApplicationType.ShowApplicationItems;

            return profile.RequirePersonPassport
                || profile.RequirePersonVisa
                || profile.RequirePersonInvitationItem
                || profile.RequirePersonWorkPermitItem;
        }

        return application?.ApplicationType?.ShowApplicationItems == true;
    }

    public static bool ShowApprovalLegProfile(Application? application)
    {
        if (GetProgressRoute(application) != ApplicationProgressRouteKind.ViaMinistries)
            return false;

        if (application?.ApplicationProfile is { } profile)
        {
            if (profile.ApprovalLegs?.Any(l => l.ApprovingMinistry != null) == true)
                return true;

            if (application.ApplicationType?.ShowApprovalLegProfile == true)
                return true;

            return false;
        }

        return application?.ApplicationType?.ShowApprovalLegProfile == true;
    }

    // --- ApplicationItem visibility ---

    public static bool ShowPreviousPassport(Application? application) =>
        Resolve(application, p => p.RequirePersonPassport, t => t.ShowPreviousPassport);

    public static bool ShowCurrentVisa(Application? application) =>
        Resolve(application, p => p.RequirePersonVisa, t => t.ShowCurrentVisa);

    public static bool ShowNextVisa(Application? application) =>
        Resolve(application, p => p.RequirePersonVisa, t => t.ShowNextVisa);

    public static bool ShowCurrentWorkPermitItem(Application? application) =>
        Resolve(application, p => p.RequirePersonWorkPermitItem, t => t.ShowCurrentWorkPermitItem);

    public static bool ShowPreviousWorkPermitItem(Application? application) =>
        Resolve(application, p => p.RequirePersonWorkPermitItem, t => t.ShowPreviousWorkPermitItem);

    public static bool ShowCurrentInvitationItem(Application? application) =>
        Resolve(application, p => p.RequirePersonInvitationItem, t => t.ShowCurrentInvitationItem);

    public static bool ShowPreviousInvitationItem(Application? application) =>
        Resolve(application, p => p.RequirePersonInvitationItem, t => t.ShowPreviousInvitationItem);

    public static bool ShowCurrentAddressOfResidence(Application? application) =>
        Resolve(application, p => p.RequirePersonAddressOfResidence, t => t.ShowCurrentAddressOfResidence);

    public static bool ShowCurrentWorkDuty(Application? application) =>
        Resolve(application, p => p.RequirePersonPosition, t => t.ShowCurrentWorkDuty);

    public static bool ShowCurrentSalary(Application? application) =>
        Resolve(application, p => p.RequirePersonSalary, t => t.ShowCurrentSalary);

    public static bool ShowCurrentMedicalRecord(Application? application) =>
        Resolve(application, p => p.RequirePersonMedical, t => t.ShowCurrentMedicalRecord);

    public static bool ShowCurrentEducation(Application? application) =>
        Resolve(application, p => p.RequirePersonEducation, t => t.ShowCurrentEducation);

    public static bool ShowInvitationItemIsCancelled(Application? application) =>
        Resolve(application, p => p.CancelInvitations, t => t.ShowInvitationItemIsCancelled);

    public static bool ShowWorkPermitItemIsCancelled(Application? application) =>
        Resolve(application, p => p.CancelWorkPermits, t => t.ShowWorkPermitItemIsCancelled);

    public static bool ShowVisaIsCancelled(Application? application) =>
        Resolve(application, p => p.CancelVisas, t => t.ShowVisaIsCancelled);

    public static bool ShowInvitationItemIsIssued(Application? application) =>
        ResolveTypeOnly(application, t => t.ShowInvitationItemIsIssued);

    public static bool ShowWorkPermitItemIsIssued(Application? application) =>
        ResolveTypeOnly(application, t => t.ShowWorkPermitItemIsIssued);

    public static bool ShowRejectionIssued(Application? application) =>
        ResolveTypeOnly(application, t => t.ShowRejectionIssued);

    public static bool ShowVisaIssued(Application? application) =>
        ResolveTypeOnly(application, t => t.ShowVisaIssued);

    public static bool ShowInvitationItemIsChanged(Application? application) =>
        ResolveTypeOnly(application, t => t.ShowInvitationItemIsChanged);

    public static bool ShowWorkPermitItemIsChanged(Application? application) =>
        ResolveTypeOnly(application, t => t.ShowWorkPermitItemIsChanged);

    public static bool ShowVisaIsChanged(Application? application) =>
        ResolveTypeOnly(application, t => t.ShowVisaIsChanged);

    private static bool Resolve(
        Application? application,
        Func<ApplicationProfile, bool> fromProfile,
        Func<ApplicationType, bool> fromType)
    {
        if (application?.ApplicationProfile is { } profile)
            return fromProfile(profile);

        if (application?.ApplicationType is { } type)
            return fromType(type);

        return false;
    }

    private static bool ResolveTypeOnly(Application? application, Func<ApplicationType, bool> fromType) =>
        application?.ApplicationType is { } type && fromType(type);
}
