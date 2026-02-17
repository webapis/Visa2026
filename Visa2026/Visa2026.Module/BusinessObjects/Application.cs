using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.Persistent.Base;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.Model;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Application")]
    [DefaultProperty(nameof(ApplicationNumber))]
    [RuleCriteria("ApplicationProcessDateRange", DefaultContexts.Save, "ProcessDate >= ApplicationDate", TargetCriteria = "ProcessDate is not null", CustomMessageTemplate = "Process Date cannot be earlier than Application Date.")]
    public class Application : BaseObject
    {
        [MaxLength(50)]
        [RuleUniqueValue]
        public virtual string ApplicationNumber { get; set; }

        [RuleRequiredField]
        public virtual DateTime ApplicationDate { get; set; }

        [ImmediatePostData]
        public virtual bool IsForFamily { get; set; }

        [ImmediatePostData, RuleRequiredField]
        [DataSourceCriteria("Category = 'Both' OR (Category = 'Employee' AND '@This.IsForFamily' = false) OR (Category = 'FamilyMember' AND '@This.IsForFamily' = true)")]
        public virtual ApplicationType ApplicationType { get; set; }

        [ModelDefault("AllowEdit", "False")]
        public virtual ApplicationProgress CurrentState { get; set; }

        public virtual DateTime? ProcessDate { get; set; }

        [MaxLength(50)]
        public virtual string ProcessNumber { get; set; }

        [Appearance("ProjectContractVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowProjectContract", Context = "DetailView")]
        public virtual ProjectContract ProjectContract { get; set; }

        public virtual Urgency Urgency { get; set; }

        [Appearance("VisaPeriodVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowVisaPeriod", Context = "DetailView")]
        public virtual VisaPeriod VisaPeriod { get; set; }

        [Appearance("VisaCategoryVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowVisaCategory", Context = "DetailView")]
        public virtual VisaCategory VisaCategory { get; set; }

        [Appearance("MinistryVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowMinistry", Context = "DetailView")]
        public virtual Ministry Ministry { get; set; }

        [Aggregated]
        [InverseProperty(nameof(ApplicationItem.Application))]
        public virtual IList<ApplicationItem> ApplicationItems { get; set; } = new ObservableCollection<ApplicationItem>();

        [Aggregated]
        [InverseProperty(nameof(Invitation.Application))]
        public virtual IList<Invitation> Invitations { get; set; } = new ObservableCollection<Invitation>();

        [Aggregated]
        [InverseProperty(nameof(Rejection.Application))]
        public virtual IList<Rejection> Rejections { get; set; } = new ObservableCollection<Rejection>();

        [Aggregated]
        [InverseProperty(nameof(WorkPermit.Application))]
        public virtual IList<WorkPermit> WorkPermits { get; set; } = new ObservableCollection<WorkPermit>();

        [Aggregated]
        [InverseProperty(nameof(ApplicationProgress.Application))]
        public virtual IList<ApplicationProgress> ProgressHistory { get; set; } = new ObservableCollection<ApplicationProgress>();
    }
}