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
    [ModelDefault("Caption", "Visa Transfer Status (Current)")]
    [ModelDefault("AllowEdit", "False")]
    [ModelDefault("AllowNew", "False")]
    [ModelDefault("AllowDelete", "False")]
    public class VisaTransferStatus
    {
        [Key]
        [Browsable(false)]
        public virtual Guid ID { get; set; }

        public virtual Guid? ApplicationID { get; set; }
        [ForeignKey(nameof(ApplicationID))]
        public virtual Application Application { get; set; }

        public virtual Guid? TransferredVisaID { get; set; }
        [ForeignKey(nameof(TransferredVisaID))]
        [ModelDefault("Caption", "Transferred Visa")]
        public virtual Visa TransferredVisa { get; set; }

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

        [Browsable(false)]
        public virtual Guid? IssuedVisaID { get; set; }
        [ForeignKey(nameof(IssuedVisaID))]
        [ModelDefault("Caption", "Issued Visa")]
        public virtual Visa IssuedVisa { get; set; }
    }
}