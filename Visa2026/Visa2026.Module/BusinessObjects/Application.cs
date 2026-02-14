using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [DefaultProperty(nameof(ApplicationNumber))]
    [RuleCriteria("ApplicationProcessDateRange", DefaultContexts.Save, "ProcessDate is null or ProcessDate >= ApplicationDate", CustomMessageTemplate = "Process Date cannot be earlier than Application Date.")]
    public class Application : BaseObject
    {
        [MaxLength(50)]
        [RuleUniqueValue]
        public virtual string ApplicationNumber { get; set; }

        [RuleRequiredField]
        public virtual DateTime ApplicationDate { get; set; }

        public virtual bool IsForFamily { get; set; }

        [Appearance("EmployeeAppTypeVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "IsForFamily", Context = "DetailView")]
        [RuleRequiredField(TargetCriteria = "!IsForFamily")]
        public virtual ApplicationTypeForEmployee? EmployeeApplicationType { get; set; }

        [Appearance("FamilyAppTypeVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "!IsForFamily", Context = "DetailView")]
        [RuleRequiredField(TargetCriteria = "IsForFamily")]
        public virtual ApplicationTypeForFamilyMember? FamilyApplicationType { get; set; }

        public virtual ApplicationStatus Status { get; set; }

        public virtual bool IsWorkPermitRequired { get; set; } = true;

        public virtual bool Cancelled { get; set; }

        public virtual DateTime? ProcessDate { get; set; }

        [MaxLength(50)]
        public virtual string ProcessNumber { get; set; }

        public virtual ProjectContract ProjectContract { get; set; }

        public virtual Urgency Urgency { get; set; }

        public virtual VisaPeriod VisaPeriod { get; set; }

        public virtual VisaCategory VisaCategory { get; set; }

        public virtual Ministry Ministry { get; set; }

        public virtual Invitation InvitationToBeChanged { get; set; }

        public virtual WorkPermitLocation NewRegistrationLocation { get; set; }

        public virtual WorkPermitLocation PreviousRegistrationLocation { get; set; }

        public virtual StrikeOffType? StrikeOffType { get; set; }

        public virtual ChangeInfoType? ChangeInfoType { get; set; }

        public virtual WorkPermitLocation BusinessTripDestination { get; set; }

        public virtual DateTime? DateOfDeparture { get; set; }

        public virtual int? DurationOfStay { get; set; }

        public virtual VisaPeriod BorderZonePeriod { get; set; }

        public virtual IList<BorderZone> BorderZones { get; set; } = new ObservableCollection<BorderZone>();

        [DevExpress.ExpressApp.DC.Aggregated]
        [InverseProperty(nameof(PersonInApplication.Application))]
        public virtual IList<PersonInApplication> PersonsInApplication { get; set; } = new ObservableCollection<PersonInApplication>();

        [DevExpress.ExpressApp.DC.Aggregated]
        [InverseProperty(nameof(Invitation.Application))]
        public virtual IList<Invitation> Invitations { get; set; } = new ObservableCollection<Invitation>();

        [DevExpress.ExpressApp.DC.Aggregated]
        [InverseProperty(nameof(Rejection.Application))]
        public virtual IList<Rejection> Rejections { get; set; } = new ObservableCollection<Rejection>();
    }
}