using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
    /// <summary>Legacy mail-merge visibility rules; disabled while <see cref="MailMergeFeature.Enabled"/> is false.</summary>
    [DefaultClassOptions]
    [NavigationItem("System")]
    [DefaultProperty(nameof(TemplateName))]
    [ModelDefault("Caption", "Mail Merge Visibility")]
    public class MailMergeVisibility : BaseObject
    {
        public MailMergeVisibility() { }

        [RuleRequiredField]
        [MaxLength(255)]
        [ModelDefault("Caption", "Template Name")]
        public virtual string TemplateName { get; set; }

        [Browsable(false)]
        public virtual string TargetTypeFullName { get; set; }

        [NotMapped]
        [ModelDefault("Caption", "Target Type")]
        [ImmediatePostData]
        [DataSourceProperty(nameof(AvailableTargetTypes))]
        public virtual Type TargetType
        {
            get => TargetTypeFullName != null ? XafTypesInfo.Instance.FindTypeInfo(TargetTypeFullName)?.Type : null;
            set => TargetTypeFullName = value?.FullName;
        }

        [FieldSize(FieldSizeAttribute.Unlimited)]
        [ModelDefault("Caption", "Visibility Criteria")]
        [CriteriaOptions(nameof(TargetType))]
        [EditorAlias(EditorAliases.PopupCriteriaPropertyEditor)]
        public virtual string VisibilityCriteria { get; set; }

        [NotMapped]
        [Browsable(false)]
        public virtual IList<Type> AvailableTargetTypes => XafTypesInfo.Instance.PersistentTypes
            .Where(t => t.IsPersistent && !t.IsAbstract && typeof(BaseObject).IsAssignableFrom(t.Type))
            .Select(t => t.Type)
            .OrderBy(t => t.Name)
            .ToList();
    }
}