using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace Visa2026.Module.BusinessObjects;

public partial class ApplicationProfileInstance
{
    // XAF Appearance criteria targets — profile-first visibility (slice 6).

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowApprovalLegProfile =>
        ApplicationProfileConfigurationResolver.ShowApprovalLegProfile(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowVisaPeriod => ApplicationProfileConfigurationResolver.ShowVisaPeriod(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowVisaCategory => ApplicationProfileConfigurationResolver.ShowVisaCategory(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowVisaType => ApplicationProfileConfigurationResolver.ShowVisaType(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowEntryCheckPoint =>
        ApplicationProfileConfigurationResolver.ShowEntryCheckPoint(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowProjectContract =>
        ApplicationProfileConfigurationResolver.ShowProjectContract(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowUrgency => ApplicationProfileConfigurationResolver.ShowUrgency(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowMigrationService =>
        ApplicationProfileConfigurationResolver.ShowMigrationService(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowBusinessTrips =>
        ApplicationProfileConfigurationResolver.ShowBusinessTrips(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowRegistrations =>
        ApplicationProfileConfigurationResolver.ShowRegistrations(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowMovementPermitLocation =>
        ApplicationProfileConfigurationResolver.ShowMovementPermitLocation(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowBorderZoneLocation =>
        ApplicationProfileConfigurationResolver.ShowBorderZoneLocation(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowFromCity => ApplicationProfileConfigurationResolver.ShowFromCity(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowToCity => ApplicationProfileConfigurationResolver.ShowToCity(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowApplicationItems =>
        ApplicationProfileConfigurationResolver.ShowApplicationItems(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowInvitations =>
        ApplicationProfileConfigurationResolver.ShowInvitations(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowRejections =>
        ApplicationProfileConfigurationResolver.ShowRejections(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowIssuedVisas =>
        ApplicationProfileConfigurationResolver.ShowIssuedVisas(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowWorkPermits =>
        ApplicationProfileConfigurationResolver.ShowWorkPermits(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowBorderZones =>
        ApplicationProfileConfigurationResolver.ShowBorderZones(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowPreviousPassport =>
        ApplicationProfileConfigurationResolver.ShowPreviousPassport(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowCurrentVisa =>
        ApplicationProfileConfigurationResolver.ShowCurrentVisa(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowNextVisa => ApplicationProfileConfigurationResolver.ShowNextVisa(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowCurrentWorkPermitItem =>
        ApplicationProfileConfigurationResolver.ShowCurrentWorkPermitItem(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowPreviousWorkPermitItem =>
        ApplicationProfileConfigurationResolver.ShowPreviousWorkPermitItem(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowCurrentInvitationItem =>
        ApplicationProfileConfigurationResolver.ShowCurrentInvitationItem(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowPreviousInvitationItem =>
        ApplicationProfileConfigurationResolver.ShowPreviousInvitationItem(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowCurrentAddressOfResidence =>
        ApplicationProfileConfigurationResolver.ShowCurrentAddressOfResidence(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowCurrentWorkDuty =>
        ApplicationProfileConfigurationResolver.ShowCurrentWorkDuty(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowCurrentSalary =>
        ApplicationProfileConfigurationResolver.ShowCurrentSalary(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowCurrentMedicalRecord =>
        ApplicationProfileConfigurationResolver.ShowCurrentMedicalRecord(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowCurrentEducation =>
        ApplicationProfileConfigurationResolver.ShowCurrentEducation(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowWorkPermittedLocations =>
        ApplicationProfileConfigurationResolver.ShowWorkPermittedLocations(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowInvitationItemIsIssued =>
        ApplicationProfileConfigurationResolver.ShowInvitationItemIsIssued(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowWorkPermitItemIsIssued =>
        ApplicationProfileConfigurationResolver.ShowWorkPermitItemIsIssued(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowRejectionIssued =>
        ApplicationProfileConfigurationResolver.ShowRejectionIssued(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowVisaIssued =>
        ApplicationProfileConfigurationResolver.ShowVisaIssued(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowInvitationItemIsCancelled =>
        ApplicationProfileConfigurationResolver.ShowInvitationItemIsCancelled(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowWorkPermitItemIsCancelled =>
        ApplicationProfileConfigurationResolver.ShowWorkPermitItemIsCancelled(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowVisaIsCancelled =>
        ApplicationProfileConfigurationResolver.ShowVisaIsCancelled(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowInvitationItemIsChanged =>
        ApplicationProfileConfigurationResolver.ShowInvitationItemIsChanged(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowWorkPermitItemIsChanged =>
        ApplicationProfileConfigurationResolver.ShowWorkPermitItemIsChanged(this);

    [Browsable(false)]
    [NotMapped]
    public bool CfgShowVisaIsChanged =>
        ApplicationProfileConfigurationResolver.ShowVisaIsChanged(this);
}
