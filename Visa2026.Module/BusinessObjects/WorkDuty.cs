using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Employee")]
    [DefaultProperty(nameof(Description))]
    [XafDisplayName("Gelmeginiň Maksady")]
    public class WorkDuty  : BaseObject
    {
        [RuleRequiredField]
        [DataSourceCriteria("IsEmployee = true")]
        [ImmediatePostData]
        public virtual Person Person { get; set; }

        [RuleRequiredField]
        [FieldSize(FieldSizeAttribute.Unlimited)]
        [XafDisplayName("Gelmeginiň Maksady")]
        public virtual string Description { get; set; }

        /// <summary>Skip-navigation M2M with <see cref="ApplicationProfileInstance"/> (same pattern as Education). Not aggregated.</summary>
        [ModelDefault("AllowEdit", "False")]
        [VisibleInListView(false)]
        public virtual IList<ApplicationProfileInstance> ApplicationProfileInstances { get; set; } = new ObservableCollection<ApplicationProfileInstance>();

    }
}
