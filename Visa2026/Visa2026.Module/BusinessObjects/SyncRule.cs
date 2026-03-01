using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using System.Linq;
using DevExpress.Xpo;

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
    [RuleCriteria("SyncRule_SourcePropertyRequiredIfValue", DefaultContexts.Save, "IsNullOrEmpty(SourceValue) Or !IsNullOrEmpty(SourceProperty)", "Source Property must be selected if Source Value is defined.")]
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
        [ImmediatePostData]
        public virtual string SourceProperty { get; set; }

        [NotMapped]
        [Browsable(false)]
        public virtual IList<string> SourceProperties
        {
            get
            {
                if (SourceType == null) return new List<string>();
                return XafTypesInfo.Instance.FindTypeInfo(SourceType).Members
                    .Where(m => m.IsPersistent && !m.IsList)
                    .Select(m => m.Name).OrderBy(n => n).ToList();
            }
        }

        [ToolTip("Optional: The rule only runs if the Source Property equals this value.")]
        [Appearance("SourceValueEnabled", Enabled = false, Criteria = "IsNullOrEmpty(SourceProperty)", Context = "DetailView")]
        public virtual string SourceValue { get; set; }

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

        [Size(SizeAttribute.Unlimited)]
        [CriteriaOptions(nameof(SourceType))]
        [EditorAlias(EditorAliases.PopupCriteriaPropertyEditor)]
        [ToolTip("Optional: The rule only runs if the Source Object matches this criteria.")]
        public virtual string SourceCriteria { get; set; }

        [Size(512)]
        [RuleRequiredField]
        [ToolTip("Path to the target object or collection. E.g., 'WorkPermit.Application.ApplicationItems'")]
        public virtual string TargetPath { get; set; }

        [Size(SizeAttribute.Unlimited)]
        [CriteriaOptions(nameof(TargetType))]
        [EditorAlias(EditorAliases.PopupCriteriaPropertyEditor)]
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
                if (TargetType == null) return new List<string>();
                return XafTypesInfo.Instance.FindTypeInfo(TargetType).Members
                    .Where(m => m.IsPersistent && !m.IsReadOnly && !m.IsList)
                    .Select(m => m.Name).OrderBy(n => n).ToList();
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

        [RuleRequiredField(DefaultContexts.Save, CustomMessageTemplate = "Target Value cannot be empty.")]
        public virtual string TargetValue { get; set; }

        public virtual bool IsActive { get; set; } = true;

        [InverseProperty(nameof(SyncRuleLog.SyncRule))]
        [DevExpress.ExpressApp.DC.Aggregated]
        public virtual IList<SyncRuleLog> Logs { get; set; } = new ObservableCollection<SyncRuleLog>();

        [NotMapped]
        [Browsable(false)]
        [RuleFromBoolProperty("SyncRule_SourceCriteriaValid", DefaultContexts.Save, "Invalid Source Criteria syntax.")]
        public bool IsSourceCriteriaValid
        {
            get
            {
                if (string.IsNullOrEmpty(SourceCriteria)) return true;
                try
                {
                    CriteriaOperator.Parse(SourceCriteria);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        [NotMapped]
        [Browsable(false)]
        [RuleFromBoolProperty("SyncRule_TargetMatchCriteriaValid", DefaultContexts.Save, "Invalid Target Match Criteria syntax.")]
        public bool IsTargetMatchCriteriaValid
        {
            get
            {
                if (string.IsNullOrEmpty(TargetMatchCriteria)) return true;
                try
                {
                    CriteriaOperator.Parse(TargetMatchCriteria);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}