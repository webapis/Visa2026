using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Aligns <c>Invitations</c> schema with legacy ApplicationResult invitation shape:
/// VisaCategory, VisaPeriod, optional visa window; drops obsolete ValidityDuration FK.
/// Issued date remains column <c>StartDate</c> (mapped to <see cref="BusinessObjects.Invitation.IssuedDate"/>).
/// </summary>
public sealed class InvitationLegacyShapeSchemaUpdater : ModuleUpdater
{
    public InvitationLegacyShapeSchemaUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseBeforeUpdateSchema()
    {
        base.UpdateDatabaseBeforeUpdateSchema();
        // Postgres ADD COLUMN fails when Invitations does not exist yet (greenfield EasyTest).
        if (!DatabaseProviderDetector.IsPostgreSql(ObjectSpace))
            ApplyEnsureColumns();
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        ApplyEnsureColumns();
        ApplyDropValidityDuration();
        BackfillRequiredLookups();
    }

    /// <summary>
    /// Existing invitation rows need VisaCategory/VisaPeriod after ValidityDuration removal.
    /// </summary>
    private void BackfillRequiredLookups()
    {
        try
        {
            var defaultCategory = ObjectSpace.GetObjectsQuery<BusinessObjects.VisaCategory>()
                .FirstOrDefault(c => c.IsDefault)
                ?? ObjectSpace.GetObjectsQuery<BusinessObjects.VisaCategory>().FirstOrDefault();
            var defaultPeriod = ObjectSpace.GetObjectsQuery<BusinessObjects.VisaPeriod>()
                .FirstOrDefault(p => p.IsDefault)
                ?? ObjectSpace.GetObjectsQuery<BusinessObjects.VisaPeriod>().FirstOrDefault();
            if (defaultCategory == null || defaultPeriod == null)
                return;

            var invitations = ObjectSpace.GetObjectsQuery<BusinessObjects.Invitation>().ToList();
            var changed = false;
            foreach (var invitation in invitations)
            {
                if (invitation.VisaCategory == null)
                {
                    invitation.VisaCategory = defaultCategory;
                    changed = true;
                }

                if (invitation.VisaPeriod == null)
                {
                    invitation.VisaPeriod = defaultPeriod;
                    changed = true;
                }

                if (invitation.ExpirationDate == null && invitation.IssuedDate != default)
                {
                    invitation.ExpirationDate = invitation.IssuedDate.AddDays(90);
                    changed = true;
                }
            }

            if (changed)
                ObjectSpace.CommitChanges();
        }
        catch
        {
            // Lookups may not be seeded yet on first greenfield pass; catalog sync runs later.
        }
    }

    private void ApplyEnsureColumns()
    {
        if (DatabaseProviderDetector.IsPostgreSql(ObjectSpace))
        {
            foreach (var sql in InvitationLegacyShapeSchemaSql.EnsureColumnsPostgresStatements)
                ExecuteNonQueryCommand(sql, true);
        }
        else
        {
            ExecuteNonQueryCommand(InvitationLegacyShapeSchemaSql.EnsureColumnsSqlServer, false);
        }
    }

    private void ApplyDropValidityDuration()
    {
        if (DatabaseProviderDetector.IsPostgreSql(ObjectSpace))
            ExecuteNonQueryCommand(InvitationLegacyShapeSchemaSql.DropValidityDurationPostgres, true);
        else
            ExecuteNonQueryCommand(InvitationLegacyShapeSchemaSql.DropValidityDurationSqlServer, false);
    }
}