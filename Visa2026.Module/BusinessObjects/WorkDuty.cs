using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Employee")]
    [DefaultProperty(nameof(Description))]
    [XafDisplayName("Gelmeginiň Maksady")]
    public class WorkDuty : BaseObject, IObjectSpaceLink, ICurrentPersonItem, ISoftDelete
    {
        [RuleRequiredField]
        [DataSourceCriteria("IsEmployee = true")]
        [ImmediatePostData]
        public virtual Person Person { get; set; }

        [RuleRequiredField]
        [FieldSize(FieldSizeAttribute.Unlimited)]
        [XafDisplayName("Gelmeginiň Maksady")]
        public virtual string Description { get; set; }

        [ImmediatePostData]
        [Appearance("WorkDuty_DisableUncheckIsActive", Enabled = false, Criteria = "IsActive")]
        public virtual bool IsActive { get; set; }

        public override void OnCreated()
        {
            base.OnCreated();
            CurrentPersonItemSync.OnCreated(this);
        }

        public override void OnSaving()
        {
            base.OnSaving();
            CurrentPersonItemSync.ApplyOnSaving(
                this,
                _ => Person,
                p => p.WorkDuties);
        }

        [Browsable(false)]
        public virtual bool IsDeleted { get; set; }

        [Browsable(false)]
        public virtual DateTime? DateDeleted { get; set; }

        [Browsable(false)]
        public virtual ApplicationUser DeletedBy { get; set; }

        #region IObjectSpaceLink
        [NotMapped]
        [Browsable(false)]
        public IObjectSpace ObjectSpace { get; set; }
        #endregion
    }
}
