using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("System")]
    [DefaultProperty(nameof(State))]
    [ImageName("BO_Audit_ChangeHistory")]
    [Appearance("StateChangeLog_NegativeState", AppearanceItemType = "ViewItem", TargetItems = "*",
        Criteria = "Contains(State, 'Cancel') Or Contains(State, 'Reject') Or Contains(State, 'Deactivate')",
        Context = "ListView", FontColor = "Red", Priority = 1)]
    [Appearance("StateChangeLog_ErrorState", AppearanceItemType = "ViewItem", TargetItems = "*",
        Criteria = "Contains(State, 'Error')",
        Context = "ListView", FontColor = "DarkRed", FontStyle = DevExpress.Drawing.DXFontStyle.Bold, Priority = 2)]
    [Appearance("StateChangeLog_PositiveState", AppearanceItemType = "ViewItem", TargetItems = "*",
        Criteria = "Contains(State, 'Issue') Or Contains(State, 'Complete') Or Contains(State, 'Start') Or Contains(State, 'Link') Or Contains(State, 'Process')",
        Context = "ListView", FontColor = "Green", Priority = 0)]
    public class StateChangeLog : BaseObject
    {
        public virtual DateTime DateTime { get; set; }

        [RuleRequiredField]
        public virtual string State { get; set; }

        [Browsable(false)]
        public virtual string TargetBoTypeFullName { get; set; }

        [NotMapped]
        [XafDisplayName("Target Type")]
        public virtual Type TargetBoType
        {
            get => !string.IsNullOrEmpty(TargetBoTypeFullName) ? XafTypesInfo.Instance.FindTypeInfo(TargetBoTypeFullName)?.Type : null;
            set => TargetBoTypeFullName = value?.FullName;
        }

        public virtual string TargetObjectId { get; set; }

        [XafDisplayName("Target Object")]
        public virtual string TargetDisplayValue { get; set; }

        [Browsable(false)]
        public virtual string SourceBoTypeFullName { get; set; }

        [NotMapped]
        [XafDisplayName("Source Type")]
        public virtual Type SourceBoType
        {
            get => !string.IsNullOrEmpty(SourceBoTypeFullName) ? XafTypesInfo.Instance.FindTypeInfo(SourceBoTypeFullName)?.Type : null;
            set => SourceBoTypeFullName = value?.FullName;
        }

        public virtual string SourceObjectId { get; set; }

        [XafDisplayName("Source Object")]
        public virtual string SourceDisplayValue { get; set; }

        public virtual string RuleName { get; set; }

        [FieldSize(FieldSizeAttribute.Unlimited)]
        public virtual string Description { get; set; }

        public virtual string User { get; set; }
    }
}