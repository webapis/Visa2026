using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Employee")]
    [DefaultProperty(nameof(Description))]
    [XafDisplayName("Gelmeginiň Maksady")]
    public class WorkDuty : SingleActiveBaseObject<Person, WorkDuty>, ISoftDelete
    {
        [RuleRequiredField]
        [DataSourceCriteria("IsEmployee = true")]
        [ImmediatePostData]
        public virtual Person Person { get; set; }

        [RuleRequiredField]
        [FieldSize(FieldSizeAttribute.Unlimited)]
        [XafDisplayName("Gelmeginiň Maksady")]
        public virtual string Description { get; set; }

        #region SingleActiveBaseObject
        public override Person GetParent()
        {
            return Person;
        }

        public override IList<WorkDuty> GetSiblings(Person parent)
        {
            return parent?.WorkDuties;
        }

        public override void SetParentActiveItem(Person parent, WorkDuty item)
        {
            parent.CurrentWorkDuty = item;
        }

        public override bool IsParentActiveItem(Person parent, WorkDuty item)
        {
            return parent.CurrentWorkDuty == item;
        }
        #endregion

        #region ISoftDelete
        [Browsable(false)]
        public virtual bool IsDeleted { get; set; }

        [Browsable(false)]
        public virtual DateTime? DateDeleted { get; set; }

        [Browsable(false)]
        public virtual ApplicationUser DeletedBy { get; set; }
        #endregion
    }
}
