using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("System")]
    [ImageName("BO_Validation")]
    public class StateChangeRule : BaseObject
    {
        [RuleRequiredField]
        [RuleUniqueValue]
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

        [NotMapped]
        [Browsable(false)]
        public virtual IList<Type> AvailableSourceTypes => XafTypesInfo.Instance.PersistentTypes
            .Where(t => t.IsPersistent && !t.IsAbstract && typeof(BaseObject).IsAssignableFrom(t.Type))
            .Select(t => t.Type)
            .OrderBy(t => t.Name)
            .ToList();

        public virtual SyncTriggerType TriggerType { get; set; }

        [DataSourceProperty(nameof(SourceProperties))]
        [ToolTip("Optional: Select a specific property to watch for PropertyChanged trigger.")]
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

        [FieldSize(FieldSizeAttribute.Unlimited)]
        [CriteriaOptions(nameof(SourceType))]
        [EditorAlias(EditorAliases.PopupCriteriaPropertyEditor)]
        [ToolTip("Optional: The rule only runs if the Source Object matches this criteria.")]
        public virtual string SourceCriteria { get; set; }

        [RuleRequiredField]
        [ToolTip("Navigation path from Source to the Tracked Object (e.g., 'Application.Visas').")]
        public virtual string TargetPath { get; set; }

        [FieldSize(FieldSizeAttribute.Unlimited)]
        [EditorAlias(EditorAliases.PopupCriteriaPropertyEditor)]
        [ToolTip("If Target Path points to a collection, this criteria is used to find the specific item(s) to log against. Use '@Source.PropName' to reference source values.")]
        public virtual string TargetMatchCriteria { get; set; }

        [ToolTip("Optional: If the Target Path resolves to an object or collection, this sub-path is used to navigate to the final object to be logged. E.g., if Target Path is 'ApplicationItems', Sub Path could be 'CurrentVisa'.")]
        [XafDisplayName("Target Sub-Path")]
        public virtual string TargetSubPath { get; set; }

        [RuleRequiredField]
        [XafDisplayName("State (Result)")]
        [ToolTip("The string to write to the log (e.g., 'Visa Extended').")]
        public virtual string State { get; set; }

        [FieldSize(FieldSizeAttribute.Unlimited)]
        [ToolTip("Optional: A template for the log description allowing macros (e.g., 'Approved by @Source.User.Name').")]
        public virtual string DescriptionTemplate { get; set; }

        public virtual bool IsActive { get; set; } = true;

        [NotMapped]
        [Browsable(false)]
        [RuleFromBoolProperty("StateChangeRule_SourceCriteriaValid", DefaultContexts.Save, "Invalid Source Criteria syntax.")]
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
        [RuleFromBoolProperty("StateChangeRule_TargetMatchCriteriaValid", DefaultContexts.Save, "Invalid Target Match Criteria syntax.")]
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