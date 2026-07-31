using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Adds <see cref="BusinessObjects.ApplicationType"/> capability flag columns before/after EF schema sync
/// (SQL Server and PostgreSQL). Required on Postgres pilot where most T-SQL schema helpers are skipped.
/// After ensuring columns, backfills <c>CanIssue*</c> from seed so DEFAULT false cannot stick when
/// <see cref="ApplicationTypeConfigurationUpdater"/> did not re-run after the columns were added.
/// </summary>
public sealed class ApplicationTypeCapabilityFlagsSchemaUpdater : ModuleUpdater
{
    public ApplicationTypeCapabilityFlagsSchemaUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseBeforeUpdateSchema()
    {
        base.UpdateDatabaseBeforeUpdateSchema();
        ApplySchemaSql();
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        ApplySchemaSql();
        SyncCapabilityFlagsFromSeed();
    }

    private void ApplySchemaSql()
    {
        if (DatabaseProviderDetector.IsPostgreSql(ObjectSpace))
            ExecuteNonQueryCommand(ApplicationTypeCapabilityFlagsSchemaSql.EnsureColumnsPostgres, false);
        else
            ExecuteNonQueryCommand(ApplicationTypeCapabilityFlagsSchemaSql.EnsureColumnsSqlServer, false);
    }

    private void SyncCapabilityFlagsFromSeed()
    {
        var synced = 0;
        foreach (var row in ApplicationTypeConfigurationSeed.Rows)
        {
            var applicationType = ObjectSpace.GetObjectsQuery<ApplicationType>()
                .FirstOrDefault(t => t.Name == row.Name);
            if (applicationType == null)
                continue;

            if (applicationType.CanIssueVisa == row.CanIssueVisa
                && applicationType.CanIssueInvitation == row.CanIssueInvitation
                && applicationType.CanIssueWorkPermit == row.CanIssueWorkPermit)
            {
                continue;
            }

            applicationType.CanIssueVisa = row.CanIssueVisa;
            applicationType.CanIssueInvitation = row.CanIssueInvitation;
            applicationType.CanIssueWorkPermit = row.CanIssueWorkPermit;
            synced++;
        }

        if (synced > 0)
        {
            ObjectSpace.CommitChanges();
            Tracing.Tracer.LogText(
                $"ApplicationTypeCapabilityFlagsSchemaUpdater: synced CanIssue* flags on {synced} ApplicationType row(s) from seed.");
        }
    }
}