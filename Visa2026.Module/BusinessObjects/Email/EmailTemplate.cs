using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects.Email;

/// <summary>
/// Office-wide subject and body text for emailed document copies. Placeholders are replaced when the
/// template is picked: <c>{PersonNames}</c>, <c>{PersonCount}</c>, <c>{DocumentList}</c>,
/// <c>{OfficerName}</c>, <c>{Date}</c>.
/// </summary>
[DefaultClassOptions]
[NavigationItem("Email")]
[ImageName("BO_Letter")]
[DefaultProperty(nameof(Name))]
public class EmailTemplate : BaseObject
{
    [RuleRequiredField]
    [MaxLength(256)]
    public virtual string Name { get; set; }

    [MaxLength(512)]
    public virtual string Subject { get; set; }

    [FieldSize(FieldSizeAttribute.Unlimited)]
    public virtual string Body { get; set; }

    /// <summary>Pre-selected when the compose step opens.</summary>
    public virtual bool IsDefault { get; set; }

    public virtual int SortOrder { get; set; }

    [NotMapped]
    public string Placeholders => "{PersonNames}  {PersonCount}  {DocumentList}  {OfficerName}  {Date}";
}
