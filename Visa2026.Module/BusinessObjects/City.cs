using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using System;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Lookup/General/Geography")]
    [ModelDefault("DefaultListViewSort", "IsMostlyUsed Desc, NameTm")]
    [GlobalLookupCatalog(GlobalLookupCatalogKind.City)]
    public class City : GlobalLookupCatalogBase, ISoftDelete
    {


        [RuleRequiredField]
        [VisibleInListView(true)]
        public virtual Region Region { get; set; }

        [VisibleInListView(true)]
        [XafDisplayName("Mostly Used")]
        public virtual bool IsMostlyUsed { get; set; }

        [InverseProperty(nameof(AddressOfResidence.City))]
        public virtual IList<AddressOfResidence> AddressesOfResidence { get; set; } = new ObservableCollection<AddressOfResidence>();

        [Browsable(false)]
        public virtual bool IsDeleted { get; set; }

        [Browsable(false)]
        public virtual DateTime? DateDeleted { get; set; }

        [Browsable(false)]
        public virtual ApplicationUser DeletedBy { get; set; }

        [VisibleInListView(false)]
        public virtual string RegionName {get;set;} 

        [VisibleInListView(false)]
        [ModelDefault("AllowEdit", "False")]
        public virtual string PdfForm_Code { get; set; }
    }
}