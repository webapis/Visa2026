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
using DevExpress.ExpressApp.Model;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Application")]
    [DefaultProperty(nameof(ApplicationNumber))]
    [RuleCriteria("ApplicationProcessDateRange", DefaultContexts.Save, "ProcessDate is null or ProcessDate >= ApplicationDate", CustomMessageTemplate = "Process Date cannot be earlier than Application Date.")]
    public class Application : BaseObject
    {
        [MaxLength(50)]
        [RuleUniqueValue]
        public virtual string ApplicationNumber { get; set; }

        [RuleRequiredField]
        public virtual DateTime ApplicationDate { get; set; }

        [ImmediatePostData]
        public virtual bool IsForFamily { get; set; }

        [ImmediatePostData]
        [Appearance("EmployeeAppTypeVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "IsForFamily", Context = "DetailView")]
        [RuleRequiredField(TargetCriteria = "!IsForFamily")]
        public virtual ApplicationTypeForEmployee? EmployeeApplicationType { get; set; }

        [ImmediatePostData]
        [Appearance("FamilyAppTypeVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "!IsForFamily", Context = "DetailView")]
        [RuleRequiredField(TargetCriteria = "IsForFamily")]
        public virtual ApplicationTypeForFamilyMember? FamilyApplicationType { get; set; }

        public virtual ApplicationStatus Status { get; set; }

        [Appearance("IsWorkPermitRequiredVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "!(!IsForFamily && EmployeeApplicationType = 'ApplicationForVisaExtention')", Context = "DetailView")]
        public virtual bool IsWorkPermitRequired { get; set; } = true;

        public virtual bool Cancelled { get; set; }

        public virtual DateTime? ProcessDate { get; set; }

        [MaxLength(50)]
        public virtual string ProcessNumber { get; set; }

        [Appearance("ProjectContractVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "!((!IsForFamily && EmployeeApplicationType In ('ApplicationForInvitation', 'ApplicationForVisaExtention', 'ApplicationForChangingInvitation', 'RugsatnamaMöhletineGöräÇakylyk', 'ApplicationForBorderZonePermision')) || (IsForFamily && FamilyApplicationType In ('ApplicationForInvitation', 'ApplicationForVisaExtention')))", Context = "DetailView")]
        public virtual ProjectContract ProjectContract { get; set; }

        public virtual Urgency Urgency { get; set; }

        [Appearance("VisaPeriodVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "!((!IsForFamily && EmployeeApplicationType In ('ApplicationForInvitation', 'ApplicationForVisaExtention', 'ApplicationForChangingInvitation', 'RugsatnamaMöhletineGöräÇakylyk')) || (IsForFamily && FamilyApplicationType In ('ApplicationForInvitation', 'ApplicationForVisaExtention')))", Context = "DetailView")]
        public virtual VisaPeriod VisaPeriod { get; set; }

        [Appearance("VisaCategoryVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "!((!IsForFamily && EmployeeApplicationType In ('ApplicationForInvitation', 'ApplicationForVisaExtention', 'ApplicationForChangingInvitation', 'RugsatnamaMöhletineGöräÇakylyk')) || (IsForFamily && FamilyApplicationType In ('ApplicationForInvitation', 'ApplicationForVisaExtention')))", Context = "DetailView")]
        public virtual VisaCategory VisaCategory { get; set; }

        [Appearance("MinistryVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "!((!IsForFamily && EmployeeApplicationType In ('ApplicationForInvitation', 'ApplicationForVisaExtention', 'ApplicationForChangingInvitation', 'RugsatnamaMöhletineGöräÇakylyk', 'ApplicationForBorderZonePermision')) || (IsForFamily && FamilyApplicationType In ('ApplicationForInvitation', 'ApplicationForVisaExtention')))", Context = "DetailView")]
        public virtual Ministry Ministry { get; set; }

   
       
        [DevExpress.ExpressApp.DC.Aggregated]
        [InverseProperty(nameof(ApplicationItem.Application))]
        public virtual IList<ApplicationItem> ApplicationItems { get; set; } = new ObservableCollection<ApplicationItem>();

        [DevExpress.ExpressApp.DC.Aggregated]
        [InverseProperty(nameof(Invitation.Application))]
        public virtual IList<Invitation> Invitations { get; set; } = new ObservableCollection<Invitation>();

        [DevExpress.ExpressApp.DC.Aggregated]
        [InverseProperty(nameof(Rejection.Application))]
        public virtual IList<Rejection> Rejections { get; set; } = new ObservableCollection<Rejection>();

        [DevExpress.ExpressApp.DC.Aggregated]
        [InverseProperty(nameof(WorkPermit.Application))]
        public virtual IList<WorkPermit> WorkPermits { get; set; } = new ObservableCollection<WorkPermit>();
    }
}