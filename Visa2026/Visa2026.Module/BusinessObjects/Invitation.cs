using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    public class Invitation : BaseObject
    {
        [MaxLength(50)]
        [RuleRequiredField]
        public virtual string InvitationNumber { get; set; }

        public virtual DateTime StartDate { get; set; }

        public virtual DateTime EndDate { get; set; }

        [RuleRequiredField]
        public virtual Application Application { get; set; }

        [InverseProperty(nameof(PersonInApplication.Invitation))]
        public virtual IList<PersonInApplication> People { get; set; } = new ObservableCollection<PersonInApplication>();
    }
}