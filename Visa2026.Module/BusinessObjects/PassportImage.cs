using System.ComponentModel;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [DisplayName("Image")]
    [NavigationItem("Images")]
    public class PassportImage : ImageBase
    {
        public virtual Passport Passport { get; set; }
    }
}