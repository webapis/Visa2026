using System.ComponentModel;
using DevExpress.ExpressApp.DC;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [DisplayName("Image")]
    public class VisaImage : ImageBase
    {
        public virtual Visa Visa { get; set; }
    }
}