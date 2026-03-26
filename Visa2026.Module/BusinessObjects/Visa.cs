using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using System.Linq;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Lookup/Visa")]
    [DefaultProperty(nameof(VisaNumber))]
    [RuleCriteria("Visa_ExpirationDate_GreaterThan_StartDate", DefaultContexts.Save, "ExpirationDate > StartDate", "Expiration Date must be later than Start Date.")]
    [Appearance("GrayOutIfDeleted", AppearanceItemType = "ViewItem", TargetItems = "*",
        Criteria = "IsDeleted", Context = "ListView", FontColor = "Gray")]
    public class Visa : SingleActiveBaseObject<Person, Visa>, IExpirationLogic, ISoftDelete
    {
        [MaxLength(50)]
        [RuleRequiredField]
        public virtual string VisaNumber { get; set; }

        [RuleRequiredField]
        public virtual VisaType VisaType { get; set; }

        [RuleRequiredField]
        public virtual VisaCategory VisaCategory { get; set; }

        [RuleRequiredField]
        public virtual VisaIssuedPlace VisaIssuedPlace { get; set; }

        [RuleRequiredField]
        [ImmediatePostData]
        public virtual DateTime IssueDate { get; set; }

        [RuleRequiredField]
        [ImmediatePostData]
        public virtual DateTime StartDate { get; set; }

        [RuleRequiredField]
        [ImmediatePostData]
        public virtual DateTime? ExpirationDate { get; set; }

        public virtual bool HasBorderZonePermit { get; set; }

        [Appearance("BorderZoneVisible", Visibility = ViewItemVisibility.Hide, Criteria = "!HasBorderZonePermit", Context = "DetailView")]
        [RuleRequiredField(TargetCriteria = "HasBorderZonePermit")]

        public virtual IList<City> BorderZoneLocations { get; set; } = new ObservableCollection<City>();
        
        public string BorderZones
        {
            get
            {
                if (BorderZoneLocations == null || !BorderZoneLocations.Any())
                {
                    return string.Empty;
                }
                return string.Join(", ", BorderZoneLocations.Select(c => c.Name));
            }
        }

        public virtual bool HasInvitation { get; set; }
        [Appearance("InvitationVisible", Visibility = ViewItemVisibility.Hide, Criteria = "!HasInvitation", Context = "DetailView")]
        [RuleRequiredField(TargetCriteria = "HasInvitation")]
        public virtual InvitationItem InvitationItem { get; set; }

        [RuleRequiredField]
        public virtual Passport Passport { get; set; }

        [RuleRequiredField]
        [XafDisplayName("Issuing Application Item")]
        public virtual ApplicationItem IssuingApplicationItem { get; set; }

        [InverseProperty("CurrentVisa")]
        [XafDisplayName("Associated Application Items")]
        [ToolTip("List of applications where this visa is/was used as the current visa.")]
        public virtual IList<ApplicationItem> AssociatedApplicationItems { get; set; } = new ObservableCollection<ApplicationItem>();

        [NotMapped]
        [Browsable(false)]
        public virtual IList<InvitationItem> AvailableInvitationItems
        {
            get => new List<InvitationItem>(); 
        }

        [RuleFromBoolProperty("Visa_PersonIsValid", DefaultContexts.Save, "The owner of the Visa is not part of the selected Application.")]
        [Browsable(false)]
        public bool IsPersonValid
        {
            get
            {
                if (IssuingApplicationItem == null || IssuingApplicationItem.Application == null || Passport?.Person == null) return true;
                return IssuingApplicationItem.Application.ApplicationItems.Any(ai => ai.Person != null && ai.Person.ID == Passport.Person.ID);
            }
        }

        [RuleFromBoolProperty("Visa_InvitationPersonIsValid", DefaultContexts.Save, "The owner of the Visa is not included in the selected Invitation.")]
        [Browsable(false)]
        public bool IsInvitationPersonValid
        {
            get
            {
                if (!HasInvitation || InvitationItem == null || Passport?.Person == null) return true;
                return InvitationItem.Person != null && InvitationItem.Person.ID == Passport.Person.ID;
            }
        }

        [FieldSize(FieldSizeAttribute.Unlimited)]
        public virtual string Notes { get; set; }

        [Aggregated]
        [InverseProperty(nameof(VisaImage.Visa))]
        public virtual IList<VisaImage> Images { get; set; } = new ObservableCollection<VisaImage>();

        [Aggregated]
        [InverseProperty(nameof(VisaDocument.Visa))]
        public virtual IList<VisaDocument> Documents { get; set; } = new ObservableCollection<VisaDocument>();

        public override void OnSaving()
        {
            base.OnSaving();
            CrossObjectSyncHelper.SyncOnSave(this);
            StateChangeTrackingHelper.TrackOnSave(this);
        }

        protected override void SetAdditionalActiveItems(Visa item)
        {
            base.SetAdditionalActiveItems(item);
            if (item?.Passport != null)
            {
                item.Passport.CurrentVisa = item;
            }
        }

        protected override void ClearAdditionalActiveItems(Visa item)
        {
            base.ClearAdditionalActiveItems(item);
            if (item?.Passport != null && item.Passport.CurrentVisa == item)
            {
                item.Passport.CurrentVisa = null;
            }
        }
        public override Person GetParent()
        {
            return Passport?.Person;
        }
        public override IList<Visa> GetSiblings(Person parent)
        {
            return parent?.Passports?.SelectMany(p => p.Visas).ToList();
        }

        public override void SetParentActiveItem(Person parent, Visa item)
        {
            if (parent != null) parent.CurrentVisa = item;
        }

        public override bool IsParentActiveItem(Person parent, Visa item)
        {
            return parent?.CurrentVisa == item;
        }

        public int DaysRemaining
        {
            get
            {
                if (!ExpirationDate.HasValue)
                {
                    return 0;
                }
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

        [Browsable(false)]
        [NotMapped]
        protected override DateTime? ChronologicalSortDate => this.IssueDate;

		public virtual bool IsCancelled { get; set; }

		public virtual bool IsChanged { get; set; }

        public virtual bool IsExtended { get; set; }

        [Browsable(false)]
        public virtual bool IsDeleted { get; set; }

        [Browsable(false)]
        public virtual DateTime? DateDeleted { get; set; }

        [Browsable(false)]
        public virtual ApplicationUser DeletedBy { get; set; }

        public override void OnCreated()
        {
            base.OnCreated();
            if (ObjectSpace != null)
            {
                VisaType = ObjectSpace.GetObjectsQuery<VisaType>().FirstOrDefault(v => v.IsDefault);
                VisaCategory = ObjectSpace.GetObjectsQuery<VisaCategory>().FirstOrDefault(vc => vc.IsDefault);
                VisaIssuedPlace = ObjectSpace.GetObjectsQuery<VisaIssuedPlace>().FirstOrDefault(vip => vip.IsDefault);
                // Defaulting IssuingApplicationItem, Passport, InvitationItem is complex and usually handled by business logic or user input.
                // For now, leaving them null as they are RuleRequiredField and will be set explicitly.
                // IssuingApplicationItem = ObjectSpace.GetObjectsQuery<ApplicationItem>().FirstOrDefault(); // Example
                // Passport = ObjectSpace.GetObjectsQuery<Passport>().FirstOrDefault(); // Example
            }
        }
    }
}