using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate;

internal static class ApplicationTypeConfigurationApplier
{
    public static void Apply(ApplicationType target, ApplicationTypeConfigurationRow source, bool overwriteShowFlags)
    {
        if (!string.IsNullOrWhiteSpace(source.NameTm))
            target.NameTm = source.NameTm;
        if (!string.IsNullOrWhiteSpace(source.Name))
            target.LocalizationKey = source.Name;
        if (!string.IsNullOrWhiteSpace(source.Code))
            target.Code = source.Code;

        target.PdfForm_Code = source.PdfForm_Code;
        target.LifecycleStage = source.LifecycleStage;
        target.Category = source.Category;
        target.DurationInDays = source.DurationInDays;

        if (overwriteShowFlags)
            ApplyShowFlags(target, source);
    }

    /// <summary>Always overwrites every <c>Show*</c> flag from seed (deploy policy).</summary>
    public static void ApplyShowFlags(ApplicationType target, ApplicationTypeConfigurationRow source)
    {
        target.ShowProjectContract = source.ShowProjectContract;
        target.ShowVisaPeriod = source.ShowVisaPeriod;
        target.ShowVisaCategory = source.ShowVisaCategory;
        target.ShowVisaType = source.ShowVisaType;
        target.ShowUrgency = source.ShowUrgency;
        target.ShowInvitations = source.ShowInvitations;
        target.ShowRejections = source.ShowRejections;
        target.ShowWorkPermits = source.ShowWorkPermits;
        target.ShowRegistrations = source.ShowRegistrations;
        target.ShowVisas = source.ShowVisas;
        target.ShowApplicationItems = source.ShowApplicationItems;
        target.ShowApplicationReason = source.ShowApplicationReason;
        target.ShowMigrationService = source.ShowMigrationService;
        target.ShowBusinessTrips = source.ShowBusinessTrips;
        target.ShowFromCity = source.ShowFromCity;
        target.ShowToCity = source.ShowToCity;
        target.ShowMovementPermitLocation = source.ShowMovementPermitLocation;
        target.ShowBorderZoneLocation = source.ShowBorderZoneLocation;
        target.ShowPreviousPassport = source.ShowPreviousPassport;
        target.ShowCurrentVisa = source.ShowCurrentVisa;
        target.ShowNextVisa = source.ShowNextVisa;
        target.ShowCurrentWorkPermitItem = source.ShowCurrentWorkPermitItem;
        target.ShowPreviousWorkPermitItem = source.ShowPreviousWorkPermitItem;
        target.ShowCurrentInvitationItem = source.ShowCurrentInvitationItem;
        target.ShowPreviousInvitationItem = source.ShowPreviousInvitationItem;
        target.ShowCurrentAddressOfResidence = source.ShowCurrentAddressOfResidence;
        target.ShowCurrentEmployeeContract = source.ShowCurrentEmployeeContract;
        target.ShowCurrentWorkDuty = source.ShowCurrentWorkDuty;
        target.ShowCurrentMedicalRecord = source.ShowCurrentMedicalRecord;
        target.ShowCurrentEducation = source.ShowCurrentEducation;
        target.ShowInvitationItemIsIssued = source.ShowInvitationItemIsIssued;
        target.ShowWorkPermitItemIsIssued = source.ShowWorkPermitItemIsIssued;
        target.ShowRejectionIssued = source.ShowRejectionIssued;
        target.ShowVisaIssued = source.ShowVisaIssued;
        target.ShowVisaIsCancelled = source.ShowVisaIsCancelled;
        target.ShowVisaIsChanged = source.ShowVisaIsChanged;
        target.ShowInvitationItemIsCancelled = source.ShowInvitationItemIsCancelled;
        target.ShowWorkPermitItemIsCancelled = source.ShowWorkPermitItemIsCancelled;
        target.ShowInvitationItemIsChanged = source.ShowInvitationItemIsChanged;
        target.ShowWorkPermitItemIsChanged = source.ShowWorkPermitItemIsChanged;
    }
}
