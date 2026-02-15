using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.Model;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Lookup/Person")]
    [FileAttachment(nameof(File))]
    public class PersonDocument : BaseObject
    {
        public virtual FileData File { get; set; }

        public virtual string Description { get; set; }

        [RuleRequiredField]
        public virtual Person Person { get; set; }
    }
}