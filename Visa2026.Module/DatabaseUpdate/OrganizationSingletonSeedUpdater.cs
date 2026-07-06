using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.Base;
using System.Linq;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;

namespace Visa2026.Module.DatabaseUpdate
{
    /// <summary>
    /// Ensures organization singleton BOs exist (upload limits, ministry review SLA, etc.).
    /// Organization tenant data (company, signatory, application numbering, etc.) is filled from tenant JSON by
    /// <see cref="LookupCatalogSyncUpdater"/>.
    /// </summary>
    public class OrganizationSingletonSeedUpdater : ModuleUpdater
    {
        public OrganizationSingletonSeedUpdater(IObjectSpace objectSpace, Version currentDBVersion)
            : base(objectSpace, currentDBVersion)
        {
        }

        public override void UpdateDatabaseAfterUpdateSchema()
        {
            base.UpdateDatabaseAfterUpdateSchema();

            _ = SystemSettings.GetOrCreateInstance(ObjectSpace);

            var removed = OrganizationSingletonHelper.CollapseToSingleRow(
                ObjectSpace, (SystemSettings _) => "Upload limits");
            if (removed > 0)
            {
                var line = $"OrganizationSingletonSeedUpdater: removed {removed} duplicate SystemSettings row(s).";
                Tracing.Tracer.LogText(line);
                System.Diagnostics.Trace.WriteLine(line);
            }

            EnsureMinistryReviewSlaSettings();

            ObjectSpace.CommitChanges();
        }

        private void EnsureMinistryReviewSlaSettings()
        {
            var settings = MinistryReviewSlaSettings.GetOrCreateInstance(ObjectSpace);
            if (settings.MaxDaysInReview <= 0)
            {
#pragma warning disable CS0618
                var legacyLeg = ObjectSpace.GetObjectsQuery<ApprovalLegProfileMinistryLeg>()
                    .Where(l => l.MaxDaysInReview > 0)
                    .OrderBy(l => l.Sequence)
                    .FirstOrDefault();

                if (legacyLeg?.MaxDaysInReview is > 0)
                {
                    settings.MaxDaysInReview = legacyLeg.MaxDaysInReview.Value;
                    settings.WarningDaysBeforeMax = legacyLeg.WarningDaysBeforeMax;
                }
                else
                {
                    settings.MaxDaysInReview = MinistryReviewSlaSettings.DefaultMaxDaysInReview;
                    settings.WarningDaysBeforeMax = MinistryReviewSlaSettings.DefaultWarningDaysBeforeMax;
                }
#pragma warning restore CS0618
            }

            var removed = OrganizationSingletonHelper.CollapseToSingleRow(
                ObjectSpace, (MinistryReviewSlaSettings _) => "Ministry review SLA");
            if (removed > 0)
            {
                var line = $"OrganizationSingletonSeedUpdater: removed {removed} duplicate MinistryReviewSlaSettings row(s).";
                Tracing.Tracer.LogText(line);
                System.Diagnostics.Trace.WriteLine(line);
            }
        }
    }
}
