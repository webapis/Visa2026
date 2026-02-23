using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Organization")]
    public class WorkPermit : BaseObject
    {
        [RuleRequiredField]
        public virtual string WorkPermitNumber { get; set; }

        public virtual DateTime StartDate { get; set; }


        [RuleRequiredField]
        public virtual Application Application { get; set; }

        [Aggregated]
        public virtual IList<WorkPermitItem> WorkPermitItems { get; set; } = new ObservableCollection<WorkPermitItem>();

        [Aggregated]
        public virtual IList<WorkPermitDocument> Documents { get; set; } = new ObservableCollection<WorkPermitDocument>();

        [NotMapped]
        [Browsable(false)]
        public virtual IList<Employee> AvailableEmployees
        {
            get
            {
                return Application?.ApplicationItems.Select(ai => ai.Employee).Where(e => e != null).ToList() ?? new List<Employee>();
            }
        }
    }
}