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
    [DefaultProperty(nameof(FullAddress))]
    [NavigationItem(false)]
    [Appearance("PrivateHouseOnly_ExpirationFields", Visibility = ViewItemVisibility.Hide, TargetItems = "ExpirationDate;DaysRemaining", Criteria = "Not (Type = 'PrivateHouse')", Context = "DetailView,ListView")]
    [Appearance("AddressDocumentsTabHiddenWhenLodging", AppearanceItemType = "LayoutItem", Visibility = ViewItemVisibility.Hide, TargetItems = "Documents", Criteria = "Type = 'Lodging'", Context = "DetailView")]
    [Appearance("AddressLodgingDocumentsTabHidden", AppearanceItemType = "LayoutItem", Visibility = ViewItemVisibility.Hide, TargetItems = "LodgingDocuments", Context = "DetailView")]
    [Appearance("AddressTabsHiddenWhenLodging", AppearanceItemType = "LayoutItem", Visibility = ViewItemVisibility.Hide, TargetItems = "Tabs", Criteria = "Type = 'Lodging'", Context = "DetailView")]
    public class AddressOfResidence : BaseObject, IExpirationLogic
    {
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
                    {
                        Lodging = null;
                    }
                }
            }
        }

        private Lodging lodging;
        [Appearance("LodgingVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Type != 'Lodging'", Context = "DetailView")]
        [RuleRequiredField(TargetCriteria = "Type = 'Lodging'")]
        [ImmediatePostData]
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
        [RuleRequiredField]
        [Appearance("FullAddressHiddenWhenLodging", AppearanceItemType = "ViewItem", TargetItems = "FullAddress", Visibility = ViewItemVisibility.Hide, Criteria = "Type = 'Lodging'", Context = "DetailView")]
        [Appearance("FullAddressHiddenWhenLodging_Layout", AppearanceItemType = "LayoutItem", TargetItems = "FullAddress", Visibility = ViewItemVisibility.Hide, Criteria = "Type = 'Lodging'", Context = "DetailView")]
        // The FullAddress is automatically populated from the selected Lodging.
        // For 'Hotel' or 'PrivateHouse' types, this field becomes editable
        // to allow for entering one-off addresses that are not stored as reusable Lodging records.
        [Appearance("FullAddressReadOnly", Enabled = false, Criteria = "Type = 'Lodging'", Context = "DetailView")]
        public virtual string FullAddress
        {
            get => (Type == ResidenceType.Lodging && Lodging != null && !string.IsNullOrWhiteSpace(Lodging.FullAddress))
                ? Lodging.FullAddress
                : fullAddress;
            set => fullAddress = value;
        }

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