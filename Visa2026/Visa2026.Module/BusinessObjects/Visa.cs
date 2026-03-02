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
    public class Visa : SingleActiveBaseObject<Person, Visa>, IExpirationLogic
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
        public virtual DateTime IssueDate { get; set; }

        [RuleRequiredField]
        public virtual DateTime StartDate { get; set; }

        [RuleRequiredField]
        public virtual DateTime ExpirationDate { get; set; }

        public virtual bool HasBorderZonePermit { get; set; }

        [Appearance("BorderZoneVisible", Visibility = ViewItemVisibility.Hide, Criteria = "!HasBorderZonePermit", Context = "DetailView")]
        [RuleRequiredField(TargetCriteria = "HasBorderZonePermit")]
        public virtual BorderZone BorderZone { get; set; }

        public virtual bool HasInvitation { get; set; }

        [Appearance("InvitationVisible", Visibility = ViewItemVisibility.Hide, Criteria = "!HasInvitation", Context = "DetailView")]
        [RuleRequiredField(TargetCriteria = "HasInvitation")]
        public virtual Invitation Invitation { get; set; }

        [RuleRequiredField]
        public virtual Passport Passport { get; set; }

        private Application application;
        [ImmediatePostData]
        public virtual Application Application
        {
            get => application;
            set
            {
                if (application != value)
                {
                    application = value;
                    if (ObjectSpace != null)
                    {
                        CrossObjectSyncHelper.SyncOnPropertyChanged(this, nameof(Application));
                    }
                }
            }
        }
        [ModelDefault("AllowEdit", "False")]
        [DataSourceCriteria("Invitation.Application.ID = '@This.Application.ID'")]
        public virtual InvitationItem InvitationItem { get; set; }
        [ModelDefault("AllowEdit", "False")]
        [DataSourceCriteria("Application.ID = '@This.Application.ID'")]
        public virtual ApplicationItem ApplicationItem { get; set; }

        [NotMapped]
        [Browsable(false)]
        public virtual IList<InvitationItem> AvailableInvitationItems
        {
            get
            {
                if (Application == null) return new List<InvitationItem>();
                return Application.Invitations?.SelectMany(i => i.InvitationItems).ToList() ?? new List<InvitationItem>();
            }
        }


        [RuleFromBoolProperty("Visa_PersonIsValid", DefaultContexts.Save, "The owner of the Visa is not part of the selected Application.")]
        [Browsable(false)]
        public bool IsPersonValid
        {
            get
            {
                if (Application == null || Passport?.Person == null) return true;
                return Application.ApplicationItems.Any(ai => ai.Person != null && ai.Person.ID == Passport.Person.ID);
            }
        }

        [RuleFromBoolProperty("Visa_InvitationPersonIsValid", DefaultContexts.Save, "The owner of the Visa is not included in the selected Invitation.")]
        [Browsable(false)]
        public bool IsInvitationPersonValid
        {
            get
            {
                if (!HasInvitation || Invitation == null || Passport?.Person == null) return true;
                return Invitation.InvitationItems.Any(ii => ii.Person != null && ii.Person.ID == Passport.Person.ID);
            }
        }

        [FieldSize(FieldSizeAttribute.Unlimited)]
        public virtual string Notes { get; set; }

        [Aggregated]
        [InverseProperty(nameof(VisaImage.Visa))]
        public virtual IList<VisaImage> Images { get; set; } = new ObservableCollection<VisaImage>();

        public override void OnSaving()
        {
            base.OnSaving();
            CrossObjectSyncHelper.SyncOnSave(this);
        }

        // This method should be called by a Controller handling the ObjectDeleting event.
        public virtual void OnDeleting()
        {
            CrossObjectSyncHelper.SyncOnDelete(this);
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

        DateTime? IExpirationLogic.ExpirationDate => ExpirationDate;

        public int DaysRemaining
        {
            get
            {
                return (ExpirationDate.Date - DateTime.Today).Days;
            }
        }

        public ExpirationState ExpirationState
        {
            get
            {
                return ExpirationLogicHelper.CalculateExpirationState(this, StartDate, ObjectSpace);
            }
        }

		public virtual bool IsCancelled { get; set; }

		public virtual bool IsChanged { get; set; }

        public virtual bool IsExtended { get; set; }
    
    }
}