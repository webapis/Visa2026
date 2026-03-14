using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("System")]
    [DefaultProperty(nameof(ReportName))]
    [ModelDefault("Caption", "Report Visibility")]
    public class ReportVisibility : BaseObject
    {
        public ReportVisibility()
        {

        }

        [RuleRequiredField]
        [MaxLength(255)]
        [ModelDefault("Caption", "Report Name")]
        public virtual string ReportName { get; set; }

        [MaxLength(255)]
        [ModelDefault("Caption", "Report Display Name")]
        public virtual string ReportDisplayName { get; set; }

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

        [Size(SizeAttribute.Unlimited)]
        [ModelDefault("Caption", "Visibility Criteria")]
        [CriteriaOptions(nameof(TargetType))]
        [EditorAlias(EditorAliases.PopupCriteriaPropertyEditor)]
        public virtual string VisibilityCriteria { get; set; }

        [NotMapped]
        [Browsable(false)]
        public virtual IList<Type> AvailableTargetTypes
        {
            get
            {
                return XafTypesInfo.Instance.PersistentTypes
                    .Where(t => t.IsPersistent && !t.IsAbstract && typeof(BaseObject).IsAssignableFrom(t.Type))
                    .Select(t => t.Type)
                    .OrderBy(t => t.Name)
                    .ToList();
            }
        }
    }
}