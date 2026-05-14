using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    /// <summary>User-defined Word report template uploaded via XAF UI.</summary>
    [DefaultClassOptions]
    [NavigationItem("Reports")]
    [DefaultProperty(nameof(TemplateName))]
    [ModelDefault("Caption", "User Report Template")]
    [FileAttachment(nameof(TemplateFile))]
    public class UserReportTemplate : BaseObject
    {
        public UserReportTemplate()
        {
            Placeholders = new List<UserReportPlaceholder>();
            ApplicableTypes = new List<ApplicationType>();
        }

        [RuleRequiredField]
        [MaxLength(255)]
        [ModelDefault("Caption", "Template Name")]
        public virtual string TemplateName { get; set; } = string.Empty;

        [MaxLength(500)]
        [ModelDefault("Caption", "Description")]
        [EditorAlias(EditorAliases.StringPropertyEditor)]
        public virtual string Description { get; set; } = string.Empty;

        [RuleRequiredField]
        [Aggregated, ExpandObjectMembers(ExpandObjectMembers.Never)]
        [ModelDefault("Caption", "Template File")]
        public virtual FileData TemplateFile { get; set; } = null!;

        [RuleRequiredField]
        [ModelDefault("Caption", "Root Business Object")]
        public virtual UserReportBoType RootBoType { get; set; } = UserReportBoType.Application;

        [RuleRequiredField]
        [ModelDefault("Caption", "Applicability Mode")]
        public virtual ApplicabilityMode ApplicabilityMode { get; set; } = ApplicabilityMode.AllTypes;

        [Size(SizeAttribute.Unlimited)]
        [ModelDefault("Caption", "Visibility Criteria")]
        [VisibleInDetailView(true)]
        [VisibleInListView(false)]
        public virtual string VisibilityCriteria { get; set; } = string.Empty;

        [ModelDefault("Caption", "Applicable Application Types")]
        [VisibleInDetailView(true)]
        [VisibleInListView(false)]
        public virtual IList<ApplicationType> ApplicableTypes { get; set; }

        [ModelDefault("Caption", "Is Active")]
        public virtual bool IsActive { get; set; } = true;

        [ModelDefault("Caption", "Sort Order")]
        public virtual int SortOrder { get; set; } = 0;

        [Browsable(false)]
        public virtual IList<UserReportPlaceholder> Placeholders { get; set; }

        [NotMapped]
        [ModelDefault("Caption", "Validation Status")]
        public virtual string ValidationStatus
        {
            get
            {
                if (Placeholders == null || !Placeholders.Any())
                    return "Not validated";

                var valid = Placeholders.Count(p => p.IsValid);
                var total = Placeholders.Count;
                var invalid = total - valid;

                return invalid == 0
                    ? $"✓ All {total} placeholders valid"
                    : $"⚠ {invalid} of {total} placeholders invalid";
            }
        }
    }
}
