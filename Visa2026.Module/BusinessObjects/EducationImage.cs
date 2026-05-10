using System.ComponentModel;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [DisplayName("Image")]
    [NavigationItem(false)]
    public class EducationImage : ImageBase
    {
        public virtual Education Education { get; set; }
    }
}