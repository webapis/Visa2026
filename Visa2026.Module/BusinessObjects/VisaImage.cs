using System.ComponentModel;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [DisplayName("Image")]
    [NavigationItem(false)]
    public class VisaImage : ImageBase
    {
        public virtual Visa Visa { get; set; }
    }
}