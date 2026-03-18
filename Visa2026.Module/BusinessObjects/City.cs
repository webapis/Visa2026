using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using System;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Lookup/General/Geography")]
    
    public class City : LookupBase, ISoftDelete
    {


        [RuleRequiredField]
        public virtual Region Region { get; set; }

        [InverseProperty(nameof(AddressOfResidence.City))]
        public virtual IList<AddressOfResidence> AddressesOfResidence { get; set; } = new ObservableCollection<AddressOfResidence>();

        [Browsable(false)]
        public virtual bool IsDeleted { get; set; }

        [Browsable(false)]
        public virtual DateTime? DateDeleted { get; set; }

        [Browsable(false)]
        public virtual ApplicationUser DeletedBy { get; set; }

        public virtual string RegionName {get;set;} 

        [ModelDefault("AllowEdit", "False")]
        public virtual string PdfForm_Code { get; set; }
    }
}