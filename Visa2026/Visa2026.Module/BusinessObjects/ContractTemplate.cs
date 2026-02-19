using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using System.ComponentModel;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [DefaultProperty(nameof(TemplateName))]
    public class ContractTemplate : BaseObject
    {
        public virtual string TemplateName { get; set; }

        public virtual string Content { get; set; }
    }
}