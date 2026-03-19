using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using DevExpress.Data.Filtering;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate
{
    public class StateChangeRulesUpdater : ModuleUpdater
    {
        public StateChangeRulesUpdater(IObjectSpace objectSpace, Version currentDBVersion) :
            base(objectSpace, currentDBVersion)
        {
        }

        public override void UpdateDatabaseAfterUpdateSchema()
        {
            base.UpdateDatabaseAfterUpdateSchema();
            System.Diagnostics.Debug.WriteLine("[StateChangeRulesUpdater] UpdateDatabaseAfterUpdateSchema started.");

            // Example Rule: Log when a visa extension application is sent to the ministry.
            // This log will be attached to the specific Visa being extended.
            CreateOrResetRule(
                name: "Log Visa Extension Process Started",
                sourceType: typeof(Application),
                trigger: SyncTriggerType.PropertyChanged,
                sourceProperty: "CurrentState",
                sourceCriteria: "[CurrentState.Code] = 'SENT_TO_MINISTRY' And [ApplicationType.Code] = 'visa_extension'",
                targetPath: "ApplicationItems",
                targetMatchCriteria: "[CurrentVisa] Is Not Null",
                targetSubPath: "CurrentVisa", // Navigate from ApplicationItem to the Visa to log against it.
                state: "Extension Process Started",
                descriptionTemplate: "Application sent to ministry."
            );

            // New Rule: Log when a Work Permit Item is issued.
            // This log will be attached to the parent WorkPermit object.
            CreateOrResetRule(
                name: "Log Work Permit Item Issued",
                sourceType: typeof(WorkPermitItem),
                trigger: SyncTriggerType.Create,
                sourceProperty: null,
                sourceCriteria: "[WorkPermit] Is Not Null And [Person] Is Not Null",
                targetPath: "WorkPermit", // Navigate from WorkPermitItem to the parent WorkPermit
                targetMatchCriteria: null,
                targetSubPath: null,
                state: "Permit Item Issued",
                descriptionTemplate: "Work permit item issued for employee @Source.Person.FullName."
            );

            System.Diagnostics.Debug.WriteLine("[StateChangeRulesUpdater] Committing changes...");
            ObjectSpace.CommitChanges();
            System.Diagnostics.Debug.WriteLine("[StateChangeRulesUpdater] Changes committed.");
        }

        private void CreateOrResetRule(string name, Type sourceType, SyncTriggerType trigger,
                                       string sourceProperty, string sourceCriteria,
                                       string targetPath, string targetMatchCriteria, string targetSubPath,
                                       string state, string descriptionTemplate)
        {
            System.Diagnostics.Debug.WriteLine($"[StateChangeRulesUpdater] Checking rule: '{name}'");
            var rule = ObjectSpace.FindObject<StateChangeRule>(CriteriaOperator.Parse("Name=?", name)) 
                       ?? ObjectSpace.CreateObject<StateChangeRule>();
            
            rule.Name = name;
            rule.SourceType = sourceType;
            rule.TriggerType = trigger;
            rule.SourceProperty = sourceProperty;
            rule.SourceCriteria = sourceCriteria;
            rule.TargetPath = targetPath;
            rule.TargetMatchCriteria = targetMatchCriteria;
            rule.TargetSubPath = targetSubPath;
            rule.State = state;
            rule.DescriptionTemplate = descriptionTemplate;
            rule.IsActive = true;
        }
    }
}