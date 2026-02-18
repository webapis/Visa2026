using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Lookup/Housing")]
    [DefaultProperty(nameof(Name))]
    public class Lodging : BaseObject
    {
        public Lodging()
        {
            Documents = new ObservableCollection<LodgingDocument>();
        }

        [RuleRequiredField]
        public virtual string Name { get; set; }

        [MaxLength(255)]
        [RuleRequiredField]
        public virtual string FullAddress { get; set; }

        [Aggregated]
        [InverseProperty(nameof(LodgingDocument.Lodging))]
        public virtual IList<LodgingDocument> Documents { get; set; }
    }
}