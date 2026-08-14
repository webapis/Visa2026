using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.DC;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("BorderZone")]
    [DefaultProperty(nameof(BorderZoneNumber))]
    [RuleCriteria("BorderZone_DateRange", DefaultContexts.Save, "ExpirationDate > StartDate", "Expiration Date must be later than Start Date.")]
    public class BorderZone : BaseObject, IExpirationLogic, IPersonLinkParent
    {
        public BorderZone()
        {
            BorderZoneItems = new ObservableCollection<BorderZoneItem>();
            Documents = new ObservableCollection<BorderZoneDocument>();
        }

        [MaxLength(50)]
        [RuleRequiredField]
        public virtual string BorderZoneNumber { get; set; }

		private DateTime startDate;
		[RuleRequiredField]
		[ImmediatePostData]
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

        public virtual DateTime? ExpirationDate { get; protected set; }

        [RuleRequiredField]
        [ImmediatePostData]
        [VisibleInListView(false)]
        [DataSourceProperty(nameof(AvailableApplicationProfileInstances))]
        [InverseProperty(nameof(ApplicationProfileInstance.BorderZones))]
        [ToolTip("Link this border-zone permit to the application that produced it.")]
        public virtual ApplicationProfileInstance ApplicationProfileInstance { get; set; }

        /// <summary>Candidate applications whose profile may produce a border-zone permit.</summary>
        [NotMapped]
        [Browsable(false)]
        public IList<ApplicationProfileInstance> AvailableApplicationProfileInstances
        {
            get
            {
                var objectSpace = ObjectSpaceHelper.Get(this);
                if (objectSpace == null)
                    return new List<ApplicationProfileInstance>();

                return objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
                    .Where(a =>
                        (a.ApplicationProfile != null && a.ApplicationProfile.ProduceBorderZone)
                        || (a.ApplicationType != null && a.ApplicationType.ShowBorderZoneLocation))
                    .OrderByDescending(a => a.ApplicationDate)
                    .ThenBy(a => a.FullApplicationNumber)
                    .ToList();
            }
        }

        [RuleFromBoolProperty(
            "BorderZone_ApplicationTypeAllowed",
            DefaultContexts.Save,
            "ApplicationProfileInstance must be a type that can produce a border-zone permit.")]
        [Browsable(false)]
        public bool IsApplicationTypeAllowed
        {
            get
            {
                if (ApplicationProfileInstance == null)
                    return true;
                return ApplicationTypeCapabilities.CanIssueBorderZone(ApplicationProfileInstance);
            }
        }
        
        public virtual bool IsCancelled { get; set; }
        
        [Aggregated]
        [InverseProperty(nameof(BorderZoneItem.BorderZone))]
        public virtual IList<BorderZoneItem> BorderZoneItems { get; set; }

        [Aggregated]
        [InverseProperty(nameof(BorderZoneDocument.BorderZone))]
        public virtual IList<BorderZoneDocument> Documents { get; set; }

        public int DaysRemaining => ExpirationLogicHelper.CalculateDaysRemaining(ExpirationDate, IsCancelled);

		public ExpirationState ExpirationState
        {
            get
            {
                return ExpirationLogicHelper.CalculateExpirationState(
                    this,
                    ExpirationAlertBusinessObjectKeys.BorderZone,
                    ObjectSpaceHelper.Get(this));
            }
        }

        [NotMapped]
        [Browsable(false)]
        public virtual IList<Person> AvailablePeople
        {
            get
            {
                return ApplicationRosterHelper.GetRosterPeople(ApplicationProfileInstance);
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
        }

        /// <summary>ListView link column that opens header document copies in the preview slot.</summary>
        [NotMapped]
        [VisibleInDetailView(false)]
        [VisibleInLookupListView(false)]
        [ModelDefault("AllowEdit", "False")]
        public string DocumentCopiesListLink =>
            Visa2026.Module.Localization.VisaUiMessages.Get("BorderZoneDocumentCopies.List.ColumnLink");

    }
}