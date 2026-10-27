using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects.Email;

/// <summary>
/// Reusable recipient for emailed document copies. Shared contacts are visible to every officer;
/// personal contacts only to <see cref="Owner"/>. See <c>docs/EMAIL_DOCUMENT_COPIES.md</c>.
/// </summary>
[DefaultClassOptions]
[NavigationItem("Email")]
[ImageName("BO_Contact")]
[DefaultProperty(nameof(DisplayName))]
[ListViewFilter("All", "", "All", true)]
[ListViewFilter("Shared", "IsShared = true", "Shared", false)]
[ListViewFilter("Mine", "IsShared = false And Owner.ID = CurrentUserId()", "My contacts", false)]
public class EmailContact : BaseObject
{
    public const string EmailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

    [NotMapped]
    [Browsable(false)]
    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? Email : $"{Name} <{Email}>";

    [MaxLength(256)]
    public virtual string Name { get; set; }

    [MaxLength(256)]
    public virtual string Organization { get; set; }

    [RuleRequiredField]
    [RuleRegularExpression(EmailPattern, CustomMessageTemplate = "Enter a valid email address.")]
    [MaxLength(320)]
    public virtual string Email { get; set; }

    /// <summary>When true every officer sees and can pick this contact; otherwise only <see cref="Owner"/>.</summary>
    public virtual bool IsShared { get; set; }

    [ModelDefault("AllowEdit", "False")]
    public virtual ApplicationUser Owner { get; set; }

    [ModelDefault("AllowEdit", "False")]
    public virtual int TimesUsed { get; set; }

    [ModelDefault("AllowEdit", "False")]
    [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy HH:mm}")]
    public virtual DateTime? LastUsedOnUtc { get; set; }

    /// <summary>Largest email this recipient accepts (MB); remembered from the last send, lowers the split size.</summary>
    [RuleRange(1, 25)]
    public virtual int? MaxEmailSizeMb { get; set; }

    [MaxLength(1024)]
    public virtual string Notes { get; set; }

    public override void OnCreated()
    {
        base.OnCreated();
        IsShared = true;
        if (ObjectSpace != null && SecuritySystem.CurrentUserId is Guid userId)
            Owner = ObjectSpace.GetObjectByKey<ApplicationUser>(userId);
    }
}
