using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.DC;
namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [DefaultProperty(nameof(FullAddress))]
    [NavigationItem("Lookup/Person")]
    [RuleCriteria("AddressOfResidence_DateRange", DefaultContexts.Save, "ExpirationDate > StartDate", "Expiration Date must be later than Start Date.", TargetCriteria = "Type = 'PrivateHouse'")]
    public class AddressOfResidence : SingleActiveBaseObject<Person, AddressOfResidence>, IExpirationLogic,ISoftDelete
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
        [DataSourceCriteria("Company == null or Company.ID == '@This.Person.Company.ID'")]
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

        private string fullAddress;
        [MaxLength(255)]
        [RuleRequiredField]
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

        private Region region;
     //   [RuleRequiredField]
        [ImmediatePostData]
        public virtual Region Region
        {
            get => region;
            set
            {
                if (region != value)
                {
                    region = value;
                    City = null;
                }
            }
        }
      //  [RuleRequiredField]
        [DataSourceCriteria("[Region] = '@This.Region'")]
        public virtual City City { get; set; }

        [Appearance("StartDateHide", Visibility = ViewItemVisibility.Hide, Criteria = "Type != 'PrivateHouse'", Context = "DetailView")]
        [RuleRequiredField(TargetCriteria = "Type = 'PrivateHouse'")]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime? StartDate { get; set; }

        [Appearance("ExpirationDateHide", Visibility = ViewItemVisibility.Hide, Criteria = "Type != 'PrivateHouse'", Context = "DetailView")]
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
        [Appearance("ImagesVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Type = 'Lodging'", Context = "DetailView")]
        public virtual IList<AddressOfResidenceImage> Images { get; set; } = new ObservableCollection<AddressOfResidenceImage>();

        [NotMapped]
        [Appearance("LodgingDocumentsVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Type != 'Lodging'", Context = "DetailView")]
        public virtual IList<LodgingDocument> LodgingDocuments
        {
            get
            {
                return Lodging?.Documents;
            }
        }

        [NotMapped]
        [Appearance("LodgingImagesVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Type != 'Lodging'", Context = "DetailView")]
        public virtual IList<LodgingImage> LodgingImages
        {
            get
            {
                return Lodging?.Images;
            }
        }

        [Browsable(false)]
        [NotMapped]
        protected override DateTime? ChronologicalSortDate => this.StartDate;

        public override Person GetParent()
        {
            return Person;
        }

        public override IList<AddressOfResidence> GetSiblings(Person parent)
        {
            return parent?.AddressesOfResidence;
        }

        public override void SetParentActiveItem(Person parent, AddressOfResidence item)
        {
            parent.CurrentAddressOfResidence = item;
        }

        public override bool IsParentActiveItem(Person parent, AddressOfResidence item)
        {
            return parent.CurrentAddressOfResidence == item;
        }

        [Appearance("DaysRemainingHide", Visibility = ViewItemVisibility.Hide, Criteria = "Type != 'PrivateHouse'", Context = "DetailView")]
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

        [Appearance("ExpirationStateHide", Visibility = ViewItemVisibility.Hide, Criteria = "Type != 'PrivateHouse'", Context = "DetailView")]
        public ExpirationState ExpirationState
        {
            get
            {
                return ExpirationLogicHelper.CalculateExpirationState(this, StartDate, ObjectSpace);
            }
        }
              [Browsable(false)]
        public virtual bool IsDeleted { get; set; }

        [Browsable(false)]
        public virtual DateTime? DateDeleted { get; set; }

        [Browsable(false)]
        public virtual ApplicationUser DeletedBy { get; set; }
    }
}