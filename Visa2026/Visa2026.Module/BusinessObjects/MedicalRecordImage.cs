using System.ComponentModel;
using DevExpress.ExpressApp.DC;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [DisplayName("Image")]
    public class MedicalRecordImage : ImageBase
    {
        public virtual MedicalRecord MedicalRecord { get; set; }
    }
}