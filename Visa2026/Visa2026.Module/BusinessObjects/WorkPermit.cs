using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.Model;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("WorkPermit")]
    public class WorkPermit : SingleActiveBaseObject<Employee, WorkPermit>
    {
        [MaxLength(50)]
        [RuleRequiredField]
        public virtual string WorkPermitNumber { get; set; }

        public virtual DateTime StartDate { get; set; }

        public virtual DateTime EndDate { get; set; }

        public virtual Employee Employee { get; set; }

        public virtual WorkPermitLocation Location { get; set; }

        public virtual WorkPermitLetter WorkPermitLetter { get; set; }

        public override Employee GetParent()
        {
            return Employee;
        }

        public override IList<WorkPermit> GetSiblings(Employee parent)
        {
            return parent?.WorkPermits;
        }

        public override void SetParentActiveItem(Employee parent, WorkPermit item)
        {
            parent.CurrentWorkPermit = item;
        }

        public override bool IsParentActiveItem(Employee parent, WorkPermit item)
        {
            return parent.CurrentWorkPermit == item;
        }
    }
}