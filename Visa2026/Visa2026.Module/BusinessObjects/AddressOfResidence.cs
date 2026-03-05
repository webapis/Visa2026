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
    [RuleCriteria("AddressOfResidence_DateRange", DefaultContexts.Save, "ExpirationDate > StartDate", "Expiration Date must be later than Start Date.")]
    public class AddressOfResidence : SingleActiveBaseObject<Person, AddressOfResidence>, IExpirationLogic
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
        public virtual Lodging Lodging
        {
            get => lodging;
            set
            {
                if (lodging != value)
                {
                    lodging = value;
                    if (lodging != null && Type.HasValue && Type.Value == ResidenceType.Lodging)
                    {
                        FullAddress = lodging.FullAddress;
                    }
                }
            }
        }

        [MaxLength(255)]
        [RuleRequiredField]
        [Appearance("FullAddressReadOnly", Enabled = false, Criteria = "Type = 'Lodging'", Context = "DetailView")]
        public virtual string FullAddress { get; set; }

        public virtual City City { get; set; }

        [RuleRequiredField]
        public virtual DateTime? StartDate { get; set; }

        [RuleRequiredField]
        public virtual DateTime? ExpirationDate { get; set; }

        [RuleRequiredField]
        public virtual Person Person { get; set; }


        [Aggregated]
        [InverseProperty(nameof(AddressOfResidenceDocument.AddressOfResidence))]
        [Appearance("DocumentsVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Type = 'Lodging'", Context = "DetailView")]
        public virtual IList<AddressOfResidenceDocument> Documents { get; set; } = new ObservableCollection<AddressOfResidenceDocument>();

        [Aggregated]
        [InverseProperty(nameof(AddressOfResidenceImage.AddressOfResidence))]
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

        public int DaysRemaining
        {
            get
            {
                if (!ExpirationDate.HasValue) return int.MaxValue;
                return (ExpirationDate.Value.Date - DateTime.Today).Days;
            }
        }

        public ExpirationState ExpirationState
        {
            get
            {
                return ExpirationLogicHelper.CalculateExpirationState(this, StartDate, ObjectSpace);
            }
        }
    }
}