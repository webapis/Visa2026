using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
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

            // "Visa Grant Letter" - Only visible for Approved applications
            CreateMailMergeVisibility(
                templateName: "Visa Grant Letter",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[CurrentState.State] = 'Approved'"
            );

            // "Rejection Notice" - Only visible for Rejected applications
            CreateMailMergeVisibility(
                templateName: "Rejection Notice",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[CurrentState.State] = 'Rejected'"
            );

            ObjectSpace.CommitChanges();
        }

        private void CreateMailMergeVisibility(string templateName, Type targetType, string criteria)
        {
            // FIX 3: Find by TemplateName only (same pattern as ReportsUpdater),
            // then unconditionally update all fields — avoids duplicates if TargetType ever changes.
            var visibility = ObjectSpace.FirstOrDefault<MailMergeVisibility>(v => v.TemplateName == templateName)
                             ?? ObjectSpace.CreateObject<MailMergeVisibility>();

            visibility.TemplateName = templateName;
            visibility.TargetTypeFullName = targetType.FullName;
            visibility.VisibilityCriteria = criteria;
        }
    }
}