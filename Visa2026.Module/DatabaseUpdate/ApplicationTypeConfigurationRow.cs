using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Code-first configuration for one <see cref="ApplicationType"/> row (keyed by <see cref="Name"/>).
/// </summary>
internal sealed class ApplicationTypeConfigurationRow
{
    public required string Name { get; init; }
    public string NameTm { get; init; } = "";
    public string Code { get; init; } = "";
    public int PdfForm_Code { get; init; }
    public ApplicationLifecycleStage LifecycleStage { get; init; }
    public ApplicationTypeCategory Category { get; init; }
    public int DurationInDays { get; init; }

    public ApplicationProgressRouteKind ApplicationProgressRoute { get; init; }
    public MinistryReviewDepth MinistryReviewDepth { get; init; }

    public bool ShowProjectContract { get; init; }
    public bool ShowVisaPeriod { get; init; }
    public bool ShowVisaCategory { get; init; }
    public bool ShowVisaType { get; init; }
    public bool ShowUrgency { get; init; }
    public bool ShowInvitations { get; init; }
    public bool ShowRejections { get; init; }
    public bool ShowWorkPermits { get; init; }
    public bool ShowRegistrations { get; init; }
    public bool ShowVisas { get; init; }
    public bool ShowApplicationItems { get; init; }
    public bool ShowApplicationReason { get; init; }
    public bool ShowMigrationService { get; init; }
    public bool ShowBusinessTrips { get; init; }
    public bool ShowFromCity { get; init; }
    public bool ShowToCity { get; init; }
    public bool ShowMovementPermitLocation { get; init; }
    public bool ShowBorderZoneLocation { get; init; }
    public bool ShowPreviousPassport { get; init; }
    public bool ShowCurrentVisa { get; init; }
    public bool ShowNextVisa { get; init; }
    public bool ShowCurrentWorkPermitItem { get; init; }
    public bool ShowPreviousWorkPermitItem { get; init; }
    public bool ShowCurrentInvitationItem { get; init; }
    public bool ShowPreviousInvitationItem { get; init; }
    public bool ShowCurrentAddressOfResidence { get; init; }
    public bool ShowCurrentWorkDuty { get; init; }
    public bool ShowCurrentSalary { get; init; }
    public bool ShowWorkPermittedLocations { get; init; }
    public bool ShowCurrentMedicalRecord { get; init; }
    public bool ShowCurrentEducation { get; init; }
    public bool ShowInvitationItemIsIssued { get; init; }
    public bool ShowWorkPermitItemIsIssued { get; init; }
    public bool ShowRejectionIssued { get; init; }
    public bool ShowVisaIssued { get; init; }
    public bool ShowVisaIsCancelled { get; init; }
    public bool ShowVisaIsChanged { get; init; }
    public bool ShowInvitationItemIsCancelled { get; init; }
    public bool ShowWorkPermitItemIsCancelled { get; init; }
    public bool ShowInvitationItemIsChanged { get; init; }
    public bool ShowWorkPermitItemIsChanged { get; init; }
}
