using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate;

public sealed class ApplicationLatestProgressSchemaUpdater : ModuleUpdater
{
    private const int BackfillBatchSize = 250;

    public ApplicationLatestProgressSchemaUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseBeforeUpdateSchema()
    {
        base.UpdateDatabaseBeforeUpdateSchema();
        ExecuteNonQueryCommand(ApplicationLatestProgressSchemaSql.EnsureColumnsSql, false);
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        ExecuteNonQueryCommand(ApplicationLatestProgressSchemaSql.EnsureColumnsSql, false);
        ExecuteNonQueryCommand(ApplicationLatestProgressSchemaSql.BackfillLatestProgressIdSql, false);
        BackfillDisplayFields();
    }

    private void BackfillDisplayFields()
    {
        while (true)
        {
            var applications = ObjectSpace.GetObjectsQuery<ApplicationProfileInstance>()
                .Where(application => application.LatestProgressId != null
                    && (application.LatestProgressDisplay == null || application.LatestProgressDisplay == string.Empty))
                .Take(BackfillBatchSize)
                .Include(application => application.LatestProgress!)
                    .ThenInclude(progress => progress.State)
                .ToList();

            if (applications.Count == 0)
                return;

            foreach (var application in applications)
                ApplicationLatestProgressSyncHelper.Apply(application, application.LatestProgress);

            ObjectSpace.CommitChanges();
        }
    }
}