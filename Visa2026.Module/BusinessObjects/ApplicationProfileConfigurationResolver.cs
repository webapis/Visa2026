using System;
using System.Linq;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Effective ApplicationProfileInstance configuration: <see cref="Application.ApplicationProfile"/> first,
/// deprecated <see cref="Application.ApplicationType"/> fallback during dual-read (slice 6).
/// Inverse of <see cref="DatabaseUpdate.ApplicationProfileFromApplicationTypeMapper"/> where mapped.
/// </summary>
public static class ApplicationProfileConfigurationResolver
{
    public static ApplicationProfileInstanceProgressRouteKind? GetProgressRoute(ApplicationProfileInstance? application)
    {
        if (application == null)
            return null;

        if (application.CreationProgressRoute.HasValue)
            return application.CreationProgressRoute.Value;

        if (application.ApplicationProfile != null)
            return application.ApplicationProfile.ProgressRoute;

        return application.ApplicationType?.ApplicationProfileInstanceProgressRoute;
    }

    public static bool HasConfiguration(ApplicationProfileInstance? application) =>
        application?.ApplicationProfile != null || application?.ApplicationType != null;

    public static bool CanIssueVisa(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.ProduceVisa, t => t.CanIssueVisa);

    public static bool CanIssueInvitation(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.ProduceInvitation, t => t.CanIssueInvitation);

    public static bool CanIssueWorkPermit(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.ProduceWorkPermit, t => t.CanIssueWorkPermit);

    public static bool CanIssueBorderZone(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.ProduceBorderZone, t => t.ShowBorderZoneLocation);

    public static bool CanIssueRejection(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.ProduceRejection, t => t.ShowRejections);

    public static bool CanBeIssuingApplicationProfileInstanceForVisa(ApplicationProfileInstance? application) =>
        CanIssueVisa(application) || CanIssueInvitation(application);

    public static int GetMigrationSlaMaxDays(ApplicationProfileInstance? application)
    {
        if (application?.ApplicationProfile is { MigrationSlaDays: > 0 } profile)
            return profile.MigrationSlaDays;

        return 0;
    }

    public static int GetMinistrySlaMaxDays(ApplicationProfileInstance? application)
    {
        if (application?.ApplicationProfile is { MinistrySlaDays: > 0 } profile)
            return profile.MinistrySlaDays;

        return 0;
    }

    public static bool HasMigrationSlaConfigured(ApplicationProfileInstance? application) =>
        GetMigrationSlaMaxDays(application) > 0;

    public static bool HasMinistrySlaConfigured(ApplicationProfileInstance? application) =>
        GetMinistrySlaMaxDays(application) > 0;

    public static int GetEmbeddedProfileMinistryLegCount(ApplicationProfileInstance? application)
    {
        var snapshotCount = application?.ApprovalLegSnapshots?
            .Count(s => !string.IsNullOrWhiteSpace(s.MinistryShortName)) ?? 0;
        if (snapshotCount > 0)
            return snapshotCount;

        return ApplicationProfileApprovalLegVersionHelper.GetConfiguredLegCount(application?.ApplicationProfile);
    }

    // --- ApplicationProfileInstance DetailView visibility (maps from profile Require* / ActionFamily) ---

    public static bool ShowVisaType(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.RequireVisaType, t => t.ShowVisaType);

    public static bool ShowEntryCheckPoint(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.RequireEntryCheckPoint, t => false);

    public static bool ShowVisaCategory(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.RequireVisaCategory, t => t.ShowVisaCategory);

    public static bool ShowVisaPeriod(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.RequireVisaPeriod, t => t.ShowVisaPeriod);

    public static bool ShowProjectContract(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.RequireProject, t => t.ShowProjectContract);

    public static bool ShowUrgency(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.RequireUrgency, t => t.ShowUrgency);

    public static bool ShowBorderZoneLocation(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.RequireBorderZone, t => t.ShowBorderZoneLocation);

    public static bool ShowMovementPermitLocation(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.RequireWorkPermitLocation, t => t.ShowMovementPermitLocation);

    public static bool ShowWorkPermittedLocations(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.RequireWorkPermitLocation, t => t.ShowWorkPermittedLocations);

    public static bool ShowFromCity(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.RequireRegionCity, t => t.ShowFromCity);

    public static bool ShowToCity(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.RequireRegionCity, t => t.ShowToCity);

    public static bool ShowRegion(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.RequireRegion, _ => false);

    public static bool ShowCity(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.RequireCity, _ => false);

    public static bool ShowBusinessTripAddress(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.RequireBusinessTripAddress, t => t.ShowBusinessTrips);

    public static bool ShowPurpose(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.RequirePurpose, t => t.ShowBusinessTrips);

    public static bool ShowBusinessTrips(ApplicationProfileInstance? application) =>
        Resolve(
            application,
            p => p.ActionFamily == ApplicationProfileActionFamily.BusinessTrip || p.RequireStartDate,
            t => t.ShowBusinessTrips);

    public static bool ShowRegistrations(ApplicationProfileInstance? application) =>
        Resolve(
            application,
            p => p.ActionFamily == ApplicationProfileActionFamily.Registration,
            t => t.ShowRegistrations);

    public static bool ShowMigrationService(ApplicationProfileInstance? application) =>
        !ShowRegistrations(application) && !ShowBusinessTrips(application);

    public static bool ShowInvitations(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.ProduceInvitation, t => t.ShowInvitations || t.CanIssueInvitation);

    public static bool ShowWorkPermits(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.ProduceWorkPermit, t => t.ShowWorkPermits || t.CanIssueWorkPermit);

    public static bool ShowBorderZones(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.ProduceBorderZone, t => t.ShowBorderZoneLocation);

    public static bool ShowIssuedVisas(ApplicationProfileInstance? application) =>
        CanIssueVisa(application);

    public static bool ShowRejections(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.ProduceRejection, t => t.ShowRejections);

    public static bool RequirePersonRejectionItem(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.RequirePersonRejectionItem, t => t.ShowRejections);

    public static bool ShowApplicationItems(ApplicationProfileInstance? application)
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

    public static bool ShowApprovalLegProfile(ApplicationProfileInstance? application)
    {
        if (GetProgressRoute(application) != ApplicationProfileInstanceProgressRouteKind.ViaMinistries)
            return false;

        if (application?.ApplicationProfile is { } profile)
        {
            if (profile.ApprovalLegs?.Any(l => l.ApprovingMinistry != null) == true
                || ApplicationProfileApprovalLegVersionHelper.GetConfiguredLegCount(profile) > 0)
                return true;

            if (application.ApplicationType?.ShowApprovalLegProfile == true)
                return true;

            return false;
        }

        return application?.ApplicationType?.ShowApprovalLegProfile == true;
    }

    // --- ApplicationRosterMergeLine visibility ---

    public static bool ShowPreviousPassport(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.RequirePersonPassport, t => t.ShowPreviousPassport);

    public static bool ShowCurrentVisa(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.RequirePersonVisa, t => t.ShowCurrentVisa);

    public static bool ShowNextVisa(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.RequirePersonVisa, t => t.ShowNextVisa);

    public static bool ShowCurrentWorkPermitItem(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.RequirePersonWorkPermitItem, t => t.ShowCurrentWorkPermitItem);

    public static bool ShowPreviousWorkPermitItem(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.RequirePersonWorkPermitItem, t => t.ShowPreviousWorkPermitItem);

    public static bool ShowCurrentInvitationItem(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.RequirePersonInvitationItem, t => t.ShowCurrentInvitationItem);

    public static bool ShowPreviousInvitationItem(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.RequirePersonInvitationItem, t => t.ShowPreviousInvitationItem);

    public static bool ShowCurrentAddressOfResidence(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.RequirePersonAddressOfResidence, t => t.ShowCurrentAddressOfResidence);

    public static bool ShowCurrentWorkDuty(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.RequirePersonPosition, t => t.ShowCurrentWorkDuty);

    public static bool ShowCurrentSalary(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.RequirePersonSalary, t => t.ShowCurrentSalary);

    public static bool ShowCurrentMedicalRecord(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.RequirePersonMedical, t => t.ShowCurrentMedicalRecord);

    public static bool ShowCurrentEducation(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.RequirePersonEducation, t => t.ShowCurrentEducation);

    /// <summary>Profile-only (no Type Show*); gates BorderZoneItem auto-link.</summary>
    public static bool RequirePersonBorderZoneItem(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.RequirePersonBorderZoneItem, _ => false);

    /// <summary>Profile-only (no Type Show*); gates TravelHistory auto-link. Business-trip profiles never require this.</summary>
    public static bool RequirePersonTravelHistory(ApplicationProfileInstance? application)
    {
        if (application?.ApplicationProfile is { ActionFamily: ApplicationProfileActionFamily.BusinessTrip })
            return false;

        return Resolve(application, p => p.RequirePersonTravelHistory, _ => false);
    }

    public static bool ShowInvitationItemIsCancelled(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.CancelInvitations, t => t.ShowInvitationItemIsCancelled);

    public static bool ShowWorkPermitItemIsCancelled(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.CancelWorkPermits, t => t.ShowWorkPermitItemIsCancelled);

    public static bool ShowVisaIsCancelled(ApplicationProfileInstance? application) =>
        Resolve(application, p => p.CancelVisas, t => t.ShowVisaIsCancelled);

    public static bool ShowInvitationItemIsIssued(ApplicationProfileInstance? application) =>
        ResolveTypeOnly(application, t => t.ShowInvitationItemIsIssued);

    public static bool ShowWorkPermitItemIsIssued(ApplicationProfileInstance? application) =>
        ResolveTypeOnly(application, t => t.ShowWorkPermitItemIsIssued);

    public static bool ShowRejectionIssued(ApplicationProfileInstance? application) =>
        ResolveTypeOnly(application, t => t.ShowRejectionIssued);

    public static bool ShowVisaIssued(ApplicationProfileInstance? application) =>
        ResolveTypeOnly(application, t => t.ShowVisaIssued);

    public static bool ShowInvitationItemIsChanged(ApplicationProfileInstance? application) =>
        ResolveTypeOnly(application, t => t.ShowInvitationItemIsChanged);

    public static bool ShowWorkPermitItemIsChanged(ApplicationProfileInstance? application) =>
        ResolveTypeOnly(application, t => t.ShowWorkPermitItemIsChanged);

    public static bool ShowVisaIsChanged(ApplicationProfileInstance? application) =>
        ResolveTypeOnly(application, t => t.ShowVisaIsChanged);

    private static bool Resolve(
        ApplicationProfileInstance? application,
        Func<ApplicationProfile, bool> fromProfile,
        Func<ApplicationType, bool> fromType)
    {
        if (application?.ApplicationProfile is { } profile)
            return fromProfile(profile);

        if (application?.ApplicationType is { } type)
            return fromType(type);

        return false;
    }

    private static bool ResolveTypeOnly(ApplicationProfileInstance? application, Func<ApplicationType, bool> fromType) =>
        application?.ApplicationType is { } type && fromType(type);
}
