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

            // ApplicationRosterMergeLine SyncRules removed (Phase B hard-remove). Roster links use sticky ResolvedLinks.
            foreach (var obsolete in ObsoleteApplicationItemRuleNames)
                DeleteRuleByName(obsolete);

            // Mark InvitationItem as Used when a Visa is saved.
            CreateOrResetRule(
                name: "Mark InvitationItem as Used",
                sourceType: typeof(Visa),
                sourceProperty: null,
                sourceValue: null,
                trigger: SyncTriggerType.Save,
                targetPath: "Invitation.InvitationItems",
                targetMatchCriteria: "[Person.ID] = '@Source.Passport.Person.ID'",
                targetType: typeof(InvitationItem),
                targetProperty: "IsUsed",
                targetValue: "true",
                sourceCriteria: null
            );

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

            CreateOrResetRule(
                name: "Clear InvitationItem on ApplicationProfileInstance Change",
                sourceType: typeof(Visa),
                sourceProperty: "IssuingApplicationProfileInstance",
                sourceValue: null,
                trigger: SyncTriggerType.PropertyChanged,
                targetPath: "@Self",
                targetMatchCriteria: null,
                targetType: typeof(Visa),
                targetProperty: "InvitationItem",
                targetValue: "@Null"
            );

            CreateOrResetRule(
                name: "Auto-Set InvitationItem on ApplicationProfileInstance Change",
                sourceType: typeof(Visa),
                sourceProperty: "IssuingApplicationProfileInstance",
                sourceValue: null,
                trigger: SyncTriggerType.PropertyChanged,
                targetPath: "@Self",
                targetMatchCriteria: null,
                targetType: typeof(Visa),
                targetProperty: "InvitationItem",
                targetValue: "@Source.AvailableInvitationItems[Person.ID = ^.Passport.Person.ID]"
            );

            DeleteRuleByName("Set Person CurrentInvitationItem on InvitationItem Create");
            DeleteRuleByName("Clear Person CurrentInvitationItem on Soft Delete");
            DeleteRuleByName("Clear Person CurrentInvitationItem on Deactivation");
            DeleteRuleByName("Deactivate Sibling Visas");
            DeleteRuleByName("Set Passport Current Visa");
            DeleteRuleByName("Pull EmployeeContract from Person");
            DeleteRuleByName("Auto-Set ApplicationRosterMergeLine on ApplicationProfileInstance Change");

            System.Diagnostics.Debug.WriteLine("[SyncRulesUpdater] Committing changes...");
            ObjectSpace.CommitChanges();
            System.Diagnostics.Debug.WriteLine("[SyncRulesUpdater] Changes committed.");
        }

        private static readonly string[] ObsoleteApplicationItemRuleNames =
        [
            "Pull Passport from Person",
            "Pull Visa from Person",
            "Pull AddressOfResidence from Person",
            "Pull PositionHistory from Person",
            "Pull MedicalRecord from Person",
            "Pull Education from Person",
            "Pull InvitationItem from Person",
            "Pull WorkPermit from Person",
            "Pull Salary from Person",
            "Auto-Set ApplicationRosterMergeLine on ApplicationProfileInstance Change",
            "Set Visa Issued Flag on ApplicationProfileInstance Item",
            "Revert Visa Issued Flag on Delete",
            "Set WorkPermit Issued Flag on ApplicationProfileInstance Item",
            "Revert WorkPermit Issued Flag on Delete",
            "Set Invitation Issued Flag on ApplicationProfileInstance Item",
            "Revert Invitation Issued Flag on Delete",
            "Set InvitationItem Changed Flag on Link",
            "Set InvitationItem Cancelled Flag on Link",
            "Revert InvitationItem Changed Flag on Unlink",
            "Set WorkPermitItem Changed Flag on Link",
            "Set WorkPermitItem Cancelled Flag on Link",
            "Revert WorkPermitItem Changed Flag on Unlink",
            "Set Visa Cancelled Flag on Link",
            "Revert Visa Cancelled Flag on Unlink",
            "Set Visa Changed Flag on Link",
            "Revert Visa Changed Flag on Unlink",
            "Set Visa Cancelled Flag on AppItem Delete",
            "Revert Visa Cancelled Flag on AppItem Delete",
        ];

        private void CreateOrResetRule(string name, Type sourceType, string sourceProperty, string sourceValue,
                                       SyncTriggerType trigger, string targetPath, string targetMatchCriteria,
                                       Type targetType, string targetProperty, string targetValue, string sourceCriteria = null)
        {
            var rule = ObjectSpace.FindObject<SyncRule>(CriteriaOperator.Parse("Name=?", name));
            if (rule == null)
            {
                rule = ObjectSpace.CreateObject<SyncRule>();
                rule.Name = name;
            }

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

        private void DeleteRuleByName(string name)
        {
            var rule = ObjectSpace.FindObject<SyncRule>(CriteriaOperator.Parse("Name=?", name));
            if (rule != null)
                ObjectSpace.Delete(rule);
        }
    }
}