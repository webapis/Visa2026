using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;

namespace Visa2026.Module.BusinessObjects
{
    /// <summary>Placeholder extracted from a user-defined Word template with validation status.</summary>
    [ModelDefault("Caption", "User Report Placeholder")]
    public class UserReportPlaceholder : BaseObject
    {
        /// <summary>Parent template this placeholder belongs to.</summary>
        public virtual UserReportTemplate Template { get; set; } = null!;

        [MaxLength(255)]
        [ModelDefault("Caption", "Placeholder Key")]
        public virtual string PlaceholderKey { get; set; } = string.Empty;

        [ModelDefault("Caption", "Is Valid")]
        public virtual bool IsValid { get; set; }

        [MaxLength(500)]
        [ModelDefault("Caption", "Resolved Property Path")]
        [VisibleInDetailView(true)]
        [VisibleInListView(false)]
        public virtual string ResolvedPropertyPath { get; set; } = string.Empty;

        [MaxLength(255)]
        [ModelDefault("Caption", "Example Value")]
        [VisibleInDetailView(true)]
        [VisibleInListView(false)]
        public virtual string ExampleValue { get; set; } = string.Empty;

        [MaxLength(500)]
        [ModelDefault("Caption", "Validation Error")]
        [VisibleInDetailView(true)]
        [VisibleInListView(false)]
        public virtual string ValidationError { get; set; } = string.Empty;

        [NotMapped]
        [ModelDefault("Caption", "Status Icon")]
        public virtual string StatusIcon => IsValid ? "✓" : "✗";
    }
}
