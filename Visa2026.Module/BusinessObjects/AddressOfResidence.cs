using System;

using System.Collections.Generic;

using System.Collections.ObjectModel;

using System.ComponentModel;

using System.ComponentModel.DataAnnotations;

using System.ComponentModel.DataAnnotations.Schema;

using DevExpress.ExpressApp.ConditionalAppearance;

using DevExpress.ExpressApp.Editors;

using DevExpress.Persistent.Base;

using DevExpress.Persistent.BaseImpl.EF;

using DevExpress.Persistent.Validation;

using DevExpress.ExpressApp.Model;

using DevExpress.ExpressApp.DC;

using System.Linq;

using Visa2026.Module.Localization;

namespace Visa2026.Module.BusinessObjects

{

    [DefaultClassOptions]

    [DefaultProperty(nameof(DisplayAddress))]

    [NavigationItem("Lookup/General/Geography")]

    [Appearance("PrivateHouseOnly_ExpirationFields", Visibility = ViewItemVisibility.Hide, TargetItems = "ExpirationDate;DaysRemaining", Criteria = "Not (Type = 'PrivateHouse')", Context = "DetailView,ListView")]

    [Appearance("AddressDocumentsTabHiddenWhenLodging", AppearanceItemType = "LayoutItem", Visibility = ViewItemVisibility.Hide, TargetItems = "Documents", Criteria = "Type = 'Lodging'", Context = "DetailView")]

    [Appearance("AddressLodgingDocumentsTabHidden", AppearanceItemType = "LayoutItem", Visibility = ViewItemVisibility.Hide, TargetItems = "LodgingDocuments", Context = "DetailView")]

    [Appearance("AddressTabsHiddenWhenLodging", AppearanceItemType = "LayoutItem", Visibility = ViewItemVisibility.Hide, TargetItems = "Tabs", Criteria = "Type = 'Lodging'", Context = "DetailView")]

    public class AddressOfResidence : BaseObject, IExpirationLogic

    {

        private const string LookupTypeCriteria = "Type = 'Lodging' Or Type = 'Hotel' Or Type = 'Hospital' Or Type = 'Other'";



        private ResidenceType? type;

        [ImmediatePostData]

        [RuleRequiredField]

        public virtual ResidenceType? Type

        {

            get => type;

            set

            {

                if (type != value)

                {

                    type = value;



                    // Prevent stale address data when the address type changes.

                    fullAddress = null;



                    if (type != ResidenceType.Lodging)

                        Lodging = null;



                    if (type != ResidenceType.Hotel)

                        Hotel = null;



                    if (type != ResidenceType.Hospital)

                        Hospital = null;



                    if (type != ResidenceType.Other)

                        OtherSite = null;

                }

            }

        }



        private Lodging lodging;

        [Appearance("LodgingVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Type != 'Lodging'", Context = "DetailView")]

        [RuleRequiredField(TargetCriteria = "Type = 'Lodging'")]

        [ImmediatePostData]

        [DataSourceCriteria("City = '@This.City'")]

        [VisibleInListView(false)]

        public virtual Lodging Lodging

        {

            get => lodging;

            set

            {

                if (lodging != value)

                {

                    lodging = value;

                    if (lodging != null && Type.HasValue && Type.Value == ResidenceType.Lodging

                        && !string.IsNullOrWhiteSpace(lodging.FullAddress))

                    {

                        FullAddress = lodging.FullAddress;

                    }

                }

            }

        }



        private Hotel hotel;

        [Appearance("HotelVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Type != 'Hotel'", Context = "DetailView")]

        [RuleRequiredField(TargetCriteria = "Type = 'Hotel'")]

        [ImmediatePostData]

        [DataSourceCriteria("City = '@This.City'")]

        [VisibleInListView(false)]

        public virtual Hotel Hotel

        {

            get => hotel;

            set

            {

                if (hotel != value)

                {

                    hotel = value;

                    if (hotel != null && Type.HasValue && Type.Value == ResidenceType.Hotel

                        && !string.IsNullOrWhiteSpace(hotel.Name))

                    {

                        FullAddress = hotel.Name;

                    }

                }

            }

        }



        private Hospital hospital;

        [Appearance("HospitalVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Type != 'Hospital'", Context = "DetailView")]

        [RuleRequiredField(TargetCriteria = "Type = 'Hospital'")]

        [ImmediatePostData]

        [DataSourceCriteria("City = '@This.City'")]

        [VisibleInListView(false)]

        public virtual Hospital Hospital

        {

            get => hospital;

            set

            {

                if (hospital != value)

                {

                    hospital = value;

                    if (hospital != null && Type.HasValue && Type.Value == ResidenceType.Hospital

                        && !string.IsNullOrWhiteSpace(hospital.Name))

                    {

                        FullAddress = hospital.Name;

                    }

                }

            }

        }



        private OtherSite otherSite;

        [Appearance("OtherSiteVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Type != 'Other'", Context = "DetailView")]

        [RuleRequiredField(TargetCriteria = "Type = 'Other'")]

        [ImmediatePostData]

        [DataSourceCriteria("City = '@This.City'")]

        [VisibleInListView(false)]

        public virtual OtherSite OtherSite

        {

            get => otherSite;

            set

            {

                if (otherSite != value)

                {

                    otherSite = value;

                    if (otherSite != null && Type.HasValue && Type.Value == ResidenceType.Other

                        && !string.IsNullOrWhiteSpace(otherSite.FullAddress))

                    {

                        FullAddress = otherSite.FullAddress;

                    }

                }

            }

        }



        /// <summary>

        /// Detail hint when <see cref="Type"/> is <see cref="ResidenceType.Lodging"/>; files are edited on <see cref="Lodging"/>.

        /// </summary>

        [NotMapped]

        [VisibleInListView(false)]

        [XafDisplayName(" ")]

        [Appearance("LodgingDocumentsGuidanceVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Type != 'Lodging'", Context = "DetailView")]

        [Appearance("LodgingDocumentsGuidance_Layout", AppearanceItemType = "LayoutItem", Visibility = ViewItemVisibility.Hide, Criteria = "Type != 'Lodging'", Context = "DetailView")]

        [ModelDefault("AllowEdit", "False")]

        [ModelDefault("RowCount", "3")]

        [FieldSize(FieldSizeAttribute.Unlimited)]

        public string LodgingDocumentsGuidance

        {

            get

            {

                if (Type != ResidenceType.Lodging)

                    return string.Empty;



                if (Lodging == null)

                    return VisaUiMessages.Get("AddressOfResidence.LodgingDocumentsGuidance");



                int fileCount = Lodging.Documents?.Count(d => d != null) ?? 0;

                return VisaUiMessages.Format("AddressOfResidence.LodgingDocumentsGuidance.WithLodging", fileCount);

            }

        }



        private string fullAddress;

        [MaxLength(255)]

        [RuleRequiredField(TargetCriteria = "Type = 'PrivateHouse'")]

        [Appearance("FullAddressHiddenWhenLookupType", AppearanceItemType = "ViewItem", TargetItems = "FullAddress", Visibility = ViewItemVisibility.Hide, Criteria = LookupTypeCriteria, Context = "DetailView")]

        [Appearance("FullAddressHiddenWhenLookupType_Layout", AppearanceItemType = "LayoutItem", TargetItems = "FullAddress", Visibility = ViewItemVisibility.Hide, Criteria = LookupTypeCriteria, Context = "DetailView")]

        [Appearance("FullAddressReadOnlyWhenLookupType", Enabled = false, Criteria = LookupTypeCriteria, Context = "DetailView")]

        public virtual string FullAddress

        {

            get

            {

                if (Type == ResidenceType.Lodging && Lodging != null && !string.IsNullOrWhiteSpace(Lodging.FullAddress))

                    return Lodging.FullAddress;



                if (Type == ResidenceType.Hotel && Hotel != null && !string.IsNullOrWhiteSpace(Hotel.Name))

                    return Hotel.Name;



                if (Type == ResidenceType.Hospital && Hospital != null && !string.IsNullOrWhiteSpace(Hospital.Name))

                    return Hospital.Name;



                if (Type == ResidenceType.Other && OtherSite != null && !string.IsNullOrWhiteSpace(OtherSite.FullAddress))

                    return OtherSite.FullAddress;



                return fullAddress;

            }

            set => fullAddress = value;

        }

        /// <summary>Region, city, and address combined for lookups and list display.</summary>
        [NotMapped]
        [VisibleInDetailView(false)]
        [VisibleInListView(true)]
        [ModelDefault("AllowEdit", "False")]
        public string DisplayAddress
        {
            get
            {
                var parts = new List<string>();
                if (Region != null)
                    parts.Add(LookupLocalization.GetDisplayName(Region));
                if (City != null)
                    parts.Add(LookupLocalization.GetDisplayName(City));
                var address = FullAddress?.Trim();
                if (!string.IsNullOrEmpty(address))
                    parts.Add(address);
                return string.Join(", ", parts);
            }
        }

        private Region region;

        [RuleRequiredField]

        [ImmediatePostData]

        [VisibleInListView(false)]

        public virtual Region Region

        {

            get => region;

            set

            {

                if (region == value)

                    return;



                region = value;

                City = null;

            }

        }



        [RuleRequiredField]

        [DataSourceCriteria("[Region] = '@This.Region'")]

        [VisibleInListView(false)]

        public virtual City City { get; set; }



        [RuleRequiredField(TargetCriteria = "Type = 'PrivateHouse'")]

        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]

        [ModelDefault("EditMask", "dd.MM.yyyy")]

        public virtual DateTime? ExpirationDate { get; set; }



        [RuleRequiredField]

        public virtual Person Person { get; set; }





        [Aggregated]

        [InverseProperty(nameof(AddressOfResidenceDocument.AddressOfResidence))]

        [Appearance("DocumentsVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Type = 'Lodging'", Context = "DetailView")]

        public virtual IList<AddressOfResidenceDocument> Documents { get; set; } = new ObservableCollection<AddressOfResidenceDocument>();



        [Aggregated]

        [InverseProperty(nameof(AddressOfResidenceImage.AddressOfResidence))]

        [VisibleInDetailView(false)]

        [VisibleInListView(false)]

        public virtual IList<AddressOfResidenceImage> Images { get; set; } = new ObservableCollection<AddressOfResidenceImage>();



        [NotMapped]

        [VisibleInDetailView(false)]

        [VisibleInListView(false)]

        public virtual IList<LodgingDocument> LodgingDocuments

        {

            get

            {

                return Lodging?.Documents;

            }

        }



        [NotMapped]

        [VisibleInDetailView(false)]

        [VisibleInListView(false)]

        public virtual IList<LodgingImage> LodgingImages

        {

            get

            {

                return Lodging?.Images;

            }

        }



        public override void OnCreated()

        {

            base.OnCreated();

            Type = ResidenceType.Lodging;

        }



        public override void OnSaving()

        {

            base.OnSaving();

        }



        public int DaysRemaining

        {

            get

            {

                if (!ExpirationDate.HasValue)

                {

                    // If there is no expiration date, for display purposes, it's better to show 0

                    // than a confusing large number like int.MaxValue.

                    return 0;

                }

                return (ExpirationDate.Value.Date - DateTime.Today).Days;

            }

        }

    }

}


