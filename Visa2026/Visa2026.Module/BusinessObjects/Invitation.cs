using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.Model;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Invitation")]
    public class Invitation : BaseObject, IExpirationLogic
    {
        [MaxLength(50)]
        [RuleRequiredField]
        public virtual string InvitationNumber { get; set; }

        public virtual DateTime StartDate { get; set; }

        public virtual DateTime ExpirationDate { get; set; }

        [RuleRequiredField]
        public virtual Application Application { get; set; }

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
    }
}