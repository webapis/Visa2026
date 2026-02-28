using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Linq;

namespace Visa2026.Module.BusinessObjects
{
    public enum SyncTriggerType
    {
        Save,
        Delete,
        Update
    }

    [DefaultClassOptions]
    [NavigationItem("System")]
    [ImageName("ModelEditor_Action_Modules")]
    public class SyncRule : BaseObject
    {
        [RuleRequiredField]
        public virtual string Name { get; set; }

        [Browsable(false)]
        public virtual string SourceTypeFullName { get; set; }

        [NotMapped]
        [RuleRequiredField]
        [ImmediatePostData]
        [DataSourceProperty(nameof(AvailableSourceTypes))]
        public virtual Type SourceType
        {
            get => SourceTypeFullName != null ? XafTypesInfo.Instance.FindTypeInfo(SourceTypeFullName)?.Type : null;
            set => SourceTypeFullName = value?.FullName;
        }

        [DataSourceProperty(nameof(SourceProperties))]
        [ToolTip("Optional: Select a specific property to trigger this rule.")]
        public virtual string SourceProperty { get; set; }

        [NotMapped]
        [Browsable(false)]
        public virtual IList<string> SourceProperties
        {
            get
            {
                return SourceType != null ? XafTypesInfo.Instance.FindTypeInfo(SourceType).Members.Where(m => m.IsVisible).Select(m => m.Name).OrderBy(n => n).ToList() : new List<string>();
            }
        }

        [NotMapped]
        [Browsable(false)]
        public virtual IList<Type> AvailableSourceTypes
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

        public virtual SyncTriggerType TriggerType { get; set; }

        [FieldSize(DevExpress.Xpo.SizeAttribute.Unlimited)]
        [CriteriaOptions(nameof(SourceType))]
        [EditorAlias(EditorAliases.PopupCriteriaPropertyEditor)]
        [ToolTip("Optional: The rule only runs if the Source Object matches this criteria.")]
        public virtual string SourceCriteria { get; set; }

        [MaxLength(512)]
        [RuleRequiredField]
        [ToolTip("Path to the target object or collection. E.g., 'WorkPermit.Application.ApplicationItems'")]
        public virtual string TargetPath { get; set; }

        [FieldSize(DevExpress.Xpo.SizeAttribute.Unlimited)]
        [ToolTip("Criteria to find the specific target item. Use '@Source.PropName' to reference source values. E.g., 'Person.Oid == @Source.Employee.Oid'")]
        public virtual string TargetMatchCriteria { get; set; }

        [Browsable(false)]
        public virtual string TargetTypeFullName { get; set; }

        [NotMapped]
        [ImmediatePostData]
        [RuleRequiredField]
        [DataSourceProperty(nameof(AvailableTargetTypes))]
        public virtual Type TargetType
        {
            get => TargetTypeFullName != null ? XafTypesInfo.Instance.FindTypeInfo(TargetTypeFullName)?.Type : null;
            set => TargetTypeFullName = value?.FullName;
        }

        [RuleRequiredField]
        [DataSourceProperty(nameof(TargetProperties))]
        public virtual string TargetProperty { get; set; }

        [NotMapped]
        [Browsable(false)]
        public virtual IList<string> TargetProperties
        {
            get
            {
                return TargetType != null ? XafTypesInfo.Instance.FindTypeInfo(TargetType).Members.Where(m => m.IsVisible).Select(m => m.Name).OrderBy(n => n).ToList() : new List<string>();
            }
        }

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

        [RuleRequiredField]
        public virtual string TargetValue { get; set; }

        public virtual bool IsActive { get; set; } = true;

        [InverseProperty(nameof(SyncRuleLog.SyncRule))]
        [DevExpress.ExpressApp.DC.Aggregated]
        public virtual IList<SyncRuleLog> Logs { get; set; } = new ObservableCollection<SyncRuleLog>();
    }
}