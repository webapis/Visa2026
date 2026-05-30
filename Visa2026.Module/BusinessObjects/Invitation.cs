using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;



namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Invitation")]
    [DefaultProperty(nameof(InvitationNumber))]
    [RuleCriteria("Invitation_DateRange", DefaultContexts.Save, "ExpirationDate > StartDate", "Expiration Date must be later than Start Date.")]
    public class Invitation : BaseObject, IExpirationLogic, IPersonLinkParent, IObjectSpaceLink, ISoftDelete
    {
        public Invitation()
        {
            InvitationItems = new ObservableCollection<InvitationItem>();
            Images = new ObservableCollection<InvitationImage>();
            Documents = new ObservableCollection<InvitationDocument>();
        }

        [MaxLength(50)]
        [RuleRequiredField]
        public virtual string InvitationNumber { get; set; }

		private DateTime startDate;
		[RuleRequiredField]
		[ImmediatePostData]
		[ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
		[ModelDefault("EditMask", "dd.MM.yyyy")]
		public virtual DateTime StartDate
		{
			get => startDate;
			set
			{
				if(startDate != value)
				{
				startDate = value;
					UpdateExpirationDate();
				}
			}
		}

        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
		[ModelDefault("EditMask", "dd.MM.yyyy")]
		public virtual DateTime? ExpirationDate { get; protected set; }

        [RuleRequiredField]
        public virtual Application Application { get; set; }
        
        public virtual bool IsCancelled { get; set; }

        public virtual bool IsChanged { get; set; }
        
        [Aggregated]
        [InverseProperty(nameof(InvitationItem.Invitation))]
        public virtual IList<InvitationItem> InvitationItems { get; set; }

        [Aggregated]
        [InverseProperty(nameof(InvitationImage.Invitation))]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        public virtual IList<InvitationImage> Images { get; set; }

        [Aggregated]
        [InverseProperty(nameof(InvitationDocument.Invitation))]
        public virtual IList<InvitationDocument> Documents { get; set; }

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

        [NotMapped]
        [Browsable(false)]
        public virtual IList<Person> AvailablePeople
        {
            get
            {
                return Application?.ApplicationItems.Select(ai => ai.Person).ToList() ?? new List<Person>();
            }
        }

		private ValidityDuration validityDuration;
		[RuleRequiredField]
		[ImmediatePostData]
		public virtual ValidityDuration ValidityDuration
		{
			get => validityDuration;
			set
			{
				if(validityDuration != value)
				{
				validityDuration = value;
					UpdateExpirationDate();
				}
			}
		}

		private void UpdateExpirationDate()
		{
			if (ValidityDuration != null && StartDate != default)
			{
				ExpirationDate = StartDate.AddDays(ValidityDuration.NumberOfDays);
			}
			else
			{
				ExpirationDate = null;
			}
		}

        public override void OnCreated()
        {
            base.OnCreated();
            if (ObjectSpace != null)
            {
                ValidityDuration = ObjectSpace.GetObjectsQuery<ValidityDuration>().FirstOrDefault(v => v.IsDefault);
            }
        }

        #region IObjectSpaceLink
        [NotMapped]
        [Browsable(false)]
        public IObjectSpace ObjectSpace { get; set; }
        #endregion

        [Browsable(false)]
        public virtual bool IsDeleted { get; set; }

        [Browsable(false)]
        public virtual DateTime? DateDeleted { get; set; }

        [Browsable(false)]
        public virtual ApplicationUser DeletedBy { get; set; }

    }
}