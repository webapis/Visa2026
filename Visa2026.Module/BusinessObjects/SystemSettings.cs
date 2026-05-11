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
        public const decimal DefaultExpirationWarningThreshold = 0.9m;
        public const int DefaultDefaultExpiringSoonDays = 30;
        public const int DefaultMaxImageSizeInMB = 2;
        public const int DefaultMaxDocumentSizeInMB = 5;

        /// <summary>Hard cap for <see cref="MaxDocumentSizeInMB"/> (admin UI). Product limit: 5 MB per file attachment.</summary>
        public const int MaxDocumentSizeInMBCap = 5;

        /// <summary>Hard cap for <see cref="MaxImageSizeInMB"/>.</summary>
        public const int MaxImageSizeInMBCap = 15;

        [ModelDefault("DisplayFormat", "{0:N2}")]
        [ModelDefault("EditMask", "N2")]
        [Description("The threshold at which an item is considered 'Expiring Soon'. E.g., 90% (0.9).")]
        public virtual decimal ExpirationWarningThreshold { get; set; }

        [Description("The default number of days before expiration to consider an item 'Expiring Soon' when a start date is not available for percentage calculation.")]
        [RuleValueComparison(DefaultContexts.Save, ValueComparisonType.GreaterThan, 0)]
        public virtual int DefaultExpiringSoonDays { get; set; }

        [Description("The maximum allowed size for uploaded images, in Megabytes (MB).")]
        [RuleValueComparison(DefaultContexts.Save, ValueComparisonType.GreaterThan, 0)]
        [RuleValueComparison("SystemSettings_MaxImageSizeCap", DefaultContexts.Save, ValueComparisonType.LessThanOrEqual, MaxImageSizeInMBCap,
            CustomMessageTemplate = "Maximum image size cannot exceed {RightOperand} MB (server safety cap).")]
        public virtual int MaxImageSizeInMB { get; set; }

        [Description("The maximum allowed size for uploaded file attachments, in Megabytes (MB).")]
        [RuleValueComparison(DefaultContexts.Save, ValueComparisonType.GreaterThan, 0)]
        [RuleValueComparison("SystemSettings_MaxDocumentSizeCap", DefaultContexts.Save, ValueComparisonType.LessThanOrEqual, MaxDocumentSizeInMBCap,
            CustomMessageTemplate = "Maximum document size cannot exceed {RightOperand} MB (server safety cap).")]
        public virtual int MaxDocumentSizeInMB { get; set; }

        public override void OnCreated()
        {
            base.OnCreated();
            ExpirationWarningThreshold = DefaultExpirationWarningThreshold;
            DefaultExpiringSoonDays = DefaultDefaultExpiringSoonDays;
            MaxImageSizeInMB = DefaultMaxImageSizeInMB;
            MaxDocumentSizeInMB = DefaultMaxDocumentSizeInMB;
        }

        public static SystemSettings? TryGetInstance(IObjectSpace objectSpace)
        {
            return objectSpace.GetObjectsQuery<SystemSettings>().FirstOrDefault();
        }

        public static SystemSettings GetOrCreateInstance(IObjectSpace objectSpace)
        {
            return TryGetInstance(objectSpace) ?? objectSpace.CreateObject<SystemSettings>();
        }
    }
}