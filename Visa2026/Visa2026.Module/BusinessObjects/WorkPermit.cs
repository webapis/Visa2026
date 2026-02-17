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
    [NavigationItem("Organization")]
    public class WorkPermit : BaseObject
    {
        [RuleRequiredField]
        [RuleUniqueValue]
        public virtual string Number { get; set; }

        [RuleRequiredField]
        public virtual DateTime Date { get; set; }

        [Aggregated]
        public virtual IList<WorkPermitItem> Items { get; set; } = new ObservableCollection<WorkPermitItem>();

        [Aggregated]
        public virtual IList<WorkPermitDocument> Documents { get; set; } = new ObservableCollection<WorkPermitDocument>();

        public virtual Application Application { get; set; }
    }
}