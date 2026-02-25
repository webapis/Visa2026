using System;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    public class BusinessTripPlan : BaseObject
    {
        public virtual DateTime StartDate { get; set; }

        public virtual DateTime EndDate { get; set; }

        [NotMapped]
        public int Duration
        {
            get
            {
                return (EndDate - StartDate).Days;
            }
        }

        public virtual Region Region { get; set; }

        [DataSourceCriteria("Region = '@This.Region'")]
        public virtual City City { get; set; }
    }
}