using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp.Model;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("System")]
    [DisplayName("Settings")]
    public class SystemSettings : BaseObject
    {
        [ModelDefault("DisplayFormat", "{0:N2}")]
        [ModelDefault("EditMask", "N2")]
        [Description("The threshold at which an item is considered 'Expiring Soon'. E.g., 90% (0.9).")]
        public virtual decimal ExpirationWarningThreshold { get; set; }

        [Description("The default number of days before expiration to consider an item 'Expiring Soon' when a start date is not available for percentage calculation.")]
        public virtual int DefaultExpiringSoonDays { get; set; }

        public override void OnCreated()
        {
            base.OnCreated();
            ExpirationWarningThreshold = 0.9m;
            DefaultExpiringSoonDays = 30;
        }

        public static SystemSettings GetInstance(IObjectSpace objectSpace)
        {
            SystemSettings instance = objectSpace.GetObjectsQuery<SystemSettings>().FirstOrDefault();
            if (instance == null)
            {
                instance = objectSpace.CreateObject<SystemSettings>();
            }
            return instance;
        }
    }
}