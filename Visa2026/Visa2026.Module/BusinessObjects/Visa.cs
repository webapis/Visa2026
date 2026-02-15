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
    [NavigationItem("Lookup/Visa")]
    public class Visa : SingleActiveBaseObject<Employee, Visa>
    {
        [MaxLength(50)]
        [RuleRequiredField]
        public virtual string VisaNumber { get; set; }

        public virtual DateTime StartDate { get; set; }

        public virtual DateTime EndDate { get; set; }

        public virtual Employee Employee { get; set; }

        public override Employee GetParent()
        {
            return Employee;
        }

        public override IList<Visa> GetSiblings(Employee parent)
        {
            return parent?.Visas;
        }

        public override void SetParentActiveItem(Employee parent, Visa item)
        {
            parent.CurrentVisa = item;
        }

        public override bool IsParentActiveItem(Employee parent, Visa item)
        {
            return parent.CurrentVisa == item;
        }
    }
}