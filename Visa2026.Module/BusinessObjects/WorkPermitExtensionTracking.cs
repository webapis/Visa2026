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
    [NavigationItem("Work Permit Tracking")]
    [ModelDefault("Caption", "Work Permit Extension History")]
    [ModelDefault("AllowEdit", "False")]
    [ModelDefault("AllowNew", "False")]
    [ModelDefault("AllowDelete", "False")]
    public class WorkPermitExtensionTracking
    {
        [Key, Browsable(false), MaxLength(100)]
        public virtual string ID { get; set; }

        public virtual Guid? ApplicationID { get; set; }
        [ForeignKey(nameof(ApplicationID))]
        public virtual Application Application { get; set; }

        public virtual Guid? ExpiringWorkPermitItemID { get; set; }
        [ForeignKey(nameof(ExpiringWorkPermitItemID))]
        [ModelDefault("Caption", "Expiring Work Permit")]
        public virtual WorkPermitItem ExpiringWorkPermitItem { get; set; }

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
        public virtual int? DaysRemaining { get; set; }
    }
}