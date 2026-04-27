using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Visa Tracking")]
    [DefaultProperty(nameof(ApplicationNumber))]
    [ModelDefault("Caption", "Visa Extension Status (Current)")]
    [ModelDefault("AllowEdit", "False")]
    [ModelDefault("AllowNew", "False")]
    [ModelDefault("AllowDelete", "False")]
    public class VisaExtensionStatus
    {
        [Key]
        [Browsable(false)]
        public virtual Guid ID { get; set; }

        public virtual Guid? ApplicationID { get; set; }
        [ForeignKey(nameof(ApplicationID))]
        public virtual Application Application { get; set; }

        public virtual Guid? ExpiringVisaID { get; set; }
        [ForeignKey(nameof(ExpiringVisaID))]
        [ModelDefault("Caption", "Expiring Visa")]
        public virtual Visa ExpiringVisa { get; set; }

        public virtual Guid? PersonID { get; set; }
        [ForeignKey(nameof(PersonID))]
        public virtual Person Person { get; set; }

        public virtual Guid? PassportID { get; set; }
        [ForeignKey(nameof(PassportID))]
        public virtual Passport Passport { get; set; }

        // --- Info Columns ---

        public virtual string ApplicationNumber { get; set; }

        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime? ApplicationDate { get; set; }

        [Browsable(false)]
        public virtual Guid? CurrentStateID { get; set; }
        [ForeignKey(nameof(CurrentStateID))]
        [ModelDefault("Caption", "Current State")]
        public virtual ApplicationState? CurrentState { get; set; }

        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime? StatusDate { get; set; }

        [ModelDefault("Caption", "Description")]
        public virtual string StatusDescription { get; set; }

        [ModelDefault("DisplayFormat", "{0} days")]
        public virtual int? DaysRemainingOnVisa { get; set; }

        [Browsable(false)]
        public virtual Guid? IssuedVisaID { get; set; }
        [ForeignKey(nameof(IssuedVisaID))]
        [ModelDefault("Caption", "Issued Visa")]
        public virtual Visa IssuedVisa { get; set; }

        [Browsable(false)]
        public virtual Guid? RejectionItemID { get; set; }
        [ForeignKey(nameof(RejectionItemID))]
        [ModelDefault("Caption", "Rejection")]
        public virtual RejectionItem RejectionItem { get; set; }

        [NotMapped]
        public string StatusColor => DaysRemainingOnVisa < 30 ? "Red" : "Green";
    }
}