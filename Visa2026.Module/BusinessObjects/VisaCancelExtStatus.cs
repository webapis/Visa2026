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
    [ModelDefault("Caption", "Visa Ext. Cancellation Status")]
    [ModelDefault("AllowEdit", "False")]
    [ModelDefault("AllowNew", "False")]
    [ModelDefault("AllowDelete", "False")]
    public class VisaCancelExtStatus
    {
        [Key]
        [Browsable(false)]
        public virtual Guid ID { get; set; }

        [Browsable(false)]
        public virtual Guid? ApplicationID { get; set; }
        [ForeignKey(nameof(ApplicationID))]
        public virtual Application Application { get; set; }

        [Browsable(false)]
        public virtual Guid? VisaID { get; set; }
        [ForeignKey(nameof(VisaID))]
        [ModelDefault("Caption", "Visa Being Cancelled")]
        public virtual Visa Visa { get; set; }

        [Browsable(false)]
        public virtual Guid? PersonID { get; set; }
        [ForeignKey(nameof(PersonID))]
        public virtual Person Person { get; set; }

        [Browsable(false)]
        public virtual Guid? PassportID { get; set; }
        [ForeignKey(nameof(PassportID))]
        public virtual Passport Passport { get; set; }

        [ModelDefault("Caption", "Cancel Application")]
        public virtual string ApplicationNumber { get; set; }

        [ModelDefault("Caption", "Cancel App. Date")]
        public virtual DateTime? ApplicationDate { get; set; }

        [ModelDefault("Caption", "Application Type")]
        public virtual string ApplicationTypeName { get; set; }

        [Browsable(false)]
        public virtual Guid? CurrentStateID { get; set; }
        [ForeignKey(nameof(CurrentStateID))]
        [ModelDefault("Caption", "Cancellation State")]
        public virtual ApplicationState? CurrentState { get; set; }

        [ModelDefault("Caption", "State Date")]
        public virtual DateTime? StatusDate { get; set; }

        [ModelDefault("Caption", "Description")]
        public virtual string StatusDescription { get; set; }

        [ModelDefault("DisplayFormat", "{0} days")]
        [ModelDefault("Caption", "Days Remaining")]
        public virtual int? DaysRemainingOnVisa { get; set; }

        [ModelDefault("Caption", "Ext. Application")]
        public virtual string ExtApplicationNumber { get; set; }

        [Browsable(false)]
        public virtual Guid? ExtCurrentStateID { get; set; }
        [ForeignKey(nameof(ExtCurrentStateID))]
        [ModelDefault("Caption", "Ext. Application State")]
        public virtual ApplicationState? ExtCurrentState { get; set; }
    }
}
