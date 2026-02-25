using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    public class BusinessTripAddress : BaseObject
    {
        [RuleRequiredField]
        [InverseProperty(nameof(BusinessObjects.BusinessTrip.Address))]
        public virtual BusinessTrip BusinessTrip { get; set; }

        public virtual City City { get; set; }

        [MaxLength(255)]
        public virtual string FullAddress { get; set; }
    }
}