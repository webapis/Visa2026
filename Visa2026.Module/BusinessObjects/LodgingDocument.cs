using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Documents")]
    public class LodgingDocument : DocumentBase
    {
        public virtual Lodging Lodging { get; set; }
    }
}