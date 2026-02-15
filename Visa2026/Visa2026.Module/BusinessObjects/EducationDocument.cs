using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.Model;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Lookup/Education")]
    [FileAttachment(nameof(File))]
    public class EducationDocument : BaseObject
    {
        public virtual FileData File { get; set; }

        public virtual string Description { get; set; }

        [RuleRequiredField]
        public virtual Education Education { get; set; }
    }
}