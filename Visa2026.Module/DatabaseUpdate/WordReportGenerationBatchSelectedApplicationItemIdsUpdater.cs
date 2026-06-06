using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Ensures <see cref="BusinessObjects.WordReportGenerationBatch.SelectedApplicationItemIdsJson"/> exists before EF schema sync.
/// </summary>
public sealed class WordReportGenerationBatchSelectedApplicationItemIdsUpdater : ModuleUpdater
{
    public WordReportGenerationBatchSelectedApplicationItemIdsUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseBeforeUpdateSchema()
    {
        base.UpdateDatabaseBeforeUpdateSchema();
        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.WordReportGenerationBatches', N'U') IS NULL
    RETURN;

IF COL_LENGTH(N'dbo.WordReportGenerationBatches', N'SelectedApplicationItemIdsJson') IS NULL
    ALTER TABLE dbo.WordReportGenerationBatches ADD SelectedApplicationItemIdsJson nvarchar(max) NULL;", false);
    }
}
