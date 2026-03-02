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
                targetPath: "Invitation.InvitationItems",
                targetMatchCriteria: "[Person.ID] = '@Source.Passport.Person.ID'",
                targetType: typeof(InvitationItem),
                targetProperty: "IsUsed",
                targetValue: "true",
                sourceCriteria: "[Application.ApplicationType.Code] = 'get_invitation'"
            );

            // 19. Rule: Revert InvitationItem IsUsed on Delete
            // When a Visa is deleted, revert the linked InvitationItem IsUsed to false.
            CreateOrResetRule(
                name: "Revert InvitationItem IsUsed on Delete",
                sourceType: typeof(Visa),
                sourceProperty: null,
                sourceValue: null,
                trigger: SyncTriggerType.Delete,
                targetPath: "Invitation.InvitationItems",
                targetMatchCriteria: "[Person.ID] = '@Source.Passport.Person.ID'",
                targetType: typeof(InvitationItem),
                targetProperty: "IsUsed",
                targetValue: "false",
                sourceCriteria: "[Invitation] Is Not Null"
            );

            // 20. Rule: Clear InvitationItem on Application Change
            // When Visa.Application changes, clear the InvitationItem to prevent invalid selection.
            CreateOrResetRule(
                name: "Clear InvitationItem on Application Change",
                sourceType: typeof(Visa),
                sourceProperty: "Application",
                sourceValue: null, // Any change
                trigger: SyncTriggerType.PropertyChanged,
                targetPath: "@Self",
                targetMatchCriteria: null,
                targetType: typeof(Visa),
                targetProperty: "InvitationItem",
                targetValue: "@Null"
            );

            // 21. Rule: Auto-Set ApplicationItem on Application Change
            // Finds the ApplicationItem for the current Person within the selected Application.
            CreateOrResetRule(
                name: "Auto-Set ApplicationItem on Application Change",
                sourceType: typeof(Visa),
                sourceProperty: "Application",
                sourceValue: null,
                trigger: SyncTriggerType.PropertyChanged,
                targetPath: "@Self",
                targetMatchCriteria: null,
                targetType: typeof(Visa),
                targetProperty: "ApplicationItem",
                targetValue: "@Source.Application.ApplicationItems[Person.ID = ^.Passport.Person.ID]"
            );

            // 22. Rule: Auto-Set InvitationItem on Application Change
            // Finds the InvitationItem for the current Person within the selected Application's invitations.
            CreateOrResetRule(
                name: "Auto-Set InvitationItem on Application Change",
                sourceType: typeof(Visa),
                sourceProperty: "Application",
                sourceValue: null,
                trigger: SyncTriggerType.PropertyChanged,
                targetPath: "@Self",
                targetMatchCriteria: null,
                targetType: typeof(Visa),
                targetProperty: "InvitationItem",
                targetValue: "@Source.AvailableInvitationItems[Person.ID = ^.Passport.Person.ID]"
            );

            // 23. Rule: Set Visa Issued Flag on Application Item
            // When a Visa is saved, mark the linked ApplicationItem as VisaIssued.
            CreateOrResetRule(
                name: "Set Visa Issued Flag on Application Item",
                sourceType: typeof(Visa),
                sourceProperty: null, // Any change or new object
                sourceValue: null,
                trigger: SyncTriggerType.Save,
                targetPath: "ApplicationItem",
                targetMatchCriteria: null,
                targetType: typeof(ApplicationItem),
                targetProperty: "VisaIssued",
                targetValue: "true",
                sourceCriteria: "[Application.ApplicationType.Code] = 'visa_extension'"
            );

            // 24. Rule: Revert Visa Issued Flag on Delete
            // When a Visa is deleted, revert the linked ApplicationItem VisaIssued to false.
            CreateOrResetRule(
                name: "Revert Visa Issued Flag on Delete",
                sourceType: typeof(Visa),
                sourceProperty: null, // Any deletion
                sourceValue: null,
                trigger: SyncTriggerType.Delete,
                targetPath: "ApplicationItem",
                targetMatchCriteria: null,
                targetType: typeof(ApplicationItem),
                targetProperty: "VisaIssued",
                targetValue: "false"
            );


            // 25. Rule: Set WorkPermit Issued Flag on Application Item
            // When a WorkPermitItem is saved, mark the linked ApplicationItem as WorkPermitItemIssued.
            CreateOrResetRule(
                name: "Set WorkPermit Issued Flag on Application Item",
                sourceType: typeof(WorkPermitItem),
                sourceProperty: null,
                sourceValue: null,
                trigger: SyncTriggerType.Save,
                targetPath: "Employee.ApplicationItems",
                targetMatchCriteria: "[Person.ID] = '@Source.Employee.ID' And [Application.ID] = '@Source.WorkPermit.Application.ID'",
                targetType: typeof(ApplicationItem),
                targetProperty: "WorkPermitItemIsIssued",
                targetValue: "true"
            );

            // 26. Rule: Revert WorkPermit Issued Flag on Delete
            // When a WorkPermitItem is deleted, revert the linked ApplicationItem WorkPermitItemIssued to false.
            CreateOrResetRule(
                name: "Revert WorkPermit Issued Flag on Delete",
                sourceType: typeof(WorkPermitItem),
                sourceProperty: null,
                sourceValue: null,
                trigger: SyncTriggerType.Delete,
                targetPath: "Employee.ApplicationItems",
                targetMatchCriteria: "[CurrentWorkPermitItem.ID] = '@Source.ID'",
                targetType: typeof(ApplicationItem),
                targetProperty: "WorkPermitItemIsIssued",
                targetValue: "false"
            );

            // 27. Rule: Set WorkPermit Cancelled Flag on Link
            // When a WorkPermitItem is linked to an ApplicationItem in a cancellation application, set the flag.
            CreateOrResetRule(
                name: "Set WorkPermit Cancelled Flag on Link",
                sourceType: typeof(ApplicationItem),
                sourceProperty: "CurrentWorkPermitItem",
                sourceValue: null,
                trigger: SyncTriggerType.PropertyChanged,
                targetPath: "@Self",
                targetMatchCriteria: "[Application.ApplicationType.Code] In ('cancel_invitation_wp', 'cancel_visa_wp', 'cancel_workpermit') And [CurrentWorkPermitItem] Is Not Null",
                targetType: typeof(ApplicationItem),
                targetProperty: "IsCancelled",
                targetValue: "true"
            );

            // 28. Rule: Clear WorkPermit Cancelled Flag on Unlink
            // When CurrentWorkPermitItem is removed (set to null), clear the cancellation flag.
            CreateOrResetRule(
                name: "Clear WorkPermit Cancelled Flag on Unlink",
                sourceType: typeof(ApplicationItem),
                sourceProperty: "CurrentWorkPermitItem",
                sourceValue: null, // Any change (we check criteria)
                trigger: SyncTriggerType.PropertyChanged,
                targetPath: "@Self",
                targetMatchCriteria: "[CurrentWorkPermitItem] Is Null",
                targetType: typeof(ApplicationItem),
                targetProperty: "IsCancelled",
                targetValue: "false"
            );

            // 29. Rule: Clear WorkPermit Issued Flag on Unlink
            // When CurrentWorkPermitItem is removed, clear the issued flag.
            CreateOrResetRule(
                name: "Clear WorkPermit Issued Flag on Unlink",
                sourceType: typeof(ApplicationItem),
                sourceProperty: "CurrentWorkPermitItem",
                sourceValue: null,
                trigger: SyncTriggerType.PropertyChanged,
                targetPath: "@Self",
                targetMatchCriteria: "[CurrentWorkPermitItem] Is Null",
                targetType: typeof(ApplicationItem),
                targetProperty: "WorkPermitItemIsIssued",
                targetValue: "false"
            );

            // 30. Rule: Set WorkPermit Issued Flag on Link
            // When CurrentWorkPermitItem is linked, set the issued flag.
            CreateOrResetRule(
                name: "Set WorkPermit Issued Flag on Link",
                sourceType: typeof(ApplicationItem),
                sourceProperty: "CurrentWorkPermitItem",
                sourceValue: null,
                trigger: SyncTriggerType.PropertyChanged,
                targetPath: "@Self",
                targetMatchCriteria: "[CurrentWorkPermitItem] Is Not Null",
                targetType: typeof(ApplicationItem),
                targetProperty: "WorkPermitItemIsIssued",
                targetValue: "true"
            );

            // 31. Rule: Set Invitation Cancelled Flag on Link
            // When an InvitationItem is linked to an ApplicationItem in a cancellation application, set the flag.
            CreateOrResetRule(
                name: "Set Invitation Cancelled Flag on Link",
                sourceType: typeof(ApplicationItem),
                sourceProperty: "CurrentInvitationItem",
                sourceValue: null,
                trigger: SyncTriggerType.PropertyChanged,
                targetPath: "@Self",
                targetMatchCriteria: "[Application.ApplicationType.Code] In ('cancel_invitation', 'cancel_invitation_wp') And [CurrentInvitationItem] Is Not Null",
                targetType: typeof(ApplicationItem),
                targetProperty: "IsCancelled",
                targetValue: "true"
            );

            // 32. Rule: Clear Invitation Cancelled Flag on Unlink
            // When CurrentInvitationItem is removed (set to null), clear the cancellation flag.
            CreateOrResetRule(
                name: "Clear Invitation Cancelled Flag on Unlink",
                sourceType: typeof(ApplicationItem),
                sourceProperty: "CurrentInvitationItem",
                sourceValue: null,
                trigger: SyncTriggerType.PropertyChanged,
                targetPath: "@Self",
                targetMatchCriteria: "[CurrentInvitationItem] Is Null",
                targetType: typeof(ApplicationItem),
                targetProperty: "IsCancelled",
                targetValue: "false"
            );

            // 33. Rule: Clear Invitation Issued Flag on Unlink
            // When CurrentInvitationItem is removed, clear the issued flag.
            CreateOrResetRule(
                name: "Clear Invitation Issued Flag on Unlink",
                sourceType: typeof(ApplicationItem),
                sourceProperty: "CurrentInvitationItem",
                sourceValue: null,
                trigger: SyncTriggerType.PropertyChanged,
                targetPath: "@Self",
                targetMatchCriteria: "[CurrentInvitationItem] Is Null",
                targetType: typeof(ApplicationItem),
                targetProperty: "InvitationItemIsIssued",
                targetValue: "false"
            );

            // 34. Rule: Set Invitation Issued Flag on Link
            // When CurrentInvitationItem is linked, set the issued flag.
            CreateOrResetRule(
                name: "Set Invitation Issued Flag on Link",
                sourceType: typeof(ApplicationItem),
                sourceProperty: "CurrentInvitationItem",
                sourceValue: null,
                trigger: SyncTriggerType.PropertyChanged,
                targetPath: "@Self",
                targetMatchCriteria: "[CurrentInvitationItem] Is Not Null",
                targetType: typeof(ApplicationItem),
                targetProperty: "InvitationItemIsIssued",
                targetValue: "true"
            );

            // 35. Rule: Set Visa Cancelled Flag on Link
            // When a Visa is linked to an ApplicationItem in a cancellation application, set the flag.
            CreateOrResetRule(
                name: "Set Visa Cancelled Flag on Link",
                sourceType: typeof(ApplicationItem),
                sourceProperty: "CurrentVisa",
                sourceValue: null,
                trigger: SyncTriggerType.PropertyChanged,
                targetPath: "@Self",
                targetMatchCriteria: "[Application.ApplicationType.Code] In ('cancel_visa', 'cancel_visa_wp') And [CurrentVisa] Is Not Null",
                targetType: typeof(ApplicationItem),
                targetProperty: "IsCancelled",
                targetValue: "true"
            );

            // 36. Rule: Clear Visa Cancelled Flag on Unlink
            // When CurrentVisa is removed (set to null), clear the cancellation flag.
            CreateOrResetRule(
                name: "Clear Visa Cancelled Flag on Unlink",
                sourceType: typeof(ApplicationItem),
                sourceProperty: "CurrentVisa",
                sourceValue: null,
                trigger: SyncTriggerType.PropertyChanged,
                targetPath: "@Self",
                targetMatchCriteria: "[CurrentVisa] Is Null",
                targetType: typeof(ApplicationItem),
                targetProperty: "IsCancelled",
                targetValue: "false"
            );

            // 37. Rule: Clear Visa Issued Flag on Unlink
            // When CurrentVisa is removed, clear the issued flag.
            CreateOrResetRule(
                name: "Clear Visa Issued Flag on Unlink",
                sourceType: typeof(ApplicationItem),
                sourceProperty: "CurrentVisa",
                sourceValue: null,
                trigger: SyncTriggerType.PropertyChanged,
                targetPath: "@Self",
                targetMatchCriteria: "[CurrentVisa] Is Null",
                targetType: typeof(ApplicationItem),
                targetProperty: "VisaIssued",
                targetValue: "false"
            );

            // 38. Rule: Set Visa Issued Flag on Link
            // When CurrentVisa is linked, set the issued flag.
            CreateOrResetRule(
                name: "Set Visa Issued Flag on Link",
                sourceType: typeof(ApplicationItem),
                sourceProperty: "CurrentVisa",
                sourceValue: null,
                trigger: SyncTriggerType.PropertyChanged,
                targetPath: "@Self",
                targetMatchCriteria: "[CurrentVisa] Is Not Null",
                targetType: typeof(ApplicationItem),
                targetProperty: "VisaIssued",
                targetValue: "true"
            );

            // 39. Rule: Set WorkPermit Changed Flag on Link
            // When a WorkPermitItem is linked to an ApplicationItem in a change application, set IsChanged on the item.
            CreateOrResetRule(
                name: "Set WorkPermit Changed Flag on Link",
                sourceType: typeof(ApplicationItem),
                sourceProperty: "CurrentWorkPermitItem",
                sourceValue: null,
                trigger: SyncTriggerType.PropertyChanged,
                targetPath: "CurrentWorkPermitItem",
                targetMatchCriteria: null,
                targetType: typeof(WorkPermitItem),
                targetProperty: "IsChanged",
                targetValue: "true",
                sourceCriteria: "[Application.ApplicationType.Code] = 'change_workpermit'"
            );

            System.Diagnostics.Debug.WriteLine("[SyncRulesUpdater] Committing changes...");
            ObjectSpace.CommitChanges();
            System.Diagnostics.Debug.WriteLine("[SyncRulesUpdater] Changes committed.");
        }

        private void CreateOrResetRule(string name, Type sourceType, string sourceProperty, string sourceValue,
                                       SyncTriggerType trigger, string targetPath, string targetMatchCriteria,
                                       Type targetType, string targetProperty, string targetValue, string sourceCriteria = null)
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
            rule.SourceCriteria = sourceCriteria;
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