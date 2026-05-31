using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using DevExpress.Persistent.Validation;
using Visa2026.Module.Editors;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Invitation")]
    [DefaultProperty(nameof(InvitationNumber))]
    [RuleCriteria("Invitation_DateRange", DefaultContexts.Save, "ExpirationDate > StartDate", "Expiration Date must be later than Start Date.")]
    [SupportsOptionalDetailFields]
    public class Invitation : BaseObject, IExpirationLogic, IPersonLinkParent, ISoftDelete, IOptionalDetailFields
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

        [RuleRequiredField]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
		[ModelDefault("EditMask", "dd.MM.yyyy")]
		public virtual DateTime? ExpirationDate { get; protected set; }

        [NotMapped]
        [ImmediatePostData]
        [Index(-1000)]
        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        [EditorAlias(OptionalDetailFieldsEditorAliases.Toggle)]
        [ModelDefault("CustomCSSClassName", "xaf-optional-fields-toggle")]
        [XafDisplayName(" ")]
        public bool ShowOptionalFields { get; set; }

        /// <summary>
        /// Optional link to a visa application. When set, invitation items are limited to people on that application.
        /// </summary>
        [ImmediatePostData]
        [VisibleInListView(false)]
        [VisibleInDetailView(true)]
        [VisibleInLookupListView(false)]
        [ToolTip("Link this invitation to an application when one exists. Leave empty for standalone invitations.")]
        public virtual Application Application { get; set; }

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

        [ModelDefault("AllowEdit", "False")]
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

        [Browsable(false)]
		public ExpirationState ExpirationState
        {
            get
            {
                return ExpirationLogicHelper.CalculateExpirationState(this, StartDate, ObjectSpaceHelper.Get(this));
            }
        }

        [NotMapped]
        [Browsable(false)]
        public virtual IList<Person> AvailablePeople
        {
            get
            {
                if (Application?.ApplicationItems != null)
                {
                    return Application.ApplicationItems
                        .Select(ai => ai.Person)
                        .Where(p => p != null)
                        .ToList()!;
                }

                IObjectSpace? objectSpace = ObjectSpaceHelper.Get(this);
                if (objectSpace == null)
                {
                    return Array.Empty<Person>();
                }

                return objectSpace.GetObjectsQuery<Person>()
                    .Where(p => !p.IsDeleted && !p.IsArchived)
                    .OrderBy(p => p.LastName)
                    .ThenBy(p => p.FirstName)
                    .ToList();
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
            var objectSpace = ObjectSpaceHelper.Get(this);
            if (objectSpace != null)
            {
                ValidityDuration = objectSpace.GetObjectsQuery<ValidityDuration>().FirstOrDefault(v => v.IsDefault);
            }

            UpdateExpirationDate();
        }

        [Browsable(false)]
        public virtual bool IsDeleted { get; set; }

        [Browsable(false)]
        public virtual DateTime? DateDeleted { get; set; }

        [Browsable(false)]
        public virtual ApplicationUser DeletedBy { get; set; }
    }
}
