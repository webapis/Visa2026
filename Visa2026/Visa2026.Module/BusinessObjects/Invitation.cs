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
    public class Invitation : SingleActiveBaseObject<Application, Invitation>, IExpirationLogic
    {
        [MaxLength(50)]
        [RuleRequiredField]
        public virtual string InvitationNumber { get; set; }

        public virtual DateTime StartDate { get; set; }

        public virtual DateTime ExpirationDate { get; set; }

        [RuleRequiredField]
        public virtual Application Application { get; set; }

        [InverseProperty(nameof(PersonInApplication.Invitation))]
        public virtual IList<PersonInApplication> People { get; set; } = new ObservableCollection<PersonInApplication>();

        public override Application GetParent()
        {
            return Application;
        }

        public override IList<Invitation> GetSiblings(Application parent)
        {
            return parent?.Invitations;
        }

        public override void SetParentActiveItem(Application parent, Invitation item)
        {
            // Application does not track a single "CurrentInvitation" property.
        }

        public override bool IsParentActiveItem(Application parent, Invitation item)
        {
            return false;
        }

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