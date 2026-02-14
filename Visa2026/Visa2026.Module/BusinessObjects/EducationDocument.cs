using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [FileAttachment(nameof(File))]
    public class EducationDocument : BaseObject
    {
        public virtual FileData File { get; set; }

        public virtual string Description { get; set; }

        [RuleRequiredField]
        public virtual Education Education { get; set; }
    }
}