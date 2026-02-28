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
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.Model;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Invitation")]
    [DefaultProperty(nameof(InvitationNumber))]
    public class Invitation : BaseObject, IExpirationLogic, IPersonLinkParent
    {
        [MaxLength(50)]
        [RuleRequiredField]
        public virtual string InvitationNumber { get; set; }

		private DateTime startDate;
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

        public virtual DateTime ExpirationDate { get; set; }

        [RuleRequiredField]
        public virtual Application Application { get; set; }
        
        public virtual bool IsCancelled { get; set; }

        public virtual bool IsChanged { get; set; }
        
        [InverseProperty(nameof(InvitationItem.Invitation))]
        [DevExpress.ExpressApp.DC.Aggregated]
        public virtual IList<InvitationItem> InvitationItems { get; set; } = new ObservableCollection<InvitationItem>();

        public virtual bool IsActive { get; set; } = true;

        DateTime? IExpirationLogic.ExpirationDate => ExpirationDate;

        public int DaysRemaining => (ExpirationDate.Date - DateTime.Today).Days;

        public ExpirationState ExpirationState
        {
            get
            {
                if (!IsActive) return ExpirationState.Archived;
                if (DaysRemaining < 0) return ExpirationState.Expired;
                if (DaysRemaining <= 30) return ExpirationState.ExpiringSoon;
                return ExpirationState.Active;
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
		}

    }
}