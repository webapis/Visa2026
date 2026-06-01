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

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Lookup/Invitation")]
    [DefaultProperty(nameof(BorderZoneNumber))]
    [RuleCriteria("BorderZone_DateRange", DefaultContexts.Save, "ExpirationDate > StartDate", "Expiration Date must be later than Start Date.")]
    public class BorderZone : BaseObject, IExpirationLogic, IPersonLinkParent, ISoftDelete
    {
        public BorderZone()
        {
            BorderZoneItems = new ObservableCollection<BorderZoneItem>();
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
        public virtual Application Application { get; set; }
        
        public virtual bool IsCancelled { get; set; }
        
        [Aggregated]
        [InverseProperty(nameof(BorderZoneItem.BorderZone))]
        public virtual IList<BorderZoneItem> BorderZoneItems { get; set; }

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
            var objectSpace = ObjectSpaceHelper.Get(this);
            if (objectSpace != null)
            {
                ValidityDuration = objectSpace.GetObjectsQuery<ValidityDuration>().FirstOrDefault(v => v.IsDefault);
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