using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.Model;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Rejection")]
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

        [InverseProperty(nameof(PersonInApplication.Rejection))]
        public virtual IList<PersonInApplication> People { get; set; } = new ObservableCollection<PersonInApplication>();
    }
}