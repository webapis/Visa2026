using System.ComponentModel;
using DevExpress.ExpressApp.DC;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [DisplayName("Image")]
    public class EducationImage : ImageBase
    {
        public virtual Education Education { get; set; }
    }
}