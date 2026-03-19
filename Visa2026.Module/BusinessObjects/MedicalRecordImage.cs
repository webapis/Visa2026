using System.ComponentModel;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [DisplayName("Image")]
    [NavigationItem("Images")]
    public class MedicalRecordImage : ImageBase
    {
        public virtual MedicalRecord MedicalRecord { get; set; }
    }
}