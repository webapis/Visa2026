using System.ComponentModel.DataAnnotations;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    public class AddressOfResidenceDocument : BaseObject
    {
        public virtual AddressOfResidence AddressOfResidence { get; set; }

        [RuleRequiredField]
        [Aggregated, ExpandObjectMembers(ExpandObjectMembers.Never)]
        public virtual FileData File { get; set; }

        [MaxLength(255)]
        public virtual string Description { get; set; }
    }
}