using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Tenant lookup: government ministries that perform <strong>review legs</strong> on an application
/// (<c>1_REVIEW_*</c> … <c>N_REVIEW_*</c>). Not migration service — use <see cref="MigrationService"/> on
/// <see cref="Application"/> and the built-in <c>AT_MIGRATION_SERVICE</c> progress step after all legs approve.
/// </summary>
[DefaultClassOptions]
[DefaultProperty(nameof(ShortNameTm))]
[NavigationItem(false)]
public class ApprovingMinistry : LookupBase
{
    /// <summary>Short label shown on <see cref="ApplicationProfileInstanceProgress"/> ministry steps.</summary>
    [RuleRequiredField]
    [MaxLength(40)]
    [XafDisplayName("Short name")]
    public virtual string ShortNameTm { get; set; }

    [ModelDefault("AllowEdit", "False")]
    public virtual bool IsActive { get; set; } = true;
}
