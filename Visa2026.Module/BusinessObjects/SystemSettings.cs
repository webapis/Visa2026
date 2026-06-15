using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Validation;
using Visa2026.Module.Services;

namespace Visa2026.Module.BusinessObjects
{
    /// <summary>
    /// Singleton upload size limits (images and file attachments). Other tenant config lives on dedicated Configuration BOs.
    /// </summary>
    [DefaultClassOptions]
    [NavigationItem("Configuration")]
    [DisplayName("Upload limits")]
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

        [Browsable(false)]
        [ModelDefault("DisplayFormat", "{0:N2}")]
        [ModelDefault("EditMask", "N2")]
        [Description("Legacy — unused at runtime. Use Configuration → Document expiration alerts.")]
        public virtual decimal ExpirationWarningThreshold { get; set; }

        [Browsable(false)]
        [Description("Legacy fallback when no ExpirationAlertRule row exists. Officers edit per-document rules under Configuration.")]
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

        /// <summary>
        /// Last applied max(global, tenant) <c>LookupCatalogs/manifest.json</c> <c>version</c>.
        /// Bump manifest version when JSON catalog content changes without an assembly version bump.
        /// </summary>
        [Browsable(false)]
        public virtual int LookupCatalogManifestVersion { get; set; }

        public override void OnCreated()
        {
            base.OnCreated();
            ExpirationWarningThreshold = DefaultExpirationWarningThreshold;
            DefaultExpiringSoonDays = DefaultDefaultExpiringSoonDays;
            MaxImageSizeInMB = DefaultMaxImageSizeInMB;
            MaxDocumentSizeInMB = DefaultMaxDocumentSizeInMB;
        }

        public static SystemSettings? TryGetInstance(IObjectSpace objectSpace) =>
            OrganizationSingletonHelper.TryGet(objectSpace, (SystemSettings _) => "Upload limits");

        public static SystemSettings GetOrCreateInstance(IObjectSpace objectSpace)
        {
            return TryGetInstance(objectSpace) ?? objectSpace.CreateObject<SystemSettings>();
        }
    }
}