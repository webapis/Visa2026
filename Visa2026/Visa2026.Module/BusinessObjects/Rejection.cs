using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Application")]
    public class Rejection : BaseObject
    {
        [RuleRequiredField]
        public virtual Application Application { get; set; }

        public virtual string Reason { get; set; }

        public virtual DateTime Date { get; set; }

        [InverseProperty(nameof(RejectionItem.Rejection))]
        [DevExpress.ExpressApp.DC.Aggregated]
        public virtual IList<RejectionItem> RejectionItems { get; set; } = new ObservableCollection<RejectionItem>();

        [NotMapped]
        [Browsable(false)]
        public virtual IList<Person> AvailablePeople
        {
            get
            {
                return Application?.ApplicationItems.Select(ai => ai.Person).ToList() ?? new List<Person>();
            }
        }
    }
}