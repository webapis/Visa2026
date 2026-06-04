using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    /// <summary>
    /// File row under <see cref="Person.FamilyRelationDocuments"/> (family member detail); ZIP packing reads these for the family-relationship section.
    /// </summary>
    [DefaultClassOptions]
    [NavigationItem("Documents")]
    public class PersonFamilyRelationDocument : DocumentBase
    {
        [RuleRequiredField]
        public virtual Person Person { get; set; }
    }
}
