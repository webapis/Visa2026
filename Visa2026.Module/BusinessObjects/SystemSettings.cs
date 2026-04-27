using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Validation;

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
        [RuleValueComparison(DefaultContexts.Save, ValueComparisonType.GreaterThan, 0)]
        public virtual int DefaultExpiringSoonDays { get; set; }

        [Description("The maximum allowed size for uploaded images, in Megabytes (MB).")]
        [RuleValueComparison(DefaultContexts.Save, ValueComparisonType.GreaterThan, 0)]
        public virtual int MaxImageSizeInMB { get; set; }

        [Description("The maximum allowed size for uploaded file attachments, in Megabytes (MB).")]
        [RuleValueComparison(DefaultContexts.Save, ValueComparisonType.GreaterThan, 0)]
        public virtual int MaxDocumentSizeInMB { get; set; }

        public override void OnCreated()
        {
            base.OnCreated();
            ExpirationWarningThreshold = 0.9m;
            DefaultExpiringSoonDays = 30;
            MaxImageSizeInMB = 2;
            MaxDocumentSizeInMB = 5;
        }

        public static SystemSettings GetInstance(IObjectSpace objectSpace)
        {
            // IMPORTANT:
            // GetObjectsQuery<T>() may only reflect database state and not include newly created (unsaved) objects.
            // During first-run initialization, multiple reads can occur while the first instance is still being created,
            // causing recursive CreateObject<SystemSettings>() calls and eventually crashing the app.
            SystemSettings instance = objectSpace.GetObjects<SystemSettings>().FirstOrDefault();
            if (instance == null)
            {
                instance = objectSpace.CreateObject<SystemSettings>();
            }
            return instance;
        }
    }
}