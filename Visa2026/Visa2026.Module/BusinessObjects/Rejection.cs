using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    public class Rejection : BaseObject
    {
        [MaxLength(50)]
        [RuleRequiredField]
        public virtual string RejectionNumber { get; set; }

        public virtual DateTime RejectionDate { get; set; }

        [MaxLength(500)]
        public virtual string Reason { get; set; }

        [RuleRequiredField]
        public virtual Application Application { get; set; }

        public virtual IList<PersonInApplication> People { get; set; } = new ObservableCollection<PersonInApplication>();
    }
}