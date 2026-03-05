using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Visa2026.Module.BusinessObjects
{
    public enum BusinessTripStatus
    {
        Planned,
        Ongoing,
        Completed,
        Cancelled
    }

    [DefaultClassOptions]
    [NavigationItem("Application")]
    [DefaultProperty(nameof(DefaultProperty))]
    public class BusinessTrip : SingleActiveBaseObject<Person, BusinessTrip>
    {
        [NotMapped]
        [Browsable(false)]
        public string DefaultProperty => $"{Person?.FullName} - {Purpose}";

        [RuleRequiredField]
        [DataSourceCriteria("IsEmployee = true")]
        public virtual Person Person { get; set; }

        [RuleRequiredField]
        [MaxLength(255)]
        public virtual string Purpose { get; set; }

        [RuleRequiredField]
        public virtual Country DestinationCountry { get; set; }

        [MaxLength(100)]
        public virtual string DestinationCity { get; set; }

        [RuleRequiredField]
        public virtual DateTime StartDate { get; set; }

        [RuleRequiredField]
        public virtual DateTime EndDate { get; set; }

        public virtual BusinessTripStatus Status { get; set; }

        public virtual Application Application { get; set; }

        [Aggregated]
        public virtual BusinessTripAddress Address { get; set; }

        public override Person GetParent()
        {
            return Person;
        }

        public override IList<BusinessTrip> GetSiblings(Person parent)
        {
            return parent?.BusinessTrips;
        }

        public override void SetParentActiveItem(Person parent, BusinessTrip item)
        {
            parent.CurrentBusinessTrip = item;
        }

        public override bool IsParentActiveItem(Person parent, BusinessTrip item)
        {
            return parent.CurrentBusinessTrip == item;
        }
    }
}