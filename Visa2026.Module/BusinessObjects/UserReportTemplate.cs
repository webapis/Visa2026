using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    /// <summary>User-defined Word or Excel report template uploaded via XAF UI.</summary>
    [DefaultClassOptions]
    [NavigationItem("Reports")]
    [DefaultProperty(nameof(TemplateName))]
    [ModelDefault("Caption", "User Report Template")]
    [FileAttachment(nameof(TemplateFile))]
    public class UserReportTemplate : BaseObject
    {
        public UserReportTemplate()
        {
            ApplicableTypeLinks = new ObservableCollection<UserReportTemplateApplicationType>();
            Placeholders = new ObservableCollection<UserReportPlaceholder>();
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

        [ModelDefault("Caption", "Output Format")]
        [ImmediatePostData]
        public virtual TemplateOutputFormat TemplateOutputFormat { get; set; } = TemplateOutputFormat.Word;

        [ModelDefault("Caption", "Excel Merge Mode")]
        [ImmediatePostData]
        [Appearance("HideExcelMergeModeForWord", Visibility = ViewItemVisibility.Hide, Criteria = "TemplateOutputFormat != ##Enum#Visa2026.Module.BusinessObjects.TemplateOutputFormat,Excel#")]
        public virtual ExcelMergeMode ExcelMergeMode { get; set; } = ExcelMergeMode.ItemList;

        [ModelDefault("Caption", "Root Business Object")]
        [ImmediatePostData]
        [ToolTip("Criteria editor member list follows this type. Changing it clears no text — re-open the criteria popup if members look wrong.")]
        public virtual UserReportBoType RootBoType { get; set; } = UserReportBoType.Application;

        [ModelDefault("Caption", "Applicability Mode")]
        public virtual ApplicabilityMode ApplicabilityMode { get; set; } = ApplicabilityMode.AllTypes;

        [FieldSize(FieldSizeAttribute.Unlimited)]
        [ModelDefault("Caption", "Visibility Criteria")]
        [CriteriaOptions(nameof(CriteriaTargetType))]
        [EditorAlias(EditorAliases.PopupCriteriaPropertyEditor)]
        [VisibleInDetailView(true)]
        [VisibleInListView(false)]
        [ToolTip("For Root = Application, evaluated on the current Application. For other roots, visible when any non-deleted row in that collection matches.")]
        public virtual string VisibilityCriteria { get; set; } = string.Empty;

        /// <summary>Type passed to the criteria property editor; aligned with <see cref="RootBoType"/>.</summary>
        [NotMapped]
        [Browsable(false)]
        public virtual Type CriteriaTargetType =>
            RootBoType switch
            {
                UserReportBoType.ApplicationItem => typeof(ApplicationItem),
                UserReportBoType.Person => typeof(Person),
                _ => typeof(Application)
            };

        [Aggregated]
        [ModelDefault("Caption", "Applicable Application Types")]
        [VisibleInDetailView(true)]
        [VisibleInListView(false)]
        public virtual IList<UserReportTemplateApplicationType> ApplicableTypeLinks { get; set; }

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

        /// <summary>
        /// Output format for generation/extract, using <see cref="TemplateOutputFormat"/> and falling back to the attached file extension
        /// when the enum was not updated (e.g. seeded .xlsx before Excel format existed).
        /// </summary>
        public TemplateOutputFormat GetEffectiveOutputFormat()
        {
            if (TemplateOutputFormat == TemplateOutputFormat.Excel)
                return TemplateOutputFormat.Excel;

            var fileName = TemplateFile?.FileName;
            if (!string.IsNullOrEmpty(fileName)
                && (fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
                    || fileName.EndsWith(".xlsm", StringComparison.OrdinalIgnoreCase)))
                return TemplateOutputFormat.Excel;

            return TemplateOutputFormat.Word;
        }
    }
}
