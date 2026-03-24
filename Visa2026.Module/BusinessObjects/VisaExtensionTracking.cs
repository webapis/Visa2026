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
    [ModelDefault("Caption", "Visa Extension History")]
    [ModelDefault("AllowEdit", "False")]
    [ModelDefault("AllowNew", "False")]
    [ModelDefault("AllowDelete", "False")]
    public class VisaExtensionTracking
    {
        // --- Navigation Links (These create the clickable links in XAF) ---

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

        // Part of Composite Key
        [Browsable(false)]
        public virtual Guid ApplicationItemID { get; set; }
        [ForeignKey(nameof(ApplicationItemID))]
        [ModelDefault("Caption", "App Item")]
        public virtual ApplicationItem ApplicationItem { get; set; }

        // Part of Composite Key
        [Browsable(false)]
        public virtual Guid ApplicationProgressID { get; set; }

        // --- Info Columns ---

        public virtual string ApplicationNumber { get; set; }

        public virtual DateTime? ApplicationDate { get; set; }

        [Browsable(false)]
        public virtual Guid? CurrentStateID { get; set; }
        [ForeignKey(nameof(CurrentStateID))]
        [ModelDefault("Caption", "State")]
        public virtual ApplicationState? CurrentState { get; set; }

        public virtual DateTime? StatusDate { get; set; }

        [ModelDefault("Caption", "Description")]
        public virtual string StatusDescription { get; set; }

        [ModelDefault("DisplayFormat", "{0} days")]
        public virtual int? DaysRemainingOnVisa { get; set; }

        // Optional: Status highlighting
        [NotMapped]
        public string StatusColor => DaysRemainingOnVisa < 30 ? "Red" : "Green";
    }
}
