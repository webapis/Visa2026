using System.ComponentModel.DataAnnotations;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    public class AddressOfResidenceDocument : DocumentBase
    {
        public virtual AddressOfResidence AddressOfResidence { get; set; }
    }
}