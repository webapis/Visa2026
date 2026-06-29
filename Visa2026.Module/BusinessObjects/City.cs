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
    public class City : GlobalLookupCatalogBase
    {


        [RuleRequiredField]
        [VisibleInListView(true)]
        public virtual Region Region { get; set; }

        [VisibleInListView(true)]
        [XafDisplayName("Mostly Used")]
        public virtual bool IsMostlyUsed { get; set; }


        [VisibleInListView(false)]
        public virtual string RegionName {get;set;} 

        [VisibleInListView(false)]
        [ModelDefault("AllowEdit", "False")]
        public virtual string PdfForm_Code { get; set; }

        [InverseProperty(nameof(Lodging.City))]
        public virtual IList<Lodging> Lodgings { get; set; } = new ObservableCollection<Lodging>();

        [InverseProperty(nameof(Hotel.City))]
        public virtual IList<Hotel> Hotels { get; set; } = new ObservableCollection<Hotel>();

        [InverseProperty(nameof(Hospital.City))]
        public virtual IList<Hospital> Hospitals { get; set; } = new ObservableCollection<Hospital>();

        [InverseProperty(nameof(OtherSite.City))]
        public virtual IList<OtherSite> OtherSites { get; set; } = new ObservableCollection<OtherSite>();
    }
}