using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using DevExpress.Data.Filtering;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate
{
    public class SyncRulesUpdater : ModuleUpdater
    {
        public SyncRulesUpdater(IObjectSpace objectSpace, Version currentDBVersion) :
            base(objectSpace, currentDBVersion)
        {
        }

        public override void UpdateDatabaseAfterUpdateSchema()
        {
            base.UpdateDatabaseAfterUpdateSchema();
            System.Diagnostics.Debug.WriteLine("[SyncRulesUpdater] UpdateDatabaseAfterUpdateSchema started.");

            // 1. Rule: Deactivate Sibling Visas
            // Ensures only one Visa is active per Passport.
            CreateOrResetRule(
                name: "Deactivate Sibling Visas",
                sourceType: typeof(Visa),
                sourceProperty: "IsActive",
                sourceValue: "true",
                trigger: SyncTriggerType.Save,
                targetPath: "Passport.Visas",
                targetMatchCriteria: "[ID] != '@Source.ID'", // Exclude self
                targetType: typeof(Visa),
                targetProperty: "IsActive",
                targetValue: "false"
            );

            // 2. Rule: Set Passport Current Visa
            // Links the Passport to the newly active Visa.
            CreateOrResetRule(
                name: "Set Passport Current Visa",
                sourceType: typeof(Visa),
                sourceProperty: "IsActive",
                sourceValue: "true",
                trigger: SyncTriggerType.Save,
                targetPath: "Passport",
                targetMatchCriteria: null, // No criteria needed for single object
                targetType: typeof(Passport),
                targetProperty: "CurrentVisa",
                targetValue: "@Source" // Assign the Visa object itself
            );

            // 3. Rule: Pull Passport from Employee
            // When ApplicationItem.Employee changes, pull the passport.
            CreateOrResetRule(
                name: "Pull Passport from Employee",
                sourceType: typeof(ApplicationItem),
                sourceProperty: "Employee",
                sourceValue: null, // Any change
                trigger: SyncTriggerType.PropertyChanged,
                targetPath: "@Self", // Update the ApplicationItem itself
                targetMatchCriteria: null,
                targetType: typeof(ApplicationItem),
                targetProperty: "CurrentPassport",
                targetValue: "@Source.Employee.CurrentPassport" // Dynamic pull
            );

            // 4. Rule: Pull Passport from FamilyMember
            // When ApplicationItem.FamilyMember changes, pull the passport.
            CreateOrResetRule(
                name: "Pull Passport from FamilyMember",
                sourceType: typeof(ApplicationItem),
                sourceProperty: "FamilyMember",
                sourceValue: null, // Any change
                trigger: SyncTriggerType.PropertyChanged,
                targetPath: "@Self",
                targetMatchCriteria: null,
                targetType: typeof(ApplicationItem),
                targetProperty: "CurrentPassport",
                targetValue: "@Source.FamilyMember.CurrentPassport"
            );

            System.Diagnostics.Debug.WriteLine("[SyncRulesUpdater] Committing changes...");
            ObjectSpace.CommitChanges();
            System.Diagnostics.Debug.WriteLine("[SyncRulesUpdater] Changes committed.");
        }

        private void CreateOrResetRule(string name, Type sourceType, string sourceProperty, string sourceValue,
                                       SyncTriggerType trigger, string targetPath, string targetMatchCriteria,
                                       Type targetType, string targetProperty, string targetValue)
        {
            System.Diagnostics.Debug.WriteLine($"[SyncRulesUpdater] Checking rule: '{name}'");
            var rule = ObjectSpace.FindObject<SyncRule>(CriteriaOperator.Parse("Name=?", name));
            if (rule == null)
            {
                System.Diagnostics.Debug.WriteLine($"[SyncRulesUpdater] Rule '{name}' not found. Creating new.");
                rule = ObjectSpace.CreateObject<SyncRule>();
                rule.Name = name;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[SyncRulesUpdater] Rule '{name}' found. Updating properties.");
            }

            // Force reset of all properties to ensure correct configuration
            rule.SourceType = sourceType;
            rule.SourceProperty = sourceProperty;
            rule.SourceValue = sourceValue;
            rule.TriggerType = trigger;
            rule.TargetPath = targetPath;
            rule.TargetMatchCriteria = targetMatchCriteria;
            rule.TargetType = targetType;
            rule.TargetProperty = targetProperty;
            rule.TargetValue = targetValue;
            rule.IsActive = true;
        }
    }
}