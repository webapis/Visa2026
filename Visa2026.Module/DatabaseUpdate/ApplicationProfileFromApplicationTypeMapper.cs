using System;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Maps deprecated <see cref="ApplicationType"/> configuration onto <see cref="ApplicationProfile"/>.
/// Used during dual-read until <see cref="Application.ApplicationType"/> is removed.
/// </summary>
public static class ApplicationProfileFromApplicationTypeMapper
{
    public static string ResolveProfileCode(ApplicationType type)
    {
        if (!string.IsNullOrWhiteSpace(type.Code))
            return type.Code.Trim();

        if (string.IsNullOrWhiteSpace(type.Name))
            return "UNKNOWN_TYPE";

        var slug = type.Name.Trim().Replace('_', '-').ToLowerInvariant();
        return slug.Length <= 64 ? slug : slug[..64];
    }

    public static void Apply(ApplicationProfile profile, ApplicationType type)
    {
        profile.Name = ResolveDisplayName(type);
        profile.Description = string.IsNullOrWhiteSpace(type.NameTm) ? null : type.NameTm.Trim();
        profile.Code = ResolveProfileCode(type);
        profile.SelectionCode = string.IsNullOrWhiteSpace(type.SelectionCode) ? null : type.SelectionCode.Trim();
        profile.ProgressRoute = type.ApplicationProfileInstanceProgressRoute;
        profile.ActionFamily = ResolveActionFamily(type);
        profile.RegistrationKind = ApplicationProfileRegistrationKindHelper.ResolveFromTypeName(
            profile.ActionFamily,
            type.Name);
        ApplyAudience(profile, type);
        ApplyProduceCancel(profile, type);
        ApplyPerApplicationRequirements(profile, type);
        ApplyPersonToggles(profile, type);
        ApplicationProfileRegistrationKindHelper.ApplyRegistrationPersonDefaults(profile);
        ApplySla(profile, type);
        profile.IsActive = true;
    }

    private static string ResolveDisplayName(ApplicationType type)
    {
        if (!string.IsNullOrWhiteSpace(type.NameTm))
            return type.NameTm.Trim();

        if (string.IsNullOrWhiteSpace(type.Name))
            return "ApplicationProfileInstance profile";

        return type.Name.Replace('_', ' ').Trim();
    }

    private static ApplicationProfileActionFamily ResolveActionFamily(ApplicationType type)
    {
        if (type.ShowRegistrations)
            return ApplicationProfileActionFamily.Registration;

        if (type.ShowVisaIsCancelled
            || type.ShowInvitationItemIsCancelled
            || type.ShowWorkPermitItemIsCancelled)
        {
            return ApplicationProfileActionFamily.Cancellation;
        }

        if (type.ShowBusinessTrips)
            return ApplicationProfileActionFamily.BusinessTrip;

        return ApplicationProfileActionFamily.Issuance;
    }

    private static void ApplyAudience(ApplicationProfile profile, ApplicationType type)
    {
        switch (type.Category)
        {
            case ApplicationTypeCategory.FamilyMember:
                profile.ForEmployee = false;
                profile.ForFamilyMember = true;
                profile.ForTemporaryVisitor = false;
                break;
            case ApplicationTypeCategory.Both:
                profile.ForEmployee = true;
                profile.ForFamilyMember = true;
                profile.ForTemporaryVisitor = false;
                break;
            default:
                profile.ForEmployee = true;
                profile.ForFamilyMember = false;
                profile.ForTemporaryVisitor = false;
                break;
        }
    }

    private static void ApplyProduceCancel(ApplicationProfile profile, ApplicationType type)
    {
        profile.ProduceInvitation = type.CanIssueInvitation;
        profile.ProduceWorkPermit = type.CanIssueWorkPermit;
        profile.ProduceVisa = type.CanIssueVisa;
        profile.ProduceBorderZone = type.ShowBorderZoneLocation;
        profile.ProduceWorkLocation = type.ShowMovementPermitLocation || type.ShowWorkPermittedLocations;
        profile.ProduceRejection = type.ShowRejections;

        profile.CancelInvitations = type.ShowInvitationItemIsCancelled;
        profile.CancelWorkPermits = type.ShowWorkPermitItemIsCancelled;
        profile.CancelVisas = type.ShowVisaIsCancelled;
        profile.CancelBorderZonePermits = false;
        profile.CancelApplicationProfileInstances = false;
    }

    private static void ApplyPerApplicationRequirements(ApplicationProfile profile, ApplicationType type)
    {
        profile.RequireVisaType = type.ShowVisaType;
        profile.RequireVisaCategory = type.ShowVisaCategory;
        profile.RequireVisaPeriod = type.ShowVisaPeriod;
        profile.RequireBorderZone = type.ShowBorderZoneLocation;
        profile.RequireMigrationService = type.ShowMigrationService;
        profile.RequireStartDate = type.ShowBusinessTrips;
        profile.RequireEndDate = type.ShowBusinessTrips;
        profile.RequireRegionCity = type.ShowFromCity || type.ShowToCity;
        profile.RequireRegion = profile.RequireRegionCity;
        profile.RequireCity = profile.RequireRegionCity;
        profile.RequireBusinessTripAddress = type.ShowBusinessTrips;
        profile.RequireProject = type.ShowProjectContract;
        profile.RequireUrgency = type.ShowUrgency;
        profile.RequireWorkPermitLocation = type.ShowMovementPermitLocation || type.ShowWorkPermittedLocations;
        profile.RequireEntryDate = false;
        profile.RequireEntryCheckPoint = false;
    }

    private static void ApplyPersonToggles(ApplicationProfile profile, ApplicationType type)
    {
        profile.RequirePersonPassport = type.ShowPreviousPassport || type.ShowApplicationItems;
        profile.RequirePersonEducation = type.ShowCurrentEducation;
        profile.RequirePersonPosition = type.ShowCurrentWorkDuty;
        profile.RequirePersonAddressOfResidence = type.ShowCurrentAddressOfResidence;
        profile.RequirePersonVisa = type.ShowCurrentVisa || type.ShowNextVisa;
        profile.RequirePersonInvitationItem = type.ShowCurrentInvitationItem || type.ShowPreviousInvitationItem;
        profile.RequirePersonWorkPermitItem = type.ShowCurrentWorkPermitItem || type.ShowPreviousWorkPermitItem;
        profile.RequirePersonBorderZoneItem = type.ShowBorderZoneLocation;
        profile.RequirePersonSalary = type.ShowCurrentSalary;
        profile.RequirePersonMedical = type.ShowCurrentMedicalRecord;
        profile.RequirePersonRejectionItem = type.ShowRejections;
        profile.RequirePersonTravelHistory = type.ShowBusinessTrips;
    }

    private static void ApplySla(ApplicationProfile profile, ApplicationType type)
    {
        profile.MinistrySlaDays = type.ApplicationProfileInstanceProgressRoute == ApplicationProfileInstanceProgressRouteKind.ViaMinistries
            ? ResolveMinistrySlaDays(type.MinistryReviewDepth)
            : 14;

        profile.MigrationSlaDays = type.MigrationSlaProfile?.MaxDaysInReview is > 0 and var maxDays
            ? maxDays
            : 14;
    }

    private static int ResolveMinistrySlaDays(MinistryReviewDepth depth) =>
        depth switch
        {
            MinistryReviewDepth.FirstAndSecondMinistry => 21,
            MinistryReviewDepth.FirstMinistryOnly => 14,
            _ => 14,
        };
}
