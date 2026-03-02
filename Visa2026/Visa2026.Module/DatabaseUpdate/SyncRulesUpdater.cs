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

            // 5. Rule: Pull Visa from Employee
            // When ApplicationItem.Employee changes, pull the visa.
            CreateOrResetRule(
                name: "Pull Visa from Employee",
                sourceType: typeof(ApplicationItem),
                sourceProperty: "Employee",
                sourceValue: null, // Any change
                trigger: SyncTriggerType.PropertyChanged,
                targetPath: "@Self", // Update the ApplicationItem itself
                targetMatchCriteria: null,
                targetType: typeof(ApplicationItem),
                targetProperty: "CurrentVisa",
                targetValue: "@Source.Employee.CurrentVisa" // Dynamic pull
            );

            // 6. Rule: Pull Visa from FamilyMember
            // When ApplicationItem.FamilyMember changes, pull the visa.
            CreateOrResetRule(
                name: "Pull Visa from FamilyMember",
                sourceType: typeof(ApplicationItem),
                sourceProperty: "FamilyMember",
                sourceValue: null, // Any change
                trigger: SyncTriggerType.PropertyChanged,
                targetPath: "@Self", // Update the ApplicationItem itself
                targetMatchCriteria: null,
                targetType: typeof(ApplicationItem),
                targetProperty: "CurrentVisa",
                targetValue: "@Source.FamilyMember.CurrentVisa" // Dynamic pull
            );

            // 7. Rule: Pull AddressOfResidence from FamilyMember
            // When ApplicationItem.FamilyMember changes, pull the address.
            CreateOrResetRule(
                name: "Pull AddressOfResidence from FamilyMember",
                sourceType: typeof(ApplicationItem),
                sourceProperty: "FamilyMember",
                sourceValue: null, // Any change
                trigger: SyncTriggerType.PropertyChanged,
                targetPath: "@Self", // Update the ApplicationItem itself
                targetMatchCriteria: null,
                targetType: typeof(ApplicationItem),
                targetProperty: "CurrentAddressOfResidence",
                targetValue: "@Source.FamilyMember.CurrentAddressOfResidence" // Dynamic pull
            );

            // 8. Rule: Pull AddressOfResidence from Employee
            // When ApplicationItem.Employee changes, pull the address.
            CreateOrResetRule(
                name: "Pull AddressOfResidence from Employee",
                sourceType: typeof(ApplicationItem),
                sourceProperty: "Employee",
                sourceValue: null, // Any change
                trigger: SyncTriggerType.PropertyChanged,
                targetPath: "@Self", // Update the ApplicationItem itself
                targetMatchCriteria: null,
                targetType: typeof(ApplicationItem),
                targetProperty: "CurrentAddressOfResidence",
                targetValue: "@Source.Employee.CurrentAddressOfResidence" // Dynamic pull
            );

            // 9. Rule: Pull PositionHistory from Employee
            // When ApplicationItem.Employee changes, pull the position history.
            CreateOrResetRule(
                name: "Pull PositionHistory from Employee",
                sourceType: typeof(ApplicationItem),
                sourceProperty: "Employee",
                sourceValue: null, // Any change
                trigger: SyncTriggerType.PropertyChanged,
                targetPath: "@Self", // Update the ApplicationItem itself
                targetMatchCriteria: null,
                targetType: typeof(ApplicationItem),
                targetProperty: "CurrentPositionHistory",
                targetValue: "@Source.Employee.CurrentPositionHistory" // Dynamic pull
            );

            // 10. Rule: Pull MedicalRecord from Employee
            // When ApplicationItem.Employee changes, pull the medical record.
            CreateOrResetRule(
                name: "Pull MedicalRecord from Employee",
                sourceType: typeof(ApplicationItem),
                sourceProperty: "Employee",
                sourceValue: null, // Any change
                trigger: SyncTriggerType.PropertyChanged,
                targetPath: "@Self", // Update the ApplicationItem itself
                targetMatchCriteria: null,
                targetType: typeof(ApplicationItem),
                targetProperty: "CurrentMedicalRecord",
                targetValue: "@Source.Employee.CurrentMedicalRecord" // Dynamic pull
            );

            // 11. Rule: Pull MedicalRecord from FamilyMember
            // When ApplicationItem.FamilyMember changes, pull the medical record.
            CreateOrResetRule(
                name: "Pull MedicalRecord from FamilyMember",
                sourceType: typeof(ApplicationItem),
                sourceProperty: "FamilyMember",
                sourceValue: null, // Any change
                trigger: SyncTriggerType.PropertyChanged,
                targetPath: "@Self", // Update the ApplicationItem itself
                targetMatchCriteria: null,
                targetType: typeof(ApplicationItem),
                targetProperty: "CurrentMedicalRecord",
                targetValue: "@Source.FamilyMember.CurrentMedicalRecord" // Dynamic pull
            );

            // 12. Rule: Pull Education from Employee
            // When ApplicationItem.Employee changes, pull the education.
            CreateOrResetRule(
                name: "Pull Education from Employee",
                sourceType: typeof(ApplicationItem),
                sourceProperty: "Employee",
                sourceValue: null, // Any change
                trigger: SyncTriggerType.PropertyChanged,
                targetPath: "@Self", // Update the ApplicationItem itself
                targetMatchCriteria: null,
                targetType: typeof(ApplicationItem),
                targetProperty: "CurrentEducation",
                targetValue: "@Source.Employee.CurrentEducation" // Dynamic pull
            );

            // 13. Rule: Pull Education from FamilyMember
            // When ApplicationItem.FamilyMember changes, pull the education.
            CreateOrResetRule(
                name: "Pull Education from FamilyMember",
                sourceType: typeof(ApplicationItem),
                sourceProperty: "FamilyMember",
                sourceValue: null, // Any change
                trigger: SyncTriggerType.PropertyChanged,
                targetPath: "@Self", // Update the ApplicationItem itself
                targetMatchCriteria: null,
                targetType: typeof(ApplicationItem),
                targetProperty: "CurrentEducation",
                targetValue: "@Source.FamilyMember.CurrentEducation" // Dynamic pull
            );

            // 14. Rule: Pull EmployeeContract from Employee
            // When ApplicationItem.Employee changes, pull the employee contract.
            CreateOrResetRule(
                name: "Pull EmployeeContract from Employee",
                sourceType: typeof(ApplicationItem),
                sourceProperty: "Employee",
                sourceValue: null, // Any change
                trigger: SyncTriggerType.PropertyChanged,
                targetPath: "@Self", // Update the ApplicationItem itself
                targetMatchCriteria: null,
                targetType: typeof(ApplicationItem),
                targetProperty: "CurrentEmployeeContract",
                targetValue: "@Source.Employee.CurrentEmployeeContract" // Dynamic pull
            );

            // 15. Rule: Pull InvitationItem from Employee
            // When ApplicationItem.Employee changes, pull the invitation item.
            CreateOrResetRule(
                name: "Pull InvitationItem from Employee",
                sourceType: typeof(ApplicationItem),
                sourceProperty: "Employee",
                sourceValue: null, // Any change
                trigger: SyncTriggerType.PropertyChanged,
                targetPath: "@Self", // Update the ApplicationItem itself
                targetMatchCriteria: null,
                targetType: typeof(ApplicationItem),
                targetProperty: "CurrentInvitationItem",
                targetValue: "@Source.Employee.CurrentInvitationItem" // Dynamic pull
            );

            // 16. Rule: Pull InvitationItem from FamilyMember
            // When ApplicationItem.FamilyMember changes, pull the invitation item.
            CreateOrResetRule(
                name: "Pull InvitationItem from FamilyMember",
                sourceType: typeof(ApplicationItem),
                sourceProperty: "FamilyMember",
                sourceValue: null, // Any change
                trigger: SyncTriggerType.PropertyChanged,
                targetPath: "@Self", // Update the ApplicationItem itself
                targetMatchCriteria: null,
                targetType: typeof(ApplicationItem),
                targetProperty: "CurrentInvitationItem",
                targetValue: "@Source.FamilyMember.CurrentInvitationItem" // Dynamic pull
            );

            // 17. Rule: Pull WorkPermitItem from Employee
            // When ApplicationItem.Employee changes, pull the work permit item.
            CreateOrResetRule(
                name: "Pull WorkPermitItem from Employee",
                sourceType: typeof(ApplicationItem),
                sourceProperty: "Employee",
                sourceValue: null, // Any change
                trigger: SyncTriggerType.PropertyChanged,
                targetPath: "@Self", // Update the ApplicationItem itself
                targetMatchCriteria: null,
                targetType: typeof(ApplicationItem),
                targetProperty: "CurrentWorkPermitItem",
                targetValue: "@Source.Employee.CurrentWorkPermitItem" // Dynamic pull
            );

            // 18. Rule: Mark InvitationItem as Used
            // When a Visa is saved, mark the linked InvitationItem as Used.
            CreateOrResetRule(
                name: "Mark InvitationItem as Used",
                sourceType: typeof(Visa),
                sourceProperty: null, // Any change or new object
                sourceValue: null,
                trigger: SyncTriggerType.Save,
                targetPath: "InvitationItem",
                targetMatchCriteria: null,
                targetType: typeof(InvitationItem),
                targetProperty: "IsUsed",
                targetValue: "true"
            );

            // 19. Rule: Revert InvitationItem IsUsed on Delete
            // When a Visa is deleted, revert the linked InvitationItem IsUsed to false.
            CreateOrResetRule(
                name: "Revert InvitationItem IsUsed on Delete",
                sourceType: typeof(Visa),
                sourceProperty: null, // Any deletion
                sourceValue: null,
                trigger: SyncTriggerType.Delete,
                targetPath: "InvitationItem",
                targetMatchCriteria: null,
                targetType: typeof(InvitationItem),
                targetProperty: "IsUsed",
                targetValue: "false"
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