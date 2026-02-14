using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    public class FamilyMember : Person
    {
        [RuleRequiredField]
        public virtual Employee Employee { get; set; }

        // You can add a 'Relationship' property here later (e.g., Spouse, Child) 
        // if you create a corresponding Enum or Lookup.
    }
}