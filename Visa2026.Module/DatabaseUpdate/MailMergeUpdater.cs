using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate
{
    public class MailMergeUpdater : ModuleUpdater
    {
        public MailMergeUpdater(IObjectSpace objectSpace, Version currentDBVersion) :
            base(objectSpace, currentDBVersion)
        {
        }

        public override void UpdateDatabaseAfterUpdateSchema()
        {
            base.UpdateDatabaseAfterUpdateSchema();

            // Example 1: "Visa Grant Letter" - Only visible for Approved applications
            CreateMailMergeVisibility(
                templateName: "Visa Grant Letter",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[CurrentState.State] = 'Approved'" // Assuming 'CurrentState' is the navigation property for status
            );

            // Example 2: "Rejection Notice" - Only visible for Rejected applications
            CreateMailMergeVisibility(
                templateName: "Rejection Notice",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[CurrentState.State] = 'Rejected'"
            );

            ObjectSpace.CommitChanges();
        }

        private void CreateMailMergeVisibility(string templateName, Type targetType, string criteria, IList<PermissionPolicyRole> roles = null)
        {
            // Try to find the existing rule to avoid duplicates
            var visibility = ObjectSpace.FirstOrDefault<MailMergeVisibility>(v => v.TemplateName == templateName && v.TargetTypeFullName == targetType.FullName);

            if (visibility == null)
            {
                visibility = ObjectSpace.CreateObject<MailMergeVisibility>();
                visibility.TemplateName = templateName;
                visibility.TargetTypeFullName = targetType.FullName;
            }

            // Update criteria
            visibility.VisibilityCriteria = criteria;

            // Note: You can add logic here to seed Roles if needed, similar to how criteria is handled.
        }
    }
}