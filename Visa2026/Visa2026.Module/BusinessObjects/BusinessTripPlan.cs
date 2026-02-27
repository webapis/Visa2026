using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [DefaultProperty(nameof(DisplayName))]
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

        [NotMapped]
        
        public string DisplayName
        {
            get
            {
                var locationParts = new List<string>();
                if (Region != null)
                {
                    locationParts.Add(Region.Name);
                }
                if (City != null)
                {
                    locationParts.Add(City.Name);
                }

                string location = string.Join(" - ", locationParts);
                string dateRange = $"{StartDate:d} - {EndDate:d}";

                if (string.IsNullOrWhiteSpace(location))
                {
                    return $"Trip Plan {dateRange} ({Duration} days)";
                }

                return $"{location} {dateRange} ({Duration} days)";
            }
        }
    }
}