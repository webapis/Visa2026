using System;
using System.ComponentModel.DataAnnotations;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Application")]
    public class ApplicationProgress : BaseObject
    {
        [RuleRequiredField]
        public virtual Application Application { get; set; }

        [RuleRequiredField]
        public virtual ApplicationState State { get; set; }

        [RuleRequiredField]
        public virtual ApplicationLocation Location { get; set; }

        [RuleRequiredField]
        public virtual DateTime Date { get; set; } = DateTime.Now;

        [MaxLength(255)]
        public virtual string Description { get; set; }
    }
}