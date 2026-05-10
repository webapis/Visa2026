using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    /// <summary>
    /// File row under <see cref="Passport.Documents"/> (Passport detail "Documents" tab); ZIP packing reads these via that collection.
    /// </summary>
    [DefaultClassOptions]
    [NavigationItem("Documents")]
    public class PassportDocument : DocumentBase
    {
        [RuleRequiredField]
        public virtual Passport Passport { get; set; }
    }
}